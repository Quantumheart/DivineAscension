using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Moq;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for CivilizationBonusSystem
///     Tests bonus lookups and player-to-civilization resolution
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationBonusSystemTests
{
    private readonly CivilizationBonusSystem _bonusSystem;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly Mock<ICivilizationManager> _mockCivilizationManager;
    private readonly Mock<ICivilizationMilestoneManager> _mockMilestoneManager;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public CivilizationBonusSystemTests()
    {
        _mockLogger = new Mock<ILoggerWrapper>();
        _mockCivilizationManager = new Mock<ICivilizationManager>();
        _mockMilestoneManager = new Mock<ICivilizationMilestoneManager>();
        _mockReligionManager = new Mock<IReligionManager>();

        _bonusSystem = new CivilizationBonusSystem(
            _mockLogger.Object,
            _mockCivilizationManager.Object,
            _mockMilestoneManager.Object,
            _mockReligionManager.Object);
    }

    #region GetFavorMultiplier Tests

    [Fact]
    public void GetFavorMultiplier_ValidCiv_ReturnsBonusValue()
    {
        // Arrange
        var civId = "test-civ-1";
        var bonuses = new CivilizationBonuses { FavorMultiplier = 1.25f };

        _mockMilestoneManager.Setup(m => m.GetActiveBonuses(civId)).Returns(bonuses);

        // Act
        var result = _bonusSystem.GetFavorMultiplier(civId);

        // Assert
        Assert.Equal(1.25f, result);
    }

    [Fact]
    public void GetFavorMultiplier_NullCivId_ReturnsDefault()
    {
        // Act
        var result = _bonusSystem.GetFavorMultiplier(null!);

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void GetFavorMultiplier_EmptyCivId_ReturnsDefault()
    {
        // Act
        var result = _bonusSystem.GetFavorMultiplier(string.Empty);

        // Assert
        Assert.Equal(1.0f, result);
    }

    #endregion

    #region GetPrestigeMultiplier Tests

    [Fact]
    public void GetPrestigeMultiplier_ValidCiv_ReturnsBonusValue()
    {
        // Arrange
        var civId = "test-civ-1";
        var bonuses = new CivilizationBonuses { PrestigeMultiplier = 1.5f };

        _mockMilestoneManager.Setup(m => m.GetActiveBonuses(civId)).Returns(bonuses);

        // Act
        var result = _bonusSystem.GetPrestigeMultiplier(civId);

        // Assert
        Assert.Equal(1.5f, result);
    }

    [Fact]
    public void GetPrestigeMultiplier_EmptyCivId_ReturnsDefault()
    {
        // Act
        var result = _bonusSystem.GetPrestigeMultiplier(string.Empty);

        // Assert
        Assert.Equal(1.0f, result);
    }

    #endregion

    #region GetConquestMultiplier Tests

    [Fact]
    public void GetConquestMultiplier_ValidCiv_ReturnsBonusValue()
    {
        // Arrange
        var civId = "test-civ-1";
        var bonuses = new CivilizationBonuses { ConquestMultiplier = 1.3f };

        _mockMilestoneManager.Setup(m => m.GetActiveBonuses(civId)).Returns(bonuses);

        // Act
        var result = _bonusSystem.GetConquestMultiplier(civId);

        // Assert
        Assert.Equal(1.3f, result);
    }

    [Fact]
    public void GetConquestMultiplier_EmptyCivId_ReturnsDefault()
    {
        // Act
        var result = _bonusSystem.GetConquestMultiplier(string.Empty);

        // Assert
        Assert.Equal(1.0f, result);
    }

    #endregion

    #region GetAllBonuses Tests

    [Fact]
    public void GetAllBonuses_ValidCiv_ReturnsAllBonuses()
    {
        // Arrange
        var civId = "test-civ-1";
        var bonuses = new CivilizationBonuses
        {
            FavorMultiplier = 1.1f,
            PrestigeMultiplier = 1.2f,
            ConquestMultiplier = 1.3f,
            BonusHolySiteSlots = 2
        };

        _mockMilestoneManager.Setup(m => m.GetActiveBonuses(civId)).Returns(bonuses);

        // Act
        var result = _bonusSystem.GetAllBonuses(civId);

        // Assert
        Assert.Equal(1.1f, result.FavorMultiplier);
        Assert.Equal(1.2f, result.PrestigeMultiplier);
        Assert.Equal(1.3f, result.ConquestMultiplier);
        Assert.Equal(2, result.BonusHolySiteSlots);
    }

    [Fact]
    public void GetAllBonuses_EmptyCivId_ReturnsDefaultBonuses()
    {
        // Act
        var result = _bonusSystem.GetAllBonuses(string.Empty);

        // Assert
        Assert.Equal(CivilizationBonuses.None.FavorMultiplier, result.FavorMultiplier);
        Assert.Equal(CivilizationBonuses.None.PrestigeMultiplier, result.PrestigeMultiplier);
        Assert.Equal(CivilizationBonuses.None.ConquestMultiplier, result.ConquestMultiplier);
        Assert.Equal(CivilizationBonuses.None.BonusHolySiteSlots, result.BonusHolySiteSlots);
    }

    #endregion

    #region GetFavorMultiplierForPlayer Tests

    [Fact]
    public void GetFavorMultiplierForPlayer_PlayerInCiv_ReturnsBonusValue()
    {
        // Arrange
        var playerUID = "player-1";
        var religionUID = "religion-1";
        var civId = "civ-1";

        var religion = new ReligionData { ReligionUID = religionUID };
        var civ = new Civilization { CivId = civId };
        var bonuses = new CivilizationBonuses { FavorMultiplier = 1.25f };

        _mockReligionManager.Setup(r => r.GetPlayerReligion(playerUID)).Returns(religion);
        _mockCivilizationManager.Setup(c => c.GetCivilizationByReligion(religionUID)).Returns(civ);
        _mockMilestoneManager.Setup(m => m.GetActiveBonuses(civId)).Returns(bonuses);

        // Act
        var result = _bonusSystem.GetFavorMultiplierForPlayer(playerUID);

        // Assert
        Assert.Equal(1.25f, result);
    }

    [Fact]
    public void GetFavorMultiplierForPlayer_PlayerNotInReligion_ReturnsDefault()
    {
        // Arrange
        var playerUID = "player-1";

        _mockReligionManager.Setup(r => r.GetPlayerReligion(playerUID)).Returns((ReligionData?)null);

        // Act
        var result = _bonusSystem.GetFavorMultiplierForPlayer(playerUID);

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void GetFavorMultiplierForPlayer_ReligionNotInCiv_ReturnsDefault()
    {
        // Arrange
        var playerUID = "player-1";
        var religionUID = "religion-1";

        var religion = new ReligionData { ReligionUID = religionUID };

        _mockReligionManager.Setup(r => r.GetPlayerReligion(playerUID)).Returns(religion);
        _mockCivilizationManager.Setup(c => c.GetCivilizationByReligion(religionUID)).Returns((Civilization?)null);

        // Act
        var result = _bonusSystem.GetFavorMultiplierForPlayer(playerUID);

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void GetFavorMultiplierForPlayer_EmptyPlayerUID_ReturnsDefault()
    {
        // Act
        var result = _bonusSystem.GetFavorMultiplierForPlayer(string.Empty);

        // Assert
        Assert.Equal(1.0f, result);
    }

    #endregion

    #region GetPrestigeMultiplierForPlayer Tests

    [Fact]
    public void GetPrestigeMultiplierForPlayer_PlayerInCiv_ReturnsBonusValue()
    {
        // Arrange
        var playerUID = "player-1";
        var religionUID = "religion-1";
        var civId = "civ-1";

        var religion = new ReligionData { ReligionUID = religionUID };
        var civ = new Civilization { CivId = civId };
        var bonuses = new CivilizationBonuses { PrestigeMultiplier = 1.35f };

        _mockReligionManager.Setup(r => r.GetPlayerReligion(playerUID)).Returns(religion);
        _mockCivilizationManager.Setup(c => c.GetCivilizationByReligion(religionUID)).Returns(civ);
        _mockMilestoneManager.Setup(m => m.GetActiveBonuses(civId)).Returns(bonuses);

        // Act
        var result = _bonusSystem.GetPrestigeMultiplierForPlayer(playerUID);

        // Assert
        Assert.Equal(1.35f, result);
    }

    [Fact]
    public void GetPrestigeMultiplierForPlayer_PlayerNotInCiv_ReturnsDefault()
    {
        // Arrange
        var playerUID = "player-1";

        _mockReligionManager.Setup(r => r.GetPlayerReligion(playerUID)).Returns((ReligionData?)null);

        // Act
        var result = _bonusSystem.GetPrestigeMultiplierForPlayer(playerUID);

        // Assert
        Assert.Equal(1.0f, result);
    }

    #endregion

    #region GetBonusHolySiteSlotsForReligion Tests

    [Fact]
    public void GetBonusHolySiteSlotsForReligion_ReligionInCiv_ReturnsBonusSlots()
    {
        // Arrange
        var religionUID = "religion-1";
        var civId = "civ-1";

        var civ = new Civilization { CivId = civId };

        _mockCivilizationManager.Setup(c => c.GetCivilizationByReligion(religionUID)).Returns(civ);
        _mockMilestoneManager.Setup(m => m.GetBonusHolySiteSlots(civId)).Returns(3);

        // Act
        var result = _bonusSystem.GetBonusHolySiteSlotsForReligion(religionUID);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetBonusHolySiteSlotsForReligion_ReligionNotInCiv_ReturnsZero()
    {
        // Arrange
        var religionUID = "religion-1";

        _mockCivilizationManager.Setup(c => c.GetCivilizationByReligion(religionUID)).Returns((Civilization?)null);

        // Act
        var result = _bonusSystem.GetBonusHolySiteSlotsForReligion(religionUID);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetBonusHolySiteSlotsForReligion_EmptyReligionUID_ReturnsZero()
    {
        // Act
        var result = _bonusSystem.GetBonusHolySiteSlotsForReligion(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion
}
