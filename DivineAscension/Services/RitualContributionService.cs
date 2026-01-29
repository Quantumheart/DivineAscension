using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
/// Handles ritual auto-discovery and contribution workflow at holy sites.
/// Manages the logic for detecting matching offerings, starting rituals automatically,
/// and tracking contributions with appropriate favor/prestige rewards.
/// </summary>
public class RitualContributionService : IRitualContributionService
{
    private const float RITUAL_CONTRIBUTION_MULTIPLIER = 0.5f; // 50% of normal favor for ritual contributions

    private readonly IRitualProgressManager _ritualProgressManager;
    private readonly IRitualLoader _ritualLoader;
    private readonly IOfferingEvaluator _offeringEvaluator;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IWorldService _worldService;
    private readonly ILoggerWrapper _logger;

    public RitualContributionService(
        IRitualProgressManager ritualProgressManager,
        IRitualLoader ritualLoader,
        IOfferingEvaluator offeringEvaluator,
        IPlayerProgressionService progressionService,
        IWorldService worldService,
        ILoggerWrapper logger)
    {
        _ritualProgressManager = ritualProgressManager ?? throw new ArgumentNullException(nameof(ritualProgressManager));
        _ritualLoader = ritualLoader ?? throw new ArgumentNullException(nameof(ritualLoader));
        _offeringEvaluator = offeringEvaluator ?? throw new ArgumentNullException(nameof(offeringEvaluator));
        _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public RitualAttemptResult TryContributeToRitual(
        HolySiteData holySite,
        ItemStack offering,
        ReligionData religion,
        string playerUID,
        string playerName)
    {
        var tier = holySite.GetTier();

        // If no active ritual, try to auto-discover and start one
        if (holySite.ActiveRitual == null)
        {
            var autoStartResult = TryAutoStartRitual(holySite, offering, religion, playerUID, playerName);
            if (autoStartResult != null)
            {
                return autoStartResult;
            }
            // No matching ritual found, return failure
            return new RitualAttemptResult(
                Success: false,
                RitualStarted: false,
                RitualCompleted: false,
                Message: string.Empty);
        }

        // Try to contribute to active ritual
        var contributionResult = _ritualProgressManager.ContributeToRitual(
            holySite.SiteUID, offering, playerUID);

        if (!contributionResult.Success)
        {
            // Contribution failed - item doesn't match any requirement
            return new RitualAttemptResult(
                Success: false,
                RitualStarted: false,
                RitualCompleted: false,
                Message: contributionResult.Message);
        }

        // Calculate reduced favor/prestige (50% of normal offering value)
        var ritualOfferingBonus = _offeringEvaluator.CalculateOfferingValue(offering, religion.Domain, tier);
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

        // Build result message
        var message = contributionResult.Message + (reducedFavor > 0 ? $" (+{reducedFavor} favor)" : "");

        return new RitualAttemptResult(
            Success: true,
            RitualStarted: false,
            RitualCompleted: contributionResult.RitualCompleted,
            Message: message,
            FavorAwarded: reducedFavor,
            PrestigeAwarded: reducedFavor,
            ShouldConsumeOffering: true);
    }

    /// <summary>
    /// Attempts to auto-discover and start a ritual when a qualifying item is offered.
    /// </summary>
    private RitualAttemptResult? TryAutoStartRitual(
        HolySiteData holySite,
        ItemStack offering,
        ReligionData religion,
        string playerUID,
        string playerName)
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

        _logger.Notification(
            $"[DivineAscension] Ritual '{ritual.Name}' discovered and started at holy site '{holySite.SiteName}' by player {playerUID}");

        // Now contribute the offering
        var contributionResult = _ritualProgressManager.ContributeToRitual(holySite.SiteUID, offering, playerUID);
        if (!contributionResult.Success)
        {
            _logger.Warning(
                $"[DivineAscension] Failed to contribute after auto-starting ritual: {contributionResult.Message}");
            return null;
        }

        // Calculate reduced favor/prestige
        var tier = holySite.GetTier();
        var ritualOfferingBonus = _offeringEvaluator.CalculateOfferingValue(offering, religion.Domain, tier);
        int reducedFavor = ritualOfferingBonus > 0
            ? (int)Math.Round(ritualOfferingBonus * RITUAL_CONTRIBUTION_MULTIPLIER)
            : 0;

        // Award progression
        if (reducedFavor > 0)
        {
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

        return new RitualAttemptResult(
            Success: true,
            RitualStarted: true,
            RitualCompleted: contributionResult.RitualCompleted,
            Message: discoveryMessage + $"\n{contributionResult.Message}" +
                     (reducedFavor > 0 ? $" (+{reducedFavor} favor)" : ""),
            FavorAwarded: reducedFavor,
            PrestigeAwarded: reducedFavor,
            ShouldConsumeOffering: true);
    }
}
