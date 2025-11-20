using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Favor;

/// <summary>
/// Tests for the /favor info command
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandInfoTests : FavorCommandsTestHelpers
{
    public FavorCommandInfoTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnFavorInfo_WithProgressToNextRank_ShowsProgress()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Aethra, favor: 300, totalFavor: 300, rank: FavorRank.Initiate);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra)).Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "War"));

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Divine Favor", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);
        Assert.Contains("300", result.StatusMessage);
        Assert.Contains("Initiate", result.StatusMessage);
        Assert.Contains("Disciple", result.StatusMessage); // Next rank
        Assert.Contains("Progress", result.StatusMessage);
    }

    [Fact]
    public void OnFavorInfo_AtMaxRank_ShowsNoNextRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Gaia, favor: 5000, totalFavor: 15000, rank: FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Gaia)).Returns(new Deity(DeityType.Gaia, nameof(DeityType.Gaia), "Hunt"));

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Avatar", result.StatusMessage);
        Assert.Contains("Maximum rank achieved", result.StatusMessage);
    }

    [Fact]
    public void OnFavorInfo_AtRankThreshold_ShowsCorrectRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Morthen, favor: 1000, totalFavor: 2000, rank: FavorRank.Zealot);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Morthen)).Returns(new Deity(DeityType.Morthen, nameof(DeityType.Morthen), "Death"));

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Zealot", result.StatusMessage);
        Assert.Contains("2,000", result.StatusMessage); // Total favor formatted
    }

    #endregion

    #region Error Cases

    [Fact]
    public void OnFavorInfo_WithoutDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.None);
        var args = CreateCommandArgs(mockPlayer.Object);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not in a religion", result.StatusMessage);
    }
    

    #endregion
}
