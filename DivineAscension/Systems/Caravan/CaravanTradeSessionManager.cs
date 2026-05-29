using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Network.Caravan;
using DivineAscension.Services;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Caravan;

/// <summary>
///     Server-authoritative state machine for shrine-hosted player-to-player trade tables
///     (#433). Owns one <see cref="CaravanTradeSession" /> per shrine position, validates the
///     client request packets, and broadcasts <see cref="TradeStateSyncPacket" /> to both
///     participants on every change.
///
///     Scope (Phase 4, slice 7): seats, offers, ready flags, anti-switcheroo, and
///     disconnect cleanup. No item movement — escrow / atomic swap and the continuous
///     distance leash land in #434. A light proximity check runs at open time only.
/// </summary>
public class CaravanTradeSessionManager : IDisposable
{
    /// <summary>Maximum distance allowed between player and shrine when sitting down.</summary>
    private const double MaxOpenInteractDistance = 6.0;

    private readonly ILoggerWrapper _logger;
    private readonly INetworkService _networkService;
    private readonly IWorldService _worldService;
    private readonly IEventService _eventService;
    private readonly IPlayerMessengerService _messenger;

    // Keyed by shrine position string ("x,y,z").
    private readonly Dictionary<string, CaravanTradeSession> _sessions = new();

    // Reverse index: player UID -> shrine key of the session they are seated at.
    private readonly Dictionary<string, string> _playerSession = new();

    public CaravanTradeSessionManager(
        ILoggerWrapper logger,
        INetworkService networkService,
        IWorldService worldService,
        IEventService eventService,
        IPlayerMessengerService messenger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
    }

