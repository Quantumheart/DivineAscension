using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.BlessingEffects;

/// <summary>
///     Registry for managing special effect handlers
///     Maps effect IDs to their handler implementations and manages their lifecycle
/// </summary>
public class SpecialEffectRegistry
{
    private readonly Dictionary<string, ISpecialEffectHandler> _handlers = new();
    private readonly Dictionary<string, HashSet<string>> _playerActiveEffects = new();
    private readonly ICoreServerAPI _sapi;
    private long? _tickListenerId;

    public SpecialEffectRegistry(ICoreServerAPI sapi)
    {
        _sapi = sapi;
    }

    /// <summary>
    ///     Initializes the registry and all registered handlers
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification($"{SystemConstants.LogPrefix} Initializing Special Effect Registry...");

        // Initialize all registered handlers
        foreach (var handler in _handlers.Values) handler.Initialize(_sapi);

        // Register game tick for time-based effects
        _tickListenerId = _sapi.Event.RegisterGameTickListener(OnGameTick, 5000); // Every 5 seconds

        _sapi.Logger.Notification(
            $"{SystemConstants.LogPrefix} Special Effect Registry initialized with {_handlers.Count} handlers");
    }

    /// <summary>
    ///     Registers a special effect handler
    /// </summary>
    public void RegisterHandler(ISpecialEffectHandler handler)
    {
        if (_handlers.ContainsKey(handler.EffectId))
        {
            _sapi.Logger.Warning(
                $"{SystemConstants.LogPrefix} Handler for effect '{handler.EffectId}' already registered, skipping");
            return;
        }

        _handlers[handler.EffectId] = handler;
        _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Registered special effect handler: {handler.EffectId}");
    }

    /// <summary>
    ///     Activates a special effect for a player
    /// </summary>
    public void ActivateEffect(string effectId, IServerPlayer player)
    {
        if (!_handlers.TryGetValue(effectId, out var handler))
        {
            _sapi.Logger.Warning(
                $"{SystemConstants.LogPrefix} No handler found for effect '{effectId}', skipping activation");
            return;
        }

        // Track active effect for this player
        if (!_playerActiveEffects.ContainsKey(player.PlayerUID))
            _playerActiveEffects[player.PlayerUID] = new HashSet<string>();

        if (_playerActiveEffects[player.PlayerUID].Add(effectId))
        {
            handler.ActivateForPlayer(player);
            _sapi.Logger.Debug(
                $"{SystemConstants.LogPrefix} Activated effect '{effectId}' for player {player.PlayerName}");
        }
    }

    /// <summary>
    ///     Deactivates a special effect for a player
    /// </summary>
    public void DeactivateEffect(string effectId, IServerPlayer player)
    {
        if (!_handlers.TryGetValue(effectId, out var handler))
        {
            _sapi.Logger.Warning(
                $"{SystemConstants.LogPrefix} No handler found for effect '{effectId}', skipping deactivation");
            return;
        }

        // Remove from active effects tracking
        if (_playerActiveEffects.TryGetValue(player.PlayerUID, out var effects) && effects.Remove(effectId))
        {
            handler.DeactivateForPlayer(player);
            _sapi.Logger.Debug(
                $"{SystemConstants.LogPrefix} Deactivated effect '{effectId}' for player {player.PlayerName}");
        }
    }

    /// <summary>
    ///     Deactivates all effects for a player (called when leaving religion or similar)
    /// </summary>
    public void DeactivateAllEffectsForPlayer(IServerPlayer player)
    {
        if (!_playerActiveEffects.TryGetValue(player.PlayerUID, out var effects)) return;

        foreach (var effectId in effects.ToList()) DeactivateEffect(effectId, player);

        _playerActiveEffects.Remove(player.PlayerUID);
    }

    /// <summary>
    ///     Refreshes effects for a player based on their unlocked blessings
    /// </summary>
    public void RefreshPlayerEffects(IServerPlayer player, List<string> activeEffectIds)
    {
        // Get currently active effects for this player
        var currentEffects = _playerActiveEffects.TryGetValue(player.PlayerUID, out var effects)
            ? new HashSet<string>(effects)
            : new HashSet<string>();

        var desiredEffects = new HashSet<string>(activeEffectIds);

        // Deactivate effects no longer present
        foreach (var effectId in currentEffects.Where(e => !desiredEffects.Contains(e)))
            DeactivateEffect(effectId, player);

        // Activate new effects
        foreach (var effectId in desiredEffects.Where(e => !currentEffects.Contains(e)))
            ActivateEffect(effectId, player);
    }

    /// <summary>
    ///     Gets all active effects for a player
    /// </summary>
    public HashSet<string> GetPlayerActiveEffects(string playerUID)
    {
        return _playerActiveEffects.TryGetValue(playerUID, out var effects)
            ? new HashSet<string>(effects)
            : new HashSet<string>();
    }

    /// <summary>
    ///     Called periodically to update time-based effects
    /// </summary>
    private void OnGameTick(float deltaTime)
    {
        foreach (var handler in _handlers.Values)
            try
            {
                handler.OnTick(deltaTime);
            }
            catch (Exception ex)
            {
                _sapi.Logger.Error(
                    $"{SystemConstants.LogPrefix} Error in special effect handler '{handler.EffectId}' OnTick: {ex}");
            }
    }

    /// <summary>
    ///     Cleanup method for disposal
    /// </summary>
    public void Dispose()
    {
        if (_tickListenerId.HasValue) _sapi.Event.UnregisterGameTickListener(_tickListenerId.Value);

        _handlers.Clear();
        _playerActiveEffects.Clear();
    }
}