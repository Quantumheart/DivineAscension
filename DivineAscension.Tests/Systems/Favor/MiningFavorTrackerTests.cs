using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class MiningFavorTrackerTests
{
    private static MiningFavorTracker CreateTracker(
        Mock<ICoreServerAPI> mockSapi,
        Mock<IPlayerReligionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        return new MiningFavorTracker(mockPlayerReligion.Object, mockSapi.Object, mockFavor.Object);
    }

    [Theory]
    [InlineData("ore-poor-copper")]
    [InlineData("ore-medium-tin")]
    [InlineData("ore-rich-iron")]
    [InlineData("ore-poor-silver")]
    [InlineData("ore-medium-gold")]
    [InlineData("ore-meteorite")]
    public void OnBlockBroken_AllOreTypes_Award2Favor(string oreBlockCode)
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-3", "OreRunner");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-3", DeityType.Khoras));

        SetupBlockAt(mockAccessor, oreBlockCode);

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        var method =
            typeof(MiningFavorTracker).GetMethod("OnBlockBroken", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        method!.Invoke(tracker, new object[] { mockPlayer.Object, selection, dropMult, handling });

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-3"),
            "mining ore",
            2), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void OnBlockBroken_WhenPlayerNotFollowingKhoras_AwardsNoFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-4", "NotKhoras");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        // Player follows Lysa (not Khoras)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-4"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-4", DeityType.Lysa));

        // Ore block would normally grant favor, but since player doesn't follow Khoras, expect none
        SetupBlockAt(mockAccessor, "ore-medium-tin");

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        var method =
            typeof(MiningFavorTracker).GetMethod("OnBlockBroken", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        method!.Invoke(tracker, new object[] { mockPlayer.Object, selection, dropMult, handling });

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        tracker.Dispose();
    }

    private static void SetupOnlinePlayer(Mock<IServerWorldAccessor> mockWorld, IServerPlayer player)
    {
        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new[] { player });
    }

    private static void SetupBlockAt(Mock<IBlockAccessor> mockBlockAccessor, string codePath)
    {
        var block = new Block
        {
            Code = new AssetLocation("game", codePath)
        };
        mockBlockAccessor.Setup(a => a.GetBlock(It.IsAny<BlockPos>())).Returns(block);
    }

    private static void InvokeOnBlockBroken(MiningFavorTracker tracker, IServerPlayer player, string blockCodePath)
    {
        // Build the world/accessor chain to return our block when handler queries it
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        SetupBlockAt(mockAccessor, blockCodePath);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);

        // Replace the ICoreServerAPI.World on a fresh mock and re-initialize tracker for cache
        var mockSapi = TestFixtures.CreateMockServerAPI();
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);

        // We need to refresh followers cache, so emulate player online
        SetupOnlinePlayer(mockWorld, player);

        // Recreate tracker bound to this sapi
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker2 = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Ensure player follows Khoras
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(TestFixtures.CreateTestPlayerReligionData(player.PlayerUID, DeityType.Khoras));

        tracker2.Initialize();

        // Call the internal handler via reflection
        var method =
            typeof(MiningFavorTracker).GetMethod("OnBlockBroken", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        method!.Invoke(tracker2, new object[] { player, selection, dropMult, handling });

        // Dispose to unhook events
        tracker2.Dispose();
    }

    [Fact]
    public void OnBlockBroken_WhenCopperOre_Awards2Favor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");

        // Wiring sapi
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        // Player follows Khoras
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Khoras));

        // Copper ore block
        SetupBlockAt(mockAccessor, "ore-poor-copper");

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        // Invoke handler via reflection
        var method =
            typeof(MiningFavorTracker).GetMethod("OnBlockBroken", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        method!.Invoke(tracker, new object[] { mockPlayer.Object, selection, dropMult, handling });

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-1"),
            "mining ore",
            2), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void OnBlockBroken_WhenStone_AwardsNoFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-2", "StoneTester");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-2", DeityType.Khoras));

        // Non-ore block path
        SetupBlockAt(mockAccessor, "stone-granite");

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        var method =
            typeof(MiningFavorTracker).GetMethod("OnBlockBroken", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        method!.Invoke(tracker, new object[] { mockPlayer.Object, selection, dropMult, handling });

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        tracker.Dispose();
    }
}