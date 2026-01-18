using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of IEventService that wraps IServerEventAPI.
/// Provides a thin abstraction layer over Vintage Story's event system for improved testability.
/// </summary>
internal sealed class ServerEventService : IEventService
{
    private readonly IServerEventAPI _eventApi;

    public ServerEventService(IServerEventAPI eventApi)
    {
        _eventApi = eventApi ?? throw new ArgumentNullException(nameof(eventApi));
    }

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

    public void OnPlayerJoin(Action<IServerPlayer> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerJoin += callback;
    }

    public void OnPlayerDisconnect(Action<IServerPlayer> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDisconnect += callback;
    }

    public void OnPlayerDeath(Action<IServerPlayer, DamageSource> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDeath += callback;
    }

    public void OnBreakBlock(Action<IServerPlayer, BlockSelection, ref float, ref EnumHandling> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.BreakBlock += callback;
    }

    public void OnDidUseBlock(Action<IServerPlayer, BlockSelection> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.DidUseBlock += callback;
    }

    public void OnDidPlaceBlock(Action<IServerPlayer, BlockSelection, ItemStack> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.DidPlaceBlock += callback;
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

    public void UnsubscribePlayerJoin(Action<IServerPlayer> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerJoin -= callback;
    }

    public void UnsubscribePlayerDisconnect(Action<IServerPlayer> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDisconnect -= callback;
    }

    public void UnsubscribePlayerDeath(Action<IServerPlayer, DamageSource> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.PlayerDeath -= callback;
    }

    public void UnsubscribeBreakBlock(Action<IServerPlayer, BlockSelection, ref float, ref EnumHandling> callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        _eventApi.BreakBlock -= callback;
    }
}
