using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IEventService for testing.
/// Stores callbacks internally and provides test helpers to trigger events manually.
/// </summary>
public sealed class FakeEventService : IEventService
{
    private readonly List<Action> _saveGameLoadedCallbacks = new();
    private readonly List<Action> _gameWorldSaveCallbacks = new();
    private readonly List<Action<IServerPlayer>> _playerJoinCallbacks = new();
    private readonly List<Action<IServerPlayer>> _playerDisconnectCallbacks = new();
    private readonly List<Action<IServerPlayer, DamageSource>> _playerDeathCallbacks = new();
    private readonly List<Action<IServerPlayer, BlockSelection, RefWrapper<float>, RefWrapper<EnumHandling>>> _breakBlockCallbacks = new();
    private readonly List<Action<IServerPlayer, BlockSelection>> _didUseBlockCallbacks = new();
    private readonly List<Action<IServerPlayer, BlockSelection, ItemStack>> _didPlaceBlockCallbacks = new();
    private readonly Dictionary<long, PeriodicCallback> _callbacks = new();
    private long _nextCallbackId = 1;

    // Subscription methods
    public void OnSaveGameLoaded(Action callback)
    {
        _saveGameLoadedCallbacks.Add(callback);
    }

    public void OnGameWorldSave(Action callback)
    {
        _gameWorldSaveCallbacks.Add(callback);
    }

    public void OnPlayerJoin(Action<IServerPlayer> callback)
    {
        _playerJoinCallbacks.Add(callback);
    }

    public void OnPlayerDisconnect(Action<IServerPlayer> callback)
    {
        _playerDisconnectCallbacks.Add(callback);
    }

    public void OnPlayerDeath(Action<IServerPlayer, DamageSource> callback)
    {
        _playerDeathCallbacks.Add(callback);
    }

    public void OnBreakBlock(Action<IServerPlayer, BlockSelection, ref float, ref EnumHandling> callback)
    {
        // Wrap the ref callback in a helper that works with RefWrapper
        _breakBlockCallbacks.Add((player, selection, dropChance, handling) => callback(player, selection, ref dropChance.Value, ref handling.Value));
    }

    public void OnDidUseBlock(Action<IServerPlayer, BlockSelection> callback)
    {
        _didUseBlockCallbacks.Add(callback);
    }

    public void OnDidPlaceBlock(Action<IServerPlayer, BlockSelection, ItemStack> callback)
    {
        _didPlaceBlockCallbacks.Add(callback);
    }

    public long RegisterGameTickListener(Action<float> callback, int intervalMs)
    {
        long id = _nextCallbackId++;
        _callbacks[id] = new PeriodicCallback(callback, intervalMs, isGameTick: true);
        return id;
    }

    public long RegisterCallback(Action<float> callback, int intervalMs)
    {
        long id = _nextCallbackId++;
        _callbacks[id] = new PeriodicCallback(callback, intervalMs, isGameTick: false);
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

    public void UnsubscribePlayerJoin(Action<IServerPlayer> callback)
    {
        _playerJoinCallbacks.Remove(callback);
    }

    public void UnsubscribePlayerDisconnect(Action<IServerPlayer> callback)
    {
        _playerDisconnectCallbacks.Remove(callback);
    }

    public void UnsubscribePlayerDeath(Action<IServerPlayer, DamageSource> callback)
    {
        _playerDeathCallbacks.Remove(callback);
    }

    public void UnsubscribeBreakBlock(Action<IServerPlayer, BlockSelection, ref float, ref EnumHandling> callback)
    {
        // Note: This is a limitation of the fake - we can't easily remove the specific wrapped callback
        // In practice, unsubscribe is rarely used in tests
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

    public void TriggerBreakBlock(IServerPlayer player, BlockSelection selection, float dropChance = 1f, EnumHandling handling = EnumHandling.PassThrough)
    {
        var dropChanceWrapper = new RefWrapper<float>(dropChance);
        var handlingWrapper = new RefWrapper<EnumHandling>(handling);

        foreach (var callback in _breakBlockCallbacks)
        {
            callback(player, selection, dropChanceWrapper, handlingWrapper);
        }
    }

    public void TriggerDidUseBlock(IServerPlayer player, BlockSelection selection)
    {
        foreach (var callback in _didUseBlockCallbacks)
        {
            callback(player, selection);
        }
    }

    public void TriggerDidPlaceBlock(IServerPlayer player, BlockSelection selection, ItemStack itemStack)
    {
        foreach (var callback in _didPlaceBlockCallbacks)
        {
            callback(player, selection, itemStack);
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

    // Helper class to wrap ref parameters
    public sealed class RefWrapper<T>
    {
        public T Value { get; set; }

        public RefWrapper(T value)
        {
            Value = value;
        }
    }

    private sealed record PeriodicCallback(Action<float> Callback, int IntervalMs, bool IsGameTick);
}
