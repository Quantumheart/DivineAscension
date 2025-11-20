using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion invite command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandInviteTests : ReligionCommandsTestHelpers
{
    public ReligionCommandInviteTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnInvitePlayer_WithValidTarget_SendsInvitation()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        var onlinePlayers = new List<IPlayer> { mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllOnlinePlayers).Returns(onlinePlayers.ToArray());

        // Act
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Invitation sent to TargetName", result.StatusMessage);
        _religionManager.Verify(m => m.InvitePlayer("religion-1", "target-1", "player-1"), Times.Once);
    }

    [Fact]
    public void OnInvitePlayer_NotifiesTargetPlayer()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        var onlinePlayers = new List<IPlayer> { mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllOnlinePlayers).Returns(onlinePlayers.ToArray());

        // Act
        _sut!.OnInvitePlayer(args);

        // Assert
        mockTarget.Verify(t => t.SendMessage(
            GlobalConstants.GeneralChatGroup,
            It.Is<string>(msg => msg.IndexOf("You have been invited to join TestReligion") >= 0),
            EnumChatType.Notification, null), Times.Once);
    }

    [Fact]
    public void OnInvitePlayer_CaseInsensitivePlayerName()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "targetname"); // lowercase

        var onlinePlayers = new List<IPlayer> { mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllOnlinePlayers).Returns(onlinePlayers.ToArray());

        // Act
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _religionManager.Verify(m => m.InvitePlayer("religion-1", "target-1", "player-1"), Times.Once);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnInvitePlayer_WithNullPlayer_ReturnsError()
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
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnInvitePlayer_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var playerData = CreatePlayerData("player-1", null); // No religion
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnInvitePlayer_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnInvitePlayer_WhenTargetNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "NonExistentPlayer");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllOnlinePlayers).Returns(Array.Empty<IPlayer>());

        // Act
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Player 'NonExistentPlayer' not found online", result.StatusMessage);
    }

    [Fact]
    public void OnInvitePlayer_WhenTargetAlreadyMember_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "InviterName");
        var mockTarget = CreateMockPlayer("target-1", "TargetName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        religion.MemberUIDs.Add("target-1"); // Target is already a member

        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TargetName");

        var onlinePlayers = new List<IPlayer> { mockTarget.Object };

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.AllOnlinePlayers).Returns(onlinePlayers.ToArray());

        // Act
        var result = _sut!.OnInvitePlayer(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("TargetName is already a member of TestReligion", result.StatusMessage);
    }

    #endregion
}
