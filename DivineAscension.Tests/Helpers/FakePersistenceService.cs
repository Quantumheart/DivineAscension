using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IPersistenceService for testing.
/// Provides in-memory storage with no actual serialization.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FakePersistenceService : IPersistenceService
{
    private readonly Dictionary<string, byte[]> _rawStore = new();
    private readonly Dictionary<string, object> _store = new();
    private readonly HashSet<string> _throwOnLoad = new();
    private readonly HashSet<string> _throwOnSave = new();

    public int Count => _store.Count + _rawStore.Count;

    public T? Load<T>(string key) where T : class
    {
        if (_throwOnLoad.Contains(key))
        {
            throw new Exception($"Simulated load error for key: {key}");
        }

        if (_store.TryGetValue(key, out var data))
        {
            return (T)data;
        }

        return null;
    }

    public void Save<T>(string key, T data) where T : class
    {
        if (_throwOnSave.Contains(key))
        {
            throw new Exception($"Simulated save error for key: {key}");
        }

        _store[key] = data;
    }

    public byte[]? LoadRaw(string key)
    {
        return _rawStore.TryGetValue(key, out var data) ? data : null;
    }

    public void SaveRaw(string key, byte[] data)
    {
        _rawStore[key] = data;
    }

    public bool Exists(string key)
    {
        return _store.ContainsKey(key) || _rawStore.ContainsKey(key);
    }

    public void Delete(string key)
    {
        _store.Remove(key);
        _rawStore.Remove(key);
    }

    // Test helper methods
    public void Clear()
    {
        _store.Clear();
        _rawStore.Clear();
        _throwOnLoad.Clear();
        _throwOnSave.Clear();
    }

    public IEnumerable<string> GetAllKeys()
    {
        return _store.Keys.Concat(_rawStore.Keys).Distinct();
    }

    /// <summary>
    /// Configures the fake service to throw an exception when loading the specified key.
    /// </summary>
    public void ThrowOnLoad(string key)
    {
        _throwOnLoad.Add(key);
    }

    /// <summary>
    /// Configures the fake service to throw an exception when saving to the specified key.
    /// </summary>
    public void ThrowOnSave(string key)
    {
        _throwOnSave.Add(key);
    }
}