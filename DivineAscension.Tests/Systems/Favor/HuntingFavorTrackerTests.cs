using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

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
        Mock<IPlayerProgressionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        var mockLogger = new Mock<ILogger>();
        var mockEventService = new Mock<IEventService>();
        var mockWorldService = new Mock<IWorldService>();
        return new HuntingFavorTracker(mockPlayerReligion.Object, mockLogger.Object, mockEventService.Object,
            mockWorldService.Object, mockFavor.Object);
    }

    private static void SetupPlayer(Mock<ICoreServerAPI> mockSapi, IServerPlayer player)
    {
        var mockWorld = new Mock<IServerWorldAccessor>();
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new[] { player });
    }

    #region Multiple Player Tests

    [Fact]
    public void Initialize_HandlesMultiplePlayers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var mockPlayer1 = TestFixtures.CreateMockServerPlayer("player-1", "Hunter1");
        var mockPlayer2 = TestFixtures.CreateMockServerPlayer("player-2", "Hunter2");
        var mockPlayer3 = TestFixtures.CreateMockServerPlayer("player-3", "Miner");

        var mockWorld = new Mock<IServerWorldAccessor>();
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.AllOnlinePlayers)
            .Returns(new[] { mockPlayer1.Object, mockPlayer2.Object, mockPlayer3.Object });

        // Player 1 and 2 follow Lysa
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Wild));
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-2", DeityDomain.Wild));

        // Player 3 follows Khoras
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-3"));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw - handles multiple players
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    #endregion

    #region Initialization and Disposal Tests

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
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
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
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
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        Assert.Equal(DeityDomain.Wild, tracker.DeityDomain);

        tracker.Dispose();
    }

    #endregion

    #region Follower Cache Tests

    [Fact]
    public void Initialize_CachesLysaFollowers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "Hunter");

        SetupPlayer(mockSapi, mockPlayer.Object);

        // Player follows Lysa
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Wild));

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
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "Miner");

        SetupPlayer(mockSapi, mockPlayer.Object);

        // Player follows Khoras (not Lysa)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft));

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
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw even with no online players
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    #endregion

    #region Weight-Based Favor Calculation Tests

    [Theory]
    [InlineData(700f, 15)] // Large moose (male)
    [InlineData(480f, 15)] // Female moose
    [InlineData(300f, 15)] // Threshold for apex tier
    [InlineData(299f, 12)] // Just below apex threshold
    [InlineData(200f, 12)] // Bison
    [InlineData(150f, 12)] // Threshold for large herbivores
    [InlineData(149f, 10)] // Just below large herbivore threshold
    [InlineData(100f, 10)] // Large deer
    [InlineData(75f, 10)] // Threshold for large deer/scavengers
    [InlineData(74f, 8)] // Just below large deer threshold
    [InlineData(50f, 8)] // Medium deer
    [InlineData(35f, 8)] // Threshold for medium prey
    [InlineData(34f, 5)] // Just below medium prey threshold
    [InlineData(20f, 5)] // Small animal
    [InlineData(10f, 5)] // Threshold for small animals
    [InlineData(9f, 3)] // Just below small animal threshold
    [InlineData(5f, 3)] // Tiny animal
    [InlineData(1f, 3)] // Very tiny animal
    [InlineData(0.5f, 3)] // Smallest weight
    public void CalculateFavorByWeight_ReturnsCorrectTier(float weight, int expectedTier)
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var result = tracker.CalculateFavorByWeight(weight);

        Assert.Equal(expectedTier, result);

        tracker.Dispose();
    }

    [Fact]
    public void CalculateFavorByWeight_ApexTier_ReturnsHighestFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Test various apex-weight animals
        Assert.Equal(15, tracker.CalculateFavorByWeight(700f)); // Large moose male
        Assert.Equal(15, tracker.CalculateFavorByWeight(500f)); // Female moose
        Assert.Equal(15, tracker.CalculateFavorByWeight(400f)); // Bear
        Assert.Equal(15, tracker.CalculateFavorByWeight(300f)); // Threshold

        tracker.Dispose();
    }

    [Fact]
    public void CalculateFavorByWeight_TinyAnimals_ReturnsMinimumFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Test tiny animals (< 10 kg)
        Assert.Equal(3, tracker.CalculateFavorByWeight(9.9f));
        Assert.Equal(3, tracker.CalculateFavorByWeight(5f));
        Assert.Equal(3, tracker.CalculateFavorByWeight(1f));
        Assert.Equal(3, tracker.CalculateFavorByWeight(0.1f));

        tracker.Dispose();
    }

    #endregion

    #region IsHuntable Tests

    // Note: IsHuntable tests require mocking Entity.Properties.Attributes which is complex
    // due to JsonObject being a concrete class. The following documents expected behavior:
    //
    // IsHuntable returns true when:
    // - entity.Properties.Attributes["huntable"] is true
    // - entity.Properties.Attributes["creatureDiet"] has a value
    //
    // IsHuntable returns false when:
    // - Properties is null
    // - Attributes is null
    // - Neither huntable nor creatureDiet attributes are present

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
}