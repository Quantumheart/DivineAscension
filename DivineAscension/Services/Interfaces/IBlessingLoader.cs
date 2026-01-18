using System.Collections.Generic;
using DivineAscension.Models;

namespace DivineAscension.Services.Interfaces;

/// <summary>
///     Interface for loading blessing definitions from external sources.
///     Enables dependency injection and testing of blessing loading logic.
/// </summary>
public interface IBlessingLoader
{
    /// <summary>
    ///     Loads all blessing definitions from the configured source.
    /// </summary>
    /// <returns>
    ///     A list of Blessing objects. Returns an empty list if loading fails.
    /// </returns>
    List<Blessing> LoadBlessings();

    /// <summary>
    ///     Gets whether the loader successfully loaded blessings.
    ///     Use this to determine if fallback to hardcoded definitions is needed.
    /// </summary>
    bool LoadedSuccessfully { get; }

    /// <summary>
    ///     Gets the count of blessings that were loaded.
    /// </summary>
    int LoadedCount { get; }
}
