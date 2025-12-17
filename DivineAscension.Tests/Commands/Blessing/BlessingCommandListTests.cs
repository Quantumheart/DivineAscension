using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandListTests : BlessingCommandsTestHelpers
{
    public BlessingCommandListTests()
    {
        _sut = InitializeMocksAndSut();
    }

    [Fact]
    public void OnBlessingsList_PlayerInReligionWithBlessings_ReturnsFormattedList()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var playerBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "blessing1",
                Name = "Divine Strike",
                Description = "Deals extra damage to enemies.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                Kind = BlessingKind.Player
            },
            new()
            {
                BlessingId = "blessing2",
                Name = "Faithful Guardian",
                Description = "Reduces damage taken by 20%.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                Kind = BlessingKind.Player
            }
        };

        foreach (var playerBlessing in playerBlessings) playerData.UnlockBlessing(playerBlessing.BlessingId);

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "religionblessing1",
                Name = "Sacred Flame",
                Description = "Inflicts divine fire on enemies.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                Kind = BlessingKind.Religion
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(playerBlessings);
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(religionBlessings);

        var religion = new ReligionData
        {
            ReligionUID = "religion-uid",
            UnlockedBlessings = new Dictionary<string, bool>
            {
                { "religionblessing1", true }
            }
        };

        _religionManager.Setup(rm => rm.GetReligion("religion-uid"))
            .Returns(religion);

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra),
            result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderPlayerBlessings, result.StatusMessage);
        Assert.Contains("Divine Strike [UNLOCKED]", result.StatusMessage);
        Assert.Contains("Faithful Guardian", result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderReligionBlessings, result.StatusMessage);
        Assert.Contains("Sacred Flame [UNLOCKED]", result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_PlayerNotFound_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Mock<Caller>().Object
        };

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorPlayerNotFound, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_PlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(new PlayerReligionData { ActiveDeity = DeityType.None });

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorMustJoinReligion, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_PlayerInReligionWithNoBlessings_ReturnsSuccess()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(new List<DivineAscension.Models.Blessing>());
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<DivineAscension.Models.Blessing>());

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra),
            result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderPlayerBlessings, result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderReligionBlessings, result.StatusMessage);
    }


    [Fact]
    public void OnBlessingsList_PlayerInReligionWithNoBlessings_ReturnsSuccessMessage()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(new List<DivineAscension.Models.Blessing>());
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<DivineAscension.Models.Blessing>());

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra),
            result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderPlayerBlessings, result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderReligionBlessings, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_PlayerInReligionWithReligionBlessings_ReturnsFormattedList()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "religionblessing1",
                Name = "Sacred Flame",
                Description = "Inflicts divine fire on enemies.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                Kind = BlessingKind.Religion
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(religionBlessings);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(new List<DivineAscension.Models.Blessing>());

        var religion = new ReligionData
        {
            ReligionUID = "religion-uid",
            UnlockedBlessings = new Dictionary<string, bool>
            {
                { "religionblessing1", true }
            }
        };

        _religionManager.Setup(rm => rm.GetReligion("religion-uid"))
            .Returns(religion);

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra),
            result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderReligionBlessings, result.StatusMessage);
        Assert.Contains("Sacred Flame [UNLOCKED]", result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_DisplaysFavorRankCorrectly()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var playerBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "blessing_zealot",
                Name = "Zealot Blessing",
                Description = "A blessing for zealots.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                Kind = BlessingKind.Player
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(playerBlessings);
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<DivineAscension.Models.Blessing>());

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains("Zealot Blessing", result.StatusMessage);
        Assert.Contains(string.Format(FormatStringConstants.FormatRequiredRank, FavorRank.Zealot),
            result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_DisplaysPrestigeRankCorrectly()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "blessing_established",
                Name = "Established Blessing",
                Description = "A blessing for established religions.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                Kind = BlessingKind.Religion
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(new List<DivineAscension.Models.Blessing>());
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(religionBlessings);

        var religion = new ReligionData
        {
            ReligionUID = "religion-uid",
            UnlockedBlessings = new Dictionary<string, bool>()
        };

        _religionManager.Setup(rm => rm.GetReligion("religion-uid"))
            .Returns(religion);

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains("Established Blessing", result.StatusMessage);
        Assert.Contains(string.Format(FormatStringConstants.FormatRequiredRank, PrestigeRank.Established),
            result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_DisplaysBlessingIdAndDescription()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var playerBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "unique_blessing_id_123",
                Name = "Test Blessing",
                Description = "This is a unique test description.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                Kind = BlessingKind.Player
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(playerBlessings);
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<DivineAscension.Models.Blessing>());

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains("Test Blessing", result.StatusMessage);
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingId, "unique_blessing_id_123"),
            result.StatusMessage);
        Assert.Contains(string.Format(FormatStringConstants.FormatDescription, "This is a unique test description."),
            result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_OutputFormatContainsAllSections()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var playerBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "player_blessing",
                Name = "Player Blessing",
                Description = "Player description",
                RequiredFavorRank = (int)FavorRank.Disciple,
                Kind = BlessingKind.Player
            }
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                BlessingId = "religion_blessing",
                Name = "Religion Blessing",
                Description = "Religion description",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                Kind = BlessingKind.Religion
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(playerBlessings);
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(religionBlessings);

        var religion = new ReligionData
        {
            ReligionUID = "religion-uid",
            UnlockedBlessings = new Dictionary<string, bool>()
        };

        _religionManager.Setup(rm => rm.GetReligion("religion-uid"))
            .Returns(religion);

        // Act
        var result = _sut!.OnList(args);

        // Assert - Verify all expected sections are present
        var message = result.StatusMessage;

        // Header
        Assert.Contains(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra), message);

        // Player blessings section
        Assert.Contains(FormatStringConstants.HeaderPlayerBlessings, message);
        Assert.Contains("Player Blessing", message);

        // Religion blessings section
        Assert.Contains(FormatStringConstants.HeaderReligionBlessings, message);
        Assert.Contains("Religion Blessing", message);

        // Verify section ordering: header appears before player blessings, player blessings before religion blessings
        var headerIndex =
            message.IndexOf(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra));
        var playerBlessingsIndex = message.IndexOf(FormatStringConstants.HeaderPlayerBlessings);
        var religionBlessingsIndex = message.IndexOf(FormatStringConstants.HeaderReligionBlessings);

        Assert.True(headerIndex < playerBlessingsIndex, "Header should appear before player blessings section");
        Assert.True(playerBlessingsIndex < religionBlessingsIndex,
            "Player blessings section should appear before religion blessings section");
    }
}