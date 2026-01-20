using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.BuffSystem.Interfaces;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Result of processing a prayer attempt.
/// </summary>
internal record PrayerResult(
    bool Success,
    string Message,
    int FavorAwarded = 0,
    int PrestigeAwarded = 0,
    int HolySiteTier = 0,
    float BuffMultiplier = 0f,
    bool ShouldConsumeOffering = false,
    bool ShouldUpdateCooldown = false);

/// <summary>
/// Handles player prayer interactions at altars.
/// Players can pray at consecrated altars with 1-hour cooldowns.
/// Optional offerings provide bonus favor.
/// </summary>
public class AltarPrayerHandler : IDisposable
{
    private const int PRAYER_COOLDOWN_MS = 3600000; // 1 hour
    private const int BASE_PRAYER_FAVOR = 5;
    private readonly IBuffManager _buffManager;
    private readonly GameBalanceConfig _config;
    private readonly IEventService _eventService;
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILogger _logger;
    private readonly IPlayerMessengerService _messenger;
    private readonly IOfferingLoader _offeringLoader;
    private readonly IPlayerProgressionDataManager _progressionDataManager;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;
    private readonly ITimeService _timeService;

    public AltarPrayerHandler(
        ILogger logger,
        IEventService eventService,
        IOfferingLoader offeringLoader,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        IPlayerProgressionDataManager progressionDataManager,
        IPlayerProgressionService progressionService,
        IPlayerMessengerService messenger,
        IWorldService worldService,
        IBuffManager buffManager,
        GameBalanceConfig config,
        ITimeService timeService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _offeringLoader = offeringLoader ?? throw new ArgumentNullException(nameof(offeringLoader));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _progressionDataManager = progressionDataManager ?? throw new ArgumentNullException(nameof(progressionDataManager));
        _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _buffManager = buffManager ?? throw new ArgumentNullException(nameof(buffManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
    }

    public void Dispose()
    {
        AltarPatches.OnAltarUsed -= OnAltarUsed;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Prayer Handler...");
        AltarPatches.OnAltarUsed += OnAltarUsed;
        _logger.Notification("[DivineAscension] Altar Prayer Handler initialized");
    }

    /// <summary>
    /// Processes a prayer attempt and calculates rewards.
    /// This method contains the core business logic and is testable without framework dependencies.
    /// </summary>
    internal PrayerResult ProcessPrayer(
        string playerUID,
        string playerName,
        BlockPos altarPosition,
        ItemStack? offering,
        long currentTime)
    {
        // Check if altar is part of a holy site
        var holySite = _holySiteManager.GetHolySiteByAltarPosition(altarPosition);
        if (holySite == null)
        {
            return new PrayerResult(
                Success: false,
                Message: "This altar is not consecrated. It must be part of a holy site.");
        }

        // Validate player can pray
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
        {
            return new PrayerResult(
                Success: false,
                Message: "You must be in a religion to pray.");
        }

        if (religion.ReligionUID != holySite.ReligionUID)
        {
            return new PrayerResult(
                Success: false,
                Message: "You can only pray at altars belonging to your religion.");
        }

        // Check cooldown using expiry time from manager
        var cooldownExpiry = _progressionDataManager.GetPrayerCooldownExpiry(playerUID);
        if (cooldownExpiry > 0 && currentTime < cooldownExpiry)
        {
            var remainingMs = cooldownExpiry - currentTime;

            // Round to nearest minute (adds 30s before dividing)
            var remainingMinutes = (int)((remainingMs + 30000) / 60000);
            if (remainingMinutes == 0 && remainingMs > 0)
                remainingMinutes = 1;

            return new PrayerResult(
                Success: false,
                Message: $"You must wait {remainingMinutes} more minute(s) before praying again.");
        }

        // Calculate holy site tier for offering validation and multipliers
        var tier = holySite.GetTier();

        // Process offering
        int offeringBonus = 0;
        string offeringName = "";
        bool offeringRejected = false;
        bool shouldConsumeOffering = false;

        if (offering != null && offering.StackSize > 0)
        {
            offeringBonus = CalculateOfferingValue(offering, religion.Domain, tier);
            if (offeringBonus == -1)
            {
                // Offering rejected due to insufficient holy site tier
                return new PrayerResult(
                    Success: false,
                    Message: "This holy site is not powerful enough to accept such a valuable offering.");
            }
            else if (offeringBonus > 0)
            {
                offeringName = offering.GetName();
                shouldConsumeOffering = true;
            }
            else
            {
                // Offering doesn't match domain - rejected
                offeringRejected = true;
            }
        }

        var prayerMultiplier = holySite.GetPrayerMultiplier();
        int totalFavor = (int)Math.Round((BASE_PRAYER_FAVOR + offeringBonus) * prayerMultiplier);
        int totalPrestige = totalFavor; // 1:1 ratio with favor

        // Calculate buff multiplier
        var buffMultiplier = tier switch
        {
            1 => _config.HolySiteTier1Multiplier,
            2 => _config.HolySiteTier2Multiplier,
            3 => _config.HolySiteTier3Multiplier,
            _ => 1.0f
        };

        // Build success message
        var message = BuildMessage(offeringBonus, totalFavor, totalPrestige, tier, prayerMultiplier, buffMultiplier,
            offeringRejected);

        // Build activity log message
        string activityMsg = offeringBonus > 0
            ? $"{playerName} prayed with {offeringName} offering (tier {tier})"
            : $"{playerName} prayed (tier {tier})";

        // Award progression (this has side effects but is acceptable here)
        _progressionService.AwardProgressionForPrayer(
            playerUID,
            holySite.ReligionUID,
            totalFavor,
            totalPrestige,
            religion.Domain,
            activityMsg);

        return new PrayerResult(
            Success: true,
            Message: message,
            FavorAwarded: totalFavor,
            PrestigeAwarded: totalPrestige,
            HolySiteTier: tier,
            BuffMultiplier: buffMultiplier,
            ShouldConsumeOffering: shouldConsumeOffering,
            ShouldUpdateCooldown: true);
    }

    private static string BuildMessage(int offeringBonus, int totalFavor, int totalPrestige, int tier,
        double prayerMultiplier,
        float buffMultiplier, bool offeringRejected)
    {
        string message;
        if (offeringBonus > 0)
        {
            message =
                $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (offering bonus: +{offeringBonus} base, tier {tier} x{prayerMultiplier:F1}). " +
                $"Divine blessing active for 1 hour ({buffMultiplier:F2}x favor/prestige gains)!";
        }
        else if (offeringRejected)
        {
            message =
                $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1}). Your offering was not suitable for this domain. " +
                $"Divine blessing active for 1 hour ({buffMultiplier:F2}x favor/prestige gains)!";
        }
        else
        {
            message =
                $"Prayer accepted! +{totalFavor} favor, +{totalPrestige} prestige (tier {tier} x{prayerMultiplier:F1}). " +
                $"Divine blessing active for 1 hour ({buffMultiplier:F2}x favor/prestige gains)!";
        }

        return message;
    }

