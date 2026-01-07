using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Favor;

/// <summary>
/// Tests for the FavorCommands class registration and initialization
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandsTests : FavorCommandsTestHelpers
{
    public FavorCommandsTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Command Registration Tests

    [Fact]
    public void RegisterCommands_ExecutesWithoutException()
    {
        // Arrange
        var mockCommandBuilder = new Mock<IChatCommand>();
        mockCommandBuilder.Setup(c => c.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.RequiresPlayer()).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.RequiresPrivilege(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.BeginSubCommand(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.WithArgs(It.IsAny<ICommandArgumentParser[]>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.HandleWith(It.IsAny<OnCommandDelegate>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.EndSubCommand()).Returns(mockCommandBuilder.Object);

        _mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        var realParsers = new CommandArgumentParsers(_mockSapi.Object);
        _mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Act
        var exception = Record.Exception(() => _sut!.RegisterCommands());

        // Assert
        Assert.Null(exception);
        _mockChatCommands.Verify(c => c.Create("favor"), Times.Once);
        _mockLogger.Verify(l => l.Notification(It.Is<string>(s => s.Contains("Favor commands registered"))),
            Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var favorCommands = InitializeMocksAndSut();

        // Assert
        Assert.NotNull(favorCommands);
    }

    [Fact]
    public void Constructor_WithNullSapi_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FavorCommands(
            null!,
            _deityRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object));
    }

    [Fact]
    public void Constructor_WithNullDeityRegistry_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FavorCommands(
            _mockSapi.Object,
            null!,
            _playerReligionDataManager.Object,
            _religionManager.Object));
    }

    [Fact]
    public void Constructor_WithNullPlayerReligionDataManager_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FavorCommands(
            _mockSapi.Object,
            _deityRegistry.Object,
            null!,
            _religionManager.Object));
    }

    [Fact]
    public void Constructor_WithNullReligionDataManager_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FavorCommands(
            _mockSapi.Object,
            _deityRegistry.Object,
            _playerReligionDataManager.Object,
            null!));
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void GetCurrentFavorRank_WithZeroFavor_ReturnsInitiate()
    {
        // This test validates the GetCurrentFavorRank helper method behavior
        // Since it's private, we test it indirectly through commands that use it

        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", favor: 0, totalFavor: 0, rank: FavorRank.Initiate);
        var args = CreateCommandArgs(mockPlayer.Object);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras))
            .Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);
        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Initiate", result.StatusMessage);
    }

    [Fact]
    public void GetCurrentFavorRank_With600TotalFavor_ReturnsDisciple()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", favor: 600, totalFavor: 600, rank: FavorRank.Disciple);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras))
            .Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);
        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Disciple", result.StatusMessage);
    }

    [Fact]
    public void GetCurrentFavorRank_With3000TotalFavor_ReturnsZealot()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", favor: 3000, totalFavor: 3000, rank: FavorRank.Zealot);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras))
            .Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);
        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Zealot", result.StatusMessage);
    }

    [Fact]
    public void GetCurrentFavorRank_With7000TotalFavor_ReturnsChampion()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", favor: 7000, totalFavor: 7000, rank: FavorRank.Champion);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras))
            .Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Champion", result.StatusMessage);
    }

    [Fact]
    public void GetCurrentFavorRank_With15000TotalFavor_ReturnsAvatar()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", favor: 15000, totalFavor: 15000, rank: FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras))
            .Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Avatar", result.StatusMessage);
    }

    #endregion
}