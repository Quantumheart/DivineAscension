using System;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
/// Manages ritual progress tracking and tier upgrades for holy sites.
/// </summary>
public class RitualProgressManager : IRitualProgressManager
{
    private readonly ILogger _logger;
    private readonly IRitualLoader _ritualLoader;
    private readonly IHolySiteManager _holySiteManager;
    private readonly IReligionManager _religionManager;
    private readonly RitualMatcher _ritualMatcher;

    public RitualProgressManager(
        ILogger logger,
        IRitualLoader ritualLoader,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ritualLoader = ritualLoader ?? throw new ArgumentNullException(nameof(ritualLoader));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _ritualMatcher = new RitualMatcher();
    }

    /// <inheritdoc />
    public RitualStartResult StartRitual(string siteUID, string ritualId, string playerUID)
    {
        // Get holy site
        var site = _holySiteManager.GetHolySite(siteUID);
        if (site == null)
        {
            return new RitualStartResult(false, "Holy site not found");
        }

        // Get religion
        var religion = _religionManager.GetReligion(site.ReligionUID);
        if (religion == null)
        {
            return new RitualStartResult(false, "Religion not found");
        }

        // Verify player is consecrator (founder of the holy site)
        if (site.FounderUID != playerUID)
        {
            return new RitualStartResult(false, "Only the site consecrator can start rituals");
        }

        // Check if ritual already active
        if (site.ActiveRitual != null)
        {
            return new RitualStartResult(false, "A ritual is already in progress at this site");
        }

        // Get ritual definition
        var ritual = _ritualLoader.GetRitualById(ritualId);
        if (ritual == null)
        {
            return new RitualStartResult(false, "Ritual not found");
        }

        // Validate domain match
        if (ritual.Domain != religion.Domain)
        {
            return new RitualStartResult(false, $"This ritual is for {ritual.Domain} domain, but your religion follows {religion.Domain}");
        }

        // Validate tier progression
        var currentTier = site.GetTier();
        if (ritual.SourceTier != currentTier)
        {
            return new RitualStartResult(false, $"This ritual requires a Tier {ritual.SourceTier} site, but this site is Tier {currentTier}");
        }

        // Initialize ritual progress
        var progressData = new RitualProgressData
        {
            RitualId = ritualId,
            StartedAt = DateTime.UtcNow,
            Progress = new()
        };

        // Initialize progress for each requirement
        foreach (var requirement in ritual.Requirements)
        {
            progressData.Progress[requirement.RequirementId] = new ItemProgress
            {
                QuantityContributed = 0,
                QuantityRequired = requirement.Quantity,
                Contributors = new()
            };
        }

        // Set active ritual
        site.ActiveRitual = progressData;

        _logger.Debug($"[DivineAscension RitualProgressManager] Started ritual '{ritual.Name}' at holy site '{site.SiteName}' (UID: {siteUID})");

        return new RitualStartResult(true, $"Started ritual: {ritual.Name}", ritual);
    }

