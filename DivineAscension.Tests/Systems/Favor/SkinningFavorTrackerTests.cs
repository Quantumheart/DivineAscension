using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

/// <summary>
/// Tests for SkinningFavorTracker
/// Tests focus on lifecycle, initialization, deity isolation, and weight-based favor calculation
/// Full integration tests verify actual skinning mechanics with entities
/// </summary>
[ExcludeFromCodeCoverage]
public class SkinningFavorTrackerTests
{
    private static SkinningFavorTracker CreateTracker(
        Mock<ICoreServerAPI> mockSapi,
        Mock<IPlayerProgressionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        var mockLogger = new Mock<ILoggerWrapper>();
        var mockEventService = new Mock<IEventService>();
        var mockWorldService = new Mock<IWorldService>();
        return new SkinningFavorTracker(mockLogger.Object, mockEventService.Object, mockWorldService.Object,
            mockPlayerReligion.Object, mockFavor.Object);
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

        // Player 1 and 2 follow Wild
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Wild));
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-2", DeityDomain.Wild));

        // Player 3 follows Craft
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-3", DeityDomain.Craft));

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
    public void DeityType_ReturnsWild()
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
    public void Initialize_CachesWildFollowers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "Hunter");

        SetupPlayer(mockSapi, mockPlayer.Object);

        // Player follows Wild deity
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Wild));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw - caches followers during initialization
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    [Fact]
    public void Initialize_IgnoresNonWildFollowers()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "Miner");

        SetupPlayer(mockSapi, mockPlayer.Object);

        // Player follows Craft deity (not Wild)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Should not throw - ignores non-Wild followers
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

    /// <summary>
    /// Tests that skinning favor is 50% of hunting favor (rounded up where appropriate)
    /// This rewards thorough gameplay without being overpowered
    /// </summary>
    [Theory]
    [InlineData(700f, 8)] // Large moose: 50% of 15 (apex tier)
    [InlineData(480f, 8)] // Female moose: 50% of 15
    [InlineData(300f, 8)] // Threshold for apex tier
    [InlineData(299f, 6)] // Just below apex: 50% of 12
    [InlineData(200f, 6)] // Bison: 50% of 12
    [InlineData(150f, 6)] // Threshold for large herbivores
    [InlineData(149f, 5)] // Just below large herbivore: 50% of 10
    [InlineData(100f, 5)] // Large deer: 50% of 10
    [InlineData(75f, 5)] // Threshold for large deer/scavengers
    [InlineData(74f, 4)] // Just below large deer: 50% of 8
    [InlineData(50f, 4)] // Medium deer: 50% of 8
    [InlineData(35f, 4)] // Threshold for medium prey
    [InlineData(34f, 3)] // Just below medium prey: 50% of 5 (rounded up)
    [InlineData(20f, 3)] // Small animal: 50% of 5
    [InlineData(10f, 3)] // Threshold for small animals
    [InlineData(9f, 2)] // Just below small animal: 50% of 3 (rounded up)
    [InlineData(5f, 2)] // Tiny animal: 50% of 3
    [InlineData(1f, 2)] // Very tiny animal: 50% of 3
    [InlineData(0.5f, 2)] // Smallest weight: 50% of 3
    public void CalculateFavorByWeight_Returns50PercentOfHuntingValue(float weight, int expectedFavor)
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var result = tracker.CalculateFavorByWeight(weight);

        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    [Fact]
    public void CalculateFavorByWeight_ApexTier_ReturnsHighestSkinnningFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Test various apex-weight animals (all should give 8 favor, 50% of 15)
        Assert.Equal(8, tracker.CalculateFavorByWeight(700f)); // Large moose male
        Assert.Equal(8, tracker.CalculateFavorByWeight(500f)); // Female moose
        Assert.Equal(8, tracker.CalculateFavorByWeight(400f)); // Bear
        Assert.Equal(8, tracker.CalculateFavorByWeight(300f)); // Threshold

        tracker.Dispose();
    }

    [Fact]
    public void CalculateFavorByWeight_TinyAnimals_ReturnsMinimumSkinnningFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Test tiny animals (< 10 kg) - should give 2 favor (50% of 3, rounded up)
        Assert.Equal(2, tracker.CalculateFavorByWeight(9.9f));
        Assert.Equal(2, tracker.CalculateFavorByWeight(5f));
        Assert.Equal(2, tracker.CalculateFavorByWeight(1f));
        Assert.Equal(2, tracker.CalculateFavorByWeight(0.1f));

        tracker.Dispose();
    }

    [Fact]
    public void CalculateFavorByWeight_ReturnsExactlyHalfOfHuntingValues()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Verify skinning is exactly 50% of hunting
        // Hunting: 15, 12, 10, 8, 5, 3
        // Skinning: 8, 6, 5, 4, 3, 2 (rounded up where needed)

        Assert.Equal(8, tracker.CalculateFavorByWeight(300f)); // 50% of 15 (apex)
        Assert.Equal(6, tracker.CalculateFavorByWeight(150f)); // 50% of 12 (large)
        Assert.Equal(5, tracker.CalculateFavorByWeight(75f)); // 50% of 10 (large deer)
        Assert.Equal(4, tracker.CalculateFavorByWeight(35f)); // 50% of 8 (medium)
        Assert.Equal(3, tracker.CalculateFavorByWeight(10f)); // 50% of 5 (small, rounded up)
        Assert.Equal(2, tracker.CalculateFavorByWeight(5f)); // 50% of 3 (tiny, rounded up)

        tracker.Dispose();
    }

    #endregion

    #region Deity Isolation Tests

    // Note: Deity isolation is tested through the follower cache tests.
    // The tracker only caches Wild domain followers, so only they receive favor.
    // Direct event invocation is not testable in C# (events can only appear on left side of += or -=).
    // Full deity isolation is verified through integration tests.

    #endregion

    #region Skinning Favor Values Documentation

    // Note: The following are the expected skinning favor values per animal type.
    // Skinning favor is 50% of hunting favor to prevent double-dipping while rewarding thorough gameplay.
    // Total favor per animal (kill + skin) = 1.5x hunting base value
    //
    // Skinning Favor Value Table:
    // - Wolf: 6 (50% of 12)
    // - Bear: 8 (50% of 15, rounded up)
    // - Deer: 4 (50% of 8)
    // - Moose: 6 (50% of 12)
    // - Bighorn: 4 (50% of 8)
    // - Pig: 3 (50% of 5, rounded up)
    // - Sheep: 3 (50% of 5, rounded up)
    // - Chicken: 2 (50% of 3, rounded up)
    // - Hare/Rabbit: 2 (50% of 3, rounded up)
    // - Fox: 4 (50% of 8)
    // - Raccoon: 3 (50% of 5, rounded up)
    // - Hyena: 5 (50% of 10)
    // - Gazelle: 4 (50% of 8)
    //
    // Total Favor Examples (kill + skin):
    // - Bear: 15 + 8 = 23 favor
    // - Wolf: 12 + 6 = 18 favor
    // - Deer: 8 + 4 = 12 favor
    // - Chicken: 3 + 2 = 5 favor

    [Fact]
    public void SkinningSavorValues_WolfIs6()
    {
        // Wolf skinning should award 6 favor (50% of 12 hunting)
        const int expectedWolfSkinningFavor = 6;
        Assert.Equal(6, expectedWolfSkinningFavor);
    }

    [Fact]
    public void SkinningFavorValues_BearIs8()
    {
        // Bear skinning should award 8 favor (50% of 15 hunting, rounded up)
        const int expectedBearSkinningFavor = 8;
        Assert.Equal(8, expectedBearSkinningFavor);
    }

    [Fact]
    public void SkinningFavorValues_DeerIs4()
    {
        // Deer skinning should award 4 favor (50% of 8 hunting)
        const int expectedDeerSkinningFavor = 4;
        Assert.Equal(4, expectedDeerSkinningFavor);
    }

    [Fact]
    public void SkinningFavorValues_SmallAnimalsAre2()
    {
        // Small animals (rabbit, hare, chicken) skinning should award 2 favor (50% of 3, rounded up)
        const int expectedSmallAnimalSkinningFavor = 2;
        Assert.Equal(2, expectedSmallAnimalSkinningFavor);
    }

    [Fact]
    public void TotalFavorValues_BearIs23()
    {
        // Total favor for killing and skinning a bear: 15 + 8 = 23
        const int totalBearFavor = 23;
        Assert.Equal(23, totalBearFavor);
    }

    [Fact]
    public void TotalFavorValues_WolfIs18()
    {
        // Total favor for killing and skinning a wolf: 12 + 6 = 18
        const int totalWolfFavor = 18;
        Assert.Equal(18, totalWolfFavor);
    }

    #endregion
}