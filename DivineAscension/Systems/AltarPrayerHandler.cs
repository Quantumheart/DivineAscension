using System;
using System.Collections.Concurrent;
using DivineAscension.API.Interfaces;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Handles player prayer interactions at altars.
/// Players can pray at consecrated altars with 1-hour cooldowns.
/// Optional offerings provide bonus favor.
/// </summary>
public class AltarPrayerHandler : IDisposable
{
    private const int PRAYER_COOLDOWN_MS = 3600000; // 1 hour
    private const int BASE_PRAYER_FAVOR = 5;

    private readonly IEventService _eventService;
    private readonly IHolySiteManager _holySiteManager;
    private readonly IReligionManager _religionManager;
    private readonly IFavorSystem _favorSystem;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IActivityLogManager _activityLogManager;
    private readonly IPlayerMessengerService _messenger;
    private readonly IWorldService _worldService;
    private readonly ILogger _logger;

    // Cooldown tracking: playerUID => last prayer timestamp (in elapsed milliseconds)
    private readonly ConcurrentDictionary<string, long> _lastPrayerTime = new();

    public AltarPrayerHandler(
        ILogger logger,
        IEventService eventService,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        IFavorSystem favorSystem,
        IReligionPrestigeManager prestigeManager,
        IActivityLogManager activityLogManager,
        IPlayerMessengerService messenger,
        IWorldService worldService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
        _prestigeManager = prestigeManager ?? throw new ArgumentNullException(nameof(prestigeManager));
        _activityLogManager = activityLogManager ?? throw new ArgumentNullException(nameof(activityLogManager));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Prayer Handler...");
        _eventService.OnDidUseBlock(OnBlockUsed);
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);
        _logger.Notification("[DivineAscension] Altar Prayer Handler initialized");
    }

    public void Dispose()
    {
        _eventService.UnsubscribeDidUseBlock(OnBlockUsed);
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);
    }

    private void OnBlockUsed(IServerPlayer player, BlockSelection blockSel)
    {
        try
        {
            // Check if player used an altar
            var block = _worldService.GetBlock(blockSel.Position);
            if (!IsAltarBlock(block))
                return;

            _logger.Debug($"[DivineAscension] Player {player.PlayerName} used altar at {blockSel.Position}");

            // Check if altar is part of a holy site
            var holySite = _holySiteManager.GetHolySiteByAltarPosition(blockSel.Position);
            if (holySite == null)
            {
                _messenger.SendMessage(player, "This altar is not consecrated. It must be part of a holy site.", EnumChatType.CommandError);
                return;
            }

            // Validate player can pray
            var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion == null)
            {
                _messenger.SendMessage(player, "You must be in a religion to pray.", EnumChatType.CommandError);
                return;
            }

            if (religion.ReligionUID != holySite.ReligionUID)
            {
                _messenger.SendMessage(player, "You can only pray at altars belonging to your religion.", EnumChatType.CommandError);
                return;
            }

            // Check cooldown
            var now = _worldService.ElapsedMilliseconds;
            if (_lastPrayerTime.TryGetValue(player.PlayerUID, out var lastTime))
            {
                var timeSince = now - lastTime;
                if (timeSince < PRAYER_COOLDOWN_MS)
                {
                    var remainingMinutes = (int)Math.Ceiling((PRAYER_COOLDOWN_MS - timeSince) / 60000.0);
                    _messenger.SendMessage(player,
                        $"You must wait {remainingMinutes} more minute(s) before praying again.",
                        EnumChatType.CommandError);
                    return;
                }
            }

            // Process offering (check active hand)
            var offering = player.Entity.RightHandItemSlot?.Itemstack;
            int offeringBonus = 0;
            string offeringName = "";
            if (offering != null && offering.StackSize > 0)
            {
                offeringBonus = CalculateOfferingValue(offering);
                if (offeringBonus > 0)
                {
                    offeringName = offering.GetName();
                    // Consume offering (1 item)
                    player.Entity.RightHandItemSlot.TakeOut(1);
                    player.Entity.RightHandItemSlot.MarkDirty();
                }
            }

            // Calculate favor with multipliers
            var tier = holySite.GetTier();
            var prayerMultiplier = holySite.GetPrayerMultiplier();
            int totalFavor = (int)Math.Round((BASE_PRAYER_FAVOR + offeringBonus) * prayerMultiplier);

            // Award favor (player progression)
            _favorSystem.AwardFavorForAction(player, "prayer", totalFavor);

            // Award prestige (religion progression) - 1:1 ratio with favor
            int totalPrestige = totalFavor;
            _prestigeManager.AddPrestige(holySite.ReligionUID, totalPrestige, "prayer");

            // Update cooldown
            _lastPrayerTime[player.PlayerUID] = now;

            // Log activity
            string activityMsg = offeringBonus > 0
                ? $"{player.PlayerName} prayed with {offeringName} offering (tier {tier})"
                : $"{player.PlayerName} prayed (tier {tier})";
            _activityLogManager.LogActivity(
                holySite.ReligionUID,
                player.PlayerUID,
                activityMsg,
                totalFavor,
                totalPrestige, // Award prestige equal to favor
                religion.Domain
            );

            // Notify player
            string msg = offeringBonus > 0
                ? $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (offering bonus: +{offeringBonus} base, tier {tier} x{prayerMultiplier:F1})"
                : $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1})";
            _messenger.SendMessage(player, msg, EnumChatType.CommandSuccess);

            _logger.Debug($"[DivineAscension] {player.PlayerName} prayed at {holySite.SiteName}, awarded {totalFavor} favor and {totalPrestige} prestige");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error in AltarPrayerHandler: {ex.Message}");
        }
    }

    private bool IsAltarBlock(Block block)
    {
        // Match any block with code path starting with "altar"
        return block?.Code?.Path?.StartsWith("altar") ?? false;
    }

    private int CalculateOfferingValue(ItemStack offering)
    {
        var path = offering.Collectible?.Code?.Path ?? string.Empty;

        // Tier 3 offerings (+5 base favor)
        if (path.Contains("ingot-gold") || path.Contains("gem-diamond"))
            return 5;

        // Tier 2 offerings (+3 base favor)
        if (path.Contains("ingot-silver") ||
            path.Contains("gem-emerald") ||
            path.Contains("gem-peridot"))
            return 3;

        // Tier 1 offerings (+1 base favor)
        if (path.Contains("ingot-copper") ||
            path.Contains("ingot-bronze") ||
            path.Contains("bread") ||
            path.Contains("honey"))
            return 1;

        return 0; // Not a valid offering
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up cooldown tracking when player disconnects
        _lastPrayerTime.TryRemove(player.PlayerUID, out _);
    }
}
