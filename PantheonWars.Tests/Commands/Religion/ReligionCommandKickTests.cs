using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion kick command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandKickTests : ReligionCommandsTestHelpers
{
    public ReligionCommandKickTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnKickPlayer_AsFounder_KicksMember()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TargetName has been removed from TestReligion", result.StatusMessage);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("target-1"), Times.Once);
    }

    [Fact]
    public void OnKickPlayer_NotifiesTargetIfOnline()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        _sut!.OnKickPlayer(args);

        // Assert
        mockTarget.Verify(t => t.SendMessage(
            GlobalConstants.GeneralChatGroup,
            It.Is<string>(msg => msg.IndexOf("You have been removed from TestReligion") >= 0),
            EnumChatType.Notification, null), Times.Once);
    }

    [Fact]
    public void OnKickPlayer_DoesNotNotifyIfTargetOffline()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockOfflineTarget = new Mock<IPlayer>(); // Not IServerPlayer
        mockOfflineTarget.Setup(p => p.PlayerUID).Returns("target-1");
        mockOfflineTarget.Setup(p => p.PlayerName).Returns("OfflineTarget");

        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "OfflineTarget");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockOfflineTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("target-1"), Times.Once);
    }

    [Fact]
    public void OnKickPlayer_CaseInsensitivePlayerName()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "targetname"); // lowercase

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("target-1"), Times.Once);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnKickPlayer_WithNullPlayer_ReturnsError()
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
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnKickPlayer_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", null); // No religion
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnKickPlayer_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnKickPlayer_WhenNotFounder_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "MemberName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1"); // Different founder
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You don't have permission to kick members", result.StatusMessage);
    }

    [Fact]
    public void OnKickPlayer_WhenTargetNotFound_ReturnsError()
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
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Player 'NonExistentPlayer' not found", result.StatusMessage);
    }

    [Fact]
    public void OnKickPlayer_WhenTargetNotMember_ReturnsError()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockNonMember = CreateMockPlayer("non-member", "NonMemberName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "NonMemberName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockNonMember.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("NonMemberName is not a member of TestReligion", result.StatusMessage);
    }

    [Fact]
    public void OnKickPlayer_WhenKickingSelf_ReturnsError()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "FounderName");

        var allPlayers = new List<IPlayer> { mockFounder.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnKickPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You cannot kick yourself", result.StatusMessage);
        Assert.Contains("Use /religion disband or /religion leave instead", result.StatusMessage);
    }

    #endregion
}
