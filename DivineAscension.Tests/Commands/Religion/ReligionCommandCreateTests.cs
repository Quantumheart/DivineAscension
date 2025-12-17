using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using Moq;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion create command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandCreateTests : ReligionCommandsTestHelpers
{
    public ReligionCommandCreateTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Error Cases - Duplicate Religion Name

    [Fact]
    public void OnCreateReligion_WithDuplicateName_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "ExistingReligion", "Khoras", "public");
        SetupParsers(args, "ExistingReligion", "Khoras", "public");

        var existingReligion = CreateReligion("religion-existing", "ExistingReligion", DeityType.Lysa, "other-player");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("ExistingReligion")).Returns(existingReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("A religion named 'ExistingReligion' already exists", result.StatusMessage);

        // Verify we never tried to create the religion
        _religionManager.Verify(
            m => m.CreateReligion(It.IsAny<string>(), It.IsAny<DeityType>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    #endregion

    #region Success Cases

    [Fact]
    public void OnCreateReligion_WithValidParameters_CreatesPublicReligion()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Khoras", "public");
        SetupParsers(args, "TestReligion", "Khoras", "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "player-1", true);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Religion 'TestReligion' created", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);

        _religionManager.Verify(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", true), Times.Once);
        _playerReligionDataManager.Verify(m => m.JoinReligion("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnCreateReligion_WithPrivateVisibility_CreatesPrivateReligion()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "SecretReligion", "Lysa", "private");
        SetupParsers(args, "SecretReligion", "Lysa", "private");

        var createdReligion = CreateReligion("religion-1", "SecretReligion", DeityType.Lysa, "player-1", false);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("SecretReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("SecretReligion", DeityType.Lysa, "player-1", false))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("SecretReligion", DeityType.Lysa, "player-1", false), Times.Once);
    }

    [Fact]
    public void OnCreateReligion_WithoutVisibilityParameter_DefaultsToPublic()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "DefaultReligion", "Lysa");
        SetupParsers(args, "DefaultReligion", "Lysa"); // Only 2 parsers, no visibility

        var createdReligion = CreateReligion("religion-1", "DefaultReligion", DeityType.Lysa, "player-1", true);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("DefaultReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("DefaultReligion", DeityType.Lysa, "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("DefaultReligion", DeityType.Lysa, "player-1", true), Times.Once);
    }

    [Fact]
    public void OnCreateReligion_AutoJoinsFounder()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Khoras", "public");
        SetupParsers(args, "TestReligion", "Khoras", "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "player-1", true);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        _playerReligionDataManager.Verify(m => m.JoinReligion("player-1", "religion-1"), Times.Once);
    }

    [Theory]
    [InlineData("Khoras")]
    [InlineData("Lysa")]
    [InlineData("Aethra")]
    [InlineData("Gaia")]
    public void OnCreateReligion_WithValidDeity_CreatesReligion(string deityName)
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", deityName, "public");
        SetupParsers(args, "TestReligion", deityName, "public");

        var deity = Enum.Parse<DeityType>(deityName);
        var createdReligion = CreateReligion("religion-1", "TestReligion", deity, "player-1", true);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", deity, "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnCreateReligion_WithNullPlayer_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Console
                // Player is not set, so it will be null when cast to IServerPlayer
            },
            Parsers = new List<ICommandArgumentParser>()
        };
        SetupParsers(args, "TestReligion", "Khoras", "public");

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnCreateReligion_WhenPlayerAlreadyHasReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "existing-religion", DeityType.Khoras);
        var args = CreateCommandArgs(mockPlayer.Object, "NewReligion", "Lysa", "public");
        SetupParsers(args, "NewReligion", "Lysa", "public");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are already in a religion", result.StatusMessage);
        Assert.Contains("Use /religion leave first", result.StatusMessage);

        // Verify we never tried to create the religion
        _religionManager.Verify(
            m => m.CreateReligion(It.IsAny<string>(), It.IsAny<DeityType>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    #endregion

    #region Error Cases - Invalid Deity

    [Fact]
    public void OnCreateReligion_WithInvalidDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "InvalidDeity", "public");
        SetupParsers(args, "TestReligion", "InvalidDeity", "public");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Invalid deity", result.StatusMessage);
        Assert.Contains("Valid options:", result.StatusMessage);

        // Verify we never tried to create the religion
        _religionManager.Verify(
            m => m.CreateReligion(It.IsAny<string>(), It.IsAny<DeityType>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void OnCreateReligion_WithNoneDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "None", "public");
        SetupParsers(args, "TestReligion", "None", "public");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Invalid deity", result.StatusMessage);
    }

    [Fact]
    public void OnCreateReligion_WithEmptyDeityName_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "", "public");
        SetupParsers(args, "TestReligion", "", "public");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Invalid deity", result.StatusMessage);
    }

    #endregion

    #region Edge Cases - Case Sensitivity

    [Theory]
    [InlineData("khoras")] // lowercase
    [InlineData("KHORAS")] // uppercase
    [InlineData("KhOrAs")] // mixed case
    public void OnCreateReligion_WithDifferentCasingForDeity_AcceptsDeity(string deityName)
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", deityName, "public");
        SetupParsers(args, "TestReligion", deityName, "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "player-1", true);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
    }

    [Theory]
    [InlineData("PRIVATE")] // uppercase
    [InlineData("Private")] // capitalized
    [InlineData("PrIvAtE")] // mixed case
    public void OnCreateReligion_WithDifferentCasingForPrivate_CreatesPrivateReligion(string visibility)
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Khoras", visibility);
        SetupParsers(args, "TestReligion", "Khoras", visibility);

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "player-1", false);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", false))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", false), Times.Once);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("anything-else")]
    [InlineData("")]
    [InlineData("yes")]
    public void OnCreateReligion_WithNonPrivateVisibility_CreatesPublicReligion(string visibility)
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Khoras", visibility);
        SetupParsers(args, "TestReligion", "Khoras", visibility);

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "player-1", true);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("TestReligion", DeityType.Khoras, "player-1", true), Times.Once);
    }

    #endregion
}