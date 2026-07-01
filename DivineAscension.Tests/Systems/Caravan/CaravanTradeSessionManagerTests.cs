using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Network.Caravan;
using DivineAscension.Services;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.Caravan;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.Systems.Caravan;

/// <summary>
///     Tests for the server-authoritative caravan trade session state machine (#433):
///     seating, sync broadcast, anti-switcheroo ready-clear, cancel, and disconnect.
///     Players are created via <see cref="FakeWorldService" /> so the manager's broadcasts
///     can resolve them; their mock entity has no world position, so the open-time
///     proximity check is skipped (covered separately by integration/play-test).
/// </summary>
[ExcludeFromCodeCoverage]
public class CaravanTradeSessionManagerTests
{
    private readonly SpyNetworkService _net = new();
    private readonly FakeWorldService _world = new();
    private readonly FakeEventService _events = new();
    private readonly SpyPlayerMessenger _messenger = new();
    private readonly FakeCaravanTradeInventory _inventory = new();
    private readonly AltarEventEmitter _emitter = new();
    private readonly CaravanTradeSessionManager _manager;

    private readonly IServerPlayer _alice;
    private readonly IServerPlayer _bob;
    private readonly IServerPlayer _carol;

    // Shrine positions kept near origin so the (0,0,0) mock entity position is in reach.
    private static readonly (int X, int Y, int Z) Shrine = (1, 0, 1);
    private static readonly (int X, int Y, int Z) OtherShrine = (3, 0, 1);

    public CaravanTradeSessionManagerTests()
    {
        var logger = new Mock<ILoggerWrapper>().Object;
        _manager = new CaravanTradeSessionManager(logger, _net, _world, _events, _messenger, _inventory, _emitter);
        _manager.Initialize();

        _alice = _world.CreatePlayer("alice", "Alice");
        _bob = _world.CreatePlayer("bob", "Bob");
        _carol = _world.CreatePlayer("carol", "Carol");
    }

    // ----- helpers -----

    private void Open(IServerPlayer player, (int X, int Y, int Z) pos)
        => _net.SimulateReceive(player, new OpenTradeRequestPacket { ShrineX = pos.X, ShrineY = pos.Y, ShrineZ = pos.Z });

    private void Offer(IServerPlayer player, (int X, int Y, int Z) pos, params string[] names)
        => _net.SimulateReceive(player, new OfferUpdatePacket
        {
            ShrineX = pos.X,
            ShrineY = pos.Y,
            ShrineZ = pos.Z,
            Offer = names.Select((n, i) => new TradeOfferSlot { ItemCode = n, DisplayName = n, Quantity = 1, SlotIndex = i }).ToList()
        });

    private void SetReady(IServerPlayer player, (int X, int Y, int Z) pos, bool ready)
        => _net.SimulateReceive(player, new SetReadyPacket { ShrineX = pos.X, ShrineY = pos.Y, ShrineZ = pos.Z, Ready = ready });

    private void Cancel(IServerPlayer player, (int X, int Y, int Z) pos)
        => _net.SimulateReceive(player, new CancelTradePacket { ShrineX = pos.X, ShrineY = pos.Y, ShrineZ = pos.Z });

    private TradeStateSyncPacket? LastSync() => _net.GetLastSentMessage<TradeStateSyncPacket>();

    private int SyncCount() => _net.GetSentMessages<TradeStateSyncPacket>().Count();

    private List<TradeStateSyncPacket> SyncsTo(string uid) => _net.GetSentMessages()
        .Where(m => m.Message is TradeStateSyncPacket && m.Player?.PlayerUID == uid)
        .Select(m => (TradeStateSyncPacket)m.Message)
        .ToList();

    // ----- tests -----

    [Fact]
    public void OpenTrade_FirstPlayer_SeatsSideA_AwaitingPartner()
    {
        Open(_alice, Shrine);

        var sync = LastSync();
        Assert.NotNull(sync);
        Assert.Equal(TradePhase.AwaitingPartner, sync!.Phase);
        Assert.Equal("alice", sync.SideAPlayerUid);
        Assert.Equal(string.Empty, sync.SideBPlayerUid);
        Assert.Single(SyncsTo("alice"));
    }

