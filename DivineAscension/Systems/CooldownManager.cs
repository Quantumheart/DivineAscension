using System;
using System.Collections.Concurrent;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Manages cooldowns for various operations to prevent griefing attacks.
///     Thread-safe implementation with admin bypass support.
/// </summary>
public class CooldownManager : ICooldownManager
{
    private readonly ICoreServerAPI _sapi;
    private readonly ModConfigData _config;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<CooldownType, long>> _cooldowns = new();
    private readonly object _cleanupLock = new();
    private long _cleanupCallbackId;

    /// <summary>
    ///     Initializes a new instance of the CooldownManager.
    /// </summary>
    /// <param name="sapi">Server API reference</param>
    /// <param name="config">Mod configuration containing cooldown durations</param>
    public CooldownManager(ICoreServerAPI sapi, ModConfigData config)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    ///     Initializes the cooldown manager and registers cleanup callback.
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Cooldown Manager...");

        // Register cleanup callback every 5 minutes (300000 ms)
        _cleanupCallbackId = _sapi.Event.RegisterCallback(CleanupExpiredCooldowns, 300000);

        _sapi.Logger.Notification("[DivineAscension] Cooldown Manager initialized");
    }

    /// <summary>
    ///     Disposes the cooldown manager and unregisters callbacks.
    /// </summary>
    public void Dispose()
    {
        if (_cleanupCallbackId != 0)
        {
            _sapi.Event.UnregisterCallback(_cleanupCallbackId);
            _cleanupCallbackId = 0;
        }
    }

    /// <summary>
    ///     Checks if an operation is allowed for a player, enforcing cooldown if necessary.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation being performed</param>
    /// <param name="errorMessage">Error message if cooldown is active (null if allowed)</param>
    /// <returns>True if operation is allowed, false if on cooldown</returns>
    public bool CanPerformOperation(string playerUID, CooldownType cooldownType, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(playerUID))
            throw new ArgumentException("PlayerUID cannot be null or empty", nameof(playerUID));

        // Check if cooldowns are disabled globally
        if (!_config.CooldownsEnabled)
        {
            errorMessage = null;
            return true;
        }

        // Check if player is admin (bypass cooldowns)
        var player = _sapi.World.PlayerByUid(playerUID);
        if (player != null && Array.IndexOf(player.Privileges, Privilege.root) >= 0)
        {
            errorMessage = null;
            return true;
        }

        var playerCooldowns = _cooldowns.GetOrAdd(playerUID, _ => new ConcurrentDictionary<CooldownType, long>());

        if (playerCooldowns.TryGetValue(cooldownType, out var expiryTimeMs))
        {
            var nowMs = _sapi.World.ElapsedMilliseconds;
            if (nowMs < expiryTimeMs)
            {
                var remainingSeconds = (expiryTimeMs - nowMs) / 1000.0;
                var formattedTime = CooldownTimeFormatter.FormatTimeRemaining(remainingSeconds);
                errorMessage = $"Please wait {formattedTime} before performing this action again.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    ///     Records that a player has performed an operation and starts the cooldown timer.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation performed</param>
    public void RecordOperation(string playerUID, CooldownType cooldownType)
    {
        if (string.IsNullOrWhiteSpace(playerUID))
            throw new ArgumentException("PlayerUID cannot be null or empty", nameof(playerUID));

        // Skip recording for admins (bypass cooldowns)
        var player = _sapi.World.PlayerByUid(playerUID);
        if (player != null && Array.IndexOf(player.Privileges, Privilege.root) >= 0)
            return;

        var cooldownDurationMs = GetCooldownDuration(cooldownType) * 1000;
        var expiryTimeMs = _sapi.World.ElapsedMilliseconds + cooldownDurationMs;

        var playerCooldowns = _cooldowns.GetOrAdd(playerUID, _ => new ConcurrentDictionary<CooldownType, long>());
        playerCooldowns[cooldownType] = expiryTimeMs;
    }

    /// <summary>
    ///     Checks if an operation is allowed and records it atomically if so.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation being performed</param>
    /// <param name="errorMessage">Error message if cooldown is active (null if allowed)</param>
    /// <returns>True if operation was allowed and recorded, false if on cooldown</returns>
    public bool TryPerformOperation(string playerUID, CooldownType cooldownType, out string? errorMessage)
    {
        if (!CanPerformOperation(playerUID, cooldownType, out errorMessage))
            return false;

        RecordOperation(playerUID, cooldownType);
        return true;
    }

    /// <summary>
    ///     Gets the remaining cooldown time in seconds for a specific operation.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation</param>
    /// <returns>Remaining time in seconds, or 0 if no cooldown is active</returns>
    public double GetRemainingCooldown(string playerUID, CooldownType cooldownType)
    {
        if (string.IsNullOrWhiteSpace(playerUID))
            throw new ArgumentException("PlayerUID cannot be null or empty", nameof(playerUID));

        if (!_cooldowns.TryGetValue(playerUID, out var playerCooldowns))
            return 0.0;

        if (!playerCooldowns.TryGetValue(cooldownType, out var expiryTimeMs))
            return 0.0;

        var nowMs = _sapi.World.ElapsedMilliseconds;
        if (nowMs >= expiryTimeMs)
            return 0.0;

        return (expiryTimeMs - nowMs) / 1000.0;
    }

    /// <summary>
    ///     Clears all cooldowns for a specific player.
    ///     Useful for testing or admin commands.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    public void ClearPlayerCooldowns(string playerUID)
    {
        if (string.IsNullOrWhiteSpace(playerUID))
            throw new ArgumentException("PlayerUID cannot be null or empty", nameof(playerUID));

        _cooldowns.TryRemove(playerUID, out _);
    }

    /// <summary>
    ///     Clears a specific cooldown for a player.
    ///     Useful for testing or admin commands.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of cooldown to clear</param>
    public void ClearSpecificCooldown(string playerUID, CooldownType cooldownType)
    {
        if (string.IsNullOrWhiteSpace(playerUID))
            throw new ArgumentException("PlayerUID cannot be null or empty", nameof(playerUID));

        if (_cooldowns.TryGetValue(playerUID, out var playerCooldowns))
            playerCooldowns.TryRemove(cooldownType, out _);
    }

    /// <summary>
    ///     Gets the configured cooldown duration in seconds for a specific operation type.
    /// </summary>
    /// <param name="cooldownType">The type of operation</param>
    /// <returns>Cooldown duration in seconds</returns>
    internal int GetCooldownDuration(CooldownType cooldownType)
    {
        return cooldownType switch
        {
            CooldownType.ReligionDeletion => _config.ReligionDeletionCooldown,
            CooldownType.MemberKick => _config.MemberKickCooldown,
            CooldownType.MemberBan => _config.MemberBanCooldown,
            CooldownType.Invite => _config.InviteCooldown,
            CooldownType.ReligionCreation => _config.ReligionCreationCooldown,
            CooldownType.Proposal => _config.ProposalCooldown,
            CooldownType.WarDeclaration => _config.WarDeclarationCooldown,
            _ => throw new ArgumentOutOfRangeException(nameof(cooldownType), cooldownType, "Unknown cooldown type")
        };
    }

    /// <summary>
    ///     Cleans up expired cooldowns to prevent memory leaks.
    ///     Called automatically every 5 minutes.
    /// </summary>
    /// <param name="dt">Delta time (unused)</param>
    private void CleanupExpiredCooldowns(float dt)
    {
        ThreadSafetyUtils.WithLock(_cleanupLock, () =>
        {
            var nowMs = _sapi.World.ElapsedMilliseconds;
            var playersToRemove = new System.Collections.Generic.List<string>();
            var totalCleaned = 0;

            foreach (var (playerUID, playerCooldowns) in _cooldowns)
            {
                var cooldownsToRemove = new System.Collections.Generic.List<CooldownType>();

                foreach (var (cooldownType, expiryTimeMs) in playerCooldowns)
                {
                    if (nowMs >= expiryTimeMs)
                        cooldownsToRemove.Add(cooldownType);
                }

                foreach (var cooldownType in cooldownsToRemove)
                {
                    playerCooldowns.TryRemove(cooldownType, out _);
                    totalCleaned++;
                }

                // Remove player entry if no cooldowns remain
                if (playerCooldowns.IsEmpty)
                    playersToRemove.Add(playerUID);
            }

            foreach (var playerUID in playersToRemove)
                _cooldowns.TryRemove(playerUID, out _);

            if (totalCleaned > 0 || playersToRemove.Count > 0)
            {
                _sapi.Logger.Debug(
                    $"[DivineAscension] Cooldown cleanup: removed {totalCleaned} expired cooldown(s) and {playersToRemove.Count} empty player record(s)");
            }
        }, "CooldownCleanup");
    }

    /// <summary>
    ///     Gets the total number of active cooldowns across all players.
    ///     Primarily for testing and diagnostics.
    /// </summary>
    internal int GetActiveCooldownCount()
    {
        var count = 0;
        foreach (var playerCooldowns in _cooldowns.Values)
            count += playerCooldowns.Count;
        return count;
    }

    /// <summary>
    ///     Gets the number of players with active cooldowns.
    ///     Primarily for testing and diagnostics.
    /// </summary>
    internal int GetPlayerCount()
    {
        return _cooldowns.Count;
    }
}
