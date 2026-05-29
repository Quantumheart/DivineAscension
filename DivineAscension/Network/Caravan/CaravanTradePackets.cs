using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.Caravan;

/// <summary>
///     Lifecycle phase of a shrine-hosted trade session, mirrored to clients via
///     <see cref="TradeStateSyncPacket" />.
/// </summary>
public enum TradePhase
{
    /// <summary>One trader is seated, waiting for a second to join.</summary>
    AwaitingPartner = 0,

    /// <summary>Both seats filled; offers and ready flags are live.</summary>
    Active = 1,

    /// <summary>Session has ended (cancelled, disconnect, or completed). Clients close their dialog.</summary>
    Closed = 2
}

/// <summary>
///     A single offered item line. Phase 4 (#433) is UI + state sync only — no item
///     movement — so this carries just enough to render the ledger entry. The full
///     <see cref="ItemCode" /> is retained for the escrow/atomic-swap work in #434.
/// </summary>
[ProtoContract]
public class TradeOfferSlot
{
    public TradeOfferSlot()
    {
    }

    /// <summary>Full collectible asset code (e.g. <c>game:axe-copper</c>). Drives #434's escrow.</summary>
    [ProtoMember(1)]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>Localized display name resolved on the offering client, shown to both parties.</summary>
    [ProtoMember(2)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Stack size offered.</summary>
    [ProtoMember(3)]
    public int Quantity { get; set; }

    /// <summary>Position within the offering side's grid (0-8).</summary>
    [ProtoMember(4)]
    public int SlotIndex { get; set; }
}

/// <summary>
///     Client → server. The recipient right-clicked a caravan shrine and wants to sit at
///     its trade table. Server auto-assigns the seat (side A then side B).
/// </summary>
[ProtoContract]
public class OpenTradeRequestPacket
{
    public OpenTradeRequestPacket()
    {
    }

    [ProtoMember(1)] public int ShrineX { get; set; }
    [ProtoMember(2)] public int ShrineY { get; set; }
    [ProtoMember(3)] public int ShrineZ { get; set; }
}

/// <summary>
///     Client → server. Explicit request to take the open seat at a shrine's trade table.
///     With server-assigned seating the common path is <see cref="OpenTradeRequestPacket" />;
///     this is defined/handled (routed through the same seating logic) and reserved for a
///     future invite-driven join.
/// </summary>
[ProtoContract]
public class JoinTradeRequestPacket
{
    public JoinTradeRequestPacket()
    {
    }

    [ProtoMember(1)] public int ShrineX { get; set; }
    [ProtoMember(2)] public int ShrineY { get; set; }
    [ProtoMember(3)] public int ShrineZ { get; set; }
}

/// <summary>
///     Client → server. Replaces the sender's entire offer for the session at the shrine.
///     Any offer change clears both sides' ready flags (anti-switcheroo).
/// </summary>
[ProtoContract]
public class OfferUpdatePacket
{
    public OfferUpdatePacket()
    {
    }

    [ProtoMember(1)] public int ShrineX { get; set; }
    [ProtoMember(2)] public int ShrineY { get; set; }
    [ProtoMember(3)] public int ShrineZ { get; set; }

    [ProtoMember(4)] public List<TradeOfferSlot> Offer { get; set; } = new();
}

/// <summary>
///     Client → server. Seal ("ready") or un-seal the sender's offer.
/// </summary>
[ProtoContract]
public class SetReadyPacket
{
    public SetReadyPacket()
    {
    }

    [ProtoMember(1)] public int ShrineX { get; set; }
    [ProtoMember(2)] public int ShrineY { get; set; }
    [ProtoMember(3)] public int ShrineZ { get; set; }

    [ProtoMember(4)] public bool Ready { get; set; }
}

/// <summary>
///     Client → server. Leave the table; tears the session down for both parties.
/// </summary>
[ProtoContract]
public class CancelTradePacket
{
    public CancelTradePacket()
    {
    }

    [ProtoMember(1)] public int ShrineX { get; set; }
    [ProtoMember(2)] public int ShrineY { get; set; }
    [ProtoMember(3)] public int ShrineZ { get; set; }
}

/// <summary>
///     Server → client. Authoritative snapshot of a trade session, broadcast to both
///     participants on every change. Each client decides which side is "self" by
///     comparing its own player UID to <see cref="SideAPlayerUid" /> / <see cref="SideBPlayerUid" />.
/// </summary>
[ProtoContract]
public class TradeStateSyncPacket
{
    public TradeStateSyncPacket()
    {
    }

    [ProtoMember(1)] public int ShrineX { get; set; }
    [ProtoMember(2)] public int ShrineY { get; set; }
    [ProtoMember(3)] public int ShrineZ { get; set; }

    [ProtoMember(4)] public TradePhase Phase { get; set; } = TradePhase.AwaitingPartner;

    [ProtoMember(5)] public string SideAPlayerUid { get; set; } = string.Empty;
    [ProtoMember(6)] public string SideAPlayerName { get; set; } = string.Empty;
    [ProtoMember(7)] public bool SideAReady { get; set; }
    [ProtoMember(8)] public List<TradeOfferSlot> SideAOffer { get; set; } = new();

    [ProtoMember(9)] public string SideBPlayerUid { get; set; } = string.Empty;
    [ProtoMember(10)] public string SideBPlayerName { get; set; } = string.Empty;
    [ProtoMember(11)] public bool SideBReady { get; set; }
    [ProtoMember(12)] public List<TradeOfferSlot> SideBOffer { get; set; } = new();
}
