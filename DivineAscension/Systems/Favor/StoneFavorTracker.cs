using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Blocks;
using DivineAscension.Collectible;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class StoneFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ILoggerWrapper logger,
    IEventService eventService,
    IWorldService worldService,
    IFavorSystem favorSystem,
    IPlayerMessengerService messenger) : IFavorTracker, IDisposable
{
    // --- Stone Gathering Tracking ---
    private const int FavorPerStoneBlock = 1; // Base stone blocks (granite, andesite, etc.)
    private const int FavorPerValuableStone = 2; // Valuable stones (marble, obsidian)

    // --- Construction Tracking ---
    private const int FavorPerBrickPlacement = 5; // Increased from 2 to 5
    private const int FavorPerConstructionBlock = 1; // Stone bricks, slabs, stairs
    private const long ConstructionCooldownMs = 1000; // 1 second cooldown (reduced from 5s)

    // --- Chiseling Tracking ---
    private const float FavorPerVoxel = 0.02f; // Favor awarded per voxel carved/added
    private const long ChiselingCooldownMs = 3000; // 3 second cooldown to prevent add/remove spam
    private const long SessionTimeoutMs = 45000; // 45s idle timeout for combo reset
    private const long SessionDecayCheckMs = 5000; // Cleanup check interval (every 5 seconds)
    private const int ComboTier2 = 3;   // 1.25x at 3 actions
    private const int ComboTier3 = 6;   // 1.5x at 6 actions
    private const int ComboTier4 = 11;  // 2.0x at 11 actions
    private const int ComboTier5 = 21;  // 2.5x at 21+ actions

    /// <summary>
    /// Tracks chiseling session state for combo multiplier system
    /// </summary>
    private class ChiselingSession
    {
        public int ComboCount { get; set; }
        public long LastActionTime { get; set; }
        public long SessionStartTime { get; set; }
        public int TotalVoxelsThisSession { get; set; }
        public float PreviousMultiplier { get; set; } = 1.0f;
    }

    private readonly IEventService
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly Dictionary<string, long> _lastConstructionTime = new(); // Renamed for clarity
    private readonly Dictionary<string, ChiselingSession> _chiselingSessions = new();
    private long _sessionCleanupCallbackId;

    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    private readonly IPlayerMessengerService _messenger =
        messenger ?? throw new ArgumentNullException(nameof(messenger));

    public void Dispose()
    {
        ClayFormingPatches.OnClayFormingFinished -= HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired -= HandlePitKilnFired;
        BlockBehaviorStone.OnStoneBlockBroken -= OnBlockBroken;
        CollectibleBehaviorChiselTracking.OnVoxelsChanged -= HandleVoxelsChanged;
        _eventService.UnsubscribeDidPlaceBlock(OnBlockPlaced);
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);

        // Unregister session cleanup callback
        if (_sessionCleanupCallbackId != 0)
        {
            _eventService.UnregisterCallback(_sessionCleanupCallbackId);
        }

        _lastConstructionTime.Clear();
        _chiselingSessions.Clear();
        _logger.Debug($"[DivineAscension] StoneFavorTracker disposed (ID: {_instanceId})");
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Stone;

    public void Initialize()
    {
        ClayFormingPatches.OnClayFormingFinished += HandleClayFormingFinished;
        PitKilnPatches.OnPitKilnFired += HandlePitKilnFired;
        // Track stone gathering
        BlockBehaviorStone.OnStoneBlockBroken += OnBlockBroken;
        // Track chiseling work
        CollectibleBehaviorChiselTracking.OnVoxelsChanged += HandleVoxelsChanged;
        // Track construction (brick/stone placement)
        _eventService.OnDidPlaceBlock(OnBlockPlaced);
        // Clean up cooldown data on player disconnect
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);
        // Register periodic session cleanup callback (every 5 seconds)
        _sessionCleanupCallbackId = _eventService.RegisterCallback(OnSessionDecayCheck, (int)SessionDecayCheckMs);
        _logger.Notification($"[DivineAscension] StoneFavorTracker initialized (ID: {_instanceId})");
    }

    private void HandleClayFormingFinished(IServerPlayer player, ItemStack stack, int clayConsumed)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        if (clayConsumed > 0)
        {
            // Clay consumed already accounts for the total batch (don't multiply by stack size)
            // Phase 1 improvement: Double pottery favor rewards
            var favorAmount = clayConsumed * 2;
            _favorSystem.AwardFavorForAction(player, "Pottery Crafting", favorAmount);

            _logger.Debug(
                $"[StoneFavorTracker] Awarded {favorAmount} favor for clay forming " +
                $"(clay consumed: {clayConsumed}, stack size: {stack?.StackSize ?? 1}, item: {stack?.Collectible?.Code?.Path ?? "unknown"})");
        }
    }

    /// <summary>
    /// Gets an existing chiseling session or creates a new one for the player
    /// </summary>
    private ChiselingSession GetOrCreateChiselingSession(string playerUID, long currentTime)
    {
        if (!_chiselingSessions.TryGetValue(playerUID, out var session))
        {
            session = new ChiselingSession
            {
                ComboCount = 0,
                LastActionTime = 0,
                SessionStartTime = currentTime,
                TotalVoxelsThisSession = 0
            };
            _chiselingSessions[playerUID] = session;
        }
        return session;
    }

    /// <summary>
    /// Checks if the session has expired and resets it if needed
    /// </summary>
    /// <returns>True if session was reset, false otherwise</returns>
    private bool CheckAndResetExpiredSession(ChiselingSession session, string playerName, long currentTime, long timeSinceLastAction)
    {
        if (session.LastActionTime > 0 && timeSinceLastAction > SessionTimeoutMs)
        {
            // Session expired - reset combo
            session.ComboCount = 0;
            session.SessionStartTime = currentTime;
            session.TotalVoxelsThisSession = 0;
            session.PreviousMultiplier = 1.0f;
            _logger.Debug(
                $"[StoneFavorTracker] {playerName}'s chiseling session expired after {timeSinceLastAction / 1000.0:F1}s idle");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the player is still in anti-spam cooldown
    /// </summary>
    private bool IsInCooldown(ChiselingSession session, long timeSinceLastAction)
    {
        return session.LastActionTime > 0 && timeSinceLastAction < ChiselingCooldownMs;
    }

    /// <summary>
    /// Updates session state with new chiseling action
    /// </summary>
    private void UpdateSessionState(ChiselingSession session, int voxelsDelta, long currentTime)
    {
        session.ComboCount++;
        session.TotalVoxelsThisSession += voxelsDelta;
        session.LastActionTime = currentTime;
    }

    /// <summary>
    /// Sends a chat notification if the player reached a new combo tier
    /// </summary>
    private void NotifyTierChange(IServerPlayer player, ChiselingSession session, float newMultiplier)
    {
        if (Math.Abs(newMultiplier - session.PreviousMultiplier) > 0.01f)
        {
            var tierName = GetChiselingStreakTierName(session.ComboCount);
            var message = $"Chiseling Streak: {tierName}! ({newMultiplier:F2}x favor)";
            _messenger.SendMessage(player, message, EnumChatType.Notification);
            session.PreviousMultiplier = newMultiplier;
        }
    }

    /// <summary>
    /// Calculates favor amount and awards it to the player
    /// </summary>
    /// <returns>The calculated favor amount</returns>
    private void CalculateAndAwardFavor(IServerPlayer player, int voxelsDelta, float multiplier)
    {
        var favorAmount = voxelsDelta * FavorPerVoxel * multiplier;
        _favorSystem.AwardFavorForAction(player, "Stone Carving", favorAmount);
    }

    private void HandleVoxelsChanged(IPlayer player, BlockPos pos, int voxelsDelta)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        if (voxelsDelta <= 0) return;

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer == null) return;

        var currentTime = _worldService.ElapsedMilliseconds;
        var session = GetOrCreateChiselingSession(player.PlayerUID, currentTime);

        // Check if session expired and reset if needed
        var timeSinceLastAction = currentTime - session.LastActionTime;
        CheckAndResetExpiredSession(session, player.PlayerName, currentTime, timeSinceLastAction);

        // Anti-spam cooldown check
        if (IsInCooldown(session, timeSinceLastAction))
            return;

        // Update session state
        UpdateSessionState(session, voxelsDelta, currentTime);

        // Calculate multiplier and notify if tier changed
        var multiplier = GetComboMultiplier(session.ComboCount);
        NotifyTierChange(serverPlayer, session, multiplier);

        // Award favor
        CalculateAndAwardFavor(serverPlayer, voxelsDelta, multiplier);
    }

    private void OnBlockPlaced(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(byPlayer.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        var placedBlock = _worldService.GetBlock(blockSel.Position);
        if (!IsConstructionBlock(placedBlock, out var favor)) return;

        // Debounce check - limit favor awards to one per cooldown period
        var currentTime = _worldService.ElapsedMilliseconds;
        if (_lastConstructionTime.TryGetValue(byPlayer.PlayerUID, out var lastTime))
        {
            if (currentTime - lastTime < ConstructionCooldownMs)
                return; // Still in cooldown
        }

        _favorSystem.AwardFavorForAction(byPlayer, "construction", favor);
        _lastConstructionTime[byPlayer.PlayerUID] = currentTime;

        _logger.Debug(
            $"[StoneFavorTracker] Awarded {favor} favor to {byPlayer.PlayerName} for placing {placedBlock.Code.Path}");
    }

    /// <summary>
    /// Checks if a block is a construction block and returns the favor amount
    /// </summary>
    private static bool IsConstructionBlock(Block block, out float favor)
    {
        favor = 0;
        if (block?.Code == null) return false;

        var path = block.Code.Path.ToLowerInvariant();

        // Bricks (raw and fired) - highest priority for Stone domain
        if (path.Contains("brick"))
        {
            favor = FavorPerBrickPlacement;
            return true;
        }

        // Stone construction blocks (slabs, stairs, walls, etc.)
        if (IsStoneConstructionBlock(path))
        {
            favor = FavorPerConstructionBlock;
            return true;
        }

        // Clay/ceramic construction blocks
        if (path.Contains("clay") || path.Contains("ceramic"))
        {
            favor = FavorPerConstructionBlock;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a block is a stone-based construction block
    /// </summary>
    private static bool IsStoneConstructionBlock(string path)
    {
        // Common stone materials
        var stoneTypes = new[]
        {
            "granite", "andesite", "basalt", "peridotite", "limestone",
            "sandstone", "claystone", "chalk", "chert", "marble"
        };

        foreach (var stoneType in stoneTypes)
        {
            if (!path.Contains(stoneType)) continue;

            // Construction block types (stairs, slabs, walls, etc.)
            if (path.Contains("stairs") || path.Contains("slab") || path.Contains("wall") ||
                path.Contains("pillar") || path.Contains("column") || path.Contains("hewn"))
            {
                return true;
            }
        }

        return false;
    }

    private int EstimateClayFromFiredItem(ItemStack stack)
    {
        if (stack?.Collectible?.Code == null) return 0;

        var path = stack.Collectible.Code.Path?.ToLowerInvariant() ?? string.Empty;

        // Known clay amounts from game data (based on screenshot)
        if (path.Contains("storagevessel") || path.Contains("vessel")) return 35;
        if (path.Contains("planter")) return 18;
        if (path.Contains("anvil") && path.Contains("mold")) return 28;
        if (path.Contains("prospecting") && path.Contains("mold")) return 13;
        if (path.Contains("pickaxe") && path.Contains("mold")) return 12;
        if (path.Contains("hammer") && path.Contains("mold")) return 12;
        if (path.Contains("blade") && path.Contains("mold")) return 12;
        if (path.Contains("lamel") && path.Contains("mold")) return 11;
        if (path.Contains("axe") && path.Contains("mold")) return 11;
        if (path.Contains("shovel") && path.Contains("mold")) return 11;
        if (path.Contains("hoe") && path.Contains("mold")) return 12;
        if (path.Contains("helve") && path.Contains("mold")) return 6;
        if (path.Contains("ingot") && path.Contains("mold")) return 2; // 2 or 5
        if (path.Contains("wateringcan")) return 10;
        if (path.Contains("jug")) return 5;
        if (path.Contains("shingles")) return 4;
        if (path.Contains("flowerpot")) return 4; // 4 or 23
        if (path.Contains("cookingpot") || path.Contains("pot")) return 4; // 4 or 24
        if (path.Contains("bowl")) return 4; // Conservative estimate (1 or 4)
        if (path.Contains("crucible")) return 2; // 2 or 13
        if (path.Contains("crock")) return 2; // 2 or 14
        if (path.Contains("brick")) return 1;

        // Default for other ceramic items
        return path.Contains("clay") || path.Contains("ceramic") ? 3 : 0;
    }

    private void OnBlockBroken(IWorldAccessor worldAccessor, BlockPos blockPos, IPlayer? player,
        EnumHandling enumHandling)
    {
        // Player can be null when blocks break automatically (gravity, neighbor updates, etc.)
        if (player == null) return;

        // Verify religion
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);
        if (deityType != DeityDomain.Stone) return;

        var serverPlayer = player as IServerPlayer;
        if (serverPlayer == null)
            return;

        _favorSystem.AwardFavorForAction(serverPlayer, "gathering stone", FavorPerStoneBlock);

        _logger.Debug(
            $"[StoneFavorTracker] Awarded {FavorPerStoneBlock} favor to {player.PlayerName} for mining");
    }

    /// <summary>
    /// Periodic callback to clean up expired chiseling sessions (prevent memory leaks)
    /// </summary>
    private void OnSessionDecayCheck(float dt)
    {
        var currentTime = _worldService.ElapsedMilliseconds;
        var expiredSessionTimeout = SessionTimeoutMs * 2; // 90 seconds (2× normal timeout)

        // Find and remove expired sessions
        var expiredPlayers = new List<string>();
        foreach (var kvp in _chiselingSessions)
        {
            var timeSinceLastAction = currentTime - kvp.Value.LastActionTime;
            if (kvp.Value.LastActionTime > 0 && timeSinceLastAction > expiredSessionTimeout)
            {
                expiredPlayers.Add(kvp.Key);
            }
        }

        // Remove expired sessions
        foreach (var playerUid in expiredPlayers)
        {
            _chiselingSessions.Remove(playerUid);
            _logger.Debug($"[StoneFavorTracker] Cleaned up expired chiseling session for player {playerUid}");
        }
    }

    /// <summary>
    /// Calculates combo multiplier based on number of consecutive chiseling actions
    /// </summary>
    private float GetComboMultiplier(int comboCount)
    {
        if (comboCount >= ComboTier5) return 2.5f;
        if (comboCount >= ComboTier4) return 2.0f;
        if (comboCount >= ComboTier3) return 1.5f;
        if (comboCount >= ComboTier2) return 1.25f;
        return 1.0f;
    }

    /// <summary>
    /// Gets the tier name for display in chat messages
    /// </summary>
    private string GetChiselingStreakTierName(int comboCount)
    {
        if (comboCount >= ComboTier5) return "Legendary";
        if (comboCount >= ComboTier4) return "Epic";
        if (comboCount >= ComboTier3) return "Great";
        if (comboCount >= ComboTier2) return "Good";
        return "Started";
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up cooldown tracking data to prevent memory leaks
        _lastConstructionTime.Remove(player.PlayerUID);
        _chiselingSessions.Remove(player.PlayerUID);
    }

    private void HandlePitKilnFired(string playerUid, List<ItemStack> firedItems)
    {
        _logger.Debug(
            $"[DivineAscension] StoneFavorTracker ({_instanceId}): Handling PitKilnFired for {playerUid}");

        if (string.IsNullOrEmpty(playerUid)) return;

        var deityType = _playerProgressionDataManager.GetPlayerDeityType(playerUid);
        if (deityType != DeityDomain.Stone) return;

        float totalFavor = 0;
        foreach (var stack in firedItems)
        {
            // For pit kiln, estimate per-item clay and multiply by stack size
            // (unlike clay forming where the recipe gives us the total)
            int clayEstimate = EstimateClayFromFiredItem(stack);
            int stackSize = stack?.StackSize ?? 1;
            // Phase 1 improvement: Double pottery favor rewards
            totalFavor += (clayEstimate * stackSize * 2);

            _logger.Debug(
                $"[StoneFavorTracker] Pit kiln item: {stack?.Collectible?.Code?.Path ?? "unknown"}, " +
                $"clay estimate: {clayEstimate}, stack: {stackSize}, favor: {clayEstimate * stackSize * 2}");
        }

        if (totalFavor > 0)
        {
            _favorSystem.AwardFavorForAction(playerUid, "Pottery firing", totalFavor, deityType);
            _logger.Debug($"[StoneFavorTracker] Total pit kiln favor awarded: {totalFavor}");
        }
    }
}