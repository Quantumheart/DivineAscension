using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moq;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Xunit;

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
    public void Initialize_RegistersAllThreeDeities_Successfully()
    {
        // Act
        _registry.Initialize();

        // Assert
        Assert.NotNull(_registry.GetDeity(DeityType.Aethra));
        Assert.NotNull(_registry.GetDeity(DeityType.Gaia));
        Assert.NotNull(_registry.GetDeity(DeityType.Morthen));
        Assert.Equal(3, _registry.GetAllDeities().Count());
    }

    [Fact]
    public void Initialize_LogsCorrectDeityCount()
    {
        // Act
        _registry.Initialize();

        // Assert
        TestFixtures.VerifyLoggerNotification(_mockLogger, "Registered 3 deities");
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
        var deity = _registry.GetDeity(DeityType.Aethra);

        // Assert
        Assert.NotNull(deity);
        Assert.Equal("Aethra", deity.Name);
        Assert.Equal("Light", deity.Domain);
        Assert.Equal(DeityAlignment.Lawful, deity.Alignment);
    }

    [Fact]
    public void GetDeity_WithInvalidType_ReturnsNull()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deity = _registry.GetDeity(DeityType.None);

        // Assert
        Assert.Null(deity);
    }

    [Fact]
    public void GetDeity_ForGaia_ReturnsCorrectDeity()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deity = _registry.GetDeity(DeityType.Gaia);

        // Assert
        Assert.NotNull(deity);
        Assert.Equal("Gaia", deity.Name);
        Assert.Equal("Nature", deity.Domain);
        Assert.Equal(DeityAlignment.Neutral, deity.Alignment);
    }

    [Fact]
    public void GetDeity_ForMorthen_ReturnsCorrectDeity()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deity = _registry.GetDeity(DeityType.Morthen);

        // Assert
        Assert.NotNull(deity);
        Assert.Equal("Morthen", deity.Name);
        Assert.Equal("Shadow & Death", deity.Domain);
        Assert.Equal(DeityAlignment.Chaotic, deity.Alignment);
    }

    [Fact]
    public void GetAllDeities_ReturnsAllRegisteredDeities()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var deities = _registry.GetAllDeities().ToList();

        // Assert
        Assert.Equal(3, deities.Count);
        Assert.Contains(deities, d => d.Name == "Aethra");
        Assert.Contains(deities, d => d.Name == "Gaia");
        Assert.Contains(deities, d => d.Name == "Morthen");
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
        Assert.True(_registry.HasDeity(DeityType.Aethra));
        Assert.True(_registry.HasDeity(DeityType.Gaia));
        Assert.True(_registry.HasDeity(DeityType.Morthen));
    }

    [Fact]
    public void HasDeity_WithUnregisteredType_ReturnsFalse()
    {
        // Arrange
        _registry.Initialize();

        // Act & Assert
        Assert.False(_registry.HasDeity(DeityType.None));
    }

    #endregion

    #region Relationship Tests

    [Fact]
    public void GetRelationship_BetweenNeutralDeities_ReturnsNeutral()
    {
        // Arrange
        _registry.Initialize();

        // Act - Aethra and Gaia are neutral
        var relationship = _registry.GetRelationship(DeityType.Aethra, DeityType.Gaia);

        // Assert
        Assert.Equal(DeityRelationshipType.Neutral, relationship);
    }

    [Fact]
    public void GetRelationship_BetweenRivalDeities_ReturnsRival()
    {
        // Arrange
        _registry.Initialize();

        // Act - Aethra (Light) and Morthen (Shadow) are rivals
        var relationship = _registry.GetRelationship(DeityType.Aethra, DeityType.Morthen);

        // Assert
        Assert.Equal(DeityRelationshipType.Rival, relationship);
    }

    [Fact]
    public void GetRelationship_RivalIsSymmetric_ReturnsRival()
    {
        // Arrange
        _registry.Initialize();

        // Act - Morthen and Aethra should also be rivals in reverse
        var relationship = _registry.GetRelationship(DeityType.Morthen, DeityType.Aethra);

        // Assert
        Assert.Equal(DeityRelationshipType.Rival, relationship);
    }

    [Fact]
    public void GetRelationship_WithSameDeity_ReturnsNeutral()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var relationship = _registry.GetRelationship(DeityType.Aethra, DeityType.Aethra);

        // Assert
        Assert.Equal(DeityRelationshipType.Neutral, relationship);
    }

    [Fact]
    public void GetRelationship_WithUnregisteredDeity_ReturnsNeutral()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var relationship = _registry.GetRelationship(DeityType.None, DeityType.Aethra);

        // Assert
        Assert.Equal(DeityRelationshipType.Neutral, relationship);
    }

    [Fact]
    public void GetRelationship_GaiaNeutralWithAll_ReturnsNeutral()
    {
        // Arrange
        _registry.Initialize();

        // Act - Gaia is neutral with both Aethra and Morthen
        var relationshipWithAethra = _registry.GetRelationship(DeityType.Gaia, DeityType.Aethra);
        var relationshipWithMorthen = _registry.GetRelationship(DeityType.Gaia, DeityType.Morthen);

        // Assert
        Assert.Equal(DeityRelationshipType.Neutral, relationshipWithAethra);
        Assert.Equal(DeityRelationshipType.Neutral, relationshipWithMorthen);
    }

    #endregion

    #region Favor Multiplier Tests

    [Fact]
    public void GetFavorMultiplier_ForNeutralDeities_Returns1Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act - Aethra and Gaia are neutral
        var multiplier = _registry.GetFavorMultiplier(DeityType.Aethra, DeityType.Gaia);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetFavorMultiplier_ForRivalDeities_Returns2Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act - Aethra and Morthen are rivals
        var multiplier = _registry.GetFavorMultiplier(DeityType.Aethra, DeityType.Morthen);

        // Assert
        Assert.Equal(2.0f, multiplier);
    }

    [Fact]
    public void GetFavorMultiplier_ForRivalReversed_Returns2Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act - Rivalry should work in both directions
        var multiplier = _registry.GetFavorMultiplier(DeityType.Morthen, DeityType.Aethra);

        // Assert
        Assert.Equal(2.0f, multiplier);
    }

    [Fact]
    public void GetFavorMultiplier_ForGaiaNeutral_Returns1Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act - Gaia is neutral with everyone
        var multiplierWithAethra = _registry.GetFavorMultiplier(DeityType.Gaia, DeityType.Aethra);
        var multiplierWithMorthen = _registry.GetFavorMultiplier(DeityType.Gaia, DeityType.Morthen);

        // Assert
        Assert.Equal(1.0f, multiplierWithAethra);
        Assert.Equal(1.0f, multiplierWithMorthen);
    }

    [Fact]
    public void GetFavorMultiplier_WithUnregisteredDeity_Returns1Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var multiplier = _registry.GetFavorMultiplier(DeityType.None, DeityType.Aethra);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetFavorMultiplier_ForSameDeity_Returns1Point0()
    {
        // Arrange
        _registry.Initialize();

        // Act - Same deity returns neutral relationship = 1.0 multiplier
        var multiplier = _registry.GetFavorMultiplier(DeityType.Aethra, DeityType.Aethra);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    #endregion

    #region Deity Properties Tests

    [Fact]
    public void Aethra_HasCorrectProperties()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var aethra = _registry.GetDeity(DeityType.Aethra);

        // Assert
        Assert.NotNull(aethra);
        Assert.Equal("Aethra", aethra.Name);
        Assert.Equal("Light", aethra.Domain);
        Assert.Equal(DeityAlignment.Lawful, aethra.Alignment);
        Assert.Equal("#FFFFE0", aethra.PrimaryColor);
        Assert.Equal("#FFD700", aethra.SecondaryColor);
        Assert.NotEmpty(aethra.Description);
        Assert.NotEmpty(aethra.Playstyle);
    }

    [Fact]
    public void Gaia_HasCorrectProperties()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var gaia = _registry.GetDeity(DeityType.Gaia);

        // Assert
        Assert.NotNull(gaia);
        Assert.Equal("Gaia", gaia.Name);
        Assert.Equal("Nature", gaia.Domain);
        Assert.Equal(DeityAlignment.Neutral, gaia.Alignment);
        Assert.Equal("#8B7355", gaia.PrimaryColor);
        Assert.Equal("#228B22", gaia.SecondaryColor);
        Assert.NotEmpty(gaia.Description);
        Assert.NotEmpty(gaia.Playstyle);
    }

    [Fact]
    public void Morthen_HasCorrectProperties()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var morthen = _registry.GetDeity(DeityType.Morthen);

        // Assert
        Assert.NotNull(morthen);
        Assert.Equal("Morthen", morthen.Name);
        Assert.Equal("Shadow & Death", morthen.Domain);
        Assert.Equal(DeityAlignment.Chaotic, morthen.Alignment);
        Assert.Equal("#4B0082", morthen.PrimaryColor);
        Assert.Equal("#2F4F4F", morthen.SecondaryColor);
        Assert.NotEmpty(morthen.Description);
        Assert.NotEmpty(morthen.Playstyle);
    }

    [Fact]
    public void Aethra_HasCorrectRelationships()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var aethra = _registry.GetDeity(DeityType.Aethra);

        // Assert
        Assert.NotNull(aethra);
        Assert.Equal(2, aethra.Relationships.Count);
        Assert.Equal(DeityRelationshipType.Neutral, aethra.Relationships[DeityType.Gaia]);
        Assert.Equal(DeityRelationshipType.Rival, aethra.Relationships[DeityType.Morthen]);
    }

    [Fact]
    public void Gaia_HasCorrectRelationships()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var gaia = _registry.GetDeity(DeityType.Gaia);

        // Assert
        Assert.NotNull(gaia);
        Assert.Equal(2, gaia.Relationships.Count);
        Assert.Equal(DeityRelationshipType.Neutral, gaia.Relationships[DeityType.Aethra]);
        Assert.Equal(DeityRelationshipType.Neutral, gaia.Relationships[DeityType.Morthen]);
    }

    [Fact]
    public void Morthen_HasCorrectRelationships()
    {
        // Arrange
        _registry.Initialize();

        // Act
        var morthen = _registry.GetDeity(DeityType.Morthen);

        // Assert
        Assert.NotNull(morthen);
        Assert.Equal(2, morthen.Relationships.Count);
        Assert.Equal(DeityRelationshipType.Neutral, morthen.Relationships[DeityType.Gaia]);
        Assert.Equal(DeityRelationshipType.Rival, morthen.Relationships[DeityType.Aethra]);
    }

    #endregion
}
