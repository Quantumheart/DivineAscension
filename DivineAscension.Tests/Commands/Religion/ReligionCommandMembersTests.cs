using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion members command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandMembersTests : ReligionCommandsTestHelpers
{
    public ReligionCommandMembersTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Edge Cases

    [Fact]
    public void OnListMembers_WhenMemberOffline_ShowsUnknown()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var memberData = CreatePlayerData("offline-member", "religion-1", DeityType.Khoras);

        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("player-1");
        religion.MemberUIDs.Add("offline-member");

        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("offline-member")).Returns(memberData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("player-1")).Returns(mockPlayer.Object);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("offline-member")).Returns((IPlayer?)null);

        // Act
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.Contains("Unknown", result.StatusMessage);
    }

    #endregion

    #region Success Cases

    [Fact]
    public void OnListMembers_WithMembers_ShowsAllMembers()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockMember = CreateMockPlayer("member-1", "MemberName");

        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        founderData.Favor = 100;
        founderData.FavorRank = FavorRank.Initiate;

        var memberData = CreatePlayerData("member-1", "religion-1", DeityType.Khoras);
        memberData.Favor = 50;
        memberData.FavorRank = FavorRank.Initiate;

        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");

        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("member-1")).Returns(memberData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockMember.Object);

        // Act
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TestReligion Members", result.StatusMessage);
        Assert.Contains("FounderName", result.StatusMessage);
        Assert.Contains("Founder", result.StatusMessage);
        Assert.Contains("MemberName", result.StatusMessage);
        Assert.Contains("Member", result.StatusMessage);
        Assert.Contains("Initiate", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage);
        Assert.Contains("50", result.StatusMessage);
    }

    [Fact]
    public void OnListMembers_ShowsMemberCount()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("founder-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);

        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");
        religion.MemberUIDs.Add("member-2");

        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData(It.IsAny<string>())).Returns(founderData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid(It.IsAny<string>())).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.Contains("(3)", result.StatusMessage);
    }

    [Fact]
    public void OnListMembers_DistinguishesFounderFromMembers()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("founder-1", "FounderName");
        var mockMember = CreateMockPlayer("member-1", "MemberName");
        var playerData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var founderData = CreatePlayerData("founder-1", "religion-1", DeityType.Khoras);
        var memberData = CreatePlayerData("member-1", "religion-1", DeityType.Khoras);

        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Khoras, "founder-1");
        religion.MemberUIDs.Add("member-1");

        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("member-1")).Returns(memberData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockPlayer.Object);
        _mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockMember.Object);

        // Act
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.Contains("FounderName (Founder)", result.StatusMessage);
        Assert.Contains("MemberName (Member)", result.StatusMessage);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnListMembers_WithNullPlayer_ReturnsError()
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
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnListMembers_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", null); // No religion
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnListMembers_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Khoras);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnListMembers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    #endregion
}