using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion leave command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandLeaveTests : ReligionCommandsTestHelpers
{
    public ReligionCommandLeaveTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnLeaveReligion_WithPlayerInReligion_LeavesSuccessfully()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnLeaveReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("You have left TestReligion", result.StatusMessage);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("player-1"), Times.Once);
    }

    [Fact]
    public void OnLeaveReligion_ReturnsReligionName()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "MyCustomReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnLeaveReligion(args);

        // Assert
        Assert.Contains("MyCustomReligion", result.StatusMessage);
    }

    [Fact]
    public void OnLeaveReligion_CallsLeaveReligionOnce()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        _sut!.OnLeaveReligion(args);

        // Assert
        _playerReligionDataManager.Verify(m => m.LeaveReligion("player-1"), Times.Once);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnLeaveReligion_WithNullPlayer_ReturnsError()
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
        SetupParsers(args);

        // Act
        var result = _sut!.OnLeaveReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnLeaveReligion_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", null); // No religion
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnLeaveReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
        _playerReligionDataManager.Verify(m => m.LeaveReligion(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OnLeaveReligion_WhenReligionNotFound_ShowsUnknownReligion()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnLeaveReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Unknown", result.StatusMessage);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("player-1"), Times.Once);
    }

    [Fact]
    public void OnLeaveReligion_AsFounder_StillLeaves()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("founder-1", "FounderPlayer");
        var playerData = CreatePlayerData("founder-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnLeaveReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("founder-1"), Times.Once);
    }

    #endregion
}
