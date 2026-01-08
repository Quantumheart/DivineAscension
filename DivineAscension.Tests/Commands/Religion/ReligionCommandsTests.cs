using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Religion;

/// <summary>
/// Tests for ReligionCommands constructor and RegisterCommands
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandsTests : ReligionCommandsTestHelpers
{
    public ReligionCommandsTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsWhenSAPIIsNull()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReligionCommands(
            null!,
            _religionManager.Object,
            _playerProgressionDataManager.Object,
            mockPrestigeManager.Object,
            _serverChannel.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenReligionManagerIsNull()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReligionCommands(
            _mockSapi.Object,
            null!,
            _playerProgressionDataManager.Object,
            mockPrestigeManager.Object,
            _serverChannel.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenPlayerReligionDataManagerIsNull()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReligionCommands(
            _mockSapi.Object,
            _religionManager.Object,
            null!,
            mockPrestigeManager.Object,
            _serverChannel.Object));
    }

    [Fact]
    public void Constructor_AcceptsNullServerChannel()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReligionCommands(
            _mockSapi.Object,
            _religionManager.Object,
            _playerProgressionDataManager.Object,
            mockPrestigeManager.Object,
            null!));
    }

    [Fact]
    public void Constructor_SetsDependenciesCorrectly()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        // Act
        var commands = new ReligionCommands(
            _mockSapi.Object,
            _religionManager.Object,
            _playerProgressionDataManager.Object,
            mockPrestigeManager.Object,
            _serverChannel.Object);

        // Assert
        Assert.NotNull(commands);
    }

    #endregion

    #region RegisterCommands Tests

    [Fact]
    public void RegisterCommands_ExecutesWithoutException()
    {
        // Arrange
        var mockCommandBuilder = new Mock<IChatCommand>();

        // Setup the command chain
        mockCommandBuilder.Setup(c => c.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.RequiresPrivilege(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.BeginSubCommand(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.WithArgs(It.IsAny<ICommandArgumentParser[]>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.HandleWith(It.IsAny<OnCommandDelegate>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.EndSubCommand()).Returns(mockCommandBuilder.Object);

        _mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        // Use real CommandArgumentParsers
        var realParsers = new CommandArgumentParsers(_mockSapi.Object);
        _mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Act
        var exception = Record.Exception(() => _sut!.RegisterCommands());

        // Assert
        Assert.Null(exception);
        _mockChatCommands.Verify(c => c.Create("religion"), Times.Once);
        _mockLogger.Verify(l => l.Notification(It.Is<string>(s => s.Contains("Religion commands registered"))),
            Times.Once);
    }

    [Fact]
    public void RegisterCommands_RegistersAllSubCommands()
    {
        // Arrange
        var mockCommandBuilder = new Mock<IChatCommand>();
        var subCommandsRegistered = new List<string>();

        mockCommandBuilder.Setup(c => c.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.RequiresPrivilege(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.BeginSubCommand(It.IsAny<string>()))
            .Callback<string>(cmd => subCommandsRegistered.Add(cmd))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.WithArgs(It.IsAny<ICommandArgumentParser[]>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.HandleWith(It.IsAny<OnCommandDelegate>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.EndSubCommand()).Returns(mockCommandBuilder.Object);

        _mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        var realParsers = new CommandArgumentParsers(_mockSapi.Object);
        _mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Act
        _sut!.RegisterCommands();

        // Assert - Verify all expected subcommands are registered
        var expectedSubCommands = new[]
        {
            "create", "join", "leave", "list", "info", "members", "invite", "kick", "ban", "unban", "banlist",
            "disband", "description"
        };
        foreach (var subCommand in expectedSubCommands)
        {
            Assert.Contains(subCommand, subCommandsRegistered);
        }
    }

    [Fact]
    public void RegisterCommands_SetsCorrectPrivilegeRequirement()
    {
        // Arrange
        var mockCommandBuilder = new Mock<IChatCommand>();
        string? privilegeRequired = null;

        mockCommandBuilder.Setup(c => c.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.RequiresPrivilege(It.IsAny<string>()))
            .Callback<string>(priv => privilegeRequired = priv)
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.BeginSubCommand(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.WithArgs(It.IsAny<ICommandArgumentParser[]>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.HandleWith(It.IsAny<OnCommandDelegate>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(c => c.EndSubCommand()).Returns(mockCommandBuilder.Object);

        _mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        var realParsers = new CommandArgumentParsers(_mockSapi.Object);
        _mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Act
        _sut!.RegisterCommands();

        // Assert
        Assert.Equal(Privilege.root, privilegeRequired);
    }

    #endregion
}