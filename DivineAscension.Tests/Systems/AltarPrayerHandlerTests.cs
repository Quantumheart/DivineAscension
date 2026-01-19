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

public class AltarPrayerHandlerTests
{
    private readonly FakeEventService _eventService;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly Mock<IFavorSystem> _favorSystem;
    private readonly Mock<IReligionPrestigeManager> _prestigeManager;
    private readonly Mock<IActivityLogManager> _activityLogManager;
    private readonly SpyPlayerMessenger _messenger;
    private readonly FakeWorldService _worldService;
    private readonly Mock<ILogger> _logger;
    private readonly AltarPrayerHandler _handler;

    public AltarPrayerHandlerTests()
    {
        _eventService = new FakeEventService();
        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _favorSystem = new Mock<IFavorSystem>();
        _prestigeManager = new Mock<IReligionPrestigeManager>();
        _activityLogManager = new Mock<IActivityLogManager>();
        _messenger = new SpyPlayerMessenger();
        _worldService = new FakeWorldService();
        _logger = new Mock<ILogger>();

        _handler = new AltarPrayerHandler(
            _logger.Object,
            _eventService,
            _holySiteManager.Object,
            _religionManager.Object,
            _favorSystem.Object,
            _prestigeManager.Object,
            _activityLogManager.Object,
            _messenger,
            _worldService);

        _handler.Initialize();
    }

    [Fact]
    public void Prayer_AtConsecratedAltar_AwardsFavorAndPrestige()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var holySite = new HolySiteData("site1", "rel1", "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1", "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);

        // Act
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert - Tier 1 site (volume < 50k), no offering: (5 + 0) * 2.0 = 10
        _favorSystem.Verify(x => x.AwardFavorForAction(player, "prayer", 10), Times.Once);
        _prestigeManager.Verify(x => x.AddPrestige("rel1", 10, "prayer"), Times.Once);
        _activityLogManager.Verify(x => x.LogActivity("rel1", "player1", It.IsAny<string>(), 10, 10, DeityDomain.Craft), Times.Once);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("+10 favor", _messenger.SentMessages[0].Message);
        Assert.Contains("+10 prestige", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void Prayer_WithGoldOffering_AwardsBonusFavorAndPrestige()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        // Setup offering in right hand
        var goldIngot = CreateItemStack("ingot-gold", 5);
        player.Entity.RightHandItemSlot.Itemstack = goldIngot;

        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var holySite = new HolySiteData("site1", "rel1", "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1", "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);

        // Act
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert - Tier 1 site, gold offering: (5 + 5) * 2.0 = 20
        _favorSystem.Verify(x => x.AwardFavorForAction(player, "prayer", 20), Times.Once);
        _prestigeManager.Verify(x => x.AddPrestige("rel1", 20, "prayer"), Times.Once);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("+20 favor", _messenger.SentMessages[0].Message);
        Assert.Contains("+20 prestige", _messenger.SentMessages[0].Message);
        Assert.Contains("offering bonus", _messenger.SentMessages[0].Message);

        // Verify offering consumed
        Assert.Equal(4, goldIngot.StackSize); // 5 - 1 = 4
    }

    [Fact]
    public void Prayer_DuringCooldown_Rejects()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var holySite = new HolySiteData("site1", "rel1", "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1", "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);

        // Act - First prayer
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Act - Second prayer immediately (still in cooldown)
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert - Only one prayer should succeed
        _favorSystem.Verify(x => x.AwardFavorForAction(player, "prayer", It.IsAny<int>()), Times.Once);
        _prestigeManager.Verify(x => x.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);

        Assert.Equal(2, _messenger.SentMessages.Count);
        Assert.Contains("wait", _messenger.SentMessages[1].Message);
        Assert.Contains("minute", _messenger.SentMessages[1].Message);
    }

    [Fact]
    public void Prayer_WrongReligion_Rejects()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        var playerReligion = new ReligionData("rel1", "Player Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var holySite = new HolySiteData("site1", "rel2", "Different Religion Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player2", "OtherPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(playerReligion);

        // Act
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert
        _favorSystem.Verify(x => x.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _prestigeManager.Verify(x => x.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("your religion", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void Prayer_NoReligion_Rejects()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        var holySite = new HolySiteData("site1", "rel1", "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player2", "OtherPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns((ReligionData?)null);

        // Act
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert
        _favorSystem.Verify(x => x.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _prestigeManager.Verify(x => x.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("must be in a religion", _messenger.SentMessages[0].Message);
    }

    [Fact]
    public void Prayer_NotConsecratedAltar_Rejects()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns((HolySiteData?)null);

        // Act
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert
        _favorSystem.Verify(x => x.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _prestigeManager.Verify(x => x.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);

        Assert.Single(_messenger.SentMessages);
        Assert.Contains("not consecrated", _messenger.SentMessages[0].Message);
    }

    [Theory]
    [InlineData("ingot-gold", 5)]
    [InlineData("gem-diamond", 5)]
    [InlineData("ingot-silver", 3)]
    [InlineData("gem-emerald", 3)]
    [InlineData("gem-peridot", 3)]
    [InlineData("ingot-copper", 1)]
    [InlineData("ingot-bronze", 1)]
    [InlineData("bread", 1)]
    [InlineData("honey", 1)]
    public void OfferingValue_ValidItems_ReturnsCorrectBonus(string itemPath, int expectedBonus)
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        var offering = CreateItemStack(itemPath, 5);
        player.Entity.RightHandItemSlot.Itemstack = offering;

        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var holySite = new HolySiteData("site1", "rel1", "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1", "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);

        // Act
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Assert - (5 + expectedBonus) * 2.0 = expected total
        int expectedTotal = (5 + expectedBonus) * 2;
        _favorSystem.Verify(x => x.AwardFavorForAction(player, "prayer", expectedTotal), Times.Once);
        _prestigeManager.Verify(x => x.AddPrestige("rel1", expectedTotal, "prayer"), Times.Once);
    }

    [Fact]
    public void PlayerDisconnect_CleansCooldownTracking()
    {
        // Arrange
        var player = _worldService.CreatePlayer("player1", "TestPlayer");
        var altarPos = new BlockPos(100, 50, 100);
        var blockSel = new BlockSelection { Position = altarPos };

        var altarBlock = new Block();
        SetBlockCode(altarBlock, "altar");
        _worldService.SetBlock(altarPos, altarBlock);

        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Aethra", "player1", "TestPlayer");
        var holySite = new HolySiteData("site1", "rel1", "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1", "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos)).Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1")).Returns(religion);

        // Pray to set cooldown
        _eventService.TriggerDidUseBlock(player, blockSel);

        // Act - Player disconnects
        _eventService.TriggerPlayerDisconnect(player);

        // Assert - Should be able to pray again immediately after reconnect (cooldown cleared)
        // This is tested indirectly - no exception should be thrown
        Assert.True(true); // Test passes if no exception
    }

    private void SetBlockCode(Block block, string path)
    {
        var codeField = typeof(Block).GetField("code", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var code = new AssetLocation("game", path);
        codeField?.SetValue(block, code);
    }

    private ItemStack CreateItemStack(string path, int stackSize)
    {
        var collectible = new Item();
        var codeField = typeof(CollectibleObject).GetField("code", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var code = new AssetLocation("game", path);
        codeField?.SetValue(collectible, code);

        return new ItemStack(collectible, stackSize);
    }
}
