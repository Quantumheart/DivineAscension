using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandReligionTests : BlessingCommandsTestHelpers
{
    public BlessingCommandReligionTests()
    {
        _sut = InitializeMocksAndSut();
    }

    [Fact]
    public void OnBlessingsReligion_PlayerNull_ReturnsErrorPlayerNotFound()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Mock<Caller>().Object
        };

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorPlayerNotFound, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsReligion_PlayerHasNoReligion_ReturnsErrorNoReligion()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new Mock<PlayerProgressionData>().Object;

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorNoReligion, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsReligion_ReligionHasNoUnlockedBlessings_ReturnsInfoMessage()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "foobar"
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), new List<DivineAscension.Models.Blessing>()));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        Assert.Equal(InfoMessageConstants.InfoNoReligionBlessings, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsReligion_DisplaysReligionNameInHeader()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new() { Name = "Holy Blessing", Category = BlessingCategory.Utility }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains("Divine Order", message);
        Assert.Contains(string.Format(FormatStringConstants.HeaderReligionBlessingsWithName, "Divine Order", 1),
            message);
    }

    [Fact]
    public void OnBlessingsReligion_DisplaysUnlockedReligionBlessingsCorrectly()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                Name = "Holy Blessing",
                Category = BlessingCategory.Utility,
                Description = "Grants divine protection to all members.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Defense", 0.15f }
                }
            }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingNameCategory, "Holy Blessing", "Utility"),
            message);
        Assert.Contains(
            string.Format(FormatStringConstants.FormatDescription, "Grants divine protection to all members."),
            message);
        Assert.Contains("Stat Modifiers:", message); // Now uses localized label
        Assert.Contains("Defense", message);
        Assert.Contains("15", message);
    }

    [Fact]
    public void OnBlessingsReligion_ShowsBlessingCountInHeader()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new() { Name = "Holy Blessing", Category = BlessingCategory.Utility },
            new() { Name = "Sacred Shield", Category = BlessingCategory.Defense },
            new() { Name = "Divine Wrath", Category = BlessingCategory.Combat }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.HeaderReligionBlessingsWithName, "Divine Order", 3),
            message);
    }

    [Fact]
    public void OnBlessingsReligion_DisplaysBlessingNamesAndCategories()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new() { Name = "Holy Blessing", Category = BlessingCategory.Utility },
            new() { Name = "Sacred Shield", Category = BlessingCategory.Defense }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingNameCategory, "Holy Blessing", "Utility"),
            message);
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingNameCategory, "Sacred Shield", "Defense"),
            message);
    }

    [Fact]
    public void OnBlessingsReligion_DisplaysBlessingDescriptions()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                Name = "Holy Blessing",
                Category = BlessingCategory.Utility,
                Description = "Grants divine protection to all members."
            }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(
            string.Format(FormatStringConstants.FormatDescription, "Grants divine protection to all members."),
            message);
    }

    [Fact]
    public void OnBlessingsReligion_StatModifiersShowForAllMembersLabel()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                Name = "Holy Blessing",
                Category = BlessingCategory.Utility,
                Description = "Grants divine protection.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Defense", 0.10f }
                }
            }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains("Stat Modifiers:", message); // Now uses localized label
    }

    [Fact]
    public void OnBlessingsReligion_StatModifiersFormattedCorrectly()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                Name = "Holy Blessing",
                Category = BlessingCategory.Utility,
                Description = "Grants divine protection.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Defense", 0.25f }
                }
            }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Defense", 25), message);
    }

    [Fact]
    public void OnBlessingsReligion_MultipleStatModifiersPerBlessing()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new()
            {
                Name = "Divine Empowerment",
                Category = BlessingCategory.Combat,
                Description = "Empowers all members.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Attack", 0.20f },
                    { "Defense", 0.15f },
                    { "Speed", 0.10f }
                }
            }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((new List<DivineAscension.Models.Blessing>(), religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Attack", 20), message);
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Defense", 15), message);
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Speed", 10), message);
    }

    [Fact]
    public void OnBlessingsReligion_ReturnsOnlyReligionBlessings()
    {
        // Arrange
        var player = new Mock<IServerPlayer>().Object;
        var playerData = new PlayerProgressionData
        {
        };
        var religion = new ReligionData
        {
            ReligionUID = "test-religion-uid",
            ReligionName = "Divine Order"
        };

        var playerBlessings = new List<DivineAscension.Models.Blessing>
        {
            new() { Name = "Personal Blessing", Category = BlessingCategory.Utility }
        };

        var religionBlessings = new List<DivineAscension.Models.Blessing>
        {
            new() { Name = "Holy Blessing", Category = BlessingCategory.Utility }
        };

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = player
            }
        };

        _playerReligionDataManager.Setup(p => p.GetOrCreatePlayerData(player.PlayerUID))
            .Returns(playerData);
        _religionManager.Setup(r => r.GetPlayerReligion(player.PlayerUID))
            .Returns(religion);
        _religionManager.Setup(rm => rm.HasReligion(It.IsAny<string>()))
            .Returns(true);
        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(player.PlayerUID))
            .Returns((playerBlessings, religionBlessings));

        // Act
        var result = _sut!.OnReligion(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains("Holy Blessing", message);
        Assert.DoesNotContain("Personal Blessing", message);
    }
}