    public void Initialize()
    {
        _networkService.RegisterMessageHandler<OpenTradeRequestPacket>(OnOpenTrade);
        _networkService.RegisterMessageHandler<JoinTradeRequestPacket>(OnJoinTrade);
        _networkService.RegisterMessageHandler<OfferUpdatePacket>(OnOfferUpdate);
        _networkService.RegisterMessageHandler<SetReadyPacket>(OnSetReady);
        _networkService.RegisterMessageHandler<CancelTradePacket>(OnCancelTrade);
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);
        _logger.Notification($"{SystemConstants.LogPrefix} CaravanTradeSessionManager initialized");
    }

    public void Dispose()
    {
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);
        _sessions.Clear();
        _playerSession.Clear();
    }

    private static string Key(int x, int y, int z) => $"{x},{y},{z}";

    // ----- Request handlers -----

    private void OnOpenTrade(IServerPlayer player, OpenTradeRequestPacket packet)
        => SeatPlayer(player, new BlockPos(packet.ShrineX, packet.ShrineY, packet.ShrineZ));

    private void OnJoinTrade(IServerPlayer player, JoinTradeRequestPacket packet)
        => SeatPlayer(player, new BlockPos(packet.ShrineX, packet.ShrineY, packet.ShrineZ));

    private void OnOfferUpdate(IServerPlayer player, OfferUpdatePacket packet)
    {
        var session = GetSessionFor(player, packet.ShrineX, packet.ShrineY, packet.ShrineZ);
        if (session == null) return;

        var side = session.GetSide(player.PlayerUID);
        if (side == TradeSide.None) return;

        // Normalize: clamp to capacity and re-number slot indices so the ledger stays tidy.
        var offer = (packet.Offer ?? new List<TradeOfferSlot>())
            .Take(CaravanTradeSession.MaxOfferSlots)
            .ToList();
        for (var i = 0; i < offer.Count; i++)
            offer[i].SlotIndex = i;

        session.SetOffer(side, offer);
        session.ClearReadyFlags(); // anti-switcheroo: any change voids both seals
        Broadcast(session);
    }

    private void OnSetReady(IServerPlayer player, SetReadyPacket packet)
    {
        var session = GetSessionFor(player, packet.ShrineX, packet.ShrineY, packet.ShrineZ);
        if (session == null) return;

        var side = session.GetSide(player.PlayerUID);
        if (side == TradeSide.None) return;

        // A seal only makes sense once both parties are present.
        if (packet.Ready && !session.HasBothSeats) return;

        session.SetReady(side, packet.Ready);
        Broadcast(session);

        if (session.BothSealed)
        {
            // Terminal state for #433: both offers sealed. The atomic swap + escrow that
            // consumes this state is #434 — no item movement happens here.
            _logger.Notification(
                $"{SystemConstants.LogPrefix} Trade at {session.ShrinePos} sealed by both parties " +
                $"({session.SideAName} / {session.SideBName}). Awaiting swap (#434).");
        }
    }

    private void OnCancelTrade(IServerPlayer player, CancelTradePacket packet)
    {
        var session = GetSessionFor(player, packet.ShrineX, packet.ShrineY, packet.ShrineZ);
        if (session == null) return;
        CloseSession(session, player.PlayerUID, "caravantrade.cancelled");
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        if (!_playerSession.TryGetValue(player.PlayerUID, out var key)) return;
        if (_sessions.TryGetValue(key, out var session))
            CloseSession(session, player.PlayerUID, "caravantrade.partner_left");
    }

    // ----- Core logic -----

    private void SeatPlayer(IServerPlayer player, BlockPos shrinePos)
    {
        var key = Key(shrinePos.X, shrinePos.Y, shrinePos.Z);

        // Already seated somewhere?
        if (_playerSession.TryGetValue(player.PlayerUID, out var existingKey))
        {
            if (existingKey == key && _sessions.TryGetValue(key, out var existing))
            {
                // Re-opening their own table — resend the snapshot so the dialog reopens.
                SendSync(player, existing);
                return;
            }

            _messenger.SendMessage(player,
                LocalizationService.Instance.Get("caravantrade.error.busy"),
                EnumChatType.CommandError);
            return;
        }

        if (!IsWithinReach(player, shrinePos))
        {
            _messenger.SendMessage(player,
                LocalizationService.Instance.Get("caravantrade.error.too_far"),
                EnumChatType.CommandError);
            return;
        }

        if (!_sessions.TryGetValue(key, out var session) || session.Phase == TradePhase.Closed)
        {
            session = new CaravanTradeSession(shrinePos);
            _sessions[key] = session;
        }

        var seat = session.Seat(player.PlayerUID, player.PlayerName);
        if (seat == TradeSide.None)
        {
            _messenger.SendMessage(player,
                LocalizationService.Instance.Get("caravantrade.error.full"),
                EnumChatType.CommandError);

            // Drop an empty session we just speculatively created with no seats taken.
            if (string.IsNullOrEmpty(session.SideAUid) && string.IsNullOrEmpty(session.SideBUid))
                _sessions.Remove(key);
            return;
        }

        _playerSession[player.PlayerUID] = key;
        Broadcast(session);
    }

    private CaravanTradeSession? GetSessionFor(IServerPlayer player, int x, int y, int z)
    {
        if (!_playerSession.TryGetValue(player.PlayerUID, out var key)) return null;
        if (key != Key(x, y, z)) return null; // packet's shrine must match the seated session
        return _sessions.TryGetValue(key, out var session) ? session : null;
    }

    private void CloseSession(CaravanTradeSession session, string? initiatorUid, string partnerMessageKey)
    {
        session.Phase = TradePhase.Closed;
        var key = Key(session.ShrinePos.X, session.ShrinePos.Y, session.ShrinePos.Z);

        // Notify the other side, if there is one and it isn't the initiator.
        var partnerUid = session.SideAUid == initiatorUid ? session.SideBUid : session.SideAUid;
        if (!string.IsNullOrEmpty(partnerUid) && partnerUid != initiatorUid)
        {
            var partner = _worldService.GetPlayerByUID(partnerUid);
            if (partner != null)
                _messenger.SendMessage(partner,
                    LocalizationService.Instance.Get(partnerMessageKey),
                    EnumChatType.Notification);
        }

        // Push a Closed snapshot so both dialogs tear down, then clear server state.
        Broadcast(session);

        if (!string.IsNullOrEmpty(session.SideAUid)) _playerSession.Remove(session.SideAUid!);
        if (!string.IsNullOrEmpty(session.SideBUid)) _playerSession.Remove(session.SideBUid!);
        _sessions.Remove(key);
    }

    private void Broadcast(CaravanTradeSession session)
    {
        SendSyncToUid(session.SideAUid, session);
        SendSyncToUid(session.SideBUid, session);
    }

    private void SendSyncToUid(string? uid, CaravanTradeSession session)
    {
        if (string.IsNullOrEmpty(uid)) return;
        var player = _worldService.GetPlayerByUID(uid!);
        if (player != null) SendSync(player, session);
    }

    private void SendSync(IServerPlayer player, CaravanTradeSession session)
        => _networkService.SendToPlayer(player, session.ToSyncPacket());

    private static bool IsWithinReach(IServerPlayer player, BlockPos shrinePos)
    {
        // Entity is null only in unit tests; a connected player always has one. Allow when
        // absent so seating logic stays testable without mocking the entity graph.
        var pos = player.Entity?.Pos;
        if (pos == null) return true;

        var dx = pos.X - (shrinePos.X + 0.5);
        var dy = pos.Y - (shrinePos.Y + 0.5);
        var dz = pos.Z - (shrinePos.Z + 0.5);
        return dx * dx + dy * dy + dz * dz <= MaxOpenInteractDistance * MaxOpenInteractDistance;
    }
}