    /// <inheritdoc />
    public RitualContributionResult ContributeToRitual(string siteUID, ItemStack offering, string playerUID)
    {
        // Get holy site
        var site = _holySiteManager.GetHolySite(siteUID);
        if (site == null)
        {
            return new RitualContributionResult(false, "Holy site not found");
        }

        // Check if ritual is active
        if (site.ActiveRitual == null)
        {
            return new RitualContributionResult(false, "No ritual in progress");
        }

        // Get ritual definition
        var ritual = _ritualLoader.GetRitualById(site.ActiveRitual.RitualId);
        if (ritual == null)
        {
            _logger.Error($"[DivineAscension RitualProgressManager] Active ritual ID '{site.ActiveRitual.RitualId}' not found in definitions");
            return new RitualContributionResult(false, "Ritual definition not found");
        }

        // Find matching requirement
        var matchingRequirement = _ritualMatcher.FindMatchingRequirement(offering, ritual.Requirements);
        if (matchingRequirement == null)
        {
            return new RitualContributionResult(false, "This item is not needed for the current ritual");
        }

        // Get progress for this requirement
        if (!site.ActiveRitual.Progress.TryGetValue(matchingRequirement.RequirementId, out var itemProgress))
        {
            _logger.Error($"[DivineAscension RitualProgressManager] Progress tracking not found for requirement '{matchingRequirement.RequirementId}'");
            return new RitualContributionResult(false, "Progress tracking error");
        }

        // Check if requirement already completed
        if (itemProgress.QuantityContributed >= itemProgress.QuantityRequired)
        {
            return new RitualContributionResult(false, $"This requirement ({matchingRequirement.DisplayName}) is already completed");
        }

        // Calculate contribution amount (respect stack size and remaining needed)
        var remainingNeeded = itemProgress.QuantityRequired - itemProgress.QuantityContributed;
        var contributionAmount = Math.Min(offering.StackSize, remainingNeeded);

        // Update progress
        itemProgress.QuantityContributed += contributionAmount;

        // Track contributor
        if (!itemProgress.Contributors.ContainsKey(playerUID))
        {
            itemProgress.Contributors[playerUID] = 0;
        }
        itemProgress.Contributors[playerUID] += contributionAmount;

        var requirementCompleted = itemProgress.QuantityContributed >= itemProgress.QuantityRequired;

        _logger.Debug($"[DivineAscension RitualProgressManager] Player {playerUID} contributed {contributionAmount}x {offering.Collectible.Code} to ritual at site {siteUID}");

        // Check if entire ritual is completed
        var ritualCompleted = CheckAndCompleteRitual(site, ritual);

        return new RitualContributionResult(
            Success: true,
            Message: ritualCompleted
                ? $"Ritual completed! {site.SiteName} is now a Tier {site.RitualTier} holy site!"
                : $"Contributed {contributionAmount}x {matchingRequirement.DisplayName} ({itemProgress.QuantityContributed}/{itemProgress.QuantityRequired})",
            RequirementId: matchingRequirement.RequirementId,
            QuantityContributed: itemProgress.QuantityContributed,
            QuantityRequired: itemProgress.QuantityRequired,
            RequirementCompleted: requirementCompleted,
            RitualCompleted: ritualCompleted
        );
    }

    /// <inheritdoc />
    public bool CancelRitual(string siteUID, string playerUID)
    {
        // Get holy site
        var site = _holySiteManager.GetHolySite(siteUID);
        if (site == null)
        {
            return false;
        }

        // Check if ritual is active
        if (site.ActiveRitual == null)
        {
            return false;
        }

        // Verify player is consecrator
        if (site.FounderUID != playerUID)
        {
            return false;
        }

        // Clear active ritual (no refunds)
        var ritualId = site.ActiveRitual.RitualId;
        site.ActiveRitual = null;

        _logger.Debug($"[DivineAscension RitualProgressManager] Cancelled ritual '{ritualId}' at holy site '{site.SiteName}' (UID: {siteUID})");

        return true;
    }

    /// <summary>
    /// Checks if all requirements are met and completes the ritual if so.
    /// </summary>
    /// <returns>True if ritual was completed</returns>
    private bool CheckAndCompleteRitual(HolySiteData site, Ritual ritual)
    {
        if (site.ActiveRitual == null)
            return false;

        // Check if all requirements are completed
        var allComplete = site.ActiveRitual.Progress.Values
            .All(p => p.QuantityContributed >= p.QuantityRequired);

        if (!allComplete)
            return false;

        // Upgrade tier
        site.RitualTier = ritual.TargetTier;
        site.ActiveRitual = null;

        _logger.Notification($"[DivineAscension RitualProgressManager] Ritual '{ritual.Name}' completed at holy site '{site.SiteName}' (UID: {site.SiteUID}). Tier upgraded to {ritual.TargetTier}");

        return true;
    }
}
