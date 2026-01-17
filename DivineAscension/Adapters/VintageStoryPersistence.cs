using DivineAscension.Services.Abstractions;
using Vintagestory.API.Server;

namespace DivineAscension.Adapters;

/// <summary>
/// Adapts Vintage Story's save game data storage to the IWorldPersistence abstraction.
/// </summary>
internal sealed class VintageStoryPersistence(ICoreServerAPI sapi) : IWorldPersistence
{
    public byte[]? GetData(string key)
    {
        return sapi.WorldManager.SaveGame.GetData(key);
    }

    public void StoreData(string key, byte[] data)
    {
        sapi.WorldManager.SaveGame.StoreData(key, data);
    }
}