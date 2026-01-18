namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for persisting and loading data from world saves.
/// Wraps ICoreServerAPI.WorldManager.SaveGame for testability.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Load typed data from the world save using ProtoBuf serialization.
    /// </summary>
    /// <typeparam name="T">The type of data to load.</typeparam>
    /// <param name="key">The unique key for the data.</param>
    /// <returns>The deserialized data if found, otherwise null.</returns>
    T? Load<T>(string key) where T : class;

    /// <summary>
    /// Save typed data to the world save using ProtoBuf serialization.
    /// </summary>
    /// <typeparam name="T">The type of data to save.</typeparam>
    /// <param name="key">The unique key for the data.</param>
    /// <param name="data">The data to serialize and save.</param>
    void Save<T>(string key, T data) where T : class;

    /// <summary>
    /// Load raw byte data from the world save.
    /// </summary>
    /// <param name="key">The unique key for the data.</param>
    /// <returns>The raw byte array if found, otherwise null.</returns>
    byte[]? LoadRaw(string key);

    /// <summary>
    /// Save raw byte data to the world save.
    /// </summary>
    /// <param name="key">The unique key for the data.</param>
    /// <param name="data">The raw byte array to save.</param>
    void SaveRaw(string key, byte[] data);

    /// <summary>
    /// Check if data exists for the specified key.
    /// </summary>
    /// <param name="key">The unique key to check.</param>
    /// <returns>True if data exists, otherwise false.</returns>
    bool Exists(string key);

    /// <summary>
    /// Delete data associated with the specified key.
    /// </summary>
    /// <param name="key">The unique key to delete.</param>
    void Delete(string key);
}
