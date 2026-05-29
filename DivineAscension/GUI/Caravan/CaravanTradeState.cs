using System.Collections.Generic;
using DivineAscension.Network.Caravan;
using DivineAscension.Systems.Caravan;
using Vintagestory.API.MathTools;

namespace DivineAscension.GUI.Caravan;

/// <summary>
///     Client-side view state for the caravan trade dialog. Holds the latest
///     server-authoritative <see cref="TradeStateSyncPacket" /> snapshot and resolves
///     which side is "self" so the local trader always renders on the left.
/// </summary>
public class CaravanTradeState
{
    public bool IsOpen { get; set; }

    public BlockPos? ShrinePos { get; private set; }
    public TradePhase Phase { get; private set; } = TradePhase.AwaitingPartner;

    public string SideAUid { get; private set; } = string.Empty;
    public string SideAName { get; private set; } = string.Empty;
    public bool SideAReady { get; private set; }
    public List<TradeOfferSlot> SideAOffer { get; private set; } = new();

    public string SideBUid { get; private set; } = string.Empty;
    public string SideBName { get; private set; } = string.Empty;
    public bool SideBReady { get; private set; }
    public List<TradeOfferSlot> SideBOffer { get; private set; } = new();

    public void Apply(TradeStateSyncPacket packet)
    {
        ShrinePos = new BlockPos(packet.ShrineX, packet.ShrineY, packet.ShrineZ);
        Phase = packet.Phase;
        SideAUid = packet.SideAPlayerUid;
        SideAName = packet.SideAPlayerName;
        SideAReady = packet.SideAReady;
        SideAOffer = packet.SideAOffer ?? new List<TradeOfferSlot>();
        SideBUid = packet.SideBPlayerUid;
        SideBName = packet.SideBPlayerName;
        SideBReady = packet.SideBReady;
        SideBOffer = packet.SideBOffer ?? new List<TradeOfferSlot>();
    }

    public TradeSide GetMySide(string myUid)
    {
        if (SideAUid == myUid) return TradeSide.A;
        if (SideBUid == myUid) return TradeSide.B;
        return TradeSide.None;
    }

    public List<TradeOfferSlot> MyOffer(string myUid)
        => GetMySide(myUid) == TradeSide.B ? SideBOffer : SideAOffer;

    public List<TradeOfferSlot> TheirOffer(string myUid)
        => GetMySide(myUid) == TradeSide.B ? SideAOffer : SideBOffer;

    public bool MyReady(string myUid) => GetMySide(myUid) == TradeSide.B ? SideBReady : SideAReady;

    public bool TheirReady(string myUid) => GetMySide(myUid) == TradeSide.B ? SideAReady : SideBReady;

    public string MyName(string myUid) => GetMySide(myUid) == TradeSide.B ? SideBName : SideAName;

    /// <summary>Partner display name, or empty while still awaiting a second trader.</summary>
    public string TheirName(string myUid) => GetMySide(myUid) == TradeSide.B ? SideAName : SideBName;

    public bool HasPartner(string myUid)
    {
        var mine = GetMySide(myUid);
        if (mine == TradeSide.B) return !string.IsNullOrEmpty(SideAUid);
        return !string.IsNullOrEmpty(SideBUid);
    }
}
