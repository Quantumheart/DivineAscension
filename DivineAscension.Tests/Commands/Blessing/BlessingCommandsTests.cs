using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Commands;
using PantheonWars.Constants;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandsTests : BlessingCommandsTestHelpers
{
    public BlessingCommandsTests()
    {
        _sut = InitializeMocksAndSut();
    }


    [Fact]
    public void BlessingCommands_Constructor_ThrowsWhenSAPIIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlessingCommands(
            null,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object));
    }

    [Fact]
    public void BlessingCommands_Constructor_ThrowsWhenBlessingRegistryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlessingCommands(
            _mockSapi.Object,
            null,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object));
    }

    [Fact]
    public void BlessingCommands_Constructor_ThrowsWhenPlayerReligionManagerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            null,
            _religionManager.Object,
            _blessingEffectSystem.Object));
    }

    [Fact]
    public void BlessingCommands_Constructor_ThrowsWhenReligionManagerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            null,
            _blessingEffectSystem.Object));
    }

    [Fact]
    public void BlessingCommands_Constructor_ThrowsWhenBlessingEffectSystemIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            null));
    }

    [Fact]
    public void BlessingCommands_Constructor_SetsDependenciesCorrectly()
    {
        // Verify that the constructor injects the dependencies.
        var commands = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        Assert.NotNull(commands);
    }

    [Fact]
    public void RegisterCommands_ExecutesWithoutException()
    {
        // Arrange
        var mockSapi = new Mock<ICoreServerAPI>();
        var mockChatCommands = new Mock<IChatCommandApi>();
        var mockCommandBuilder = new Mock<IChatCommand>();
        var mockLogger = new Mock<ILogger>();

        // Use real CommandArgumentParsers instead of mocking it
        var realParsers = new CommandArgumentParsers(mockSapi.Object);
        mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Setup fluent builder chain - all methods return the builder itself
        mockCommandBuilder.Setup(b => b.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.RequiresPlayer()).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.RequiresPrivilege(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.BeginSubCommand(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.WithArgs(It.IsAny<ICommandArgumentParser>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.HandleWith(It.IsAny<OnCommandDelegate>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.EndSubCommand()).Returns(mockCommandBuilder.Object);

        mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        mockSapi.Setup(s => s.ChatCommands).Returns(mockChatCommands.Object);
        mockSapi.Setup(s => s.Logger).Returns(mockLogger.Object);

        // Create BlessingCommands with properly configured mock
        var blessingCommands = new BlessingCommands(
            mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act & Assert - should not throw any exceptions
        var exception = Record.Exception(() => blessingCommands.RegisterCommands());

        Assert.Null(exception);
        mockChatCommands.Verify(c => c.Create("blessings"), Times.Once);
        mockSapi.Verify(c => c.Logger.Notification(LogMessageConstants.LogBlessingCommandsRegistered), Times.Once);
    }

    [Fact]
    public void RegisterCommands_ConfiguresSubCommandsCorrectly()
    {
        // Arrange
        var mockSapi = new Mock<ICoreServerAPI>();
        var mockChatCommands = new Mock<IChatCommandApi>();
        var mockCommandBuilder = new Mock<IChatCommand>();
        var mockLogger = new Mock<ILogger>();

        // Use real CommandArgumentParsers instead of mocking it
        var realParsers = new CommandArgumentParsers(mockSapi.Object);
        mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Setup fluent builder chain - all methods return the builder itself
        // Each method in the chain must be mocked separately for fluent APIs
        mockCommandBuilder.Setup(b => b.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.RequiresPlayer()).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.RequiresPrivilege(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.BeginSubCommand(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.WithArgs(It.IsAny<ICommandArgumentParser>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.HandleWith(It.IsAny<OnCommandDelegate>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.EndSubCommand()).Returns(mockCommandBuilder.Object);

        mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        mockSapi.Setup(s => s.ChatCommands).Returns(mockChatCommands.Object);
        mockSapi.Setup(s => s.Logger).Returns(mockLogger.Object);

        // Create BlessingCommands with properly configured mock
        var blessingCommands = new BlessingCommands(
            mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act
        blessingCommands.RegisterCommands();

        // Verify subcommand configurations
        mockChatCommands.Verify(c => c.Create(BlessingCommandConstants.CommandName), Times.Once);
        mockCommandBuilder.Verify(b => b.WithDescription(BlessingDescriptionConstants.CommandDescription),
            Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandList), Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandPlayer), Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandReligion), Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandInfo), Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandTree), Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandUnlock), Times.Once);
        mockCommandBuilder.Verify(b => b.BeginSubCommand(BlessingCommandConstants.SubCommandActive), Times.Once);
        mockCommandBuilder.Verify(b => b.EndSubCommand(), Times.Exactly(7)); // 7 subcommands
    }

    [Fact]
    public void ShouldConfigureRequiresPlayerAndPrivilege()
    {
        // Arrange
        var mockSapi = new Mock<ICoreServerAPI>();
        var mockChatCommands = new Mock<IChatCommandApi>();
        var mockCommandBuilder = new Mock<IChatCommand>();
        var mockLogger = new Mock<ILogger>();

        // Use real CommandArgumentParsers instead of mocking it
        var realParsers = new CommandArgumentParsers(mockSapi.Object);
        mockChatCommands.Setup(c => c.Parsers).Returns(realParsers);

        // Setup fluent builder chain - all methods return the builder itself
        mockCommandBuilder.Setup(b => b.WithDescription(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.RequiresPlayer()).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.RequiresPrivilege(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.BeginSubCommand(It.IsAny<string>())).Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.WithArgs(It.IsAny<ICommandArgumentParser>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.HandleWith(It.IsAny<OnCommandDelegate>()))
            .Returns(mockCommandBuilder.Object);
        mockCommandBuilder.Setup(b => b.EndSubCommand()).Returns(mockCommandBuilder.Object);

        mockChatCommands.Setup(c => c.Create(It.IsAny<string>())).Returns(mockCommandBuilder.Object);

        mockSapi.Setup(s => s.ChatCommands).Returns(mockChatCommands.Object);
        mockSapi.Setup(s => s.Logger).Returns(mockLogger.Object);

        // Create BlessingCommands with properly configured mock
        var blessingCommands = new BlessingCommands(
            mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act
        blessingCommands.RegisterCommands();

        // Assert - Verify that RequiresPlayer() and RequiresPrivilege() were called
        mockCommandBuilder.Verify(b => b.RequiresPlayer(), Times.Once,
            "RequiresPlayer() should be called once during command registration");
        mockCommandBuilder.Verify(b => b.RequiresPrivilege(Privilege.chat), Times.Once,
            "RequiresPrivilege(Privilege.chat) should be called once during command registration");
    }
}