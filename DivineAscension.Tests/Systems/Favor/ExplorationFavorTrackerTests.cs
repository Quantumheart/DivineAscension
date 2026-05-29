using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class ExplorationFavorTrackerTests
{
    private static ExplorationFavorTracker CreateTracker(
        Mock<IEventService> mockEventService,
        Mock<IWorldService> mockWorldService,
        Mock<IPlayerProgressionDataManager> mockPlayerProgression,
        Mock<IFavorSystem> mockFavor,
        DivineAscension.Configuration.GameBalanceConfig? config = null)
    {
        var mockLogger = new Mock<ILoggerWrapper>();
        return new ExplorationFavorTracker(mockLogger.Object, mockEventService.Object,
            mockWorldService.Object, mockPlayerProgression.Object, mockFavor.Object,
            config ?? new DivineAscension.Configuration.GameBalanceConfig());
    }

    private static (Mock<IServerPlayer>, PlayerProgressionData) WirePlayer(
        Mock<IPlayerProgressionDataManager> mockMgr,
        string uid = "player-1")
    {
        var data = new PlayerProgressionData(uid);
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.SetupGet(p => p.PlayerUID).Returns(uid);
        var d = data;
        mockMgr.Setup(m => m.TryGetPlayerData(uid, out d!)).Returns(true);
        return (mockPlayer, data);
    }

    [Fact]
    public void DeityDomain_IsCaravan()
    {
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(), TestFixtures.CreateMockFavorSystem());

        Assert.Equal(DeityDomain.Caravan, tracker.DeityDomain);
    }

    [Fact]
    public void Initialize_RegistersTwoSecondCallback()
    {
        var mockEventService = new Mock<IEventService>();
        mockEventService.Setup(e => e.RegisterCallback(It.IsAny<Action<float>>(), It.IsAny<int>())).Returns(42L);

        var tracker = CreateTracker(mockEventService, new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(), TestFixtures.CreateMockFavorSystem());
        tracker.Initialize();

        mockEventService.Verify(e => e.RegisterCallback(It.IsAny<Action<float>>(),
            ExplorationFavorTracker.TICK_INTERVAL_MS), Times.Once);

        tracker.Dispose();
        mockEventService.Verify(e => e.UnregisterCallback(42L), Times.Once);
    }

    [Fact]
    public void TryAwardChunkFavor_NewChunk_AwardsBaseFavorAndRecords()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, data) = WirePlayer(mockMgr);
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(), mockMgr, mockFavor);

        var awarded = tracker.TryAwardChunkFavor(mockPlayer.Object, chunkKey: 7L, multiplier: 1.0f);

        Assert.True(awarded);
        Assert.True(data.HasDiscoveredChunk(7L));
        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "discovered chunk",
            ExplorationFavorTracker.BASE_CHUNK_FAVOR, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void TryAwardChunkFavor_RevisitedChunk_NoFavor()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, data) = WirePlayer(mockMgr);
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(), mockMgr, mockFavor);

        data.TryAddDiscoveredChunk(7L);

        var awarded = tracker.TryAwardChunkFavor(mockPlayer.Object, 7L, 1.0f);

        Assert.False(awarded);
        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);
    }

    [Fact]
    public void TryAwardChunkFavor_AppliesMultiplier()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, _) = WirePlayer(mockMgr);
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(), mockMgr, mockFavor);

        tracker.TryAwardChunkFavor(mockPlayer.Object, 7L, multiplier: 2.5f);

        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "discovered chunk",
            ExplorationFavorTracker.BASE_CHUNK_FAVOR * 2.5f, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void TryAwardChunkFavor_AtSoftCap_StopsAwarding()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, data) = WirePlayer(mockMgr);
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(), mockMgr, mockFavor);

        for (long i = 0; i < PlayerProgressionData.DiscoveredChunksSoftCap; i++)
        {
            data.TryAddDiscoveredChunk(i);
        }

        var awarded = tracker.TryAwardChunkFavor(mockPlayer.Object, long.MaxValue, 1.0f);

        Assert.False(awarded);
        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);
    }

    [Fact]
    public void TryAwardTraderBonus_FirstEncounter_AwardsTenFavor()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, data) = WirePlayer(mockMgr);
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(), mockMgr, mockFavor);

        var awarded = tracker.TryAwardTraderBonus(mockPlayer.Object, traderEntityId: 555L);

        Assert.True(awarded);
        Assert.True(data.HasDiscoveredTrader(555L));
        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "encountered trader",
            ExplorationFavorTracker.TRADER_BONUS_FAVOR, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void TryAwardTraderBonus_RepeatEncounter_NoFavor()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, _) = WirePlayer(mockMgr);
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(), mockMgr, mockFavor);

        tracker.TryAwardTraderBonus(mockPlayer.Object, 555L);
        var second = tracker.TryAwardTraderBonus(mockPlayer.Object, 555L);

        Assert.False(second);
        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Once);
    }

    [Fact]
    public void TryAwardChunkFavor_AppliesConfigExplorationMultiplier()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, _) = WirePlayer(mockMgr);
        var config = new DivineAscension.Configuration.GameBalanceConfig
        {
            CaravanExplorationFavorMultiplier = 3.0f
        };
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(),
            mockMgr, mockFavor, config);

        tracker.TryAwardChunkFavor(mockPlayer.Object, 7L, multiplier: 1.0f);

        // BASE_CHUNK_FAVOR (1) * stat-multiplier (1) * config (3) = 3
        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "discovered chunk",
            3f, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void TryAwardTraderBonus_AppliesConfigExplorationMultiplier()
    {
        var mockMgr = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var (mockPlayer, _) = WirePlayer(mockMgr);
        var config = new DivineAscension.Configuration.GameBalanceConfig
        {
            CaravanExplorationFavorMultiplier = 0.5f
        };
        var tracker = CreateTracker(new Mock<IEventService>(), new Mock<IWorldService>(),
            mockMgr, mockFavor, config);

        tracker.TryAwardTraderBonus(mockPlayer.Object, 555L);

        // TRADER_BONUS_FAVOR (10) * 0.5 = 5
        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "encountered trader",
            5f, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void PackChunkKey_IgnoresVerticalDifference()
    {
        // Verticals not represented in the key — moving up/down inside the same column should
        // produce the same chunkKey (no double-award for digging straight down).
        var a = ExplorationFavorTracker.PackChunkKey(5, -3);
        var b = ExplorationFavorTracker.PackChunkKey(5, -3);
        Assert.Equal(a, b);

        var c = ExplorationFavorTracker.PackChunkKey(5, 7);
        Assert.NotEqual(a, c);
    }
}
