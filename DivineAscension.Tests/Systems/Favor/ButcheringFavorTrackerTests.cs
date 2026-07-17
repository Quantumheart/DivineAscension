using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Butchering;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

/// <summary>
///     Tests for ButcheringFavorTracker.
///     Unlike SkinningFavorTracker (which subscribes to a static event that cannot be raised
///     from tests), ButcheringFavorTracker subscribes to an instance ButcheringEventEmitter, so
///     the skin/butcher award path can be exercised directly via Raise*() calls.
/// </summary>
[ExcludeFromCodeCoverage]
public class ButcheringFavorTrackerTests
{
    private static ButcheringFavorTracker CreateTracker(
        Mock<IEventService> mockEventService,
        Mock<IWorldService> mockWorldService,
        Mock<IPlayerProgressionDataManager> mockPlayerProgression,
        Mock<IFavorSystem> mockFavor,
        ButcheringEventEmitter emitter)
    {
        var mockLogger = new Mock<ILoggerWrapper>();
        return new ButcheringFavorTracker(
            mockLogger.Object, mockEventService.Object, mockWorldService.Object,
            mockPlayerProgression.Object, mockFavor.Object, emitter);
    }

    private static Mock<IServerPlayer> CreatePlayer(string uid = "player-uid")
    {
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns(uid);
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");
        return mockPlayer;
    }

    private static ItemStack CreateTestItemStack(string collectibleCode)
    {
        var item = new Item
        {
            Code = new AssetLocation("butchering", collectibleCode)
        };
        return new ItemStack(item);
    }

    #region Lifecycle and Deity Tests

    [Fact]
    public void DeityDomain_IsWild()
    {
        var emitter = new ButcheringEventEmitter();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            TestFixtures.CreateMockFavorSystem(), emitter);

