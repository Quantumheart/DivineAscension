using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion banlist command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandBanlistTests : ReligionCommandsTestHelpers
{
    public ReligionCommandBanlistTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases

    [Fact]
    public void OnListBannedPlayers_AsFounder_ShowsBannedPlayers()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockBannedPlayer = CreateMockPlayer("banned-1", "BannedPlayer");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");

        var bannedPlayers = new List<BanEntry>
        {
            new BanEntry("banned-1", "founder-1", "Violation of rules", DateTime.UtcNow.AddDays(7))
        };

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _religionManager.Setup(m => m.GetBannedPlayers("religion-1")).Returns(bannedPlayers);
        _mockWorldService.Setup(w => w.GetPlayerByUID("banned-1")).Returns(mockBannedPlayer.Object);
        _mockWorldService.Setup(w => w.GetPlayerByUID("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Banned players in TestReligion", result.StatusMessage);
        Assert.Contains("BannedPlayer", result.StatusMessage);
        Assert.Contains("Violation of rules", result.StatusMessage);
        Assert.Contains("FounderName", result.StatusMessage);
        Assert.Contains("Expires:", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_WithPermanentBan_ShowsPermanent()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockBannedPlayer = CreateMockPlayer("banned-1", "BannedPlayer");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");

        var bannedPlayers = new List<BanEntry>
        {
            new BanEntry("banned-1", "founder-1", "Permanent ban", null) // No expiry
        };

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _religionManager.Setup(m => m.GetBannedPlayers("religion-1")).Returns(bannedPlayers);
        _mockWorldService.Setup(w => w.GetPlayerByUID("banned-1")).Returns(mockBannedPlayer.Object);
        _mockWorldService.Setup(w => w.GetPlayerByUID("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.Contains("permanent", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_WhenNoBans_ShowsEmptyMessage()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _religionManager.Setup(m => m.GetBannedPlayers("religion-1")).Returns(new List<BanEntry>());

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("No players are currently banned from your religion", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_ShowsMultipleBans()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var mockBanned1 = CreateMockPlayer("banned-1", "FirstBanned");
        var mockBanned2 = CreateMockPlayer("banned-2", "SecondBanned");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");

        var bannedPlayers = new List<BanEntry>
        {
            new BanEntry("banned-1", "founder-1", "First reason", DateTime.UtcNow.AddDays(5)),
            new BanEntry("banned-2", "founder-1", "Second reason", null)
        };

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _religionManager.Setup(m => m.GetBannedPlayers("religion-1")).Returns(bannedPlayers);
        _mockWorldService.Setup(w => w.GetPlayerByUID("banned-1")).Returns(mockBanned1.Object);
        _mockWorldService.Setup(w => w.GetPlayerByUID("banned-2")).Returns(mockBanned2.Object);
        _mockWorldService.Setup(w => w.GetPlayerByUID("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.Contains("FirstBanned", result.StatusMessage);
        Assert.Contains("SecondBanned", result.StatusMessage);
        Assert.Contains("First reason", result.StatusMessage);
        Assert.Contains("Second reason", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_WhenBannedPlayerOffline_ShowsUnknown()
    {
        // Arrange
        var mockFounder = CreateMockPlayer("founder-1", "FounderName");
        var founderData = CreatePlayerData("founder-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");

        var bannedPlayers = new List<BanEntry>
        {
            new BanEntry("banned-1", "founder-1", "Test", null)
        };

        var args = CreateCommandArgs(mockFounder.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("founder-1")).Returns(founderData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _religionManager.Setup(m => m.GetBannedPlayers("religion-1")).Returns(bannedPlayers);
        _mockWorldService.Setup(w => w.GetPlayerByUID("banned-1")).Returns((IServerPlayer?)null);
        _mockWorldService.Setup(w => w.GetPlayerByUID("founder-1")).Returns(mockFounder.Object);

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.Contains("Unknown", result.StatusMessage);
    }

    #endregion

    #region Error Cases - Player Validation

    [Fact]
    public void OnListBannedPlayers_WithNullPlayer_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Console
            },
            Parsers = new List<ICommandArgumentParser>()
        };
        SetupParsers(args);

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Command can only be used by players", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_WhenPlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);

        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You are not in any religion", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_WhenReligionNotFound_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "PlayerName");
        var playerData = CreatePlayerData("player-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetPlayerReligion("founder-1")).Returns((ReligionData?)null);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Could not find your religion data", result.StatusMessage);
    }

    [Fact]
    public void OnListBannedPlayers_WhenNotFounder_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "MemberName");
        var playerData = CreatePlayerData("player-1");
        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _playerProgressionDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _religionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _religionManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        // Act
        var result = _sut!.OnListBannedPlayers(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("You don't have permission to view the ban list", result.StatusMessage);
    }

    #endregion
}