using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Network;
using DivineAscension.Services;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Lectern;

/// <summary>
/// Handles lectern right-click events. Validates the interaction server-side
/// and sends an <see cref="OpenMenuPacket"/> to the originating client.
/// Tracks each player's last-used lectern and closes the menu when they
/// walk beyond <see cref="AutoCloseDistance"/> blocks.
/// </summary>
public class LecternInteractionHandler : IDisposable
{
    /// <summary>Squared distance in blocks at which the menu auto-closes.</summary>
    private const double AutoCloseDistanceSquared = 4.0 * 4.0;

    /// <summary>Maximum distance allowed between player and lectern at open time.</summary>
    private const double MaxOpenInteractDistance = 6.0;

    /// <summary>How often the proximity check runs.</summary>
    private const int ProximityCheckIntervalMs = 500;

    private readonly LecternEventEmitter _emitter;
    private readonly INetworkService _networkService;
    private readonly IEventService _eventService;
    private readonly IWorldService _worldService;
    private readonly ILoggerWrapper _logger;
    private readonly Dictionary<string, BlockPos> _activeSessions = new();
    private long _tickListenerId;

    public LecternInteractionHandler(
        LecternEventEmitter emitter,
        INetworkService networkService,
        IEventService eventService,
        IWorldService worldService,
        ILoggerWrapper logger)
    {
        _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize()
    {
        _emitter.OnLecternUsed += OnLecternUsed;
        _tickListenerId = _eventService.RegisterGameTickListener(OnProximityTick, ProximityCheckIntervalMs);
        _logger.Notification("[DivineAscension] Lectern Interaction Handler initialized");
    }

    public void Dispose()
    {
        _emitter.OnLecternUsed -= OnLecternUsed;
        if (_tickListenerId != 0)
        {
            _eventService.UnregisterCallback(_tickListenerId);
            _tickListenerId = 0;
        }
        _activeSessions.Clear();
    }

    private void OnLecternUsed(IServerPlayer player, BlockSelection blockSel)
    {
        var pos = blockSel.Position;
        if (pos == null)
            return;

        // Proximity check rejects rare cases where the client claimed a far-away block.
        var playerPos = player.Entity?.Pos;
        if (playerPos == null)
            return;

        var dx = playerPos.X - (pos.X + 0.5);
        var dy = playerPos.Y - (pos.Y + 0.5);
        var dz = playerPos.Z - (pos.Z + 0.5);
        if (dx * dx + dy * dy + dz * dz > MaxOpenInteractDistance * MaxOpenInteractDistance)
        {
            _logger.Debug($"[DivineAscension] Rejected lectern open from {player.PlayerName}: too far from lectern");
            return;
        }

        _activeSessions[player.PlayerUID] = pos.Copy();
        _networkService.SendToPlayer(player, new OpenMenuPacket
        {
            LecternX = pos.X,
            LecternY = pos.Y,
            LecternZ = pos.Z
        });
    }

    private void OnProximityTick(float dt)
    {
        if (_activeSessions.Count == 0)
            return;

        List<string>? toClose = null;
        foreach (var (uid, lecternPos) in _activeSessions)
        {
            var player = _worldService.GetPlayerByUID(uid);
            if (player?.Entity?.Pos == null)
            {
                (toClose ??= new List<string>()).Add(uid);
                continue;
            }

            var pos = player.Entity.Pos;
            var dx = pos.X - (lecternPos.X + 0.5);
            var dy = pos.Y - (lecternPos.Y + 0.5);
            var dz = pos.Z - (lecternPos.Z + 0.5);

            if (dx * dx + dy * dy + dz * dz > AutoCloseDistanceSquared)
            {
                _networkService.SendToPlayer(player, new CloseMenuPacket());
                (toClose ??= new List<string>()).Add(uid);
            }
        }

        if (toClose != null)
        {
            foreach (var uid in toClose)
                _activeSessions.Remove(uid);
        }
    }
}
