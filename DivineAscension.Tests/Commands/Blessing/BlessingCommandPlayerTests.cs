using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Commands;
using PantheonWars.Constants;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandPlayerTests : BlessingCommandsTestHelpers
{
    public BlessingCommandPlayerTests()
    {
        _sut = InitializeMocksAndSut();
    }

    [Fact]
    public void OnBlessingsPlayer_PlayerNull_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Mock<Caller>().Object
        };

        // Act
        var result = _sut!.OnPlayer(args);

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorPlayerNotFound, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsPlayer_PlayerHasNoUnlockedBlessings_ReturnsInfoMessage()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = new List<PantheonWars.Models.Blessing>(),
                Item2 = new List<PantheonWars.Models.Blessing>()
            }); // Simulate no blessings


        // Act
        var result = _sut!.OnPlayer(args);

        // Assert
        Assert.Equal(InfoMessageConstants.InfoNoPlayerBlessings, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsPlayer_PlayerHasUnlockedBlessings_ReturnsFormattedList()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Divine Strike",
                Category = BlessingCategory.Combat,
                Description = "Deals extra damage to enemies.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Damage", 0.2f }
                }
            }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });

        var sut = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act
        var result = sut.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.HeaderUnlockedPlayerBlessings, 1), message);
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingNameCategory, "Divine Strike", "Combat"),
            message);
        Assert.Contains(string.Format(FormatStringConstants.FormatDescription, "Deals extra damage to enemies."),
            message);
        Assert.Contains(FormatStringConstants.LabelEffects, message);
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Damage", 20), message);
    }

    [Fact]
    public void OnBlessingsPlayer_HeaderDisplaysBlessingCount()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new() { Name = "Divine Strike" },
            new() { Name = "Faithful Guardian" }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });

        var sut = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act
        var result = sut.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.HeaderUnlockedPlayerBlessings, 2), message);
    }

    [Fact]
    public void OnBlessingsPlayer_DisplayBlessingNamesAndCategories()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();
        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Divine Strike",
                Category = BlessingCategory.Combat
            },
            new()
            {
                Name = "Faithful Guardian",
                Category = BlessingCategory.Defense
            }
        };

        mockBlessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });

        var sut = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            mockBlessingEffectSystem.Object);

        // Act
        var result = sut.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingNameCategory, "Divine Strike", "Combat"),
            message);
        Assert.Contains(string.Format(FormatStringConstants.FormatBlessingNameCategory, "Faithful Guardian", "Defense"),
            message);
    }

    [Fact]
    public void OnBlessingsPlayer_DisplayBlessingDescriptions()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Divine Strike",
                Description = "Deals extra damage to enemies."
            }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });

        var sut = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act
        var result = sut.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatDescription, "Deals extra damage to enemies."),
            message);
    }

    [Fact]
    public void OnBlessingsPlayer_DisplayStatModifiersAsPercentage()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Divine Strike",
                Category = BlessingCategory.Combat,
                Description = "Deals extra damage to enemies.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Damage", 0.2f }
                }
            }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });

        var sut = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);

        // Act
        var result = sut.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Damage", 20), message);
    }

    [Fact]
    public void OnBlessingsPlayer_NoStatModifiers_DoesNotShowEffectsLabel()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();
        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Faithful Guardian",
                Category = BlessingCategory.Defense,
                Description = "Reduces incoming damage."
            }
        };

        mockBlessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });

        var sut = new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            mockBlessingEffectSystem.Object);

        // Act
        var result = sut.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.DoesNotContain(FormatStringConstants.LabelEffects, message);
    }

    [Fact]
    public void OnBlessingsPlayer_MultipleStatModifiers_DisplayAll()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Divine Strike",
                Category = BlessingCategory.Combat,
                Description = "Deals extra damage to enemies.",
                StatModifiers = new Dictionary<string, float>
                {
                    { "Damage", 0.2f },
                    { "Crit Chance", 0.15f }
                }
            }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns(new ValueTuple<List<PantheonWars.Models.Blessing>, List<PantheonWars.Models.Blessing>>
            {
                Item1 = playerBlessings
            });


        // Act
        var result = _sut!.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Damage", 20), message);
        Assert.Contains(string.Format(FormatStringConstants.FormatStatModifier, "Crit Chance", 15), message);
        _blessingEffectSystem.Verify(p => p.GetActiveBlessings(args.Caller.Player.PlayerUID), Times.Once);
    }

    [Fact]
    public void OnBlessingsPlayer_ReturnsOnlyPlayerBlessings()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };


        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new() { Name = "Divine Strike", Category = BlessingCategory.Combat, Description = "Deals extra damage." }
        };

        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                Name = "Religion Boon", Category = BlessingCategory.Utility, Description = "Bonus for religion members."
            }
        };

        _blessingEffectSystem.Setup(p => p.GetActiveBlessings(It.IsAny<string>()))
            .Returns((playerBlessings, religionBlessings));


        // Act
        var result = _sut!.OnPlayer(args);

        // Assert
        var message = result.StatusMessage;
        Assert.Contains(string.Format(FormatStringConstants.HeaderUnlockedPlayerBlessings, 1), message);
        Assert.Contains("Divine Strike", message);
        Assert.DoesNotContain("Religion Boon", message);
    }
}