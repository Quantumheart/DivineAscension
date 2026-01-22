using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class SmeltingFavorTrackerTests
{
    private static SmeltingFavorTracker CreateTracker(
        FakeWorldService fakeWorldService,
        Mock<IPlayerProgressionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        var mockLogger = new Mock<ILoggerWrapper>();
        return new SmeltingFavorTracker(mockLogger.Object, fakeWorldService, mockPlayerReligion.Object,
            mockFavor.Object);
    }

    private static MethodInfo GetHandleMethod()
    {
        var mi = typeof(SmeltingFavorTracker).GetMethod("HandleMoldPoured",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        return mi!;
    }

    [Fact]
    public void HandleMoldPoured_ToolMold_FullFavorAwarded()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-smelt-1", "Smelter");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-smelt-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-smelt-1", DeityDomain.Craft));

        mockPlayerReligion.Setup(m => m.GetPlayerDeityType("player-smelt-1"))
            .Returns(DeityDomain.Craft);

        var tracker = CreateTracker(fakeWorldService, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        var method = GetHandleMethod();

        // 100 units into tool mold => 100 * 0.01 = 1.0 favor
        method.Invoke(tracker, new object?[] { "player-smelt-1", new BlockPos(0, 0, 0), 100, true });

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-smelt-1"),
            "smelting",
            It.Is<float>(f => Math.Abs(f - 1.0f) < 0.001f)
        ), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void HandleMoldPoured_IngotMold_ReducedFavorAwarded()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-smelt-2", "Ingotter");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-smelt-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-smelt-2", DeityDomain.Craft));

        mockPlayerReligion.Setup(m => m.GetPlayerDeityType("player-smelt-2"))
            .Returns(DeityDomain.Craft);

        var tracker = CreateTracker(fakeWorldService, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        var method = GetHandleMethod();

        // 100 units into ingot mold => 100 * 0.01 * 0.4 = 0.4 favor
        method.Invoke(tracker, new object?[] { "player-smelt-2", new BlockPos(1, 2, 3), 100, false });

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-smelt-2"),
            "smelting",
            It.Is<float>(f => Math.Abs(f - 0.4f) < 0.001f)
        ), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void HandleMoldPoured_NonKhorasFollower_NoFavor()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-smelt-3", "OtherDeity");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Lysa
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-smelt-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-smelt-3", DeityDomain.Wild));

        var tracker = CreateTracker(fakeWorldService, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        var method = GetHandleMethod();
        method.Invoke(tracker, new object?[] { "player-smelt-3", new BlockPos(5, 5, 5), 100, true });

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<float>()),
            Times.Never);

        tracker.Dispose();
    }
}