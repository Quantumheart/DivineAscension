using System;
using Vintagestory.API.Common;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for subscribing to game events and registering periodic callbacks.
/// Wraps ICoreServerAPI.Event for testability.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Subscribe to the SaveGameLoaded event, fired when a save game is loaded.
    /// </summary>
    void OnSaveGameLoaded(Action callback);

    /// <summary>
    /// Subscribe to the GameWorldSave event, fired when the game world is being saved.
    /// </summary>
    void OnGameWorldSave(Action callback);

    /// <summary>
    /// Subscribe to the PlayerJoin event, fired when a player joins the server.
    /// </summary>
    void OnPlayerJoin(PlayerDelegate callback);

    /// <summary>
    /// Subscribe to the PlayerDisconnect event, fired when a player disconnects from the server.
    /// </summary>
    void OnPlayerDisconnect(PlayerDelegate callback);

    /// <summary>
    /// Subscribe to the PlayerDeath event, fired when a player dies.
    /// </summary>
    void OnPlayerDeath(PlayerDeathDelegate callback);

    /// <summary>
    /// Subscribe to the BreakBlock event, fired when a player breaks a block.
    /// </summary>
    void OnBreakBlock(BlockBreakDelegate callback);

    /// <summary>
    /// Subscribe to the DidUseBlock event, fired after a player uses a block.
    /// </summary>
    void OnDidUseBlock(BlockUsedDelegate callback);

    /// <summary>
    /// Subscribe to the DidPlaceBlock event, fired after a player places a block.
    /// </summary>
    void OnDidPlaceBlock(BlockPlacedDelegate callback);

    /// <summary>
    /// Register a game tick listener that is called periodically.
    /// </summary>
    /// <param name="callback">The callback to execute. Receives delta time in seconds.</param>
    /// <param name="intervalMs">The interval in milliseconds between calls.</param>
    /// <returns>A callback ID that can be used to unregister the listener.</returns>
    long RegisterGameTickListener(Action<float> callback, int intervalMs);

    /// <summary>
    /// Register a callback that is called periodically.
    /// </summary>
    /// <param name="callback">The callback to execute. Receives delta time in seconds.</param>
    /// <param name="intervalMs">The interval in milliseconds between calls.</param>
    /// <returns>A callback ID that can be used to unregister the callback.</returns>
    long RegisterCallback(Action<float> callback, int intervalMs);

    /// <summary>
    /// Unregister a previously registered callback or game tick listener.
    /// </summary>
    /// <param name="callbackId">The ID returned from RegisterCallback or RegisterGameTickListener.</param>
    void UnregisterCallback(long callbackId);

    /// <summary>
    /// Unsubscribe from the SaveGameLoaded event.
    /// </summary>
    void UnsubscribeSaveGameLoaded(Action callback);

    /// <summary>
    /// Unsubscribe from the GameWorldSave event.
    /// </summary>
    void UnsubscribeGameWorldSave(Action callback);

    /// <summary>
    /// Unsubscribe from the PlayerJoin event.
    /// </summary>
    void UnsubscribePlayerJoin(PlayerDelegate callback);

    /// <summary>
    /// Unsubscribe from the PlayerDisconnect event.
    /// </summary>
    void UnsubscribePlayerDisconnect(PlayerDelegate callback);

    /// <summary>
    /// Unsubscribe from the PlayerDeath event.
    /// </summary>
    void UnsubscribePlayerDeath(PlayerDeathDelegate callback);

    /// <summary>
    /// Unsubscribe from the BreakBlock event.
    /// </summary>
    void UnsubscribeBreakBlock(BlockBreakDelegate callback);
}