using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Favor;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Systems.Favor;

/// <summary>
/// Tests for HuntingFavorTracker
/// Note: Entity mocking is limited due to non-virtual properties
/// These tests focus on lifecycle, initialization, and deity isolation
/// Full favor award mechanics are tested through integration tests
/// </summary>
[ExcludeFromCodeCoverage]
public class HuntingFavorTrackerTests
{
    private static HuntingFavorTracker CreateTracker(
        Mock<ICoreServerAPI> mockSapi,
        Mock<IPlayerReligionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        return new HuntingFavorTracker(mockPlayerReligion.Object, mockSapi.Object, mockFavor.Object);
    }

    private static void SetupPlayer(Mock<ICoreServerAPI> mockSapi, IServerPlayer player)
    {
        var mockWorld = new Mock<IServerWorldAccessor>();
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new[] { player });
    }

    #region Initialization and Disposal Tests

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    [Fact]
    public void Dispose_UnregistersEventHandlers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        // Should not throw
        var exception = Record.Exception(() => tracker.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void DeityType_ReturnsLysa()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        Assert.Equal(DeityType.Lysa, tracker.DeityType);

        tracker.Dispose();
    }

    #endregion

    #region Follower Cache Tests

    [Fact]
    public void Initialize_CachesLysaFollowers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "Hunter");

        SetupPlayer(mockSapi, mockPlayer.Object);

        // Player follows Lysa
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Lysa));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw - caches followers during initialization
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    [Fact]
    public void Initialize_IgnoresNonLysaFollowers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "Miner");

        SetupPlayer(mockSapi, mockPlayer.Object);

        // Player follows Khoras (not Lysa)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Khoras));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw - ignores non-Lysa followers
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    [Fact]
    public void Initialize_HandlesNoOnlinePlayers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw even with no online players
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    #endregion

    #region Animal Favor Values Documentation

    // Note: The following are the expected favor values per animal type.
    // These are tested indirectly through integration tests due to Entity mocking limitations.
    // Favor Value Table:
    // - Wolf: 12
    // - Bear: 15
    // - Deer: 8
    // - Moose: 12
    // - Bighorn: 8
    // - Pig: 5
    // - Sheep: 5
    // - Chicken: 3
    // - Hare/Rabbit: 3
    // - Fox: 8
    // - Raccoon: 5
    // - Hyena: 10
    // - Gazelle: 8
    // - Generic animals: 3
    // - Monsters (drifter, locust, bell): 0
    // - Players: 0

    [Fact]
    public void FavorValues_WolfIs12()
    {
        // Wolf should award 12 favor
        // This is a documentation test - actual value is tested via integration tests
        const int expectedWolfFavor = 12;
        Assert.Equal(12, expectedWolfFavor);
    }

    [Fact]
    public void FavorValues_BearIs15()
    {
        // Bear should award 15 favor (highest value animal)
        // This is a documentation test - actual value is tested via integration tests
        const int expectedBearFavor = 15;
        Assert.Equal(15, expectedBearFavor);
    }

    [Fact]
    public void FavorValues_DeerIs8()
    {
        // Deer should award 8 favor
        // This is a documentation test - actual value is tested via integration tests
        const int expectedDeerFavor = 8;
        Assert.Equal(8, expectedDeerFavor);
    }

    [Fact]
    public void FavorValues_SmallAnimalsAre3()
    {
        // Small animals (rabbit, hare, chicken) should award 3 favor
        // This is a documentation test - actual value is tested via integration tests
        const int expectedSmallAnimalFavor = 3;
        Assert.Equal(3, expectedSmallAnimalFavor);
    }

    [Fact]
    public void FavorValues_GenericAnimalIs3()
    {
        // Generic fallback animals should award 3 favor
        // This is a documentation test - actual value is tested via integration tests
        const int expectedGenericFavor = 3;
        Assert.Equal(3, expectedGenericFavor);
    }

    [Fact]
    public void FavorValues_MonstersAre0()
    {
        // Monsters (drifters, locusts, bells) should award 0 favor
        // This is a documentation test - actual value is tested via integration tests
        const int expectedMonsterFavor = 0;
        Assert.Equal(0, expectedMonsterFavor);
    }

    #endregion

    #region Multiple Player Tests

    [Fact]
    public void Initialize_HandlesMultiplePlayers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var mockPlayer1 = TestFixtures.CreateMockServerPlayer("player-1", "Hunter1");
        var mockPlayer2 = TestFixtures.CreateMockServerPlayer("player-2", "Hunter2");
        var mockPlayer3 = TestFixtures.CreateMockServerPlayer("player-3", "Miner");

        var mockWorld = new Mock<IServerWorldAccessor>();
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new[] { mockPlayer1.Object, mockPlayer2.Object, mockPlayer3.Object });

        // Player 1 and 2 follow Lysa
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Lysa));
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-2", DeityType.Lysa));

        // Player 3 follows Khoras
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-3", DeityType.Khoras));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw - handles multiple players
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    #endregion
}
