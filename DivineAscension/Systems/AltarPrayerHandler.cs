using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.BuffSystem.Interfaces;
using DivineAscension.Systems.Interfaces;
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
    private const float RITUAL_CONTRIBUTION_MULTIPLIER = 0.5f; // 50% of normal favor for ritual contributions
    private readonly AltarEventEmitter _altarEventEmitter;
    private readonly IBuffManager _buffManager;
    private readonly GameBalanceConfig _config;
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILogger _logger;
    private readonly IPlayerMessengerService _messenger;
    private readonly IOfferingLoader _offeringLoader;
    private readonly IPlayerProgressionDataManager _progressionDataManager;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IReligionManager _religionManager;
    private readonly IRitualLoader _ritualLoader;
    private readonly IRitualProgressManager _ritualProgressManager;
    private readonly ITimeService _timeService;
    private readonly IWorldService _worldService;

    public AltarPrayerHandler(
        ILogger logger,
        IOfferingLoader offeringLoader,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        IPlayerProgressionDataManager progressionDataManager,
        IPlayerProgressionService progressionService,
        IPlayerMessengerService messenger,
        IBuffManager buffManager,
        GameBalanceConfig config,
        ITimeService timeService,
        AltarEventEmitter altarEventEmitter,
        IRitualProgressManager ritualProgressManager,
        IRitualLoader ritualLoader,
        IWorldService worldService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _offeringLoader = offeringLoader ?? throw new ArgumentNullException(nameof(offeringLoader));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _progressionDataManager =
            progressionDataManager ?? throw new ArgumentNullException(nameof(progressionDataManager));
        _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _buffManager = buffManager ?? throw new ArgumentNullException(nameof(buffManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        _altarEventEmitter = altarEventEmitter ?? throw new ArgumentNullException(nameof(altarEventEmitter));
        _ritualProgressManager = ritualProgressManager ?? throw new ArgumentNullException(nameof(ritualProgressManager));
        _ritualLoader = ritualLoader ?? throw new ArgumentNullException(nameof(ritualLoader));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
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

        if (religion.ReligionUID != holySite.ReligionUID)
        {
            return new PrayerResult(
                Success: false,
                Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_WRONG_RELIGION));
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
            // Try to auto-discover and start ritual if not active
            if (holySite.ActiveRitual == null)
            {
                var autoStartResult = TryAutoStartRitual(holySite, offering, religion, playerUID);
                if (autoStartResult != null)
                {
                    // Successfully discovered and started a ritual!
                    return autoStartResult;
                }
            }

            // Try to contribute to active ritual
            if (holySite.ActiveRitual != null)
            {
                var contributionResult = _ritualProgressManager.ContributeToRitual(
                    holySite.SiteUID, offering, playerUID);

                if (contributionResult.Success)
                {
                    // Calculate reduced favor/prestige (50% of normal offering value)
                    var ritualOfferingBonus = CalculateOfferingValue(offering, religion.Domain, tier);
                    int reducedFavor = ritualOfferingBonus > 0
                        ? (int)Math.Round(ritualOfferingBonus * RITUAL_CONTRIBUTION_MULTIPLIER)
                        : 0;

                    // Award progression for ritual contribution
                    if (reducedFavor > 0)
                    {
                        string ritualActivityMsg = contributionResult.RitualCompleted
                            ? $"{playerName} completed a ritual (tier {tier})"
                            : $"{playerName} contributed to ritual (tier {tier})";

                        _progressionService.AwardProgressionForPrayer(
                            playerUID,
                            holySite.ReligionUID,
                            reducedFavor,
                            reducedFavor, // 1:1 ratio
                            religion.Domain,
                            ritualActivityMsg);
                    }

                    // Return success without updating cooldown
                    return new PrayerResult(
                        Success: true,
                        Message: contributionResult.Message + (reducedFavor > 0 ? $" (+{reducedFavor} favor)" : ""),
                        FavorAwarded: reducedFavor,
                        PrestigeAwarded: reducedFavor,
                        HolySiteTier: tier,
                        BuffMultiplier: 0f, // No buff for ritual contributions
                        ShouldConsumeOffering: true,
                        ShouldUpdateCooldown: false // No cooldown for ritual contributions
                    );
                }
                // If contribution failed, continue with normal prayer flow
            }
        }

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
                    Message: LocalizationService.Instance.Get(LocalizationKeys.PRAYER_OFFERING_TIER_REJECTED));
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

    /// <summary>
    /// Attempts to auto-discover and start a ritual when a qualifying item is offered.
    /// </summary>
    private PrayerResult? TryAutoStartRitual(HolySiteData holySite, ItemStack offering, ReligionData religion, string playerUID)
    {
        var currentTier = holySite.GetTier();

        // Can't start ritual if already at max tier
        if (currentTier >= 3)
            return null;

        var targetTier = currentTier + 1;

        // Find ritual for this tier upgrade
        var ritual = _ritualLoader.GetRitualForTierUpgrade(religion.Domain, currentTier, targetTier);
        if (ritual == null)
            return null;

        // Check if offering matches any requirement in any step
        var ritualMatcher = new RitualMatcher();
        var matchingRequirement = false;
        foreach (var step in ritual.Steps)
        {
            if (ritualMatcher.FindMatchingRequirement(offering, step.Requirements) != null)
            {
                matchingRequirement = true;
                break;
            }
        }

        if (!matchingRequirement)
            return null; // Offering doesn't match any ritual requirements

        // Auto-start the ritual!
        var startResult = _ritualProgressManager.StartRitual(holySite.SiteUID, ritual.RitualId, playerUID);
        if (!startResult.Success)
            return null;

        _logger.Notification($"[DivineAscension] Ritual '{ritual.Name}' discovered and started at holy site '{holySite.SiteName}' by player {playerUID}");

        // Now contribute the offering
        var contributionResult = _ritualProgressManager.ContributeToRitual(holySite.SiteUID, offering, playerUID);
        if (!contributionResult.Success)
        {
            _logger.Warning($"[DivineAscension] Failed to contribute after auto-starting ritual: {contributionResult.Message}");
            return null;
        }

        // Calculate reduced favor/prestige
        var tier = holySite.GetTier();
        var ritualOfferingBonus = CalculateOfferingValue(offering, religion.Domain, tier);
        int reducedFavor = ritualOfferingBonus > 0
            ? (int)Math.Round(ritualOfferingBonus * RITUAL_CONTRIBUTION_MULTIPLIER)
            : 0;

        // Award progression
        if (reducedFavor > 0)
        {
            var playerName = _worldService.GetPlayerByUID(playerUID)?.PlayerName ?? "Unknown";
            string activityMsg = $"{playerName} discovered ritual '{ritual.Name}' (tier {tier})";

            _progressionService.AwardProgressionForPrayer(
                playerUID,
                holySite.ReligionUID,
                reducedFavor,
                reducedFavor,
                religion.Domain,
                activityMsg);
        }

        // Return success with discovery message
        var discoveryMessage = LocalizationService.Instance.Get(
            LocalizationKeys.RITUAL_STARTED,
            ritual.Name,
            holySite.SiteName,
            targetTier);

        return new PrayerResult(
            Success: true,
            Message: discoveryMessage + $"\n{contributionResult.Message}" + (reducedFavor > 0 ? $" (+{reducedFavor} favor)" : ""),
            FavorAwarded: reducedFavor,
            PrestigeAwarded: reducedFavor,
            HolySiteTier: tier,
            BuffMultiplier: 0f,
            ShouldConsumeOffering: true,
            ShouldUpdateCooldown: false);
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