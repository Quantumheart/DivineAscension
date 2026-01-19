using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.Systems;

public class AltarPlacementHandlerTests
{
    private readonly FakeEventService _eventService;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly FakeWorldService _worldService;
    private readonly SpyPlayerMessenger _messenger;
    private readonly Mock<ILogger> _logger;
    private readonly AltarPlacementHandler _handler;

    public AltarPlacementHandlerTests()
    {
        _eventService = new FakeEventService();
        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _worldService = new FakeWorldService();
        _messenger = new SpyPlayerMessenger();
        _logger = new Mock<ILogger>();

        _handler = new AltarPlacementHandler(
            _logger.Object,
            _eventService,
            _holySiteManager.Object,
            _religionManager.Object,
            _worldService,
            _messenger);

        _handler.Initialize();
    }

    [Fact]
    public void AltarPlaced_WithReligionAndLandClaim_CreatesHolySite()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        // Setup land claim
        var claim = new LandClaim
        {
            OwnedByPlayerUid = "player1",
            Areas = new List<Cuboidi> { new Cuboidi(90, 40, 90, 110, 60, 110) }
        };
        _worldService.AddLandClaim(altarPos, new[] { claim });

        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);
        _holySiteManager.Setup(x => x.GetReligionHolySites("rel1")).Returns(new List<HolySiteData>());

        var createdSite = new HolySiteData("site1", "rel1", "Test Religion - Site 1",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1", "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.ConsecrateHolySiteWithAltar(
            "rel1", "Test Religion - Site 1", It.IsAny<List<Cuboidi>>(), "player1", "TestPlayer", altarPos))
            .Returns(createdSite);

        // Act
        _eventService.TriggerDidPlaceBlock(player, blockSel, null, 0);

        // Assert
        _holySiteManager.Verify(x => x.ConsecrateHolySiteWithAltar(
            "rel1", "Test Religion - Site 1", It.IsAny<List<Cuboidi>>(), "player1", "TestPlayer", altarPos), Times.Once);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("consecrated", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void AltarPlaced_NoReligion_AllowsPlacementButNoSite()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns((ReligionData?)null);

        // Act
        _eventService.TriggerDidPlaceBlock(player, blockSel, null, 0);

        // Assert
        _holySiteManager.Verify(x => x.ConsecrateHolySiteWithAltar(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Cuboidi>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BlockPos>()), Times.Never);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("must be in a religion", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void AltarPlaced_NoLandClaim_AllowsPlacementButWarns()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);
        // No land claim set

        // Act
        _eventService.TriggerDidPlaceBlock(player, blockSel, null, 0);

        // Assert
        _holySiteManager.Verify(x => x.ConsecrateHolySiteWithAltar(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Cuboidi>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BlockPos>()), Times.Never);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("no land claim detected", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void AltarPlaced_NotOwnedClaim_RejectsHolySiteCreation()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        // Setup land claim owned by different player
        var claim = new LandClaim
        {
            OwnedByPlayerUid = "player2",
            Areas = new List<Cuboidi> { new Cuboidi(90, 40, 90, 110, 60, 110) }
        };
        _worldService.AddLandClaim(altarPos, new[] { claim });

        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);

        // Act
        _eventService.TriggerDidPlaceBlock(player, blockSel, null, 0);

        // Assert
        _holySiteManager.Verify(x => x.ConsecrateHolySiteWithAltar(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Cuboidi>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BlockPos>()), Times.Never);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("your own land claims", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void NonAltarBlock_DoesNothing()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var pos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = pos };

        var regularBlock = new Block();
        SetBlockCode(regularBlock, "stone");
        _worldService.SetBlock(pos, regularBlock);

        // Act
        _eventService.TriggerDidPlaceBlock(player, blockSel, null, 0);

        // Assert
        _holySiteManager.Verify(x => x.ConsecrateHolySiteWithAltar(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Cuboidi>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BlockPos>()), Times.Never);

        Assert.Empty(_messenger.SentMessages);
    }

    [Fact]
    public void Initialize_SubscribesToDidPlaceBlockEvent()
    {
        // Arrange & Act done in constructor

        // Assert
        Assert.True(_eventService.HasDidPlaceBlockSubscribers());
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Act
        _handler.Dispose();

        // Assert
        Assert.False(_eventService.HasDidPlaceBlockSubscribers());
    }

    private void SetBlockCode(Block block, string path)
    {
        var codeField = typeof(Block).GetField("code", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var code = new AssetLocation("game", path);
        codeField?.SetValue(block, code);
    }
}
