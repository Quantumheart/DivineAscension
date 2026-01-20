using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
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
    private readonly IActivityLogManager _activityLogManager;

    private readonly IEventService _eventService;
    private readonly IFavorSystem _favorSystem;
    private readonly IHolySiteManager _holySiteManager;

    // Cooldown tracking: playerUID => last prayer timestamp (in elapsed milliseconds)
    private readonly ConcurrentDictionary<string, long> _lastPrayerTime = new();
    private readonly ILogger _logger;
    private readonly IPlayerMessengerService _messenger;
    private readonly IOfferingLoader _offeringLoader;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public AltarPrayerHandler(
        ILogger logger,
        IEventService eventService,
        IOfferingLoader offeringLoader,
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
        _offeringLoader = offeringLoader ?? throw new ArgumentNullException(nameof(offeringLoader));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
        _prestigeManager = prestigeManager ?? throw new ArgumentNullException(nameof(prestigeManager));
        _activityLogManager = activityLogManager ?? throw new ArgumentNullException(nameof(activityLogManager));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    public void Dispose()
    {
        _eventService.UnsubscribeDidUseBlock(OnBlockUsed);
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Prayer Handler...");
        _eventService.OnDidUseBlock(OnBlockUsed);
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);
        _logger.Notification("[DivineAscension] Altar Prayer Handler initialized");
    }

    [ExcludeFromCodeCoverage]
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
                _messenger.SendMessage(player, "This altar is not consecrated. It must be part of a holy site.",
                    EnumChatType.CommandError);
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
                _messenger.SendMessage(player, "You can only pray at altars belonging to your religion.",
                    EnumChatType.CommandError);
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

            // Calculate holy site tier for offering validation and multipliers
            var tier = holySite.GetTier();

            // Process offering (check active hand)
            var offering = player.Entity.RightHandItemSlot?.Itemstack;
            int offeringBonus = 0;
            string offeringName = "";
            bool offeringRejected = false;
            if (offering != null && offering.StackSize > 0)
            {
                offeringBonus = CalculateOfferingValue(offering, religion.Domain, tier);
                if (offeringBonus == -1)
                {
                    // Offering rejected due to insufficient holy site tier
                    _messenger.SendMessage(player,
                        "This holy site is not powerful enough to accept such a valuable offering.",
                        EnumChatType.CommandError);
                    return;
                }
                else if (offeringBonus > 0)
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
                msg =
                    $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (offering bonus: +{offeringBonus} base, tier {tier} x{prayerMultiplier:F1})";
            }
            else if (offeringRejected)
            {
                msg =
                    $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1}). Your offering was not suitable for this domain.";
            }
            else
            {
                msg =
                    $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1})";
            }

            _messenger.SendMessage(player, msg, EnumChatType.CommandSuccess);

            _logger.Debug(
                $"[DivineAscension] {player.PlayerName} prayed at {holySite.SiteName}, awarded {totalFavor} favor and {totalPrestige} prestige");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error in AltarPrayerHandler: {ex.Message}");
        }
    }

    [ExcludeFromCodeCoverage]
    private bool IsAltarBlock(Block block)
    {
        // Match any block with code path starting with "altar"
        return block?.Code?.Path?.StartsWith("altar") ?? false;
    }

    /// <summary>
    /// Calculate offering value based on domain-specific items with tier gating.
    /// </summary>
    /// <param name="offering">The item stack being offered</param>
    /// <param name="domain">The deity domain to match against</param>
    /// <param name="holySiteTier">The tier of the holy site (1-3)</param>
    /// <returns>
    /// Offering value if valid and tier-acceptable,
    /// -1 if rejected by tier gate (holy site tier too low),
    /// 0 if not a valid offering for this domain
    /// </returns>
    private int CalculateOfferingValue(ItemStack offering, DeityDomain domain, int holySiteTier)
    {
        var fullCode = offering.Collectible?.Code?.ToString() ?? string.Empty;

        var match = _offeringLoader.FindOfferingByItemCode(fullCode, domain);

        if (match == null)
            return 0; // Not a valid offering for this domain

        // Tier gating: check minimum holy site tier requirement
        if (holySiteTier < match.MinHolySiteTier)
            return -1; // Special value to indicate "rejected by tier gate"

        return match.Value;
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up cooldown tracking when player disconnects
        _lastPrayerTime.TryRemove(player.PlayerUID, out _);
    }
}