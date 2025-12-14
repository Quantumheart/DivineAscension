using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion unban command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandUnbanTests : ReligionCommandsTestHelpers
{
    public ReligionCommandUnbanTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnUnbanPlayer_AsFounder_UnbansBannedPlayer()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _religionManager.Setup(m => m.UnbanPlayer("religion-1", "target-1")).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TargetName has been unbanned from TestReligion", result.StatusMessage);
        _religionManager.Verify(m => m.UnbanPlayer("religion-1", "target-1"), Times.Once);
    }

    [Fact]
    public void OnUnbanPlayer_WhenPlayerNotBanned_ReturnsError()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _religionManager.Setup(m => m.UnbanPlayer("religion-1", "target-1")).Returns(false);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("TargetName is not banned from TestReligion", result.StatusMessage);
    }

    [Fact]
    public void OnUnbanPlayer_CaseInsensitivePlayerName()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "targetname"); // lowercase

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _religionManager.Setup(m => m.UnbanPlayer("religion-1", "target-1")).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.UnbanPlayer("religion-1", "target-1"), Times.Once);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnUnbanPlayer_WithNullPlayer_ReturnsError()
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
        SetupParsers(args, "TargetName");

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnUnbanPlayer_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", null);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnUnbanPlayer_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnUnbanPlayer_WhenNotFounder_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "MemberName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You don't have permission to unban players", result.StatusMessage);
    }

    [Fact]
    public void OnUnbanPlayer_WhenTargetNotFound_ReturnsError()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "NonExistentPlayer");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(new[] { mockFounder.Object });

        // Act
        var result = _sut!.OnUnbanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Player 'NonExistentPlayer' not found", result.StatusMessage);
    }

    #endregion
}
