using System.Collections.Generic;
using DivineAscension.Models;

namespace DivineAscension.Services.Interfaces;

/// <summary>
///     Interface for loading milestone definitions from JSON configuration
/// </summary>
public interface IMilestoneDefinitionLoader
{
    /// <summary>
    ///     Loads all milestone definitions from the configuration file
    /// </summary>
    void LoadMilestones();

    /// <summary>
    ///     Gets a milestone definition by ID
    /// </summary>
    /// <param name="milestoneId">The milestone ID to look up</param>
    /// <returns>The milestone definition, or null if not found</returns>
    MilestoneDefinition? GetMilestone(string milestoneId);

    /// <summary>
    ///     Gets all milestone definitions
    /// </summary>
    IReadOnlyList<MilestoneDefinition> GetAllMilestones();

    /// <summary>
    ///     Gets all major milestones (those that grant rank)
    /// </summary>
    IReadOnlyList<MilestoneDefinition> GetMajorMilestones();

    /// <summary>
    ///     Gets all minor milestones (one-time payouts only)
    /// </summary>
    IReadOnlyList<MilestoneDefinition> GetMinorMilestones();
}