    [ExcludeFromCodeCoverage]
    private void OnAltarUsed(IServerPlayer player, BlockSelection blockSel)
    {
        try
        {
            _logger.Debug($"[DivineAscension] Player {player.PlayerName} used altar at {blockSel.Position}");

            // Capture current time once for consistency between checking and updating cooldown
            var currentTime = _timeService.ElapsedMilliseconds;

            // Call testable core logic
            var result = ProcessPrayer(
                player.PlayerUID,
                player.PlayerName,
                blockSel.Position,
                player.Entity.RightHandItemSlot?.Itemstack,
                currentTime);

            // Handle side effects based on result
            if (!result.Success)
            {
                _messenger.SendMessage(player, result.Message, EnumChatType.CommandError);
                return;
            }

            // Consume offering if needed
            if (result.ShouldConsumeOffering && player.Entity.RightHandItemSlot != null)
            {
                player.Entity.RightHandItemSlot.TakeOut(1);
                player.Entity.RightHandItemSlot.MarkDirty();
            }

            // Apply holy site buff
            var statModifiers = new Dictionary<string, float>
            {
                { VintageStoryStats.HolySiteFavorMultiplier, result.BuffMultiplier },
                { VintageStoryStats.HolySitePrestigeMultiplier, result.BuffMultiplier }
            };

            _buffManager.ApplyEffect(
                player.Entity,
                "holy_site_prayer_buff",
                3600f, // 1 hour in seconds
                "altar_prayer",
                player.PlayerUID,
                statModifiers,
                isBuff: true);

            _logger.Debug(
                $"[DivineAscension] Applied holy site buff ({result.BuffMultiplier:F2}x) to {player.PlayerName} for 1 hour");

            // Set cooldown through manager
            if (result.ShouldUpdateCooldown)
            {
                _progressionDataManager.SetPrayerCooldownExpiry(
                    player.PlayerUID,
                    currentTime + PRAYER_COOLDOWN_MS);
            }

            // Notify player
            _messenger.SendMessage(player, result.Message, EnumChatType.CommandSuccess);

            _logger.Debug(
                $"[DivineAscension] {player.PlayerName} prayed, awarded {result.FavorAwarded} favor and {result.PrestigeAwarded} prestige");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error in AltarPrayerHandler: {ex.Message}");
        }
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
}