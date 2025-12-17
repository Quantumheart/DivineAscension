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