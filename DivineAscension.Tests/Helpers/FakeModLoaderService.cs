using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IModLoaderService for testing.
/// Allows tests to control which mods are "enabled" and register mod systems.
/// </summary>
[ExcludeFromCodeCoverage]
public class FakeModLoaderService : IModLoaderService
{
    private readonly HashSet<string> _enabledMods = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ModSystem> _modSystems = new();

    /// <summary>
    /// Registers a mod system for retrieval by tests.
    /// </summary>
    /// <typeparam name="T">The type of mod system</typeparam>
    /// <param name="instance">The mod system instance</param>
    public void RegisterModSystem<T>(T instance) where T : ModSystem
    {
        if (instance == null) return;

        var typeName = typeof(T).Name;
        _modSystems[typeName] = instance;
    }

    /// <summary>
    /// Marks a mod as enabled for testing.
    /// </summary>
    /// <param name="modId">The mod ID to enable</param>
    public void EnableMod(string modId)
    {
        if (!string.IsNullOrEmpty(modId))
            _enabledMods.Add(modId);
    }

    /// <summary>
    /// Marks a mod as disabled for testing.
    /// </summary>
    /// <param name="modId">The mod ID to disable</param>
    public void DisableMod(string modId)
    {
        if (!string.IsNullOrEmpty(modId))
            _enabledMods.Remove(modId);
    }

    /// <summary>
    /// Clears all enabled mods and registered systems.
    /// </summary>
    public void Clear()
    {
        _enabledMods.Clear();
        _modSystems.Clear();
    }

    public bool IsModEnabled(string modId)
    {
        if (string.IsNullOrEmpty(modId)) return false;
        return _enabledMods.Contains(modId);
    }

    public T? GetModSystem<T>() where T : ModSystem
    {
        var typeName = typeof(T).Name;
        return _modSystems.TryGetValue(typeName, out var system) ? system as T : null;
    }

    public ModSystem? GetModSystemByName(string systemName)
    {
        if (string.IsNullOrEmpty(systemName)) return null;
        return _modSystems.TryGetValue(systemName, out var system) ? system : null;
    }
}
