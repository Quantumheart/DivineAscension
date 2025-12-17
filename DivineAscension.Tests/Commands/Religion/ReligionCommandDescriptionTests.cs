using System.Diagnostics.CodeAnalysis;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion description command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandDescriptionTests : ReligionCommandsTestHelpers
{
    public ReligionCommandDescriptionTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnSetDescription_AsFounder_SetsDescription()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "This is a great religion dedicated to Khoras");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Description set for TestReligion", result.StatusMessage);
        Assert.Equal("This is a great religion dedicated to Khoras", religion.Description);
    }

    [Fact]
    public void OnSetDescription_UpdatesExistingDescription()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.Description = "Old description";

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "New description");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Equal("New description", religion.Description);
    }

    [Fact]
    public void OnSetDescription_AcceptsLongDescription()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var longDescription = "This is a very long description that spans multiple sentences. " +
                              "It contains lots of information about the religion. " +
                              "We worship Khoras and follow his teachings.";

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, longDescription);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Equal(longDescription, religion.Description);
    }

    [Fact]
    public void OnSetDescription_AcceptsEmptyString()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.Description = "Old description";

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Equal("", religion.Description);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnSetDescription_WithNullPlayer_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Console
            },
            Parsers = new List<ICommandArgumentParser>()
        };
        SetupParsers(args, "Test description");

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnSetDescription_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", null);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "Test description");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnSetDescription_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "Test description");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnSetDescription_WhenNotFounder_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "MemberName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "Test description");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnSetDescription(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You don't have permission to edit the religion description", result.StatusMessage);
    }

    #endregion
}