    [Fact]
    public void OpenTrade_SecondPlayer_SeatsSideB_Active_BroadcastsToBoth()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);

        var sync = LastSync();
        Assert.NotNull(sync);
        Assert.Equal(TradePhase.Active, sync!.Phase);
        Assert.Equal("alice", sync.SideAPlayerUid);
        Assert.Equal("bob", sync.SideBPlayerUid);

        // Both participants receive the post-join snapshot.
        Assert.Contains(SyncsTo("alice"), s => s.Phase == TradePhase.Active);
        Assert.Contains(SyncsTo("bob"), s => s.Phase == TradePhase.Active);
    }

    [Fact]
    public void OpenTrade_ThirdPlayer_RejectedAsFull()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);
        Open(_carol, Shrine);

        // Carol is never seated, so she never receives a sync.
        Assert.Empty(SyncsTo("carol"));
        Assert.True(_messenger.GetMessageCountForPlayer("carol") > 0);
    }

    [Fact]
    public void OfferUpdate_BroadcastsToBoth_AndClampsToNine()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);

        var twelve = Enumerable.Range(0, 12).Select(i => $"item{i}").ToArray();
        Offer(_alice, Shrine, twelve);

        var sync = LastSync();
        Assert.NotNull(sync);
        Assert.Equal(CaravanTradeSession.MaxOfferSlots, sync!.SideAOffer.Count);
        Assert.Equal(0, sync.SideAOffer[0].SlotIndex);
        Assert.Equal(8, sync.SideAOffer[8].SlotIndex);
    }

    [Fact]
    public void OfferUpdate_ClearsBothReadyFlags_AntiSwitcheroo()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);
        SetReady(_alice, Shrine, true);

        var sealedSync = LastSync();
        Assert.True(sealedSync!.SideAReady);

        // Bob changes his offer — Alice's seal must break (any change voids both seals).
        Offer(_bob, Shrine, "axe");

        var after = LastSync();
        Assert.False(after!.SideAReady);
        Assert.False(after.SideBReady);
    }

    [Fact]
    public void SetReady_BeforePartnerJoins_IsIgnored()
    {
        Open(_alice, Shrine);
        SetReady(_alice, Shrine, true);

        var sync = LastSync();
        Assert.False(sync!.SideAReady);
    }

    [Fact]
    public void SetReady_BothParties_WithItems_SwapsAndClosesSession()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);
        Offer(_alice, Shrine, "axe");
        Offer(_bob, Shrine, "bread");
        SetReady(_alice, Shrine, true);
        SetReady(_bob, Shrine, true);

        // The atomic swap fired exactly once and the table closed.
        Assert.Equal(1, _inventory.SwapCount);
        var sync = LastSync();
        Assert.Equal(TradePhase.Closed, sync!.Phase);
        Assert.True(_messenger.GetMessageCountForPlayer("alice") > 0);
        Assert.True(_messenger.GetMessageCountForPlayer("bob") > 0);

        // Session is gone — further packets produce no new sync.
        var before = SyncCount();
        Offer(_alice, Shrine, "stick");
        Assert.Equal(before, SyncCount());
    }

    [Fact]
    public void SetReady_BothParties_MissingItems_AbortsSwap_UnsealsAndKeepsOpen()
    {
        _inventory.CanProvide["alice"] = false; // Alice no longer holds her offer

        Open(_alice, Shrine);
        Open(_bob, Shrine);
        Offer(_alice, Shrine, "axe");
        Offer(_bob, Shrine, "bread");
        SetReady(_alice, Shrine, true);
        SetReady(_bob, Shrine, true);

        // No items moved; both seals dropped; table still open and usable.
        Assert.Equal(0, _inventory.SwapCount);
        var sync = LastSync();
        Assert.Equal(TradePhase.Active, sync!.Phase);
        Assert.False(sync.SideAReady);
        Assert.False(sync.SideBReady);
        Assert.True(_messenger.GetMessageCountForPlayer("alice") > 0);

        // Session lives — Bob can still update his offer.
        var before = SyncCount();
        Offer(_bob, Shrine, "cheese");
        Assert.True(SyncCount() > before);
    }

    [Fact]
    public void ShrineBroken_ClosesActiveSession()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);

        _emitter.RaiseAltarBroken(_alice, new BlockPos(Shrine.X, Shrine.Y, Shrine.Z));

        var sync = LastSync();
        Assert.Equal(TradePhase.Closed, sync!.Phase);

        // Session torn down: later packets are ignored.
        var before = SyncCount();
        Offer(_bob, Shrine, "bread");
        Assert.Equal(before, SyncCount());
    }

    [Fact]
    public void LeashTick_WithBothInReach_LeavesSessionOpen()
    {
        // Mock entities report no position, so the reach check treats both as in range.
        Open(_alice, Shrine);
        Open(_bob, Shrine);

        var before = SyncCount();
        _events.TriggerPeriodicCallbacks(1f);

        // No spurious close.
        Assert.Equal(before, SyncCount());
        Offer(_bob, Shrine, "bread");
        Assert.True(SyncCount() > before);
    }

    [Fact]
    public void CancelTrade_ClosesSession_NotifiesPartner_AndIgnoresLaterPackets()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);

        Cancel(_alice, Shrine);

        // A Closed snapshot is pushed so dialogs tear down.
        var sync = LastSync();
        Assert.NotNull(sync);
        Assert.Equal(TradePhase.Closed, sync!.Phase);

        // Partner Bob is told the table closed.
        Assert.True(_messenger.GetMessageCountForPlayer("bob") > 0);

        // Session is gone — further offers from Bob produce no new sync.
        var before = SyncCount();
        Offer(_bob, Shrine, "bread");
        Assert.Equal(before, SyncCount());
    }

    [Fact]
    public void Disconnect_ClosesSession_NotifiesRemainingPartner()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);

        _events.TriggerPlayerDisconnect(_alice);

        var sync = LastSync();
        Assert.NotNull(sync);
        Assert.Equal(TradePhase.Closed, sync!.Phase);
        Assert.True(_messenger.GetMessageCountForPlayer("bob") > 0);
    }

    [Fact]
    public void OpenTrade_SamePlayerReopens_ResendsSnapshotWithoutDoubleSeating()
    {
        Open(_alice, Shrine);
        Open(_alice, Shrine);

        var sync = LastSync();
        Assert.Equal("alice", sync!.SideAPlayerUid);
        Assert.Equal(string.Empty, sync.SideBPlayerUid);
    }

    [Fact]
    public void OpenTrade_WhileSeatedElsewhere_IsRejected()
    {
        Open(_alice, Shrine);
        Open(_alice, OtherShrine);

        // No session ever syncs the other shrine; Alice gets an error instead.
        Assert.DoesNotContain(_net.GetSentMessages<TradeStateSyncPacket>(), s => s.ShrineX == OtherShrine.X);
        Assert.True(_messenger.GetMessageCountForPlayer("alice") > 0);
    }
}

/// <summary>
///     Test double for the inventory seam: records swap calls and lets a test mark a player as
///     no longer holding their offer. Item movement itself is play-tested (no real VS inventory).
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeCaravanTradeInventory : ICaravanTradeInventory
{
    /// <summary>Per-UID provisioning result; absent UID defaults to true (player holds the offer).</summary>
    public Dictionary<string, bool> CanProvide { get; } = new();

    public int SwapCount { get; private set; }

    public bool CanProvideOffer(IServerPlayer player, IReadOnlyList<TradeOfferSlot> offer)
        => !CanProvide.TryGetValue(player.PlayerUID, out var ok) || ok;

    public void SwapOffers(
        IServerPlayer sideA, IReadOnlyList<TradeOfferSlot> offerA,
        IServerPlayer sideB, IReadOnlyList<TradeOfferSlot> offerB,
        BlockPos dropPos)
        => SwapCount++;
}
