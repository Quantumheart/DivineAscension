using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using Moq;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;

namespace PantheonWars.Tests.Systems;

/// <summary>
///     Unit tests for DeityRegistry
///     Tests initialization, deity retrieval, relationships, and favor multipliers
/// </summary>
[ExcludeFromCodeCoverage]
public class DeityRegistryTests
{
    private readonly Mock<ICoreAPI> _mockApi;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DeityRegistry _registry;

    public DeityRegistryTests()
    {
        _mockApi = TestFixtures.CreateMockCoreAPI();
        _mockLogger = new Mock<ILogger>();
        _mockApi.Setup(a => a.Logger).Returns(_mockLogger.Object);

        _registry = new DeityRegistry(_mockApi.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersKhorasAndLysa_Successfully()
    {
        // Act
        _registry.Initialize();

        // Assert
        Assert.NotNull(_registry.GetDeity(DeityType.Khoras));
        Assert.NotNull(_registry.GetDeity(DeityType.Lysa));
        Assert.Equal(2, _registry.GetAllDeities().Count());
    }

    [Fact]
    public void Initialize_LogsCorrectDeityCount()
    {
        // Act
        _registry.Initialize();

        // Assert
        TestFixtures.VerifyLoggerNotification(_mockLogger, "Registered 2 deities");
    }

    [Fact]
    public void Initialize_LogsInitializationMessage()
    {
        // Act
        _registry.Initialize();

        // Assert
        TestFixtures.VerifyLoggerNotification(_mockLogger, "Initializing Deity Registry");
    }

    #endregion

    #region Retrieval Tests

    [Fact]
    public void GetDeity_WithValidType_ReturnsCorrectDeity()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deity = _registry.GetDeity(DeityType.Khoras);

        // Assert
        Assert.NotNull(deity);
        Assert.Equal("Khoras", deity.Name);
        Assert.Equal("War", deity.Domain);
        Assert.Equal(DeityAlignment.Lawful, deity.Alignment);
    }


    [Fact]
    public void GetDeity_ForLysa_ReturnsCorrectDeity()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deity = _registry.GetDeity(DeityType.Lysa);

        // Assert
        Assert.NotNull(deity);
        Assert.Equal("Lysa", deity.Name);
        Assert.Equal("Hunt", deity.Domain);
        Assert.Equal(DeityAlignment.Neutral, deity.Alignment);
    }

    [Fact]
    public void GetAllDeities_ReturnsAllRegisteredDeities()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deities = _registry.GetAllDeities().ToList();

        // Assert
        Assert.Equal(2, deities.Count);
        Assert.Contains(deities, d => d.Name == "Khoras");
        Assert.Contains(deities, d => d.Name == "Lysa");
    }

    [Fact]
    public void GetAllDeities_BeforeInitialization_ReturnsEmpty()
    {
        // Act
        var deities = _registry.GetAllDeities().ToList();

        // Assert
        Assert.Empty(deities);
    }

    [Fact]
    public void HasDeity_WithRegisteredType_ReturnsTrue()
    {
        // Arrange
        _registry.Initialize();

        // Act & Assert
        Assert.True(_registry.HasDeity(DeityType.Khoras));
        Assert.True(_registry.HasDeity(DeityType.Lysa));
    }

    #endregion

    #region Relationship Tests

    [Fact]
    public void GetRelationship_BetweenAlliedDeities_ReturnsAllied()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var relationship = _registry.GetRelationship(DeityType.Khoras, DeityType.Lysa);

        // Assert
        Assert.Equal(DeityRelationshipType.Allied, relationship);
    }

    [Fact]
    public void GetRelationship_WithSameDeity_ReturnsNeutral()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var relationship = _registry.GetRelationship(DeityType.Khoras, DeityType.Khoras);

        // Assert
        Assert.Equal(DeityRelationshipType.Neutral, relationship);
    }

    [Fact]
    public void GetRelationship_WithUnregisteredDeity_ReturnsNeutral()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var relationship = _registry.GetRelationship(DeityType.Khoras, DeityType.Aethra);

        // Assert
        Assert.Equal(DeityRelationshipType.Neutral, relationship);
    }

    #endregion

    #region Favor Multiplier Tests

    [Fact]
    public void GetFavorMultiplier_ForAlliedDeities_Returns0Point5()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var multiplier = _registry.GetFavorMultiplier(DeityType.Khoras, DeityType.Lysa);

        // Assert
        Assert.Equal(0.5f, multiplier);
    }

    [Fact]
    public void GetFavorMultiplier_ForNeutralDeities_Returns1Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var multiplier = _registry.GetFavorMultiplier(DeityType.Khoras, DeityType.Aethra);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetFavorMultiplier_ForSameDeity_Returns1Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act - Checking same deity (through Neutral relationship)
        var multiplier = _registry.GetFavorMultiplier(DeityType.Khoras, DeityType.Khoras);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    #endregion

    #region Deity Properties Tests

    [Fact]
    public void Khoras_HasCorrectProperties()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var khoras = _registry.GetDeity(DeityType.Khoras);

        // Assert
        Assert.NotNull(khoras);
        Assert.Equal("Khoras", khoras.Name);
        Assert.Equal("War", khoras.Domain);
        Assert.Equal(DeityAlignment.Lawful, khoras.Alignment);
        Assert.Equal("#8B0000", khoras.PrimaryColor);
        Assert.Equal("#FFD700", khoras.SecondaryColor);
        Assert.NotEmpty(khoras.Description);
        Assert.NotEmpty(khoras.Playstyle);
        Assert.NotEmpty(khoras.AbilityIds);
    }

    [Fact]
    public void Lysa_HasCorrectProperties()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var lysa = _registry.GetDeity(DeityType.Lysa);

        // Assert
        Assert.NotNull(lysa);
        Assert.Equal("Lysa", lysa.Name);
        Assert.Equal("Hunt", lysa.Domain);
        Assert.Equal(DeityAlignment.Neutral, lysa.Alignment);
        Assert.Equal("#228B22", lysa.PrimaryColor);
        Assert.Equal("#8B4513", lysa.SecondaryColor);
        Assert.NotEmpty(lysa.Description);
        Assert.NotEmpty(lysa.Playstyle);
        Assert.NotEmpty(lysa.AbilityIds);
    }

    #endregion
}