using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandActiveTests : BlessingCommandsTestHelpers
{
    public BlessingCommandActiveTests()
    {
        _sut = InitializeMocksAndSut();
    }
    

    #region No Blessings Scenarios

    [Fact]
    public void OnBlessingsActive_NoPlayerOrReligionBlessings_ShowsNoneForBoth()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>();
        var religionBlessings = new List<PantheonWars.Models.Blessing>();
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("None", result.StatusMessage);
        var noneCount = result.StatusMessage.Split("None").Length - 1;
        Assert.Equal(2, noneCount); // Should appear twice (player and religion sections)
    }

    [Fact]
    public void OnBlessingsActive_PlayerBlessingsOnlyNoReligion_ShowsPlayerBlessingsAndNoneForReligion()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "player1",
                Name = "Player Blessing 1",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Player blessing",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var religionBlessings = new List<PantheonWars.Models.Blessing>();
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Player Blessing 1", result.StatusMessage);
        Assert.Contains("None", result.StatusMessage); // For religion section
    }

    [Fact]
    public void OnBlessingsActive_ReligionBlessingsOnlyNoPlayer_ShowsReligionBlessingsAndNoneForPlayer()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>();
        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "religion1",
                Name = "Religion Blessing 1",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description = "Religion blessing",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Religion Blessing 1", result.StatusMessage);
        Assert.Contains("None", result.StatusMessage); // For player section
    }

    #endregion

    #region With Blessings Scenarios

    [Fact]
    public void OnBlessingsActive_BothPlayerAndReligionBlessings_DisplaysBoth()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "player1",
                Name = "Player Blessing",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Player",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "religion1",
                Name = "Religion Blessing",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description = "Religion",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Player Blessing", result.StatusMessage);
        Assert.Contains("Religion Blessing", result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsActive_PlayerBlessings_DisplaysCorrectCount()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "player1",
                Name = "Blessing One",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "First",
                StatModifiers = new Dictionary<string, float>()
            },
            new()
            {
                BlessingId = "player2",
                Name = "Blessing Two",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Second",
                StatModifiers = new Dictionary<string, float>()
            },
            new()
            {
                BlessingId = "player3",
                Name = "Blessing Three",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Third",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var religionBlessings = new List<PantheonWars.Models.Blessing>();
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("3", result.StatusMessage); // Count of 3
    }

    [Fact]
    public void OnBlessingsActive_ReligionBlessings_DisplaysCorrectCount()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>();
        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "religion1",
                Name = "Religion One",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description = "First",
                StatModifiers = new Dictionary<string, float>()
            },
            new()
            {
                BlessingId = "religion2",
                Name = "Religion Two",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description = "Second",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("2", result.StatusMessage); // Count of 2
    }

    [Fact]
    public void OnBlessingsActive_MultipleBlessings_DisplaysAllBlessingNames()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "player1",
                Name = "Speed Boost",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Boost",
                StatModifiers = new Dictionary<string, float>()
            },
            new()
            {
                BlessingId = "player2",
                Name = "Health Boost",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Health",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "religion1",
                Name = "Divine Protection",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Defense,
                Description = "Protection",
                StatModifiers = new Dictionary<string, float>()
            }
        };
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Speed Boost", result.StatusMessage);
        Assert.Contains("Health Boost", result.StatusMessage);
        Assert.Contains("Divine Protection", result.StatusMessage);
    }

    #endregion

    #region Combined Modifiers

    [Fact]
    public void OnBlessingsActive_NoActiveModifiers_ShowsNoActiveModifiersMessage()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>();
        var religionBlessings = new List<PantheonWars.Models.Blessing>();
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("No active modifiers", result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsActive_WithActiveModifiers_CallsFormatStatModifiers()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>();
        var religionBlessings = new List<PantheonWars.Models.Blessing>();
        var combinedModifiers = new Dictionary<string, float>
        {
            { "walkspeed", 0.15f },
            { "maxhealthExtraPoints", 0.20f }
        };

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);
        _blessingEffectSystem.Setup(bes => bes.FormatStatModifiers(combinedModifiers))
            .Returns("walkspeed: +15%\nmaxhealthExtraPoints: +20%");

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        _blessingEffectSystem.Verify(bes => bes.FormatStatModifiers(combinedModifiers), Times.Once);
    }

    [Fact]
    public void OnBlessingsActive_CombinedModifiersFromBothSources_DisplaysCombinedResult()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "player1",
                Name = "Player Blessing",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Player",
                StatModifiers = new Dictionary<string, float> { { "walkspeed", 0.10f } }
            }
        };
        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "religion1",
                Name = "Religion Blessing",
                Deity = DeityType.Aethra,
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description = "Religion",
                StatModifiers = new Dictionary<string, float> { { "walkspeed", 0.05f } }
            }
        };
        var combinedModifiers = new Dictionary<string, float>
        {
            { "walkspeed", 0.15f } // Combined from both
        };

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);
        _blessingEffectSystem.Setup(bes => bes.FormatStatModifiers(combinedModifiers))
            .Returns("walkspeed: +15%");

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("15%", result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsActive_VerifiesGetActiveBlessingsAndGetCombinedStatModifiersCalled()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player-uid");

        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = mockPlayer.Object
            }
        };

        var playerBlessings = new List<PantheonWars.Models.Blessing>();
        var religionBlessings = new List<PantheonWars.Models.Blessing>();
        var combinedModifiers = new Dictionary<string, float>();

        _blessingEffectSystem.Setup(bes => bes.GetActiveBlessings("test-player-uid"))
            .Returns((playerBlessings, religionBlessings));
        _blessingEffectSystem.Setup(bes => bes.GetCombinedStatModifiers("test-player-uid"))
            .Returns(combinedModifiers);

        // Act
        var result = _sut!.OnActive(args);

        // Assert
        _blessingEffectSystem.Verify(bes => bes.GetActiveBlessings("test-player-uid"), Times.Once);
        _blessingEffectSystem.Verify(bes => bes.GetCombinedStatModifiers("test-player-uid"), Times.Once);
    }

    #endregion
}
