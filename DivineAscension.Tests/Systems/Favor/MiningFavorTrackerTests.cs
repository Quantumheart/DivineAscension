using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
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
        IWorldService worldService,
        Mock<IPlayerProgressionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        var mockLogger = new Mock<ILogger>();
        return new MiningFavorTracker(mockLogger.Object, worldService, mockPlayerReligion.Object, mockFavor.Object);
    }

    [Fact]
    public void OnBlockBroken_WhenPlayerNotFollowingKhoras_AwardsNoFavor()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-4", "NotKhoras");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Lysa (not Khoras/Craft)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-4"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-4", DeityDomain.Wild));
        mockPlayerReligion.Setup(m => m.GetPlayerDeityType("player-4"))
            .Returns(DeityDomain.Wild);

        // Ore block would normally grant favor, but since player doesn't follow Craft, expect none
        var block = new Block { Code = new AssetLocation("game", "ore-medium-tin") };
        fakeWorldService.SetBlock(new BlockPos(0, 0, 0), block);

        var tracker = CreateTracker(fakeWorldService, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        float dropMult = 1f;
        EnumHandling handling = EnumHandling.PassThrough;
        var selection = new BlockSelection { Position = new BlockPos(0, 0, 0) };
        tracker.OnBlockBroken(mockPlayer.Object, selection, ref dropMult, ref handling);

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        tracker.Dispose();
    }


    [Fact]
    public void OnBlockBroken_WhenCopperOre_Awards1Favor()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Craft (Khoras domain)
        mockPlayerProgression.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft));
        mockPlayerProgression.Setup(d => d.GetPlayerDeityType("player-1"))
            .Returns(DeityDomain.Craft);

        // Copper ore block (low tier = 1, poor quality = 1.0x, total = 1)
        var block = new Block { Code = new AssetLocation("game", "ore-poor-copper") };
        fakeWorldService.SetBlock(new BlockPos(0, 0, 0), block);

        var tracker = CreateTracker(fakeWorldService, mockPlayerProgression, mockFavor);
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
        var fakeWorldService = new FakeWorldService();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-2", "StoneTester");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-2"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-2", DeityDomain.Craft));
        mockPlayerReligion.Setup(m => m.GetPlayerDeityType("player-2"))
            .Returns(DeityDomain.Craft);

        // Non-ore block path
        var block = new Block { Code = new AssetLocation("game", "stone-granite") };
        fakeWorldService.SetBlock(new BlockPos(0, 0, 0), block);

        var tracker = CreateTracker(fakeWorldService, mockPlayerReligion, mockFavor);
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