using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion join command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandJoinTests : ReligionCommandsTestHelpers
{
    public ReligionCommandJoinTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnJoinReligion_WithValidReligion_JoinsSuccessfully()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1"); // No current religion
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        var result = _sut!.OnJoinReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("You have joined TestReligion", result.StatusMessage);
        Assert.Contains("Craft", result.StatusMessage);
        _playerProgressionDataManager.Verify(m => m.JoinReligion("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnJoinReligion_RemovesInvitationAfterJoining()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        _sut!.OnJoinReligion(args);

        // Assert
        _religionManager.Verify(m => m.RemoveInvitation("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnJoinReligion_WhenSwitchingReligion_AppliesPenalty()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1"); // Has current religion
        var religion = CreateReligion("religion-1", "NewReligion", DeityDomain.Wild, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "NewReligion");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("NewReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        _sut!.OnJoinReligion(args);

        // Assert
        _religionManager.Verify(m => m.RemoveInvitation("player-1", "religion-1"), Times.Once);
        _playerProgressionDataManager.Verify(m => m.JoinReligion("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnJoinReligion_WhenNoCurrentReligion_DoesNotApplyPenalty()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1"); // No current religion
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        _sut!.OnJoinReligion(args);

        // Assert
        _playerProgressionDataManager.Verify(m => m.HandleReligionSwitch(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnJoinReligion_WithNullPlayer_ReturnsError()
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
        SetupParsers(args, "TestReligion");

        // Act
        var result = _sut!.OnJoinReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnJoinReligion_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "NonExistentReligion");

        _religionManager.Setup(m => m.GetReligionByName("NonExistentReligion")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnJoinReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Religion 'NonExistentReligion' not found", result.StatusMessage);
    }

    [Fact]
    public void OnJoinReligion_WhenCannotJoin_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var religion = CreateReligion("religion-1", "PrivateReligion", DeityDomain.Craft, "founder-1", isPublic: false);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "PrivateReligion");

        _religionManager.Setup(m => m.GetReligionByName("PrivateReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(false);

        // Act
        var result = _sut!.OnJoinReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("This religion is private and you have not been invited", result.StatusMessage);
    }

    #endregion
}