using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Network.Caravan;
using DivineAscension.Services;
using DivineAscension.Systems.Altar;
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

    /// <summary>How often the distance leash re-checks that both traders are still in reach.</summary>
    private const int LeashIntervalMs = 1000;

    private readonly ILoggerWrapper _logger;
    private readonly INetworkService _networkService;
    private readonly IWorldService _worldService;
    private readonly IEventService _eventService;
    private readonly IPlayerMessengerService _messenger;
    private readonly ICaravanTradeInventory _inventory;
    private readonly AltarEventEmitter _altarEventEmitter;

    // Keyed by shrine position string ("x,y,z").
    private readonly Dictionary<string, CaravanTradeSession> _sessions = new();

    // Reverse index: player UID -> shrine key of the session they are seated at.
    private readonly Dictionary<string, string> _playerSession = new();

    private long _leashCallbackId;

    public CaravanTradeSessionManager(
        ILoggerWrapper logger,
        INetworkService networkService,
        IWorldService worldService,
        IEventService eventService,
        IPlayerMessengerService messenger,
        ICaravanTradeInventory inventory,
        AltarEventEmitter altarEventEmitter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _altarEventEmitter = altarEventEmitter ?? throw new ArgumentNullException(nameof(altarEventEmitter));
    }

    public void Initialize()
    {
        _networkService.RegisterMessageHandler<OpenTradeRequestPacket>(OnOpenTrade);
        _networkService.RegisterMessageHandler<JoinTradeRequestPacket>(OnJoinTrade);
        _networkService.RegisterMessageHandler<OfferUpdatePacket>(OnOfferUpdate);
        _networkService.RegisterMessageHandler<SetReadyPacket>(OnSetReady);
        _networkService.RegisterMessageHandler<CancelTradePacket>(OnCancelTrade);
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);
        // A broken shrine tears down its trade: no escrow exists, so both traders simply keep
        // the items still sitting in their own inventories.
        _altarEventEmitter.OnAltarBroken += OnShrineBroken;
        // Continuous distance leash: walking out of reach cancels the trade (#434).
        _leashCallbackId = _eventService.RegisterGameTickListener(OnLeashTick, LeashIntervalMs);
        _logger.Notification($"{SystemConstants.LogPrefix} CaravanTradeSessionManager initialized");
    }

    public void Dispose()
    {
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);
        _altarEventEmitter.OnAltarBroken -= OnShrineBroken;
        if (_leashCallbackId != 0)
            _eventService.UnregisterCallback(_leashCallbackId);
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
            TryCompleteTrade(session);
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

    private void OnShrineBroken(IServerPlayer player, BlockPos pos)
    {
        var key = Key(pos.X, pos.Y, pos.Z);
        if (_sessions.TryGetValue(key, out var session))
            CloseSession(session, player.PlayerUID, "caravantrade.shrine_broken");
    }

    /// <summary>
    ///     Re-checks every active table that both seated traders are still within reach of the
    ///     shrine; cancels the trade if either has wandered off. No item movement is involved, so
    ///     a cancelled trade just closes both dialogs.
    /// </summary>
    private void OnLeashTick(float _)
    {
        if (_sessions.Count == 0) return;

        // Snapshot: CloseSession mutates _sessions.
        foreach (var session in _sessions.Values.ToList())
        {
            if (session.Phase == TradePhase.Closed) continue;

            if (IsSeatedPlayerOutOfReach(session.SideAUid, session.ShrinePos) ||
                IsSeatedPlayerOutOfReach(session.SideBUid, session.ShrinePos))
                CloseSession(session, initiatorUid: null, "caravantrade.too_far_leashed");
        }
    }

    private bool IsSeatedPlayerOutOfReach(string? uid, BlockPos shrinePos)
    {
        if (string.IsNullOrEmpty(uid)) return false; // empty seat never leashes the table
        var player = _worldService.GetPlayerByUID(uid!);
        if (player == null) return false; // disconnect is handled by OnPlayerDisconnect
        return !IsWithinReach(player, shrinePos);
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

    /// <summary>
    ///     Both parties sealed identical, current offers. Re-validate that each still holds the
    ///     goods, then swap atomically. Any failure aborts with both items untouched — the offers
    ///     are un-sealed so the traders can fix them, and the table stays open.
    /// </summary>
    private void TryCompleteTrade(CaravanTradeSession session)
    {
        var sideA = string.IsNullOrEmpty(session.SideAUid) ? null : _worldService.GetPlayerByUID(session.SideAUid!);
        var sideB = string.IsNullOrEmpty(session.SideBUid) ? null : _worldService.GetPlayerByUID(session.SideBUid!);

        if (sideA == null || sideB == null)
        {
            // A party vanished between sealing and the swap — disconnect handling will close the
            // table. Un-seal so a stale seal can't auto-fire if they return.
            AbortSwap(session, "caravantrade.swap_failed");
            return;
        }

        // Re-validate against live inventories: a player may have dropped or used an offered item
        // after sealing. Both checks pass before anything moves, so the swap below cannot lose.
        if (!_inventory.CanProvideOffer(sideA, session.SideAOffer) ||
            !_inventory.CanProvideOffer(sideB, session.SideBOffer))
        {
            AbortSwap(session, "caravantrade.swap_missing_items");
            return;
        }

        _inventory.SwapOffers(sideA, session.SideAOffer, sideB, session.SideBOffer, session.ShrinePos);

        _logger.Notification(
            $"{SystemConstants.LogPrefix} Caravan trade at {session.ShrinePos} completed " +
            $"({session.SideAName} <-> {session.SideBName}).");

        // Favor grants for a completed barter are #435 — intentionally not fired here.

        session.Phase = TradePhase.Closed;
        Broadcast(session);
        _messenger.SendMessage(sideA, LocalizationService.Instance.Get("caravantrade.completed"),
            EnumChatType.CommandSuccess);
        _messenger.SendMessage(sideB, LocalizationService.Instance.Get("caravantrade.completed"),
            EnumChatType.CommandSuccess);
        TeardownSession(session);
    }

    /// <summary>
    ///     A both-sealed trade could not commit. Nothing moved; un-seal both offers, tell both
    ///     traders, and leave the table open so they can adjust.
    /// </summary>
    private void AbortSwap(CaravanTradeSession session, string messageKey)
    {
        session.ClearReadyFlags();
        Broadcast(session);
        NotifySeated(session.SideAUid, messageKey);
        NotifySeated(session.SideBUid, messageKey);
    }

    private void NotifySeated(string? uid, string messageKey)
    {
        if (string.IsNullOrEmpty(uid)) return;
        var player = _worldService.GetPlayerByUID(uid!);
        if (player != null)
            _messenger.SendMessage(player, LocalizationService.Instance.Get(messageKey),
                EnumChatType.Notification);
    }

    private void CloseSession(CaravanTradeSession session, string? initiatorUid, string partnerMessageKey)
    {
        session.Phase = TradePhase.Closed;

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
        TeardownSession(session);
    }

    /// <summary>Clears server-side state for a session whose phase is already terminal.</summary>
    private void TeardownSession(CaravanTradeSession session)
    {
        var key = Key(session.ShrinePos.X, session.ShrinePos.Y, session.ShrinePos.Z);
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
