using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Favor;

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

    #region Success Cases

    [Fact]
    public void OnCheckFavor_WithValidDeity_ShowsCurrentFavor()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Aethra, favor: 500, totalFavor: 1000, rank: FavorRank.Disciple);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra)).Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "War"));

        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("500", result.StatusMessage);
        Assert.Contains("Aethra", result.StatusMessage);
        Assert.Contains("Disciple", result.StatusMessage);
    }

    [Fact]
    public void OnCheckFavor_WithZeroFavor_ShowsZero()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Gaia, favor: 0, totalFavor: 0, rank: FavorRank.Initiate);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Gaia)).Returns(new Deity(DeityType.Gaia, nameof(DeityType.Gaia), "Earth"));

        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("0 favor", result.StatusMessage);
        Assert.Contains("Gaia", result.StatusMessage);
    }

    [Fact]
    public void OnCheckFavor_WithHighRank_ShowsCorrectRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Morthen, favor: 7000, totalFavor: 15000, rank: FavorRank.Avatar);
        var args = CreateCommandArgs(mockPlayer.Object);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Morthen)).Returns(new Deity(DeityType.Morthen, nameof(DeityType.Morthen), "Death"));

        // Act
        var result = _sut!.OnCheckFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Avatar", result.StatusMessage);
        Assert.Contains("7000", result.StatusMessage);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void OnCheckFavor_WithoutDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.None);
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
}
