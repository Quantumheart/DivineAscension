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
public class AnvilFavorTrackerTests
{
    private readonly FakeWorldService _fakeWorldService;
    private readonly Mock<IFavorSystem> _mockFavorSystem;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerProgressionDataManager;

    public AnvilFavorTrackerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _fakeWorldService = new FakeWorldService();
        _mockPlayerProgressionDataManager = new Mock<IPlayerProgressionDataManager>();
        _mockFavorSystem = new Mock<IFavorSystem>();
    }

    private AnvilFavorTracker CreateTracker()
    {
        return new AnvilFavorTracker(
            _mockLogger.Object,
            _fakeWorldService,
            _mockPlayerProgressionDataManager.Object,
            _mockFavorSystem.Object
        );
    }

    #region Initialize and Dispose Tests

    [Fact]
    public void Initialize_DoesNotThrow()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act & Assert
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        // Act & Assert
        var exception = Record.Exception(() => tracker.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        // Act & Assert - Should not throw on multiple disposes
        tracker.Dispose();
        var exception = Record.Exception(() => tracker.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void DeityDomain_ReturnsCraft()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act & Assert
        Assert.Equal(DeityDomain.Craft, tracker.DeityDomain);
    }

    #endregion

    #region HandleAnvilRecipeCompleted Tests

    [Fact]
    public void HandleAnvilRecipeCompleted_WithNullPlayerUid_DoesNotAwardFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();
        var pos = new BlockPos(0, 0, 0);

        // Act
        tracker.HandleAnvilRecipeCompleted(null, pos, null);

        // Assert
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithEmptyPlayerUid_DoesNotAwardFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();
        var pos = new BlockPos(0, 0, 0);

        // Act
        tracker.HandleAnvilRecipeCompleted(string.Empty, pos, null);

        // Assert
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithPlayerNotFound_DoesNotAwardFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();
        var pos = new BlockPos(0, 0, 0);

        // World service returns null for player
        // (FakeWorldService returns null by default if player not added)

        // Act
        tracker.HandleAnvilRecipeCompleted("unknown-player", pos, null);

        // Assert
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithNonCraftDeity_DoesNotAwardFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        // Player follows Wild deity, not Craft
        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Wild);

        var pos = new BlockPos(0, 0, 0);

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", pos, null);

        // Assert
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithCraftDeityAndCopperItem_AwardsLowTierFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Craft);

        // Create copper item
        var itemStack = CreateTestItemStack("ingot-copper");

        var pos = new BlockPos(0, 0, 0);
        _fakeWorldService.SetBlock(pos, CreateMockBlock("anvil"));

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", pos, itemStack);

        // Assert - Should award 5 favor (low tier)
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(mockPlayer.Object, "smithing", 5),
            Times.Once
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithCraftDeityAndBronzeItem_AwardsMidTierFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Craft);

        // Create bronze item
        var itemStack = CreateTestItemStack("ingot-bronze");

        var pos = new BlockPos(0, 0, 0);
        _fakeWorldService.SetBlock(pos, CreateMockBlock("anvil"));

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", pos, itemStack);

        // Assert - Should award 10 favor (mid tier)
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(mockPlayer.Object, "smithing", 10),
            Times.Once
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithCraftDeityAndIronItem_AwardsHighTierFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Craft);

        // Create iron item
        var itemStack = CreateTestItemStack("ingot-iron");

        var pos = new BlockPos(0, 0, 0);
        _fakeWorldService.SetBlock(pos, CreateMockBlock("anvil"));

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", pos, itemStack);

        // Assert - Should award 15 favor (high tier)
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(mockPlayer.Object, "smithing", 15),
            Times.Once
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithCraftDeityAndSteelItem_AwardsEliteTierFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Craft);

        // Create steel item
        var itemStack = CreateTestItemStack("ingot-steel");

        var pos = new BlockPos(0, 0, 0);
        _fakeWorldService.SetBlock(pos, CreateMockBlock("anvil"));

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", pos, itemStack);

        // Assert - Should award 20 favor (elite tier)
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(mockPlayer.Object, "smithing", 20),
            Times.Once
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithNullOutput_AwardsMidTierFavor()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Craft);

        var pos = new BlockPos(0, 0, 0);
        _fakeWorldService.SetBlock(pos, CreateMockBlock("anvil"));

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", pos, null);

        // Assert - Should award 10 favor (mid tier default)
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(mockPlayer.Object, "smithing", 10),
            Times.Once
        );

        // Cleanup
        tracker.Dispose();
    }

    [Fact]
    public void HandleAnvilRecipeCompleted_WithHelveHammer_AppliesPenalty()
    {
        // Arrange
        var tracker = CreateTracker();
        tracker.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        _fakeWorldService.AddPlayer(mockPlayer.Object);

        _mockPlayerProgressionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityDomain.Craft);

        // Create steel item (base 20 favor)
        var itemStack = CreateTestItemStack("ingot-steel");

        var anvilPos = new BlockPos(10, 5, 10);
        _fakeWorldService.SetBlock(anvilPos, CreateMockBlock("anvil"));

        // Place helve hammer adjacent to anvil (north face)
        var helvePos = anvilPos.AddCopy(BlockFacing.NORTH);
        _fakeWorldService.SetBlock(helvePos, CreateMockBlock("helvehammer"));

        // Act
        tracker.HandleAnvilRecipeCompleted("player-uid", anvilPos, itemStack);

        // Assert - Should award 13 favor (20 * 0.65 = 13)
        _mockFavorSystem.Verify(
            f => f.AwardFavorForAction(mockPlayer.Object, "smithing", 13),
            Times.Once
        );

        // Cleanup
        tracker.Dispose();
    }

    #endregion

    #region CheckHelveHammerUsage Tests

    [Fact]
    public void CheckHelveHammerUsage_WithNoHelveHammer_ReturnsFalse()
    {
        // Arrange
        var tracker = CreateTracker();
        var anvilPos = new BlockPos(0, 0, 0);

        // Set anvil block
        _fakeWorldService.SetBlock(anvilPos, CreateMockBlock("anvil"));

        // Set non-helve blocks around anvil
        foreach (var face in BlockFacing.ALLFACES)
        {
            var adjacentPos = anvilPos.AddCopy(face);
            _fakeWorldService.SetBlock(adjacentPos, CreateMockBlock("stone"));
        }

        // Act
        var result = tracker.CheckHelveHammerUsage(anvilPos);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckHelveHammerUsage_WithHelveHammerNorth_ReturnsTrue()
    {
        // Arrange
        var tracker = CreateTracker();
        var anvilPos = new BlockPos(0, 0, 0);

        _fakeWorldService.SetBlock(anvilPos, CreateMockBlock("anvil"));

        // Place helve hammer to the north
        var helvePos = anvilPos.AddCopy(BlockFacing.NORTH);
        _fakeWorldService.SetBlock(helvePos, CreateMockBlock("helvehammer"));

        // Act
        var result = tracker.CheckHelveHammerUsage(anvilPos);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CheckHelveHammerUsage_WithHelveHammerEast_ReturnsTrue()
    {
        // Arrange
        var tracker = CreateTracker();
        var anvilPos = new BlockPos(0, 0, 0);

        _fakeWorldService.SetBlock(anvilPos, CreateMockBlock("anvil"));

        // Place helve hammer to the east
        var helvePos = anvilPos.AddCopy(BlockFacing.EAST);
        _fakeWorldService.SetBlock(helvePos, CreateMockBlock("helvehammer"));

        // Act
        var result = tracker.CheckHelveHammerUsage(anvilPos);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CheckHelveHammerUsage_WithHelveHammerUp_ReturnsTrue()
    {
        // Arrange
        var tracker = CreateTracker();
        var anvilPos = new BlockPos(0, 0, 0);

        _fakeWorldService.SetBlock(anvilPos, CreateMockBlock("anvil"));

        // Place helve hammer above
        var helvePos = anvilPos.AddCopy(BlockFacing.UP);
        _fakeWorldService.SetBlock(helvePos, CreateMockBlock("helvehammer"));

        // Act
        var result = tracker.CheckHelveHammerUsage(anvilPos);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Helper Methods

    private Block CreateMockBlock(string code)
    {
        return new Block
        {
            Code = new AssetLocation("game", code)
        };
    }

    private ItemStack CreateTestItemStack(string collectibleCode)
    {
        var item = new Item
        {
            Code = new AssetLocation("game", collectibleCode)
        };
        return new ItemStack(item);
    }

    #endregion
}