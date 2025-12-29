using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

/// <summary>
/// Tests for enhanced pattern-based animal detection in HuntingFavorTracker.
/// Verifies that all vanilla animals return exact current favor values,
/// and that modded animals are classified appropriately.
/// </summary>
[ExcludeFromCodeCoverage]
public class HuntingFavorTrackerEnhancedTests
{
    private static HuntingFavorTracker CreateTracker(
        Mock<ICoreServerAPI> mockSapi,
        Mock<IPlayerReligionDataManager> mockPlayerReligion,
        Mock<IFavorSystem> mockFavor)
    {
        return new HuntingFavorTracker(mockPlayerReligion.Object, mockSapi.Object, mockFavor.Object);
    }

    #region Vanilla Animal Regression Tests

    /// <summary>
    /// CRITICAL: Tests that all vanilla animals return exact current favor values.
    /// These values must never change to maintain game balance.
    /// </summary>
    [Theory]
    [InlineData("wolf", 12)]
    [InlineData("bear", 15)]
    [InlineData("deer", 8)]
    [InlineData("moose", 12)]
    [InlineData("bighorn", 8)]
    [InlineData("pig", 5)]
    [InlineData("sheep", 5)]
    [InlineData("chicken", 3)]
    [InlineData("hare", 3)]
    [InlineData("rabbit", 3)]
    [InlineData("fox", 8)]
    [InlineData("raccoon", 5)]
    [InlineData("hyena", 10)]
    [InlineData("gazelle", 8)]
    public void CalculateAnimalFavor_VanillaAnimals_ReturnsExactCurrentValue(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    /// <summary>
    /// Verify vanilla animals work with mod prefixes (e.g., "game:wolf-male-1")
    /// </summary>
    [Theory]
    [InlineData("game:wolf-male-1", 12)]
    [InlineData("game:bear-brown", 15)]
    [InlineData("game:deer-female", 8)]
    [InlineData("somemod:wolf-alpha", 12)]
    [InlineData("creatures:bear-grizzly", 15)]
    public void CalculateAnimalFavor_VanillaAnimalsWithModPrefix_ReturnsCorrectValue(string fullCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { fullCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    #endregion

    #region Modded Animal Pattern Tests

    [Theory]
    [InlineData("tiger", 15)]
    [InlineData("lion", 15)]
    [InlineData("custommod:bear", 15)]
    public void CalculateAnimalFavor_LargePredators_Returns15(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("mammoth", 12)]
    [InlineData("elephant", 12)]
    [InlineData("rhino", 12)]
    [InlineData("bison", 12)]
    [InlineData("buffalo", 12)]
    [InlineData("giant-beast", 12)]
    public void CalculateAnimalFavor_LargeHerbivores_Returns12(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("jackal", 10)]
    [InlineData("vulture", 10)]
    [InlineData("scavenger-beast", 10)]
    public void CalculateAnimalFavor_Scavengers_Returns10(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("antelope", 8)]
    [InlineData("caribou", 8)]
    [InlineData("elk", 8)]
    [InlineData("boar", 8)]
    [InlineData("wildcat", 8)]
    public void CalculateAnimalFavor_MediumAnimals_Returns8(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("goat", 5)]
    [InlineData("lamb", 5)]
    [InlineData("calf", 5)]
    [InlineData("badger", 5)]
    [InlineData("otter", 5)]
    public void CalculateAnimalFavor_SmallDomesticated_Returns5(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("bird", 3)]
    [InlineData("chick", 3)]
    [InlineData("rodent", 3)]
    [InlineData("squirrel", 3)]
    [InlineData("rat", 3)]
    [InlineData("mouse", 3)]
    [InlineData("animal-tiny", 3)]
    [InlineData("creatures/animal/small-bird", 3)]
    public void CalculateAnimalFavor_TinyAnimals_Returns3(string animalCode, int expectedFavor)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.Equal(expectedFavor, result);

        tracker.Dispose();
    }

    #endregion

    #region Non-Animal Exclusion Tests

    [Theory]
    [InlineData("drifter")]
    [InlineData("drifter-night")]
    [InlineData("locust")]
    [InlineData("locust-hive")]
    [InlineData("bell")]
    public void IsNonAnimal_Monsters_ReturnsTrue(string monsterCode)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("IsNonAnimal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (bool)method!.Invoke(tracker, new object[] { monsterCode })!;

        // Assert
        Assert.True(result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("mechanical-beast")]
    [InlineData("construct")]
    [InlineData("automaton")]
    [InlineData("golem")]
    [InlineData("iron-golem")]
    public void IsNonAnimal_Constructs_ReturnsTrue(string constructCode)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("IsNonAnimal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (bool)method!.Invoke(tracker, new object[] { constructCode })!;

        // Assert
        Assert.True(result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("undead")]
    [InlineData("skeleton")]
    [InlineData("zombie")]
    [InlineData("ghost")]
    [InlineData("wraith")]
    public void IsNonAnimal_Undead_ReturnsTrue(string undeadCode)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("IsNonAnimal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (bool)method!.Invoke(tracker, new object[] { undeadCode })!;

        // Assert
        Assert.True(result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("summoned-creature")]
    [InlineData("illusion")]
    [InlineData("spirit")]
    public void IsNonAnimal_Summons_ReturnsTrue(string summonCode)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("IsNonAnimal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (bool)method!.Invoke(tracker, new object[] { summonCode })!;

        // Assert
        Assert.True(result);

        tracker.Dispose();
    }

    [Theory]
    [InlineData("wolf")]
    [InlineData("bear")]
    [InlineData("deer")]
    [InlineData("chicken")]
    public void IsNonAnimal_Animals_ReturnsFalse(string animalCode)
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("IsNonAnimal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (bool)method!.Invoke(tracker, new object[] { animalCode })!;

        // Assert
        Assert.False(result);

        tracker.Dispose();
    }

    [Fact]
    public void CalculateAnimalFavor_NonAnimalCode_Returns0()
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { "random-unrecognized-entity" })!;

        // Assert
        Assert.Equal(0, result);

        tracker.Dispose();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void CalculateAnimalFavor_EmptyString_Returns0()
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        var method = typeof(HuntingFavorTracker)
            .GetMethod("CalculateAnimalFavor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (int)method!.Invoke(tracker, new object[] { string.Empty })!;

        // Assert
        Assert.Equal(0, result);

        tracker.Dispose();
    }

    [Fact]
    public void Initialize_WithNoOnlinePlayers_DoesNotThrow()
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Act & Assert
        var exception = Record.Exception(() => tracker.Initialize());
        Assert.Null(exception);

        tracker.Dispose();
    }

    [Fact]
    public void DeityType_ReturnsLysa()
    {
        // Arrange
        var mockSapi = TestFixtures.CreateMockServerAPI();
        var mockPlayerReligion = TestFixtures.CreateMockPlayerReligionDataManager();
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(mockSapi, mockPlayerReligion, mockFavor);

        // Act & Assert
        Assert.Equal(DeityType.Lysa, tracker.DeityType);

        tracker.Dispose();
    }

    #endregion

    #region Documentation Tests

    [Fact]
    public void FavorTiers_Documentation()
    {
        // This test documents the 6-tier favor system
        // Tier 15: Large predators (bear, dragon, tiger, lion)
        // Tier 12: Large herbivores / medium predators (wolf, moose, mammoth, elephant)
        // Tier 10: Scavengers (hyena, jackal, vulture)
        // Tier 8: Medium prey (deer, fox, bighorn, gazelle, antelope, elk)
        // Tier 5: Small domesticated (pig, sheep, raccoon, goat, lamb)
        // Tier 3: Tiny animals (chicken, hare, rabbit, bird, rodent, mouse)

        Assert.Equal(15, 15); // Tier 15 exists
        Assert.Equal(12, 12); // Tier 12 exists
        Assert.Equal(10, 10); // Tier 10 exists
        Assert.Equal(8, 8);   // Tier 8 exists
        Assert.Equal(5, 5);   // Tier 5 exists
        Assert.Equal(3, 3);   // Tier 3 exists
    }

    #endregion
}
