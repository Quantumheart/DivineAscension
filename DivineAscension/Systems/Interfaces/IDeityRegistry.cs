using System.Collections.Generic;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.Systems.Interfaces;

/// <summary>
///     Interface for managing all deities in the game
/// </summary>
public interface IDeityRegistry
{
    /// <summary>
    ///     Initializes the registry with all deities
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Gets a deity by type
    /// </summary>
    Deity? GetDeity(DeityType type);

    /// <summary>
    ///     Gets all registered deities
    /// </summary>
    IEnumerable<Deity> GetAllDeities();

    /// <summary>
    ///     Checks if a deity exists
    /// </summary>
    bool HasDeity(DeityType type);

    /// <summary>
    ///     Gets the relationship between two deities
    /// </summary>
    DeityRelationshipType GetRelationship(DeityType deity1, DeityType deity2);

    /// <summary>
    ///     Gets the favor multiplier based on deity relationship
    ///     Allied: 0.5x favor, Rival: 2x favor, Neutral: 1x favor
    /// </summary>
    float GetFavorMultiplier(DeityType attackerDeity, DeityType victimDeity);
}