using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace DivineAscension.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion disband command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandDisbandTests : ReligionCommandsTestHelpers
{
    public ReligionCommandDisbandTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnDisbandReligion_AsFounder_DisbandsSingleMemberReligion()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TestReligion has been disbanded", result.StatusMessage);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("founder-1"), Times.AtLeastOnce);
        _religionManager.Verify(m => m.DeleteReligion("religion-1", "founder-1"), Times.Once);
    }

    [Fact]
    public void OnDisbandReligion_WithMultipleMembers_RemovesAllMembers()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockMember1 = CreateMockPlayer("member-1", "Member1");
        var mockMember2 = CreateMockPlayer("member-2", "Member2");

        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");
        religion.MemberUIDs.Add("member-2");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockMember1.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-2")).Returns(mockMember2.Object);

        // Act
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("founder-1"), Times.AtLeastOnce);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("member-1"), Times.AtLeastOnce);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("member-2"), Times.AtLeastOnce);
    }

    [Fact]
    public void OnDisbandReligion_NotifiesOtherMembers()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockMember = CreateMockPlayer("member-1", "MemberName");

        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockMember.Object);

        // Act
        _sut!.OnDisbandReligion(args);

        // Assert
        mockMember.Verify(m => m.SendMessage(
            GlobalConstants.GeneralChatGroup,
            It.Is<string>(msg => msg.IndexOf("TestReligion has been disbanded") >= 0),
            EnumChatType.Notification, null), Times.Once);
    }

    [Fact]
    public void OnDisbandReligion_DoesNotNotifyFounder()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockMember = CreateMockPlayer("member-1", "MemberName");

        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockMember.Object);

        // Act
        _sut!.OnDisbandReligion(args);

        // Assert - Founder should NOT receive chat notification (only command result)
        mockFounder.Verify(m => m.SendMessage(
            It.IsAny<int>(),
            It.Is<string>(msg => msg.IndexOf("disbanded") >= 0),
            It.IsAny<EnumChatType>(), null), Times.Never);
    }

    [Fact]
    public void OnDisbandReligion_SendsReligionStateChangedPacket()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockMember = CreateMockPlayer("member-1", "MemberName");

        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockMember.Object);

        // Act
        _sut!.OnDisbandReligion(args);

        // Assert
        _serverChannel.Verify(s => s.SendPacket(
            It.Is<ReligionStateChangedPacket>(p =>
                p.HasReligion == false && p.Reason.IndexOf("TestReligion has been disbanded") >= 0),
            mockFounder.Object), Times.AtLeastOnce);

        _serverChannel.Verify(s => s.SendPacket(
            It.Is<ReligionStateChangedPacket>(p =>
                p.HasReligion == false && p.Reason.IndexOf("TestReligion has been disbanded") >= 0),
            mockMember.Object), Times.AtLeastOnce);
    }

    [Fact]
    public void OnDisbandReligion_HandlesOfflineMembers()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");

        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("offline-member");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("offline-member")).Returns((IPlayer?)null);

        // Act
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m => m.LeaveReligion("offline-member"), Times.AtLeastOnce);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnDisbandReligion_WithNullPlayer_ReturnsError()
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
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnDisbandReligion_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", null);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnDisbandReligion_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnDisbandReligion_WhenNotFounder_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "MemberName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);

        // Act
        var result = _sut!.OnDisbandReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You don't have permission to disband the religion", result.StatusMessage);
    }

    #endregion
}