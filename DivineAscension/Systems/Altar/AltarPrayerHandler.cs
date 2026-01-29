using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.BuffSystem.Interfaces;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Altar;

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

    private readonly AltarEventEmitter _altarEventEmitter;
    private readonly IBuffManager _buffManager;
    private readonly GameBalanceConfig _config;
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILoggerWrapper _logger;
    private readonly IPlayerMessengerService _messenger;
    private readonly IOfferingEvaluator _offeringEvaluator;
    private readonly IPrayerEffectsService _prayerEffectsService;
    private readonly IPlayerProgressionDataManager _progressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly IRitualContributionService _ritualContributionService;
    private readonly ITimeService _timeService;

    public AltarPrayerHandler(
        ILoggerWrapper logger,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        IPlayerProgressionDataManager progressionDataManager,
        IPlayerMessengerService messenger,
        IBuffManager buffManager,
        GameBalanceConfig config,
        ITimeService timeService,
        AltarEventEmitter altarEventEmitter,
        IOfferingEvaluator offeringEvaluator,
        IPrayerEffectsService prayerEffectsService,
        IRitualContributionService ritualContributionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _progressionDataManager =
            progressionDataManager ?? throw new ArgumentNullException(nameof(progressionDataManager));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _buffManager = buffManager ?? throw new ArgumentNullException(nameof(buffManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        _altarEventEmitter = altarEventEmitter ?? throw new ArgumentNullException(nameof(altarEventEmitter));
        _offeringEvaluator = offeringEvaluator ?? throw new ArgumentNullException(nameof(offeringEvaluator));
        _prayerEffectsService = prayerEffectsService ?? throw new ArgumentNullException(nameof(prayerEffectsService));
        _ritualContributionService = ritualContributionService ?? throw new ArgumentNullException(nameof(ritualContributionService));
    }

    public void Dispose()
    {
        _altarEventEmitter.OnAltarUsed -= OnAltarUsed;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Prayer Handler...");
        _altarEventEmitter.OnAltarUsed += OnAltarUsed;
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
                Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_ALTAR_NOT_CONSECRATED));
        }

        // Validate player can pray
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
        {
            return new PrayerResult(
                Success: false,
                Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_NO_RELIGION));
        }

        // Check if player can pray at this holy site:
        // 1. Same religion (member of the religion that owns the holy site)
        // 2. Same domain (player's religion worships the same deity domain)
        if (religion.ReligionUID != holySite.ReligionUID)
        {
            // Not a member - check if same domain allows prayer
            var holySiteOwnerReligion = _religionManager.GetReligion(holySite.ReligionUID);
            var holySiteDomain = holySiteOwnerReligion?.Domain ?? DeityDomain.None;

            if (religion.Domain != holySiteDomain || holySiteDomain == DeityDomain.None)
            {
                return new PrayerResult(
                    Success: false,
                    Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_WRONG_DOMAIN));
            }
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
                Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_COOLDOWN, remainingMinutes));
        }

        // Calculate holy site tier for offering validation and multipliers
        var tier = holySite.GetTier();

        // Check for ritual contribution (auto-discover and start if needed)
        if (offering != null && offering.StackSize > 0)
        {
            var ritualResult = _ritualContributionService.TryContributeToRitual(
                holySite, offering, religion, playerUID, playerName);

            if (ritualResult.Success)
            {
                // Ritual contribution succeeded - return without updating cooldown
                return new PrayerResult(
                    Success: true,
                    Message: ritualResult.Message,
                    FavorAwarded: ritualResult.FavorAwarded,
                    PrestigeAwarded: ritualResult.PrestigeAwarded,
                    HolySiteTier: tier,
                    BuffMultiplier: 0f, // No buff for ritual contributions
                    ShouldConsumeOffering: ritualResult.ShouldConsumeOffering,
                    ShouldUpdateCooldown: false // No cooldown for ritual contributions
                );
            }
            // If ritual contribution failed (not a ritual item), continue with normal prayer flow
        }

        // Process offering
        int offeringBonus = 0;
        bool offeringRejected = false;
        bool shouldConsumeOffering = false;

        if (offering != null && offering.StackSize > 0)
        {
            offeringBonus = _offeringEvaluator.CalculateOfferingValue(offering, religion.Domain, tier);
            if (offeringBonus == -1)
            {
                // Offering rejected due to insufficient holy site tier
                return new PrayerResult(
                    Success: false,
                    Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_OFFERING_TIER_REJECTED));
            }

            if (offeringBonus > 0)
            {
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
            message = LocalizationService.Instance.Get(
                LocalizationKeys.PRAYER_SUCCESS_WITH_OFFERING,
                totalFavor,
                totalPrestige,
                offeringBonus,
                tier,
                prayerMultiplier,
                buffMultiplier);
        }
        else if (offeringRejected)
        {
            message = LocalizationService.Instance.Get(
                LocalizationKeys.PRAYER_SUCCESS_OFFERING_REJECTED,
                totalFavor,
                totalPrestige,
                tier,
                prayerMultiplier,
                buffMultiplier);
        }
        else
        {
            message = LocalizationService.Instance.Get(
                LocalizationKeys.PRAYER_SUCCESS_NO_OFFERING,
                totalFavor,
                totalPrestige,
                tier,
                prayerMultiplier,
                buffMultiplier);
        }

        return message;
    }

    [ExcludeFromCodeCoverage]
    private void OnAltarUsed(IPlayer player, BlockSelection blockSel)
    {
        try
        {
            var serverPlayer = player as IServerPlayer;
            _logger.Debug($"[DivineAscension] Player {player.PlayerName} used altar at {blockSel.Position}");

            if (serverPlayer == null)
                return;
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
                _messenger.SendMessage(serverPlayer, result.Message, EnumChatType.CommandError);
                return;
            }

            // Get player's domain for particle color
            var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
            var domain = playerReligion?.Domain ?? DeityDomain.None;

            // Play prayer effects (animation, particles, sound) via extracted service
            _prayerEffectsService.PlayPrayerEffects(player, blockSel.Position, result.HolySiteTier, domain);

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
            _messenger.SendMessage(serverPlayer, result.Message, EnumChatType.CommandSuccess);

            _logger.Debug(
                $"[DivineAscension] {player.PlayerName} prayed, awarded {result.FavorAwarded} favor and {result.PrestigeAwarded} prestige");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error in AltarPrayerHandler: {ex.Message}");
        }
    }
}
