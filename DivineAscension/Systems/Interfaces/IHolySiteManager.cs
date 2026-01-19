using System.Collections.Generic;
using DivineAscension.Data;
using Vintagestory.API.MathTools;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
/// Interface for managing holy site creation and queries.
/// Holy sites match land claim boundaries and provide territory and prayer bonuses based on tier.
/// </summary>
public interface IHolySiteManager
{
    /// <summary>
    /// Initializes the manager and registers event handlers.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Disposes resources and unregisters event handlers.
    /// </summary>
    void Dispose();

    /// <summary>
    /// Gets the maximum number of holy sites a religion can have based on prestige tier.
    /// Tier is calculated as PrestigeRank + 1 (1-5 range), capped at 5 sites maximum.
    /// </summary>
    int GetMaxSitesForReligion(string religionUID);

    /// <summary>
    /// Checks if a religion can create another holy site based on prestige limits.
    /// </summary>
    bool CanCreateHolySite(string religionUID);

    /// <summary>
    /// Consecrates a land claim as a holy site.
    /// Returns null if validation fails (empty name, empty areas, prestige limit reached, or overlapping site).
    /// </summary>
    HolySiteData? ConsecrateHolySite(string religionUID, string siteName,
        List<Cuboidi> claimAreas, string founderUID);

    /// <summary>
    /// Removes a holy site and all its areas.
    /// Returns false if site not found.
    /// </summary>
    bool DeconsacrateHolySite(string siteUID);

    /// <summary>
    /// Gets a holy site by its UID.
    /// </summary>
    HolySiteData? GetHolySite(string siteUID);

    /// <summary>
    /// Gets the holy site at a specific block position.
    /// </summary>
    HolySiteData? GetHolySiteAtPosition(BlockPos pos);

    /// <summary>
    /// Checks if a player is currently in a holy site.
    /// </summary>
    bool IsPlayerInHolySite(string playerUID, out HolySiteData? site);

    /// <summary>
    /// Gets all holy sites owned by a religion.
    /// </summary>
    List<HolySiteData> GetReligionHolySites(string religionUID);

    /// <summary>
    /// Gets all holy sites in the world.
    /// </summary>
    List<HolySiteData> GetAllHolySites();

    /// <summary>
    /// Handles religion deletion by removing all associated holy sites.
    /// Called when a religion is deleted to maintain data consistency.
    /// </summary>
    void HandleReligionDeleted(string religionUID);
}