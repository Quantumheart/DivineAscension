using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of IEventService that wraps IServerEventAPI.
/// Provides a thin abstraction layer over Vintage Story's event system for improved testability.
/// </summary>
internal sealed class ServerEventService(IServerEventAPI eventApi) : IEventService
{
    private readonly IServerEventAPI _eventApi = eventApi ?? throw new ArgumentNullException(nameof(eventApi));

    public void OnSaveGameLoaded(Action callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.SaveGameLoaded += callback;
    }

    public void OnGameWorldSave(Action callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.GameWorldSave += callback;
    }

    public void OnPlayerJoin(PlayerDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerJoin += callback;
    }

    public void OnPlayerDisconnect(PlayerDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDisconnect += callback;
    }

    public void OnPlayerDeath(PlayerDeathDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDeath += callback;
    }

    public void OnBreakBlock(BlockBreakDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.BreakBlock += callback;
    }

    public void OnDidUseBlock(BlockUsedDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.DidUseBlock += callback;
    }

    public void OnDidPlaceBlock(BlockPlacedDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.DidPlaceBlock += callback;
    }

    public void OnEntityDeath(EntityDeathDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.OnEntityDeath += callback;
    }

    public long RegisterGameTickListener(Action<float> callback, int intervalMs)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        return _eventApi.RegisterGameTickListener(callback, intervalMs);
    }

    public long RegisterCallback(Action<float> callback, int intervalMs)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        return _eventApi.RegisterCallback(callback, intervalMs);
    }

    public void UnregisterCallback(long callbackId)
    {
        _eventApi.UnregisterCallback(callbackId);
    }

    public void UnsubscribeSaveGameLoaded(Action callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.SaveGameLoaded -= callback;
    }

    public void UnsubscribeGameWorldSave(Action callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.GameWorldSave -= callback;
    }

    public void UnsubscribePlayerJoin(PlayerDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerJoin -= callback;
    }

    public void UnsubscribePlayerDisconnect(PlayerDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDisconnect -= callback;
    }

    public void UnsubscribePlayerDeath(PlayerDeathDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDeath -= callback;
    }

    public void UnsubscribeBreakBlock(BlockBreakDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.BreakBlock -= callback;
    }

    public void UnsubscribeDidPlaceBlock(BlockPlacedDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.DidPlaceBlock -= callback;
    }

    public void UnsubscribeEntityDeath(EntityDeathDelegate callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.OnEntityDeath -= callback;
    }
}