using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of IPersistenceService that wraps ISaveGame.
/// Provides a thin abstraction layer over Vintage Story's save system for improved testability.
/// </summary>
internal sealed class ServerPersistenceService : IPersistenceService
{
    private readonly ISaveGame _saveGame;

    public ServerPersistenceService(ISaveGame saveGame)
    {
        _saveGame = saveGame ?? throw new ArgumentNullException(nameof(saveGame));
    }

    public T? Load<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        byte[]? data = _saveGame.GetData(key);
        if (data == null || data.Length == 0)
        {
            return null;
        }

        return SerializerUtil.Deserialize<T>(data);
    }

    public void Save<T>(string key, T data) where T : class
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (data == null) throw new ArgumentNullException(nameof(data));

        byte[] serialized = SerializerUtil.Serialize(data);
        _saveGame.StoreData(key, serialized);
    }

    public byte[]? LoadRaw(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        return _saveGame.GetData(key);
    }

    public void SaveRaw(string key, byte[] data)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (data == null) throw new ArgumentNullException(nameof(data));

        _saveGame.StoreData(key, data);
    }

    public bool Exists(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        byte[]? data = _saveGame.GetData(key);
        return data != null && data.Length > 0;
    }

    public void Delete(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        // Vintage Story's ISaveGame doesn't have a delete method,
        // so we store empty byte array as a workaround
        _saveGame.StoreData(key, Array.Empty<byte>());
    }
}
