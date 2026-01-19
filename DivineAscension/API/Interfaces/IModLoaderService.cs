using Vintagestory.API.Common;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for checking mod dependencies and accessing mod systems.
/// Wraps IModLoader for testability.
/// </summary>
public interface IModLoaderService
{
    /// <summary>
    /// Checks if a specific mod is currently enabled.
    /// </summary>
    /// <param name="modId">The mod identifier to check</param>
    /// <returns>True if the mod is enabled, false otherwise</returns>
    bool IsModEnabled(string modId);

    /// <summary>
    /// Gets a specific mod system by type.
    /// </summary>
    /// <typeparam name="T">The type of mod system to retrieve</typeparam>
    /// <returns>The mod system instance if found, null otherwise</returns>
    T? GetModSystem<T>() where T : ModSystem;

    /// <summary>
    /// Gets a mod system by name (for cases where type reference isn't available).
    /// </summary>
    /// <param name="systemName">The name of the mod system</param>
    /// <returns>The mod system if found, null otherwise</returns>
    ModSystem? GetModSystemByName(string systemName);
}
