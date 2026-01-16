using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Favor;

/// <summary>
/// Tests for admin favor commands (add, remove, reset, max, settotal)
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorCommandAdminTests : FavorCommandsTestHelpers
{
    public FavorCommandAdminTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region /favor reset tests

    [Fact]
    public void OnResetFavor_Always_ResetsFavorToZero()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Harvest, 5000, 10000);
        var args = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(args, new object[] { null });


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Harvest);

        // Act
        var result = _sut!.OnResetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("reset to 0", result.StatusMessage);
        Assert.Contains("5,000", result.StatusMessage); // Old value
        Assert.Equal(0, playerData.Favor);
    }

    #endregion

    #region /favor max tests

    [Fact]
    public void OnMaxFavor_Always_SetsFavorToMaximum()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Harvest, 100, 500);
        var args = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(args, new object[] { null });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);
        // Act
        var result = _sut!.OnMaxFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("99,999", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage); // Old value
        Assert.Equal(99999, playerData.Favor);
    }

    #endregion

    #region Common error cases

    [Fact]
    public void AdminCommands_WithoutDeity_ReturnError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.None);
        var argsReset = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(argsReset, new object[] { null });
        var argsMax = CreateAdminCommandArgs(mockPlayer.Object);
        SetupParsers(argsMax, new object[] { null });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act & Assert
        var resetResult = _sut!.OnResetFavor(argsReset);
        Assert.Equal(EnumCommandStatus.Error, resetResult.Status);

        var maxResult = _sut.OnMaxFavor(argsMax);
        Assert.Equal(EnumCommandStatus.Error, maxResult.Status);
    }

    #endregion

    #region /favor add with target player tests

    [Fact]
    public void OnAddFavor_WithTargetPlayer_AddsFavor()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Wild, 100, 500);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "50", "TargetPlayer");
        SetupParsers(args, 50, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);
        _playerReligionDataManager.Setup(m => m.AddFavor("player-2", 50, It.IsAny<string>()))
            .Callback(() => targetData.Favor += 50);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Wild);

        // Act
        var result = _sut!.OnAddFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Added 50 favor", result.StatusMessage);
        Assert.Contains("TargetPlayer", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage);
        Assert.Contains("150", result.StatusMessage);
        Assert.Equal(150, targetData.Favor);
    }

    #endregion

    #region /favor remove with target player tests

    [Fact]
    public void OnRemoveFavor_WithTargetPlayer_RemovesFavor()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Stone, 200, 500);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "50", "TargetPlayer");
        SetupParsers(args, 50, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);
        _playerReligionDataManager.Setup(m => m.RemoveFavor("player-2", 50, It.IsAny<string>()))
            .Callback(() => targetData.Favor = Math.Max(0, targetData.Favor - 50))
            .Returns(true);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Stone);

        // Act
        var result = _sut!.OnRemoveFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Removed", result.StatusMessage);
        Assert.Contains("TargetPlayer", result.StatusMessage);
        Assert.Contains("200", result.StatusMessage);
        Assert.Contains("150", result.StatusMessage);
        Assert.Equal(150, targetData.Favor);
    }

    #endregion

    #region /favor reset with target player tests

    [Fact]
    public void OnResetFavor_WithTargetPlayer_ResetsFavor()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Harvest, 5000, 10000);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "TargetPlayer");
        SetupParsers(args, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Harvest);

        // Act
        var result = _sut!.OnResetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("reset to 0", result.StatusMessage);
        Assert.Contains("TargetPlayer", result.StatusMessage);
        Assert.Contains("5,000", result.StatusMessage);
        Assert.Equal(0, targetData.Favor);
    }

    #endregion

    #region /favor max with target player tests

    [Fact]
    public void OnMaxFavor_WithTargetPlayer_SetsFavorToMaximum()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Wild, 100, 500);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "TargetPlayer");
        SetupParsers(args, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Wild);

        // Act
        var result = _sut!.OnMaxFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("99,999", result.StatusMessage);
        Assert.Contains("TargetPlayer", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage);
        Assert.Equal(99999, targetData.Favor);
    }

    #endregion

    #region /favor add tests

    [Fact]
    public void OnAddFavor_WithValidAmount_AddsFavor()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft, favor: 100, totalFavor: 500);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "50");
        SetupParsers(args, 50, (string)null);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _playerReligionDataManager.Setup(m => m.AddFavor("player-1", 50, It.IsAny<string>()))
            .Callback(() => playerData.Favor += 50);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnAddFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Added 50 favor", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage);
        Assert.Contains("150", result.StatusMessage);
    }

    [Fact]
    public void OnAddFavor_WithZeroOrNegative_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "0");
        SetupParsers(args, 0, (string)null);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnAddFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("greater than 0", result.StatusMessage);
    }

    #endregion

    #region /favor remove tests

    [Fact]
    public void OnRemoveFavor_WithValidAmount_RemovesFavor()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Wild, favor: 200, totalFavor: 500);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "50");
        SetupParsers(args, 50, (string)null);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _playerReligionDataManager.Setup(m => m.RemoveFavor("player-1", 50, It.IsAny<string>()))
            .Callback(() => playerData.Favor = Math.Max(0, playerData.Favor - 50))
            .Returns(true);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnRemoveFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Removed", result.StatusMessage);
        Assert.Contains("200", result.StatusMessage);
        Assert.Contains("150", result.StatusMessage);
    }

    [Fact]
    public void OnRemoveFavor_WithMoreThanCurrent_RemovesOnlyAvailable()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Harvest, favor: 50, totalFavor: 500);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "100");
        SetupParsers(args, 100, (string)null);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _playerReligionDataManager.Setup(m => m.RemoveFavor("player-1", 100, It.IsAny<string>()))
            .Callback(() => playerData.Favor = 0)
            .Returns(true);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnRemoveFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("50", result.StatusMessage); // Actual removed
        Assert.Contains("→ 0", result.StatusMessage);
    }

    [Fact]
    public void OnRemoveFavor_WithZeroOrNegative_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "-10");
        SetupParsers(args, -10, (string)null);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnRemoveFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("greater than 0", result.StatusMessage);
    }

    #endregion

    #region /favor settotal tests

    [Fact]
    public void OnSetTotalFavor_WithValidAmount_SetsTotalAndUpdatesRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Harvest, 100, 100,
            FavorRank.Initiate);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "5000");
        SetupParsers(args, 5000, (string)null);


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
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("5,000", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage); // Old total
        Assert.Contains("Rank updated", result.StatusMessage);
        Assert.Equal(5000, playerData.TotalFavorEarned);
    }

    [Fact]
    public void OnSetTotalFavor_WithSameRank_ShowsRankUnchanged()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Stone, 100, 600,
            FavorRank.Disciple);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "700");
        SetupParsers(args, 700, (string)null);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Stone);

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
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Rank unchanged", result.StatusMessage);
        Assert.Contains("Disciple", result.StatusMessage);
        Assert.Equal(700, playerData.TotalFavorEarned);
    }

    [Fact]
    public void OnSetTotalFavor_WithNegativeAmount_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "-100");
        SetupParsers(args, -100, (string)null);

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("cannot be negative", result.StatusMessage);
    }

    [Fact]
    public void OnSetTotalFavor_WithTooLargeAmount_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var playerData = CreatePlayerData("player-1", DeityDomain.Craft);
        var args = CreateAdminCommandArgs(mockPlayer.Object, "1000000");
        SetupParsers(args, 1000000, (string)null);


        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain(It.IsAny<string>()))
            .Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("cannot exceed 999,999", result.StatusMessage);
    }

    [Fact]
    public void OnSetTotalFavor_WithTargetPlayer_SetsTotalAndUpdatesRank()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Harvest, 100, 100, FavorRank.Initiate);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "5000", "TargetPlayer");
        SetupParsers(args, 5000, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Harvest);

        _playerReligionDataManager
            .Setup(m => m.GetPlayerFavorRank("player-2"))
            .Returns(() =>
            {
                // Calculate rank based on current TotalFavorEarned
                if (targetData.TotalFavorEarned >= 10000) return FavorRank.Avatar;
                if (targetData.TotalFavorEarned >= 5000) return FavorRank.Champion;
                if (targetData.TotalFavorEarned >= 2000) return FavorRank.Zealot;
                if (targetData.TotalFavorEarned >= 500) return FavorRank.Disciple;
                return FavorRank.Initiate;
            });

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("5,000", result.StatusMessage);
        Assert.Contains("100", result.StatusMessage); // Old total
        Assert.Contains("Rank updated", result.StatusMessage);
        Assert.Equal(5000, targetData.TotalFavorEarned);
    }

    [Fact]
    public void OnSetTotalFavor_WithTargetPlayer_RankUnchanged()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Wild, 100, 600, FavorRank.Disciple);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "700", "TargetPlayer");
        SetupParsers(args, 700, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Wild);

        _playerReligionDataManager
            .Setup(m => m.GetPlayerFavorRank("player-2"))
            .Returns(() =>
            {
                // Calculate rank based on current TotalFavorEarned
                if (targetData.TotalFavorEarned >= 10000) return FavorRank.Avatar;
                if (targetData.TotalFavorEarned >= 5000) return FavorRank.Champion;
                if (targetData.TotalFavorEarned >= 2000) return FavorRank.Zealot;
                if (targetData.TotalFavorEarned >= 500) return FavorRank.Disciple;
                return FavorRank.Initiate;
            });

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("700", result.StatusMessage);
        Assert.Contains("600", result.StatusMessage); // Old total
        Assert.Contains("Rank unchanged", result.StatusMessage);
        Assert.Contains("Disciple", result.StatusMessage);
        Assert.Equal(700, targetData.TotalFavorEarned);
    }

    [Fact]
    public void OnSetTotalFavor_WithNonExistentTarget_ReturnsError()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "5000", "NonExistentPlayer");
        SetupParsers(args, 5000, "NonExistentPlayer");

        _mockWorld.Setup(w => w.AllPlayers).Returns(new[] { adminPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Cannot find player", result.StatusMessage);
        Assert.Contains("NonExistentPlayer", result.StatusMessage);
    }

    [Fact]
    public void OnSetTotalFavor_WithTargetWithoutDeity_ReturnsError()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.None, 0, 0);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "5000", "TargetPlayer");
        SetupParsers(args, 5000, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.None);

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not in a religion", result.StatusMessage);
    }

    [Fact]
    public void OnSetTotalFavor_WithCaseInsensitiveTargetName_FindsPlayer()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Stone, 100, 100);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "1000", "targetplayer"); // lowercase
        SetupParsers(args, 1000, "targetplayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);

        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Stone);

        // Act
        var result = _sut!.OnSetTotalFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("1,000", result.StatusMessage);
        Assert.Equal(1000, targetData.TotalFavorEarned);
    }

    #endregion

    #region /favor set with target player tests

    [Fact]
    public void OnSetFavor_WithTargetPlayer_SetsFavor()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var targetPlayer = CreateMockPlayer("player-2", "TargetPlayer");

        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);
        var targetData = CreatePlayerData("player-2", DeityDomain.Harvest, 100, 500);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "5000", "TargetPlayer");
        SetupParsers(args, 5000, "TargetPlayer");

        _mockWorld.Setup(w => w.AllPlayers)
            .Returns(new[] { adminPlayer.Object, targetPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-2")).Returns(targetData);


        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerReligion("player-2"))
            .Returns(TestFixtures.CreateTestReligion());

        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("player-2")).Returns(DeityDomain.Harvest);

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("5,000", result.StatusMessage);
        Assert.Contains("TargetPlayer", result.StatusMessage);
        Assert.Equal(5000, targetData.Favor);
    }

    [Fact]
    public void OnSetFavor_WithNonExistentTarget_ReturnsError()
    {
        // Arrange
        var adminPlayer = CreateMockPlayer("admin-1", "Admin");
        var adminData = CreatePlayerData("admin-1", DeityDomain.Craft, 1000, 2000);

        var args = CreateAdminCommandArgs(adminPlayer.Object, "5000", "NonExistent");
        SetupParsers(args, 5000, "NonExistent");

        _mockWorld.Setup(w => w.AllPlayers).Returns(new[] { adminPlayer.Object });

        _playerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(adminData);
        _religionManager.Setup(pr => pr.GetPlayerReligion("admin-1"))
            .Returns(TestFixtures.CreateTestReligion());
        _religionManager.Setup(pr => pr.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Craft);

        // Act
        var result = _sut!.OnSetFavor(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Cannot find player", result.StatusMessage);
    }

    #endregion
}