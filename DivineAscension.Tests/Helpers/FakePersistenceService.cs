using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IPersistenceService for testing.
/// Provides in-memory storage with no actual serialization.
/// </summary>
public sealed class FakePersistenceService : IPersistenceService
{
    private readonly Dictionary<string, object> _store = new();
    private readonly Dictionary<string, byte[]> _rawStore = new();

    public T? Load<T>(string key) where T : class
    {
        if (_store.TryGetValue(key, out var data))
        {
            return (T)data;
        }
        return null;
    }

    public void Save<T>(string key, T data) where T : class
    {
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
    }

    public int Count => _store.Count + _rawStore.Count;

    public IEnumerable<string> GetAllKeys()
    {
        return _store.Keys.Concat(_rawStore.Keys).Distinct();
    }
}
