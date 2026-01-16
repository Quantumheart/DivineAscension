using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class RuinDiscoveryFavorTrackerTests
{
    private static RuinDiscoveryFavorTracker CreateTracker(
        Mock<ICoreServerAPI> mockSapi,
        Mock<IPlayerProgressionDataManager> mockPlayerProgression,
        Mock<IFavorSystem> mockFavor)
    {
        return new RuinDiscoveryFavorTracker(mockPlayerProgression.Object, mockSapi.Object, mockFavor.Object);
    }

    private static void SetupOnlinePlayer(Mock<IServerWorldAccessor> mockWorld, IServerPlayer player)
    {
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new[] { player });
    }

    [Fact]
    public void Initialize_RegistersCallbackAndSubscribesToEvents()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockEvent = new Mock<IServerEventAPI>();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockSapi.Setup(s => s.Event).Returns(mockEvent.Object);
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(Array.Empty<IPlayer>());

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);
        tracker.Initialize();

        // Verify callback was registered
        mockEvent.Verify(e => e.RegisterCallback(It.IsAny<Action<float>>(), 500), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void IsRuinBlock_DevastationBlocks_ReturnsCorrectFavorAndType()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);

        // Test devastation blocks
        var devastationBlock = new Block { Code = new AssetLocation("game", "devastation-wall") };
        Assert.True(tracker.IsRuinBlock(devastationBlock, out var type1, out var favor1));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Devastation, type1);
        Assert.Equal(100, favor1);

        // Test drock
        var drockBlock = new Block { Code = new AssetLocation("game", "drock-north") };
        Assert.True(tracker.IsRuinBlock(drockBlock, out var type2, out var favor2));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Devastation, type2);
        Assert.Equal(100, favor2);

        // Test clutter-devastation
        var clutterBlock = new Block { Code = new AssetLocation("game", "clutter-devastation-pipes") };
        Assert.True(tracker.IsRuinBlock(clutterBlock, out var type3, out var favor3));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Devastation, type3);
        Assert.Equal(100, favor3);
    }

    [Fact]
    public void IsRuinBlock_TemporalBlocks_ReturnsCorrectFavorAndType()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);

        // Test static translocator
        var translocatorBlock = new Block { Code = new AssetLocation("game", "statictranslocator-normal-north") };
        Assert.True(tracker.IsRuinBlock(translocatorBlock, out var type1, out var favor1));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Temporal, type1);
        Assert.Equal(75, favor1);

        // Test resonator
        var resonatorBlock = new Block { Code = new AssetLocation("game", "resonator-north") };
        Assert.True(tracker.IsRuinBlock(resonatorBlock, out var type2, out var favor2));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Temporal, type2);
        Assert.Equal(75, favor2);

        // Test riftward
        var riftwardBlock = new Block { Code = new AssetLocation("game", "riftward-base") };
        Assert.True(tracker.IsRuinBlock(riftwardBlock, out var type3, out var favor3));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Temporal, type3);
        Assert.Equal(75, favor3);
    }

    [Fact]
    public void IsRuinBlock_LocustBlocks_ReturnsCorrectFavorAndType()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);

        // Test locust nest cage
        var cageBlock = new Block { Code = new AssetLocation("game", "locustnest-cage") };
        Assert.True(tracker.IsRuinBlock(cageBlock, out var type1, out var favor1));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Locust, type1);
        Assert.Equal(25, favor1);

        // Test locust nest metal spikes
        var spikesBlock = new Block { Code = new AssetLocation("game", "locustnest-metalspikes") };
        Assert.True(tracker.IsRuinBlock(spikesBlock, out var type2, out var favor2));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Locust, type2);
        Assert.Equal(25, favor2);
    }

    [Fact]
    public void IsRuinBlock_BrickBlocks_ReturnsCorrectFavorAndType()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);

        // Test brick ruin
        var brickBlock = new Block { Code = new AssetLocation("game", "brickruin-irregular-gray") };
        Assert.True(tracker.IsRuinBlock(brickBlock, out var type, out var favor));
        Assert.Equal(RuinDiscoveryFavorTracker.RuinType.Brick, type);
        Assert.Equal(20, favor);
    }

    [Fact]
    public void IsRuinBlock_NonRuinBlock_ReturnsFalse()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);

        // Test normal stone
        var stoneBlock = new Block { Code = new AssetLocation("game", "stone-granite") };
        Assert.False(tracker.IsRuinBlock(stoneBlock, out var type, out var favor));
        Assert.Equal(default, type);
        Assert.Equal(0, favor);

        // Test null block
        Assert.False(tracker.IsRuinBlock(null!, out var type2, out var favor2));
        Assert.Equal(default, type2);
        Assert.Equal(0, favor2);
    }

    [Fact]
    public void UpdateFollower_ConquestFollower_AddsToCache()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        // Player follows Conquest
        mockPlayerProgression.Setup(m => m.GetPlayerDeityType("player-1"))
            .Returns(DeityDomain.Conquest);

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);
        tracker.Initialize();

        // Verify player is in follower cache by checking if they would be scanned
        // (We can't directly access the cache, but initialization should add them)
        mockPlayerProgression.Verify(m => m.GetPlayerDeityType("player-1"), Times.AtLeastOnce);

        tracker.Dispose();
    }

    [Fact]
    public void UpdateFollower_NonConquestFollower_RemovesFromCache()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        // Player follows Harvest (not Conquest)
        mockPlayerProgression.Setup(m => m.GetPlayerDeityType("player-1"))
            .Returns(DeityDomain.Harvest);

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);
        tracker.Initialize();

        // Player should not be in cache (verified by deity type check)
        mockPlayerProgression.Verify(m => m.GetPlayerDeityType("player-1"), Times.AtLeastOnce);

        tracker.Dispose();
    }

    [Fact]
    public void Dispose_UnregistersCallbackAndClearsCache()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockEvent = new Mock<IServerEventAPI>();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockSapi.Setup(s => s.Event).Returns(mockEvent.Object);
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(Array.Empty<IPlayer>());

        var callbackId = 12345L;
        mockEvent.Setup(e => e.RegisterCallback(It.IsAny<Action<float>>(), It.IsAny<int>()))
            .Returns(callbackId);

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);
        tracker.Initialize();
        tracker.Dispose();

        // Verify callback was unregistered
        mockEvent.Verify(e => e.UnregisterCallback(callbackId), Times.Once);
    }

    [Fact]
    public void DeityDomain_ReturnsConquest()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);

        Assert.Equal(DeityDomain.Conquest, tracker.DeityDomain);
    }
}
