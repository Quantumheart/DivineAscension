using System.Diagnostics.CodeAnalysis;
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
        Mock<IPlayerProgressionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        return new MiningFavorTracker(mockPlayerReligion.Object, mockSapi.Object, mockFavor.Object);
    }

    [Fact]
    public void OnBlockBroken_WhenPlayerNotFollowingKhoras_AwardsNoFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-4", "NotKhoras");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        // Player follows Lysa (not Khoras)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-4"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-4", DeityDomain.Wild));

        // Ore block would normally grant favor, but since player doesn't follow Khoras, expect none
        SetupBlockAt(mockAccessor, "ore-medium-tin");

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        tracker.OnBlockBroken(mockPlayer.Object, selection, ref dropMult, ref handling);

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
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker2 = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Ensure player follows Khoras
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(TestFixtures.CreateTestPlayerReligionData(player.PlayerUID, DeityDomain.Craft));

        tracker2.Initialize();

        // Call the internal handler directly
        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        tracker2.OnBlockBroken(player, selection, ref dropMult, ref handling);

        // Dispose to unhook events
        tracker2.Dispose();
    }

    [Fact]
    public void OnBlockBroken_WhenCopperOre_Awards1Favor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockReligionManager = TestFixtures.CreateMockReligionManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");

        // Wiring sapi
        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        // Player follows Khoras
        mockPlayerProgression.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft));
        mockPlayerProgression.Setup(d => d.GetPlayerDeityType("player-1"))
            .Returns(DeityDomain.Craft);

        // Copper ore block (low tier = 1, poor quality = 1.0x, total = 1)
        SetupBlockAt(mockAccessor, "ore-poor-copper");

        var tracker = CreateTracker(mockSapi, mockPlayerProgression, mockFavor);
        tracker.Initialize();

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        tracker.OnBlockBroken(mockPlayer.Object, selection, ref dropMult, ref handling);

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-1"),
            "mining ore",
            1), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void OnBlockBroken_WhenStone_AwardsNoFavor()
    {
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockAccessor = new Mock<IBlockAccessor>();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-2", "StoneTester");

        mockSapi.Setup(s => s.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.BlockAccessor).Returns(mockAccessor.Object);
        SetupOnlinePlayer(mockWorld, mockPlayer.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-2", DeityDomain.Craft));

        // Non-ore block path
        SetupBlockAt(mockAccessor, "stone-granite");

        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        tracker.OnBlockBroken(mockPlayer.Object, selection, ref dropMult, ref handling);

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        tracker.Dispose();
    }
}