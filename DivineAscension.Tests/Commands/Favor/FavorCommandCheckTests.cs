using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Favor;

/// <summary>
/// Tests for the /favor and /favor get commands
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandCheckTests : FavorCommandsTestHelpers
{
    public FavorCommandCheckTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Error Cases

    [Fact]
    public void OnCheckFavor_WithoutDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.None);
        var args = CreateCommandArgs(mockPlayer.Object);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not in a religion", result.StatusMessage);
    }

    #endregion

    #region Success Cases

    [Fact]
    public void OnCheckFavor_WithValidDeity_ShowsCurrentFavor()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft, 500, 1000,
            FavorRank.Disciple);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        _playerReligionDataManager
            .Setup(m => m.GetPlayerFavorRank("player-1"))
            .Returns(() =>
            {
                // Calculate rank based on current TotalFavorEarned
                if (playerData.TotalFavorEarned >= 10000) return FavorRank.Avatar;
                if (playerData.TotalFavorEarned >= 5000) return FavorRank.Champion;
                if (playerData.TotalFavorEarned >= 2000) return FavorRank.Zealot;
                if (playerData.TotalFavorEarned >= 500) return FavorRank.Disciple;
                return FavorRank.Initiate;
            });

        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("500", result.StatusMessage);
        Assert.Contains("Craft", result.StatusMessage);
        Assert.Contains("Disciple", result.StatusMessage);
    }

    [Fact]
    public void OnCheckFavor_WithZeroFavor_ShowsZero()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData =
            CreatePlayerData("player-1", DeityDomain.Wild, 0, 0, FavorRank.Initiate);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Wild);
        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("0 favor", result.StatusMessage);
        Assert.Contains("Wild", result.StatusMessage);
    }

    [Fact]
    public void OnCheckFavor_WithHighRank_ShowsCorrectRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Harvest, 7000, 15000,
            FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Harvest);

        _playerReligionDataManager
            .Setup(m => m.GetPlayerFavorRank("player-1"))
            .Returns(() =>
            {
                // Calculate rank based on current TotalFavorEarned
                if (playerData.TotalFavorEarned >= 10000) return FavorRank.Avatar;
                if (playerData.TotalFavorEarned >= 5000) return FavorRank.Champion;
                if (playerData.TotalFavorEarned >= 2000) return FavorRank.Zealot;
                if (playerData.TotalFavorEarned >= 500) return FavorRank.Disciple;
                return FavorRank.Initiate;
            });

        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Avatar", result.StatusMessage);
        Assert.Contains("7000", result.StatusMessage);
    }

    #endregion
}