using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace DivineAscension.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion ban command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandBanTests : ReligionCommandsTestHelpers
{
    public ReligionCommandBanTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnBanPlayer_AsFounder_BansMember()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName", "Bad behavior", 7);

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TargetName has been banned from TestReligion", result.StatusMessage);
        Assert.Contains("for 7 days", result.StatusMessage);
        _playerProgressionDataManager.Verify(m => m.LeaveReligion("target-1"), Times.Once);
        _religionManager.Verify(m => m.BanPlayer("religion-1", "target-1", "founder-1", "Bad behavior", 7), Times.Once);
    }

    [Fact]
    public void OnBanPlayer_WithoutDays_BansPermanently()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName", "Bad behavior");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("permanent", result.StatusMessage);
        _religionManager.Verify(m => m.BanPlayer("religion-1", "target-1", "founder-1", "Bad behavior", null),
            Times.Once);
    }

    [Fact]
    public void OnBanPlayer_WithoutReason_UsesDefaultReason()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        _religionManager.Verify(m => m.BanPlayer("religion-1", "target-1", "founder-1", "No reason provided", null),
            Times.Once);
    }

    [Fact]
    public void OnBanPlayer_RemovesMemberBeforeBanning()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("target-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        _sut!.OnBanPlayer(args);

        // Assert - LeaveReligion should be called before BanPlayer
        _playerProgressionDataManager.Verify(m => m.LeaveReligion("target-1"), Times.Once);
        _religionManager.Verify(
            m => m.BanPlayer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public void OnBanPlayer_BansNonMemberPlayer()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        // Target is NOT a member

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName", "Banned preemptively");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _playerProgressionDataManager.Verify(m => m.LeaveReligion("target-1"), Times.Never);
        _religionManager.Verify(m => m.BanPlayer("religion-1", "target-1", "founder-1", "Banned preemptively", null),
            Times.Once);
    }

    [Fact]
    public void OnBanPlayer_NotifiesTargetIfOnline()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "TargetName", "Violation of rules");

        var allPlayers = new List<IPlayer> { mockFounder.Object, mockTarget.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        _sut!.OnBanPlayer(args);

        // Assert
        mockTarget.Verify(t => t.SendMessage(
            GlobalConstants.GeneralChatGroup,
            It.Is<string>(msg =>
                msg.IndexOf("You have been banned from TestReligion") >= 0 && msg.IndexOf("Violation of rules") >= 0),
            EnumChatType.Notification, null), Times.Once);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnBanPlayer_WithNullPlayer_ReturnsError()
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
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnBanPlayer_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnBanPlayer_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnBanPlayer_WhenNotFounder_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "MemberName");
        var playerData = CreatePlayerData("player-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You don't have permission to ban players", result.StatusMessage);
    }

    [Fact]
    public void OnBanPlayer_WhenTargetNotFound_ReturnsError()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "NonExistentPlayer");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(new[] { mockFounder.Object });

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Player 'NonExistentPlayer' not found", result.StatusMessage);
    }

    [Fact]
    public void OnBanPlayer_WhenBanningSelf_ReturnsError()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args, "FounderName");

        var allPlayers = new List<IPlayer> { mockFounder.Object };

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockWorld.Setup(w => w.AllPlayers).Returns(allPlayers.ToArray());

        // Act
        var result = _sut!.OnBanPlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You cannot ban yourself", result.StatusMessage);
    }

    #endregion
}