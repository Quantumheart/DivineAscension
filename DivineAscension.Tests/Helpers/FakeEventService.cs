using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IEventService for testing.
/// Stores callbacks internally and provides test helpers to trigger events manually.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FakeEventService : IEventService
{
    private readonly List<BlockBreakDelegate> _breakBlockCallbacks = new();
    private readonly Dictionary<long, PeriodicCallback> _callbacks = new();
    private readonly List<BlockPlacedDelegate> _didPlaceBlockCallbacks = new();
    private readonly List<BlockUsedDelegate> _didUseBlockCallbacks = new();
    private readonly List<EntityDeathDelegate> _entityDeathCallbacks = new();
    private readonly List<Action> _gameWorldSaveCallbacks = new();
    private readonly List<PlayerDeathDelegate> _playerDeathCallbacks = new();
    private readonly List<PlayerDelegate> _playerDisconnectCallbacks = new();
    private readonly List<PlayerDelegate> _playerJoinCallbacks = new();
    private readonly List<Action> _saveGameLoadedCallbacks = new();
    private long _nextCallbackId = 1;

    // Test inspection properties
    public int SaveGameLoadedCallbackCount => _saveGameLoadedCallbacks.Count;
    public int GameWorldSaveCallbackCount => _gameWorldSaveCallbacks.Count;
    public int PlayerJoinCallbackCount => _playerJoinCallbacks.Count;
    public int PlayerDisconnectCallbackCount => _playerDisconnectCallbacks.Count;
    public int PlayerDeathCallbackCount => _playerDeathCallbacks.Count;
    public int BreakBlockCallbackCount => _breakBlockCallbacks.Count;
    public int DidUseBlockCallbackCount => _didUseBlockCallbacks.Count;
    public int DidPlaceBlockCallbackCount => _didPlaceBlockCallbacks.Count;
    public int EntityDeathCallbackCount => _entityDeathCallbacks.Count;

    // Subscription methods
    public void OnSaveGameLoaded(Action callback)
    {
        _saveGameLoadedCallbacks.Add(callback);
    }

    public void OnGameWorldSave(Action callback)
    {
        _gameWorldSaveCallbacks.Add(callback);
    }

    public void OnPlayerJoin(PlayerDelegate callback)
    {
        _playerJoinCallbacks.Add(callback);
    }

    public void OnPlayerDisconnect(PlayerDelegate callback)
    {
        _playerDisconnectCallbacks.Add(callback);
    }

    public void OnPlayerDeath(PlayerDeathDelegate callback)
    {
        _playerDeathCallbacks.Add(callback);
    }

    public void OnBreakBlock(BlockBreakDelegate callback)
    {
        _breakBlockCallbacks.Add(callback);
    }

    public void OnDidUseBlock(BlockUsedDelegate callback)
    {
        _didUseBlockCallbacks.Add(callback);
    }

    public void OnDidPlaceBlock(BlockPlacedDelegate callback)
    {
        _didPlaceBlockCallbacks.Add(callback);
    }

    public void OnEntityDeath(EntityDeathDelegate callback)
    {
        _entityDeathCallbacks.Add(callback);
    }

    public long RegisterGameTickListener(Action<float> callback, int intervalMs)
    {
        long id = _nextCallbackId++;
        _callbacks[id] = new PeriodicCallback(callback, intervalMs, IsGameTick: true);
        return id;
    }

    public long RegisterCallback(Action<float> callback, int intervalMs)
    {
        long id = _nextCallbackId++;
        _callbacks[id] = new PeriodicCallback(callback, intervalMs, IsGameTick: false);
        return id;
    }

    public void UnregisterCallback(long callbackId)
    {
        _callbacks.Remove(callbackId);
    }

    // Unsubscribe methods
    public void UnsubscribeSaveGameLoaded(Action callback)
    {
        _saveGameLoadedCallbacks.Remove(callback);
    }

    public void UnsubscribeGameWorldSave(Action callback)
    {
        _gameWorldSaveCallbacks.Remove(callback);
    }

    public void UnsubscribePlayerJoin(PlayerDelegate callback)
    {
        _playerJoinCallbacks.Remove(callback);
    }

    public void UnsubscribePlayerDisconnect(PlayerDelegate callback)
    {
        _playerDisconnectCallbacks.Remove(callback);
    }

    public void UnsubscribePlayerDeath(PlayerDeathDelegate callback)
    {
        _playerDeathCallbacks.Remove(callback);
    }

    public void UnsubscribeBreakBlock(BlockBreakDelegate callback)
    {
        _breakBlockCallbacks.Remove(callback);
    }

    public void UnsubscribeDidUseBlock(BlockUsedDelegate callback)
    {
        _didUseBlockCallbacks.Remove(callback);
    }

    public void UnsubscribeDidPlaceBlock(BlockPlacedDelegate callback)
    {
        _didPlaceBlockCallbacks.Remove(callback);
    }

    public void UnsubscribeEntityDeath(EntityDeathDelegate callback)
    {
        _entityDeathCallbacks.Remove(callback);
    }

    // Test helper methods to trigger events
    public void TriggerSaveGameLoaded()
    {
        foreach (var callback in _saveGameLoadedCallbacks)
        {
            callback();
        }
    }

    public void TriggerGameWorldSave()
    {
        foreach (var callback in _gameWorldSaveCallbacks)
        {
            callback();
        }
    }

    public void TriggerPlayerJoin(IServerPlayer player)
    {
        foreach (var callback in _playerJoinCallbacks)
        {
            callback(player);
        }
    }

    public void TriggerPlayerDisconnect(IServerPlayer player)
    {
        foreach (var callback in _playerDisconnectCallbacks)
        {
            callback(player);
        }
    }

    public void TriggerPlayerDeath(IServerPlayer player, DamageSource damageSource)
    {
        foreach (var callback in _playerDeathCallbacks)
        {
            callback(player, damageSource);
        }
    }

    public void TriggerBreakBlock(IServerPlayer player, BlockSelection selection, float dropChance = 1f,
        EnumHandling handling = EnumHandling.PassThrough)
    {
        foreach (var callback in _breakBlockCallbacks)
        {
            callback(player, selection, ref dropChance, ref handling);
        }
    }

    public void TriggerDidUseBlock(IServerPlayer player, BlockSelection selection)
    {
        foreach (var callback in _didUseBlockCallbacks)
        {
            callback(player, selection);
        }
    }

    public void TriggerDidPlaceBlock(IServerPlayer player, BlockSelection selection, ItemStack itemStack,
        int oldBlockId = 0)
    {
        foreach (var callback in _didPlaceBlockCallbacks)
        {
            callback(player, oldBlockId, selection, itemStack);
        }
    }

    public void TriggerEntityDeath(Entity entity, DamageSource damageSource)
    {
        foreach (var callback in _entityDeathCallbacks)
        {
            callback(entity, damageSource);
        }
    }

    public void TriggerPeriodicCallbacks(float deltaTime)
    {
        foreach (var callback in _callbacks.Values)
        {
            callback.Callback(deltaTime);
        }
    }

    public void TriggerCallback(long callbackId, float deltaTime)
    {
        if (_callbacks.TryGetValue(callbackId, out var callback))
        {
            callback.Callback(deltaTime);
        }
    }

    public bool HasBreakBlockSubscribers() => _breakBlockCallbacks.Count > 0;
    public bool HasDidPlaceBlockSubscribers() => _didPlaceBlockCallbacks.Count > 0;
    public bool HasDidUseBlockSubscribers() => _didUseBlockCallbacks.Count > 0;

    /// <summary>
    /// Gets the number of registered periodic callbacks.
    /// </summary>
    public int RegisteredCallbackCount => _callbacks.Count;

    /// <summary>
    /// Triggers all registered periodic callbacks with the given delta time.
    /// Alias for TriggerPeriodicCallbacks for consistency with test naming.
    /// </summary>
    public void TriggerPeriodicCallback(float deltaTime)
    {
        TriggerPeriodicCallbacks(deltaTime);
    }

    private sealed record PeriodicCallback(Action<float> Callback, int IntervalMs, bool IsGameTick);
}