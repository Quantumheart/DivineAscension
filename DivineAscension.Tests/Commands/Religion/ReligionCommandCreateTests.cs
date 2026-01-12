using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Religion;

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
        var args = CreateCommandArgs(mockPlayer.Object, "ExistingReligion", "Craft", "Craft", "public");
        SetupParsers(args, "ExistingReligion", "Craft", "Craft", "public");

        var existingReligion =
            CreateReligion("religion-existing", "ExistingReligion", DeityDomain.Wild, "other-player");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("ExistingReligion")).Returns(existingReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("A religion named 'ExistingReligion' already exists", result.StatusMessage);

        // Verify we never tried to create the religion
        _religionManager.Verify(
            m => m.CreateReligion(It.IsAny<string>(), It.IsAny<DeityDomain>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>()),
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
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Craft", "Craft", "public");
        SetupParsers(args, "TestReligion", "Craft", "Craft", "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1", true);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Religion 'TestReligion' created", result.StatusMessage);
        Assert.Contains("Craft", result.StatusMessage);

        _religionManager.Verify(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", true),
            Times.Once);
        _playerProgressionDataManager.Verify(m => m.SetPlayerReligionData("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnCreateReligion_WithPrivateVisibility_CreatesPrivateReligion()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "SecretReligion", "Wild", "Wild", "private");
        SetupParsers(args, "SecretReligion", "Wild", "Wild", "private");

        var createdReligion = CreateReligion("religion-1", "SecretReligion", DeityDomain.Wild, "player-1", false);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("SecretReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("SecretReligion", DeityDomain.Wild, "Wild", "player-1", false))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("SecretReligion", DeityDomain.Wild, "Wild", "player-1", false),
            Times.Once);
    }

    [Fact]
    public void OnCreateReligion_WithoutVisibilityParameter_DefaultsToPublic()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "DefaultReligion", "Wild", "Wild");
        SetupParsers(args, "DefaultReligion", "Wild", "Wild"); // Only 3 parsers, no visibility

        var createdReligion = CreateReligion("religion-1", "DefaultReligion", DeityDomain.Wild, "player-1", true);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("DefaultReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("DefaultReligion", DeityDomain.Wild, "Wild", "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("DefaultReligion", DeityDomain.Wild, "Wild", "player-1", true),
            Times.Once);
    }

    [Fact]
    public void OnCreateReligion_AutoJoinsFounder()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Craft", "Craft", "public");
        SetupParsers(args, "TestReligion", "Craft", "Craft", "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1", true);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        _playerProgressionDataManager.Verify(m => m.SetPlayerReligionData("player-1", "religion-1"), Times.Once);
    }

    [Theory]
    [InlineData("Craft", "Craft", DeityDomain.Craft)]
    [InlineData("Wild", "Wild", DeityDomain.Wild)]
    [InlineData("Harvest", "Harvest", DeityDomain.Harvest)]
    [InlineData("Stone", "Stone", DeityDomain.Stone)]
    public void OnCreateReligion_WithValidDeity_CreatesReligion(string domainName, string deityName,
        DeityDomain expectedDomain)
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", domainName, deityName, "public");
        SetupParsers(args, "TestReligion", domainName, deityName, "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", expectedDomain, "player-1", true);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", expectedDomain, deityName, "player-1", true))
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
        SetupParsers(args, "TestReligion", "Craft", "Craft", "public");

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
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "NewReligion", "Wild", "Wild", "public");
        SetupParsers(args, "NewReligion", "Wild", "Wild", "public");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are already in a religion", result.StatusMessage);
        Assert.Contains("Use /religion leave first", result.StatusMessage);

        // Verify we never tried to create the religion
        _religionManager.Verify(
            m => m.CreateReligion(It.IsAny<string>(), It.IsAny<DeityDomain>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    #endregion

    #region Error Cases - Invalid Deity

    [Fact]
    public void OnCreateReligion_WithInvalidDomain_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "InvalidDomain", "Craft", "public");
        SetupParsers(args, "TestReligion", "InvalidDomain", "Craft", "public");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Invalid deity", result.StatusMessage);
        Assert.Contains("Valid options:", result.StatusMessage);

        // Verify we never tried to create the religion
        _religionManager.Verify(
            m => m.CreateReligion(It.IsAny<string>(), It.IsAny<DeityDomain>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void OnCreateReligion_WithNoneDomain_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "None", "Craft", "public");
        SetupParsers(args, "TestReligion", "None", "Craft", "public");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

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
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Craft", "", "public");
        SetupParsers(args, "TestReligion", "Craft", "", "public");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Deity name is required", result.StatusMessage);
    }

    #endregion

    #region Edge Cases - Case Sensitivity

    [Theory]
    [InlineData("craft")] // lowercase
    [InlineData("CRAFT")] // uppercase
    [InlineData("CrAfT")] // mixed case
    public void OnCreateReligion_WithDifferentCasingForDomain_AcceptsDomain(string domainName)
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", domainName, "Craft", "public");
        SetupParsers(args, "TestReligion", domainName, "Craft", "public");

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1", true);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", true))
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
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Craft", "Craft", visibility);
        SetupParsers(args, "TestReligion", "Craft", "Craft", visibility);

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1", false);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", false))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", false),
            Times.Once);
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
        var args = CreateCommandArgs(mockPlayer.Object, "TestReligion", "Craft", "Craft", visibility);
        SetupParsers(args, "TestReligion", "Craft", "Craft", visibility);

        var createdReligion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1", true);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", true))
            .Returns(createdReligion);

        // Act
        var result = _sut!.OnCreateReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.CreateReligion("TestReligion", DeityDomain.Craft, "Craft", "player-1", true),
            Times.Once);
    }

    #endregion
}