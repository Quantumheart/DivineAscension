using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Moq;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Favor;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class AnvilFavorTrackerTests
{
    private static AnvilFavorTracker CreateTracker(
        Mock<ICoreServerAPI> mockSapi,
        Mock<IPlayerReligionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        return new AnvilFavorTracker(mockPlayerReligion.Object, mockSapi.Object, mockFavor.Object);
    }

    private static MethodInfo GetHandleMethod()
    {
        var mi = typeof(AnvilFavorTracker).GetMethod("HandleAnvilRecipeCompleted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        return mi!;
    }

    private static (Mock<IServerWorldAccessor> world, Mock<IBlockAccessor> accessor) SetupWorld(
        Mock<ICoreServerAPI> mockSapi)
    {
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        return (mockWorld, mockAccessor);
    }

    private static void SetupPlayer(Mock<IServerWorldAccessor> mockWorld, IServerPlayer player)
    {
        mockWorld.Setup(w => w.PlayerByUid(player.PlayerUID)).Returns(player);
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new[] { player });
    }

    private static void SetupHelveAdjacent(Mock<IBlockAccessor> mockAccessor, BlockPos anvilPos, bool present)
    {
        // For simplicity, set helvehammer to the +X face only
        var helveBlock = new Block { Code = new AssetLocation("game", "helvehammer") };
        var airBlock = new Block { Code = new AssetLocation("game", "air") };

        foreach (var face in BlockFacing.ALLFACES)
        {
            var adj = anvilPos.AddCopy(face);
            if (present && face == BlockFacing.EAST)
            {
                mockAccessor.Setup(a => a.GetBlock(adj)).Returns(helveBlock);
            }
            else
            {
                mockAccessor.Setup(a => a.GetBlock(adj)).Returns(airBlock);
            }
        }
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_NullOutput_AwardsMidTierFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var (mockWorld, mockAccessor) = SetupWorld(mockSapi);
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var player = TestFixtures.CreateMockServerPlayer("player-anvil-1", "Smith");
        SetupPlayer(mockWorld, player.Object);

        // Player follows Khoras
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-anvil-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-anvil-1", DeityType.Khoras));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();
        var method = GetHandleMethod();

        var pos = new BlockPos(10, 64, 10);
        SetupHelveAdjacent(mockAccessor, pos, present: false);

        // Null outputPreview → defaults to FavorMidTier (10)
        method.Invoke(tracker, new object?[] { "player-anvil-1", pos, null });

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-anvil-1"),
            "smithing",
            10), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithHelveHammer_AppliesPenalty()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var (mockWorld, mockAccessor) = SetupWorld(mockSapi);
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var player = TestFixtures.CreateMockServerPlayer("player-anvil-2", "Hammerer");
        SetupPlayer(mockWorld, player.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-anvil-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-anvil-2", DeityType.Khoras));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();
        var method = GetHandleMethod();

        var pos = new BlockPos(20, 70, 20);
        SetupHelveAdjacent(mockAccessor, pos, present: true);

        // Null outputPreview → base 10, helve penalty 35% → floor(6.5)=6
        method.Invoke(tracker, new object?[] { "player-anvil-2", pos, null });

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-anvil-2"),
            "smithing",
            6), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_NonKhorasFollower_NoFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var (mockWorld, mockAccessor) = SetupWorld(mockSapi);
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var player = TestFixtures.CreateMockServerPlayer("player-anvil-3", "Other");
        SetupPlayer(mockWorld, player.Object);

        // Player follows Lysa
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-anvil-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-anvil-3", DeityType.Lysa));

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();
        var method = GetHandleMethod();

        var pos = new BlockPos(0, 0, 0);
        SetupHelveAdjacent(mockAccessor, pos, present: false);

        method.Invoke(tracker, new object?[] { "player-anvil-3", pos, null });

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        tracker.Dispose();
    }
}