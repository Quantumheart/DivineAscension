using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Blocks;
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
public class MiningFavorTrackerTests : IDisposable
{
    public MiningFavorTrackerTests()
    {
        // Clear any leftover subscribers from previous tests
        BlockBehaviorOre.ClearSubscribers();
    }

    public void Dispose()
    {
        // Clean up subscribers after each test
        BlockBehaviorOre.ClearSubscribers();
    }

    private static MiningFavorTracker CreateTracker(
        IWorldService worldService,
        Mock<IPlayerProgressionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        var mockLogger = new Mock<ILoggerWrapper>();
        return new MiningFavorTracker(mockLogger.Object, worldService, mockPlayerReligion.Object, mockFavor.Object);
    }

    [Fact]
    public void OnOreBlockBroken_WhenPlayerNotFollowingCraft_AwardsNoFavor()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-4", "NotKhoras");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Wild (not Craft)
        mockPlayerReligion.Setup(m => m.GetOrCreatePlayerData("player-4"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-4", DeityDomain.Wild));
        mockPlayerReligion.Setup(m => m.GetPlayerDeityType("player-4"))
            .Returns(DeityDomain.Wild);

        // Ore block would normally grant favor, but since player doesn't follow Craft, expect none
        var block = new Block { Code = new AssetLocation("game", "ore-medium-tin") };
        fakeWorldService.SetBlock(new BlockPos(0, 0, 0), block);

        var tracker = CreateTracker(fakeWorldService, mockPlayerReligion, mockFavor);
        tracker.Initialize();

        // Trigger the ore block broken event
        var mockWorld = new Mock<IWorldAccessor>();
        BlockBehaviorOre.TriggerOreBlockBroken(mockWorld.Object, new BlockPos(0, 0, 0), mockPlayer.Object, block, EnumHandling.PassThrough);

        mockFavor.Verify(m => m.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);

        tracker.Dispose();
    }


    [Fact]
    public void OnOreBlockBroken_WhenCopperOre_Awards1Favor()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Craft domain
        mockPlayerProgression.Setup(m => m.GetOrCreatePlayerData("player-1"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft));
        mockPlayerProgression.Setup(d => d.GetPlayerDeityType("player-1"))
            .Returns(DeityDomain.Craft);

        // Copper ore block (low tier = 1, poor quality = 1.0x, total = 1)
        var block = new Block { Code = new AssetLocation("game", "ore-poor-copper") };
        fakeWorldService.SetBlock(new BlockPos(0, 0, 0), block);

        var tracker = CreateTracker(fakeWorldService, mockPlayerProgression, mockFavor);
        tracker.Initialize();

        // Trigger the ore block broken event
        var mockWorld = new Mock<IWorldAccessor>();
        BlockBehaviorOre.TriggerOreBlockBroken(mockWorld.Object, new BlockPos(0, 0, 0), mockPlayer.Object, block, EnumHandling.PassThrough);

        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-1"),
            "mining ore",
            1), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void OnOreBlockBroken_WhenIronOre_AwardsEliteTierFavor()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-3", "IronMiner");

        fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Craft domain
        mockPlayerProgression.Setup(m => m.GetOrCreatePlayerData("player-3"))
            .Returns(TestFixtures.CreateTestPlayerReligionData("player-3", DeityDomain.Craft));
        mockPlayerProgression.Setup(d => d.GetPlayerDeityType("player-3"))
            .Returns(DeityDomain.Craft);

        // Iron ore block (elite tier = 4, medium quality = 1.25x, total = 5)
        var block = new Block { Code = new AssetLocation("game", "ore-medium-hematite-granite") };
        fakeWorldService.SetBlock(new BlockPos(0, 0, 0), block);

        var tracker = CreateTracker(fakeWorldService, mockPlayerProgression, mockFavor);
        tracker.Initialize();

        // Trigger the ore block broken event
        var mockWorld = new Mock<IWorldAccessor>();
        BlockBehaviorOre.TriggerOreBlockBroken(mockWorld.Object, new BlockPos(0, 0, 0), mockPlayer.Object, block, EnumHandling.PassThrough);

        // Elite tier (4) * medium quality (1.25) = 5
        mockFavor.Verify(m => m.AwardFavorForAction(
            It.Is<IServerPlayer>(p => p.PlayerUID == "player-3"),
            "mining ore",
            5), Times.Once);

        tracker.Dispose();
    }

    [Fact]
    public void GetMineralTierFavor_ReturnsCorrectTierForOreTypes()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(fakeWorldService, mockPlayerProgression, mockFavor);

        // Low tier (copper, tin, bismuth)
        var copperBlock = new Block { Code = new AssetLocation("game", "ore-poor-nativecopper-granite") };
        Assert.Equal(1, tracker.GetMineralTierFavor(copperBlock));

        // Mid tier (galena, sphalerite)
        var galenaBlock = new Block { Code = new AssetLocation("game", "ore-medium-galena-claystone") };
        Assert.Equal(2, tracker.GetMineralTierFavor(galenaBlock));

        // High tier (silver, gold)
        var silverBlock = new Block { Code = new AssetLocation("game", "ore-rich-nativesilver-granite") };
        Assert.Equal(3, tracker.GetMineralTierFavor(silverBlock));

        // Elite tier (iron ores)
        var ironBlock = new Block { Code = new AssetLocation("game", "ore-bountiful-hematite-peridotite") };
        Assert.Equal(4, tracker.GetMineralTierFavor(ironBlock));

        // Super elite tier (titanium, nickel, chromium)
        var titaniumBlock = new Block { Code = new AssetLocation("game", "ore-poor-ilmenite-basalt") };
        Assert.Equal(5, tracker.GetMineralTierFavor(titaniumBlock));

        tracker.Dispose();
    }

    [Fact]
    public void GetOreQualityMultiplier_ReturnsCorrectMultiplierForQualityGrades()
    {
        var fakeWorldService = new FakeWorldService();
        var mockPlayerProgression = TestFixtures.CreateMockPlayerProgressionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();

        var tracker = CreateTracker(fakeWorldService, mockPlayerProgression, mockFavor);

        var poorBlock = new Block { Code = new AssetLocation("game", "ore-poor-copper-granite") };
        Assert.Equal(1.0f, tracker.GetOreQualityMultiplier(poorBlock));

        var mediumBlock = new Block { Code = new AssetLocation("game", "ore-medium-copper-granite") };
        Assert.Equal(1.25f, tracker.GetOreQualityMultiplier(mediumBlock));

        var richBlock = new Block { Code = new AssetLocation("game", "ore-rich-copper-granite") };
        Assert.Equal(1.5f, tracker.GetOreQualityMultiplier(richBlock));

        var bountifulBlock = new Block { Code = new AssetLocation("game", "ore-bountiful-copper-granite") };
        Assert.Equal(2.0f, tracker.GetOreQualityMultiplier(bountifulBlock));

        tracker.Dispose();
    }
}
