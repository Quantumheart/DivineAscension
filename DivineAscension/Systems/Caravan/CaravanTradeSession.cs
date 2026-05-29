using System.Collections.Generic;
using DivineAscension.Network.Caravan;
using Vintagestory.API.MathTools;

namespace DivineAscension.Systems.Caravan;

/// <summary>
///     Which seat at a trade table a player occupies.
/// </summary>
public enum TradeSide
{
    None = 0,
    A = 1,
    B = 2
}

/// <summary>
///     Server-authoritative state for a single shrine-hosted trade table. One session
///     exists per shrine position while a trade is in progress. State is mirrored to
///     clients via <see cref="TradeStateSyncPacket" />.
///
///     Phase 4 (#433) tracks seats, offers, and ready flags only — no item movement.
///     Escrow / atomic swap plug into this same session in #434.
/// </summary>
public class CaravanTradeSession
{
    /// <summary>Maximum offered stacks per side (the former 3×3 grid = 9).</summary>
    public const int MaxOfferSlots = 9;

    public CaravanTradeSession(BlockPos shrinePos)
    {
        ShrinePos = shrinePos.Copy();
    }

    public BlockPos ShrinePos { get; }

    public TradePhase Phase { get; set; } = TradePhase.AwaitingPartner;

    public string? SideAUid { get; set; }
    public string SideAName { get; set; } = string.Empty;
    public bool SideAReady { get; set; }
    public List<TradeOfferSlot> SideAOffer { get; set; } = new();

    public string? SideBUid { get; set; }
    public string SideBName { get; set; } = string.Empty;
    public bool SideBReady { get; set; }
    public List<TradeOfferSlot> SideBOffer { get; set; } = new();

    /// <summary>True once both seats are filled.</summary>
    public bool HasBothSeats => !string.IsNullOrEmpty(SideAUid) && !string.IsNullOrEmpty(SideBUid);

    /// <summary>True once both parties have sealed their offers — the terminal state handed to #434.</summary>
    public bool BothSealed => HasBothSeats && SideAReady && SideBReady;

    public TradeSide GetSide(string playerUid)
    {
        if (SideAUid == playerUid) return TradeSide.A;
        if (SideBUid == playerUid) return TradeSide.B;
        return TradeSide.None;
    }

    public bool IsParticipant(string playerUid) => GetSide(playerUid) != TradeSide.None;

    /// <summary>
    ///     Seat a player into the first free seat. Returns the seat taken, or
    ///     <see cref="TradeSide.None" /> if the table is already full with other players.
    /// </summary>
    public TradeSide Seat(string playerUid, string playerName)
    {
        if (string.IsNullOrEmpty(SideAUid))
        {
            SideAUid = playerUid;
            SideAName = playerName;
            return TradeSide.A;
        }

        if (SideAUid == playerUid)
            return TradeSide.A;

        if (string.IsNullOrEmpty(SideBUid))
        {
            SideBUid = playerUid;
            SideBName = playerName;
            Phase = TradePhase.Active;
            return TradeSide.B;
        }

        if (SideBUid == playerUid)
            return TradeSide.B;

        return TradeSide.None;
    }

    public void SetOffer(TradeSide side, List<TradeOfferSlot> offer)
    {
        if (side == TradeSide.A) SideAOffer = offer;
        else if (side == TradeSide.B) SideBOffer = offer;
    }

    public void SetReady(TradeSide side, bool ready)
    {
        if (side == TradeSide.A) SideAReady = ready;
        else if (side == TradeSide.B) SideBReady = ready;
    }

    /// <summary>
    ///     Anti-switcheroo: any offer change voids both sealed offers so neither party
    ///     can complete a trade whose contents shifted under them.
    /// </summary>
    public void ClearReadyFlags()
    {
        SideAReady = false;
        SideBReady = false;
    }

    public TradeStateSyncPacket ToSyncPacket()
    {
        return new TradeStateSyncPacket
        {
            ShrineX = ShrinePos.X,
            ShrineY = ShrinePos.Y,
            ShrineZ = ShrinePos.Z,
            Phase = Phase,
            SideAPlayerUid = SideAUid ?? string.Empty,
            SideAPlayerName = SideAName,
            SideAReady = SideAReady,
            SideAOffer = SideAOffer,
            SideBPlayerUid = SideBUid ?? string.Empty,
            SideBPlayerName = SideBName,
            SideBReady = SideBReady,
            SideBOffer = SideBOffer
        };
    }
}
