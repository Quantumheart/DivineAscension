using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion info command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandInfoTests : ReligionCommandsTestHelpers
{
    public ReligionCommandInfoTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases - With Religion Name

    [Fact]
    public void OnReligionInfo_WithReligionName_ShowsReligionInfo()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        religion.Description = "A great religion";
        religion.PrestigeRank = PrestigeRank.Renowned;
        religion.Prestige = 500;
        religion.TotalPrestige = 1000;
        religion.MemberUIDs.Add("member-1");

        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TestReligion", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);
        Assert.Contains("Public", result.StatusMessage);
        Assert.Contains("2", result.StatusMessage); // member count
        Assert.Contains("Renowned", result.StatusMessage);
        Assert.Contains("500", result.StatusMessage);
        Assert.Contains("1000", result.StatusMessage);
        Assert.Contains("FounderName", result.StatusMessage);
        Assert.Contains("A great religion", result.StatusMessage);
    }

    [Fact]
    public void OnReligionInfo_WithPrivateReligion_ShowsPrivate()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var religion = CreateReligion("religion-1", "PrivateReligion", DeityType.Aethra, "founder-1", isPublic: false);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "PrivateReligion");

        _religionManager.Setup(m => m.GetReligionByName("PrivateReligion")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.Contains("Private", result.StatusMessage);
    }

    [Fact]
    public void OnReligionInfo_WithReligionName_WhenNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "NonExistent");

        _religionManager.Setup(m => m.GetReligionByName("NonExistent")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Religion 'NonExistent' not found", result.StatusMessage);
    }

    #endregion

    #region Success Cases - No Religion Name (Current Religion)

    [Fact]
    public void OnReligionInfo_WithoutName_ShowsCurrentReligion()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var religion = CreateReligion("religion-1", "MyReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("MyReligion", result.StatusMessage);
    }

    [Fact]
    public void OnReligionInfo_WithoutName_WhenNoReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", null); // No religion
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
        Assert.Contains("Specify a religion name to view", result.StatusMessage);
    }

    [Fact]
    public void OnReligionInfo_WithoutName_WhenReligionDataNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", "religion-1", DeityType.Aethra);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnReligionInfo_WithNullPlayer_ReturnsError()
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
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OnReligionInfo_WhenFounderOffline_ShowsUnknown()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns((IPlayer?)null);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.Contains("Unknown", result.StatusMessage);
    }

    [Fact]
    public void OnReligionInfo_WithoutDescription_DoesNotShowDescription()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var religion = CreateReligion("religion-1", "TestReligion", DeityType.Aethra, "founder-1");
        religion.Description = null;
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "TestReligion");

        _religionManager.Setup(m => m.GetReligionByName("TestReligion")).Returns(religion);
        _mockWorld.Setup(w => w.PlayerByUid("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnReligionInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        // Should not contain "Description:" if description is null/empty
    }

    #endregion
}
