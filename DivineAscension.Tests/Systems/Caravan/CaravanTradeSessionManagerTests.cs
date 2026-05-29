using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Network.Caravan;
using DivineAscension.Services;
using DivineAscension.Systems.Caravan;
using DivineAscension.Tests.Helpers;
using Moq;
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
        _manager = new CaravanTradeSessionManager(logger, _net, _world, _events, _messenger);
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
        SetReady(_bob, Shrine, true);

        var sealedSync = LastSync();
        Assert.True(sealedSync!.SideAReady);
        Assert.True(sealedSync.SideBReady);

        // Alice changes her offer — both seals must break.
        Offer(_alice, Shrine, "axe");

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
    public void SetReady_BothParties_ReachesSealedState()
    {
        Open(_alice, Shrine);
        Open(_bob, Shrine);
        SetReady(_alice, Shrine, true);
        SetReady(_bob, Shrine, true);

        var sync = LastSync();
        Assert.True(sync!.SideAReady);
        Assert.True(sync.SideBReady);
        Assert.Equal(TradePhase.Active, sync.Phase);
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
