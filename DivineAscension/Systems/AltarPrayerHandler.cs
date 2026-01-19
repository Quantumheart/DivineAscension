using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
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

    // Domain-specific offering configurations
    private static readonly Dictionary<DeityDomain, DomainOfferingConfig> DomainOfferings = new()
    {
        [DeityDomain.Craft] = new DomainOfferingConfig
        {
            Tier1Items = new[] { "ingot-copper", "ingot-bronze", "ingot-tinbronze", "pickaxe-copper", "axe-copper", "shovel-copper" },
            Tier1Value = 2,
            Tier2Items = new[] { "ingot-iron", "ingot-steel", "pickaxe-iron", "axe-iron", "saw-iron", "hammer-iron" },
            Tier2Value = 4,
            Tier3Items = new[] { "ingot-gold", "ingot-silver", "gear-rusty", "gear-temporal", "anvil" },
            Tier3Value = 8
        },
        [DeityDomain.Wild] = new DomainOfferingConfig
        {
            Tier1Items = new[] { "bushmeat", "redmeat-raw", "poultry-raw", "hide-raw-small", "feather" },
            Tier1Value = 1,
            Tier2Items = new[] { "hide-raw-medium", "hide-raw-large", "redmeat-cured", "fat", "honey" },
            Tier2Value = 3,
            Tier3Items = new[] { "hide-raw-huge", "pelt", "resonance", "forlornhope" },
            Tier3Value = 6
        },
        [DeityDomain.Conquest] = new DomainOfferingConfig
        {
            Tier1Items = new[] { "spear-copper", "sword-copper", "blade-copper", "arrow-copper" },
            Tier1Value = 3,
            Tier2Items = new[] { "spear-iron", "sword-iron", "blade-iron", "armor-body-chain", "armor-head-chain" },
            Tier2Value = 5,
            Tier3Items = new[] { "sword-steel", "spear-steel", "armor-body-plate", "armor-head-basinet", "arrow-steel" },
            Tier3Value = 10
        },
        [DeityDomain.Harvest] = new DomainOfferingConfig
        {
            Tier1Items = new[] { "carrot", "onion", "turnip", "parsnip", "grain-flax", "grain-rice" },
            Tier1Value = 2,
            Tier2Items = new[] { "bread-", "flaxtwine", "linen", "vegetable-pickled", "fruit-pickled" },
            Tier2Value = 4,
            Tier3Items = new[] { "honeycomb", "vegetable-cured", "fruit-cured", "cheese-", "flour" },
            Tier3Value = 7
        },
        [DeityDomain.Stone] = new DomainOfferingConfig
        {
            Tier1Items = new[] { "stone-", "ore-quartz", "ore-copper", "clay-" },
            Tier1Value = 2,
            Tier2Items = new[] { "ore-iron", "ore-lead", "ore-silver", "gem-quartz", "coal" },
            Tier2Value = 5,
            Tier3Items = new[] { "gem-diamond", "gem-emerald", "gem-peridot", "ore-gold", "crystal" },
            Tier3Value = 9
        }
    };

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
            bool offeringRejected = false;
            if (offering != null && offering.StackSize > 0)
            {
                offeringBonus = CalculateOfferingValue(offering, religion.Domain);
                if (offeringBonus > 0)
                {
                    offeringName = offering.GetName();
                    // Consume offering (1 item)
                    player.Entity.RightHandItemSlot.TakeOut(1);
                    player.Entity.RightHandItemSlot.MarkDirty();
                }
                else
                {
                    // Offering doesn't match domain - rejected
                    offeringRejected = true;
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
            string msg;
            if (offeringBonus > 0)
            {
                msg = $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (offering bonus: +{offeringBonus} base, tier {tier} x{prayerMultiplier:F1})";
            }
            else if (offeringRejected)
            {
                msg = $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1}). Your offering was not suitable for this domain.";
            }
            else
            {
                msg = $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1})";
            }
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

    /// <summary>
    /// Calculate offering value based on domain-specific items.
    /// Returns 0 if the offering doesn't match the domain.
    /// </summary>
    private int CalculateOfferingValue(ItemStack offering, DeityDomain domain)
    {
        var path = offering.Collectible?.Code?.Path ?? string.Empty;

        if (!DomainOfferings.TryGetValue(domain, out var config))
            return 0;

        // Check tier 3 items (highest value)
        if (config.Tier3Items.Any(item => path.Contains(item)))
            return config.Tier3Value;

        // Check tier 2 items
        if (config.Tier2Items.Any(item => path.Contains(item)))
            return config.Tier2Value;

        // Check tier 1 items
        if (config.Tier1Items.Any(item => path.Contains(item)))
            return config.Tier1Value;

        return 0; // Not a valid offering for this domain
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up cooldown tracking when player disconnects
        _lastPrayerTime.TryRemove(player.PlayerUID, out _);
    }
}

/// <summary>
/// Configuration for domain-specific offerings
/// </summary>
internal class DomainOfferingConfig
{
    public string[] Tier1Items { get; init; } = Array.Empty<string>();
    public int Tier1Value { get; init; }

    public string[] Tier2Items { get; init; } = Array.Empty<string>();
    public int Tier2Value { get; init; }

    public string[] Tier3Items { get; init; } = Array.Empty<string>();
    public int Tier3Value { get; init; }
}
