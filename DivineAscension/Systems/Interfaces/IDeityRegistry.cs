using System.Collections.Generic;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

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
}