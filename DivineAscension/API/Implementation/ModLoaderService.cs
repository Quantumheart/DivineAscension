using System;
using System.Linq;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Implementation of IModLoaderService that wraps IModLoader.
/// Works for both client and server contexts.
/// </summary>
public class ModLoaderService : IModLoaderService
{
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    /// <param name="modLoader">The mod loader instance</param>
    public ModLoaderService(IModLoader modLoader)
    {
        _modLoader = modLoader ?? throw new ArgumentNullException(nameof(modLoader));
    }

    public bool IsModEnabled(string modId)
    {
        if (string.IsNullOrEmpty(modId))
            throw new ArgumentException("Mod ID cannot be null or empty", nameof(modId));

        return _modLoader.IsModEnabled(modId);
    }

    public T? GetModSystem<T>() where T : ModSystem
    {
        return _modLoader.GetModSystem<T>();
    }

    public ModSystem? GetModSystemByName(string systemName)
    {
        if (string.IsNullOrEmpty(systemName))
            throw new ArgumentException("System name cannot be null or empty", nameof(systemName));

        return _modLoader.Systems?.FirstOrDefault(s => s.GetType().Name == systemName);
    }
}
