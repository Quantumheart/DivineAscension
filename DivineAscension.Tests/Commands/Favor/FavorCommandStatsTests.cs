using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Favor;

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

    #region Success Cases

    [Fact]
    public void OnFavorStats_WithAllStats_ShowsCompleteInformation()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Khoras, 1500, 3000,
            FavorRank.Zealot);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras))
            .Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);
        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Divine Statistics", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);
        Assert.Contains("1,500", result.StatusMessage); // Current favor
        Assert.Contains("3,000", result.StatusMessage); // Total favor
        Assert.Contains("Zealot", result.StatusMessage);
    }

    [Fact]
    public void OnFavorStats_WithoutLastSwitch_DoesNotShowJoinDate()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Lysa, 100, 100,
            FavorRank.Initiate);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Lysa))
            .Returns(new Deity(DeityType.Lysa, nameof(DeityType.Lysa), "Hunt"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Lysa);

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
        var playerData = CreatePlayerData("player-1", DeityType.Aethra, 5000, 12000,
            FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra))
            .Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "Death"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Khoras);
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
        var playerData = CreatePlayerData("player-1", DeityType.Aethra, 600, 600,
            FavorRank.Disciple);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra))
            .Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "Light"));
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeity(It.IsAny<string>()))
            .Returns(DeityType.Aethra);
        // Act
        var result = _sut!.OnFavorStats(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Next Rank:", result.StatusMessage);
        Assert.Contains("Favor Needed:", result.StatusMessage);
    }

    #endregion
}