        Assert.Equal(DeityDomain.Wild, tracker.DeityDomain);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        var emitter = new ButcheringEventEmitter();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            TestFixtures.CreateMockFavorSystem(), emitter);

        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void Dispose_UnregistersEventHandlers()
    {
        var emitter = new ButcheringEventEmitter();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            TestFixtures.CreateMockFavorSystem(), emitter);
        tracker.Initialize();

        var exception = Record.Exception(() => tracker.Dispose());
        Assert.Null(exception);

        emitter.ClearSubscribers();
    }

    #endregion

    #region Workload-Based Favor Calculation Tests

    [Theory]
    [InlineData("large", 8)]
    [InlineData("medium", 5)]
    [InlineData("small", 3)]
    [InlineData("unknown", 2)]
    [InlineData("", 2)]
    [InlineData(null, 2)]
    public void CalculateFavorByWorkload_ReturnsTieredFavor(string? workload, int expectedFavor)
    {
        var emitter = new ButcheringEventEmitter();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            TestFixtures.CreateMockFavorSystem(), emitter);

        var result = tracker.CalculateFavorByWorkload(workload!);

        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    #endregion

    #region Skinning Award Tests

    [Fact]
    public void HandleSkinned_AwardsWildFavorForLargeWorkload()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deadsheep-male-bighorn-1-dead");
        var pos = new BlockPos(0, 0, 0);

        emitter.RaiseAnimalSkinned(player.Object, pos, stack, "large");

        mockFavor.Verify(
            f => f.AwardFavorForAction(player.Object, "skinning deadsheep-male-bighorn-1-dead", 8,
                DeityDomain.Wild),
            Times.Once);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void HandleSkinned_AwardsWildFavorForMediumWorkload()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deaddeer-male-1-dead");
        var pos = new BlockPos(0, 0, 0);

        emitter.RaiseAnimalSkinned(player.Object, pos, stack, "medium");

        mockFavor.Verify(
            f => f.AwardFavorForAction(player.Object, "skinning deaddeer-male-1-dead", 5,
                DeityDomain.Wild),
            Times.Once);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    #endregion

    #region Butchering Award Tests

    [Fact]
    public void HandleButchered_AwardsWildFavorForLargeWorkload()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deadsheep-male-bighorn-1-bledout");
        var pos = new BlockPos(0, 0, 0);

        emitter.RaiseAnimalButchered(player.Object, pos, stack, "large");

        mockFavor.Verify(
            f => f.AwardFavorForAction(player.Object, "butchering deadsheep-male-bighorn-1-bledout",
                8, DeityDomain.Wild),
            Times.Once);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void HandleButchered_AwardsWildFavorForSmallWorkload()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deadchicken-rooster-1-bledout");
        var pos = new BlockPos(0, 0, 0);

        emitter.RaiseAnimalButchered(player.Object, pos, stack, "small");

        mockFavor.Verify(
            f => f.AwardFavorForAction(player.Object, "butchering deadchicken-rooster-1-bledout", 3,
                DeityDomain.Wild),
            Times.Once);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    #endregion

    #region Cooldown Tests

    [Fact]
    public void Cooldown_PreventsDuplicateSkinningAwardWithinWindow()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deadsheep-male-bighorn-1-dead");
        var pos = new BlockPos(0, 0, 0);

        // Raise twice in quick succession (same player + workstation)
        emitter.RaiseAnimalSkinned(player.Object, pos, stack, "large");
        emitter.RaiseAnimalSkinned(player.Object, pos, stack, "large");

        // The 5s per-player+pos cooldown should suppress the second award
        mockFavor.Verify(
            f => f.AwardFavorForAction(player.Object, "skinning deadsheep-male-bighorn-1-dead", 8,
                DeityDomain.Wild),
            Times.Once);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void Cooldown_AllowsAwardAtDifferentWorkstation()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deadsheep-male-bighorn-1-dead");

        // Two different workstations → both should award (cooldown is per-pos)
        emitter.RaiseAnimalSkinned(player.Object, new BlockPos(0, 0, 0), stack, "large");
        emitter.RaiseAnimalSkinned(player.Object, new BlockPos(10, 0, 10), stack, "large");

        mockFavor.Verify(
            f => f.AwardFavorForAction(player.Object, "skinning deadsheep-male-bighorn-1-dead", 8,
                DeityDomain.Wild),
            Times.Exactly(2));

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    #endregion

    #region Null-Guard and Disposal Tests

    [Fact]
    public void HandleSkinned_NullPlayer_DoesNotAwardOrThrow()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var stack = CreateTestItemStack("deadsheep-male-bighorn-1-dead");
        var pos = new BlockPos(0, 0, 0);

        var exception = Record.Exception(() =>
            emitter.RaiseAnimalSkinned(null!, pos, stack, "large"));

        Assert.Null(exception);
        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void HandleButchered_NullStack_DoesNotAwardOrThrow()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();

        var player = CreatePlayer();
        var pos = new BlockPos(0, 0, 0);

        var exception = Record.Exception(() =>
            emitter.RaiseAnimalButchered(player.Object, pos, null!, "large"));

        Assert.Null(exception);
        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);

        tracker.Dispose();
        emitter.ClearSubscribers();
    }

    [Fact]
    public void Dispose_StopsAwardingFavor()
    {
        var emitter = new ButcheringEventEmitter();
        var mockFavor = new Mock<IFavorSystem>();
        var tracker = CreateTracker(
            new Mock<IEventService>(), new Mock<IWorldService>(),
            TestFixtures.CreateMockPlayerProgressionDataManager(),
            mockFavor, emitter);
        tracker.Initialize();
        tracker.Dispose();

        var player = CreatePlayer();
        var stack = CreateTestItemStack("deadsheep-male-bighorn-1-dead");
        var pos = new BlockPos(0, 0, 0);

        emitter.RaiseAnimalSkinned(player.Object, pos, stack, "large");

        // After Dispose the tracker should have unsubscribed — no favor awarded
        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);

        emitter.ClearSubscribers();
    }

    #endregion
}