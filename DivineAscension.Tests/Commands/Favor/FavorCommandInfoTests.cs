using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Favor;

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

    #region Error Cases

    [Fact]
    public void OnFavorInfo_WithoutDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.None);
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

    #region Success Cases

    [Fact]
    public void OnFavorInfo_WithProgressToNextRank_ShowsProgress()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft, 300, 300);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Divine Favor", result.StatusMessage);
        Assert.Contains(nameof(DeityDomain.Craft), result.StatusMessage);
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
        var playerData = CreatePlayerData("player-1", DeityDomain.Wild, 5000, 15000,
            FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Wild);
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
        var playerData = CreatePlayerData("player-1", DeityDomain.Harvest, 1000, 2000,
            FavorRank.Zealot);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Harvest);

        // Act
        var result = _sut!.OnFavorInfo(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Zealot", result.StatusMessage);
        Assert.Contains("2,000", result.StatusMessage); // Total favor formatted
    }

    #endregion
}