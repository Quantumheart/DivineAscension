using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Commands.Favor;

/// <summary>
/// Tests for the /favor set command (admin only)
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandSetTests : FavorCommandsTestHelpers
{
    public FavorCommandSetTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnSetFavor_WithValidAmount_SetsFavor()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Khoras, favor: 100, totalFavor: 500);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "1000");
        SetupParsers(args, 1000);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras)).Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("1,000", result.StatusMessage);
        Assert.Equal(1000, playerData.Favor);
    }

    [Fact]
    public void OnSetFavor_WithZero_SetsFavorToZero()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Lysa, favor: 5000, totalFavor: 5000);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "0");
        SetupParsers(args, 0);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Lysa)).Returns(new Deity(DeityType.Lysa, nameof(DeityType.Lysa), "Hunt"));

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Equal(0, playerData.Favor);
    }

    [Fact]
    public void OnSetFavor_WithMaxValue_SetsFavorToMax()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Aethra);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "999999");
        SetupParsers(args, 999999);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Aethra)).Returns(new Deity(DeityType.Aethra, nameof(DeityType.Aethra), "Death"));

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Equal(999999, playerData.Favor);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void OnSetFavor_WithNegativeAmount_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Khoras);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "-100");
        SetupParsers(args, -100);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras)).Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("cannot be negative", result.StatusMessage);
    }

    [Fact]
    public void OnSetFavor_WithTooLargeAmount_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.Khoras);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "1000000");
        SetupParsers(args, 1000000);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _deityRegistry.Setup(d => d.GetDeity(DeityType.Khoras)).Returns(new Deity(DeityType.Khoras, nameof(DeityType.Khoras), "War"));

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("cannot exceed 999,999", result.StatusMessage);
    }

    [Fact]
    public void OnSetFavor_WithoutDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityType.None);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "100");
        SetupParsers(args, 100);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not in a religion", result.StatusMessage);
    }
    
    #endregion
}
