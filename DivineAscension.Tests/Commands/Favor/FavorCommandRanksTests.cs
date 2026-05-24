using System.Diagnostics.CodeAnalysis;
using DivineAscension.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Favor;

/// <summary>
/// Tests for the /favor ranks command
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandRanksTests : FavorCommandsTestHelpers
{
    public FavorCommandRanksTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnListRanks_Always_ShowsAllRanks()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);

        // Act
        var result = _sut!.OnListRanks(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Favor Ranks", result.StatusMessage);
        Assert.Contains("Initiate", result.StatusMessage);
        Assert.Contains("Disciple", result.StatusMessage);
        Assert.Contains("Zealot", result.StatusMessage);
        Assert.Contains("Champion", result.StatusMessage);
        Assert.Contains("Avatar", result.StatusMessage);
    }

    [Fact]
    public void OnListRanks_Always_ShowsRequirements()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);

        // Act
        var result = _sut!.OnListRanks(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("500", result.StatusMessage); // Disciple requirement
        Assert.Contains("2,000", result.StatusMessage); // Zealot requirement
        Assert.Contains("5,000", result.StatusMessage); // Champion requirement
        Assert.Contains("10,000", result.StatusMessage); // Avatar requirement
    }

    [Fact]
    public void OnListRanks_WithCustomThresholds_ReflectsConfiguredValues()
    {
        // Arrange — server admin tuned thresholds via GameBalanceConfig
        _gameBalanceConfig.DiscipleThreshold = 750;
        _gameBalanceConfig.ZealotThreshold = 3000;
        _gameBalanceConfig.ChampionThreshold = 7500;
        _gameBalanceConfig.AvatarThreshold = 15000;
        _sut = InitializeMocksAndSut();

        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);

        // Act
        var result = _sut!.OnListRanks(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("750", result.StatusMessage);
        Assert.Contains("3,000", result.StatusMessage);
        Assert.Contains("7,500", result.StatusMessage);
        Assert.Contains("15,000", result.StatusMessage);
        Assert.DoesNotContain("10,000", result.StatusMessage); // old default Avatar
    }

    [Fact]
    public void OnListRanks_Always_ShowsBlessingsMessage()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);

        // Act
        var result = _sut!.OnListRanks(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("blessings", result.StatusMessage);
    }

    #endregion
}