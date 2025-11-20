using System;
using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Favor;

/// <summary>
/// Tests for the /favor stats command
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandStatsTests : FavorCommandsTestHelpers
{
    public FavorCommandStatsTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnFavorStats_WithAllStats_ShowsCompleteInformation()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Aethra, favor: 1500, totalFavor: 3000, rank: FavorRank.Zealot);
        playerData.KillCount = 42;
        playerData.LastReligionSwitch = DateTime.UtcNow.AddDays(-30);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra)).Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "Light"));

        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Divine Statistics", result.StatusMessage);
        Assert.Contains("Aethra", result.StatusMessage);
        Assert.Contains("1,500", result.StatusMessage); // Current favor
        Assert.Contains("3,000", result.StatusMessage); // Total favor
        Assert.Contains("Zealot", result.StatusMessage);
        Assert.Contains("42", result.StatusMessage); // Kill count
        Assert.Contains("Days Served", result.StatusMessage);
    }

    [Fact]
    public void OnFavorStats_WithoutLastSwitch_DoesNotShowJoinDate()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Gaia, favor: 100, totalFavor: 100, rank: FavorRank.Initiate);
        playerData.LastReligionSwitch = null;
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Gaia)).Returns(new Deity(DeityType.Gaia, nameof(DeityType.Gaia), "Nature"));

        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.DoesNotContain("Days Served", result.StatusMessage);
        Assert.DoesNotContain("Join Date", result.StatusMessage);
    }

    [Fact]
    public void OnFavorStats_AtMaxRank_DoesNotShowNextRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Morthen, favor: 5000, totalFavor: 12000, rank: FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Morthen)).Returns(new Deity(DeityType.Morthen, nameof(DeityType.Morthen), "Death"));

        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Avatar", result.StatusMessage);
        Assert.DoesNotContain("Next Rank:", result.StatusMessage);
    }

    [Fact]
    public void OnFavorStats_BelowMaxRank_ShowsNextRankInfo()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Aethra, favor: 600, totalFavor: 600, rank: FavorRank.Disciple);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra)).Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "Light"));

        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Next Rank:", result.StatusMessage);
        Assert.Contains("Favor Needed:", result.StatusMessage);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void OnFavorStats_WithoutDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.None);
        var args = CreateCommandArgs(mockPlayer.Object);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not in a religion", result.StatusMessage);
    }
    
    #endregion
}
