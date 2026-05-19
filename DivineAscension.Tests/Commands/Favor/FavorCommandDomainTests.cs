using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using DivineAscension.Tests.Helpers;
using Moq;

namespace DivineAscension.Tests.Commands.Favor;

/// <summary>
///     Coverage for Issue #251: per-deity domain arg on /favor subcommands.
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandDomainTests : FavorCommandsTestHelpers
{
    public FavorCommandDomainTests()
    {
        _sut = InitializeMocksAndSut();
    }

    [Fact]
    public void OnAddFavor_NoDomainArg_DefaultsToPatron()
    {
        var mockPlayer = CreateMockPlayer("p1", "P1");
        var playerData = CreatePlayerData("p1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(args, 500, null, null);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("p1")).Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion("p1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(r => r.GetPlayerActiveDeityDomain("p1")).Returns(DeityDomain.Craft);

        var result = _sut!.OnAddFavor(args);

        Assert.Equal(Vintagestory.API.Common.EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m =>
            m.AddFavor("p1", DeityDomain.Craft, 500), Times.Once);
        _playerReligionDataManager.Verify(m =>
            m.AddFavor(It.IsAny<string>(), DeityDomain.Wild, It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void OnAddFavor_DomainArgOverridesPatron()
    {
        var mockPlayer = CreateMockPlayer("p1", "P1");
        var playerData = CreatePlayerData("p1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(args, 500, "Wild", null);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("p1")).Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion("p1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(r => r.GetPlayerActiveDeityDomain("p1")).Returns(DeityDomain.Craft);

        var result = _sut!.OnAddFavor(args);

        Assert.Equal(Vintagestory.API.Common.EnumCommandStatus.Success, result.Status);
        _playerReligionDataManager.Verify(m =>
            m.AddFavor("p1", DeityDomain.Wild, 500), Times.Once);
    }

    [Fact]
    public void OnAddFavor_InvalidDomainAfterPlayerName_ReturnsError()
    {
        var mockPlayer = CreateMockPlayer("p1", "P1");
        var playerData = CreatePlayerData("p1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(args, 500, "P1", "Notadomain");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("p1")).Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerActiveDeityDomain("p1")).Returns(DeityDomain.Craft);

        var result = _sut!.OnAddFavor(args);

        Assert.Equal(Vintagestory.API.Common.EnumCommandStatus.Error, result.Status);
        Assert.Contains("Notadomain", result.StatusMessage);
    }

    [Fact]
    public void OnCheckFavor_DomainArg_ShowsThatDeity()
    {
        var mockPlayer = CreateMockPlayer("p1", "P1");
        var playerData = CreatePlayerData("p1", DeityDomain.Wild, favor: 750);
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "Wild");

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("p1")).Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion("p1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(r => r.GetPlayerActiveDeityDomain("p1")).Returns(DeityDomain.Craft);
        _playerReligionDataManager.Setup(m => m.GetPlayerFavorRank("p1", DeityDomain.Wild))
            .Returns(FavorRank.Disciple);

        var result = _sut!.OnCheckFavor(args);

        Assert.Equal(Vintagestory.API.Common.EnumCommandStatus.Success, result.Status);
        Assert.Contains("Wild", result.StatusMessage);
        Assert.Contains("750", result.StatusMessage);
    }

    [Fact]
    public void OnFavorStats_NoDomain_ShowsPerDeitySummary()
    {
        var mockPlayer = CreateMockPlayer("p1", "P1");
        var playerData = CreatePlayerData("p1", DeityDomain.Craft, favor: 100, totalFavor: 500);
        playerData.SetFavor(DeityDomain.Wild, 250);
        playerData.SetTotalFavorEarned(DeityDomain.Wild, 800);
        var args = CreateCommandArgs(mockPlayer.Object);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("p1")).Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion("p1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(r => r.GetPlayerActiveDeityDomain("p1")).Returns(DeityDomain.Craft);

        var result = _sut!.OnFavorStats(args);

        Assert.Equal(Vintagestory.API.Common.EnumCommandStatus.Success, result.Status);
        Assert.Contains("Per-deity favor:", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage);
        Assert.Contains("250", result.StatusMessage);
    }
}
