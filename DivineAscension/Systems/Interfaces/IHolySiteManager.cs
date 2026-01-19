using System.Collections.Generic;
using DivineAscension.Data;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
/// Interface for managing holy site creation, expansion, and queries.
/// Holy sites provide territory and prayer bonuses based on tier.
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
    /// Creates a new holy site at the specified chunk.
    /// Returns null if validation fails (empty name, prestige limit reached, or chunk already claimed).
    /// </summary>
    HolySiteData? ConsecrateHolySite(string religionUID, string siteName,
        SerializableChunkPos centerChunk, string founderUID);

    /// <summary>
    /// Expands a holy site by adding a new chunk.
    /// Returns false if site not found, chunk already claimed, or max size reached (6 chunks).
    /// </summary>
    bool ExpandHolySite(string siteUID, SerializableChunkPos newChunk);

    /// <summary>
    /// Removes a holy site and all its chunks.
    /// Returns false if site not found.
    /// </summary>
    bool DeconsacrateHolySite(string siteUID);

    /// <summary>
    /// Gets a holy site by its UID.
    /// </summary>
    HolySiteData? GetHolySite(string siteUID);

    /// <summary>
    /// Gets the holy site at a specific chunk position.
    /// </summary>
    HolySiteData? GetHolySiteAtChunk(SerializableChunkPos chunk);

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