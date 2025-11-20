using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Religion;

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
        var playerData = CreatePlayerData("player-1", null); // No current religion
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(true);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        var result = _sut!.OnJoinReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("You have joined TestReligion", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);
        _playerReligionDataManager.Verify(m => m.JoinReligion("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnJoinReligion_RemovesInvitationAfterJoining()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", null);
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(true);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
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
        var playerData = CreatePlayerData("player-1", "old-religion"); // Has current religion
        var religion = CreateReligion("religion-1", "NewReligion", DeityType.Gaia, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "NewReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(true);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("NewReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        _sut!.OnJoinReligion(args);

        // Assert
        _playerReligionDataManager.Verify(m => m.HandleReligionSwitch("player-1"), Times.Once);
        _playerReligionDataManager.Verify(m => m.JoinReligion("player-1", "religion-1"), Times.Once);
    }

    [Fact]
    public void OnJoinReligion_WhenNoCurrentReligion_DoesNotApplyPenalty()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", null); // No current religion
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1", isPublic: true);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(true);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _religionManager.Setup(m => m.CanJoinReligion("religion-1", "player-1")).Returns(true);

        // Act
        _sut!.OnJoinReligion(args);

        // Assert
        _playerReligionDataManager.Verify(m => m.HandleReligionSwitch(It.IsAny<string>()), Times.Never);
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
    public void OnJoinReligion_WhenOnCooldown_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(false);
        _playerReligionDataManager.Setup(m => m.GetSwitchCooldownRemaining("player-1"))
            .Returns(new TimeSpan(5, 12, 0, 0)); // 5 days, 12 hours

        // Act
        var result = _sut!.OnJoinReligion(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You must wait", result.StatusMessage);
        Assert.Contains("5 days", result.StatusMessage);
        Assert.Contains("12 hours", result.StatusMessage);
    }

    [Fact]
    public void OnJoinReligion_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "NonExistentReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(true);
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
        var religion = CreateReligion("religion-1", "PrivateReligion", DeityType.Aethra, "founder-1", isPublic: false);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "PrivateReligion");

        _playerReligionDataManager.Setup(m => m.CanSwitchReligion("player-1")).Returns(true);
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
