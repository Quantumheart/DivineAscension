namespace DivineAscension.Services.Abstractions;

/// <summary>
/// Abstraction for world data persistence.
/// Decouples business logic from Vintage Story's save game data storage.
/// </summary>
public interface IWorldPersistence
{
    /// <summary>
    /// Retrieves stored data by key.
    /// </summary>
    /// <param name="key">The data key</param>
    /// <returns>The stored data, or null if not found</returns>
    byte[]? GetData(string key);

    /// <summary>
    /// Stores data with the specified key.
    /// </summary>
    /// <param name="key">The data key</param>
    /// <param name="data">The data to store</param>
    void StoreData(string key, byte[] data);
}