using System.Text;
using DivineAscension.Models.Dto;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Services;

public class BlessingLoaderTests
{
    private readonly Mock<ICoreAPI> _mockApi;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly Mock<IAssetManager> _mockAssetManager;

    public BlessingLoaderTests()
    {
        _mockApi = TestFixtures.CreateMockCoreAPI();
        _mockLogger = new Mock<ILoggerWrapper>();
        _mockAssetManager = new Mock<IAssetManager>();
        _mockApi.Setup(a => a.Assets).Returns(_mockAssetManager.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullApi_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlessingLoader(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithValidApi_CreatesInstance()
    {
        // Act
        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(loader);
        Assert.False(loader.LoadedSuccessfully);
        Assert.Equal(0, loader.LoadedCount);
    }

    #endregion

    #region LoadBlessings Tests - No Files

    [Fact]
    public void LoadBlessings_WhenNoFilesExist_ReturnsEmptyList()
    {
        // Arrange
        _mockAssetManager
            .Setup(a => a.Get(It.IsAny<AssetLocation>()))
            .Returns((IAsset?)null);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Empty(result);
        Assert.False(loader.LoadedSuccessfully);
        Assert.Equal(0, loader.LoadedCount);
    }

    #endregion

    #region LoadBlessings Tests - Single File

    [Fact]
    public void LoadBlessings_WithValidCraftFile_LoadsBlessings()
    {
        // Arrange
        var craftJson = CreateValidCraftJson();
        SetupMockAsset("config/blessings/craft.json", craftJson);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(loader.LoadedSuccessfully);
        Assert.Equal(result.Count, loader.LoadedCount);
        Assert.All(result, b => Assert.Equal(DeityDomain.Craft, b.Domain));
    }

    [Fact]
    public void LoadBlessings_WithValidBlessing_MapsAllFieldsCorrectly()
    {
        // Arrange
        var craftJson = CreateSingleBlessingJson();
        SetupMockAsset("config/blessings/craft.json", craftJson);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        var blessing = result[0];
        Assert.Equal("test_blessing", blessing.BlessingId);
        Assert.Equal("Test Blessing", blessing.Name);
        Assert.Equal("A test description.", blessing.Description);
        Assert.Equal(DeityDomain.Craft, blessing.Domain);
        Assert.Equal(BlessingKind.Player, blessing.Kind);
        Assert.Equal(BlessingCategory.Utility, blessing.Category);
        Assert.Equal("hammer-drop", blessing.IconName);
        Assert.Equal(0, blessing.RequiredFavorRank);
        Assert.Equal(0, blessing.RequiredPrestigeRank);
        Assert.Empty(blessing.PrerequisiteBlessings!);
        Assert.Single(blessing.StatModifiers);
        Assert.Equal(0.10f, blessing.StatModifiers["toolDurability"]);
        Assert.Empty(blessing.SpecialEffects!);
    }

    #endregion

    #region LoadBlessings Tests - Multiple Files

    [Fact]
    public void LoadBlessings_WithMultipleFiles_LoadsAllDomains()
    {
        // Arrange
        SetupMockAsset("config/blessings/craft.json", CreateValidCraftJson());
        SetupMockAsset("config/blessings/wild.json", CreateValidWildJson());

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.True(loader.LoadedSuccessfully);
        Assert.Contains(result, b => b.Domain == DeityDomain.Craft);
        Assert.Contains(result, b => b.Domain == DeityDomain.Wild);
    }

    #endregion

    #region LoadBlessings Tests - Error Handling

    [Fact]
    public void LoadBlessings_WithMalformedJson_SkipsFileAndContinues()
    {
        // Arrange
        SetupMockAsset("config/blessings/craft.json", "{ invalid json }");
        SetupMockAsset("config/blessings/wild.json", CreateValidWildJson());

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.True(loader.LoadedSuccessfully);
        Assert.All(result, b => Assert.Equal(DeityDomain.Wild, b.Domain));
    }

    [Fact]
    public void LoadBlessings_WithInvalidDomain_SkipsFile()
    {
        // Arrange
        var invalidDomainJson = @"{
            ""domain"": ""InvalidDomain"",
            ""version"": 1,
            ""blessings"": []
        }";
        SetupMockAsset("config/blessings/craft.json", invalidDomainJson);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Empty(result);
        Assert.False(loader.LoadedSuccessfully);
    }

    [Fact]
    public void LoadBlessings_WithMissingBlessingId_SkipsBlessing()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": """",
                    ""name"": ""Test"",
                    ""kind"": ""Player"",
                    ""category"": ""Utility""
                },
                {
                    ""blessingId"": ""valid_id"",
                    ""name"": ""Valid"",
                    ""kind"": ""Player"",
                    ""category"": ""Utility""
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        Assert.Equal("valid_id", result[0].BlessingId);
    }

    [Fact]
    public void LoadBlessings_WithMissingName_SkipsBlessing()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": """",
                    ""kind"": ""Player"",
                    ""category"": ""Utility""
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void LoadBlessings_WithInvalidKind_SkipsBlessing()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": ""Test"",
                    ""kind"": ""InvalidKind"",
                    ""category"": ""Utility""
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void LoadBlessings_WithInvalidCategory_UsesUtilityAsDefault()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": ""Test"",
                    ""kind"": ""Player"",
                    ""category"": ""InvalidCategory""
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        Assert.Equal(BlessingCategory.Utility, result[0].Category);
    }

    [Fact]
    public void LoadBlessings_WithUnknownStatKey_StillIncludesBlessing()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": ""Test"",
                    ""kind"": ""Player"",
                    ""category"": ""Utility"",
                    ""statModifiers"": {
                        ""unknownStat"": 0.5
                    }
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        Assert.Contains("unknownStat", result[0].StatModifiers.Keys);
    }

    #endregion

    #region LoadBlessings Tests - Optional Fields

    [Fact]
    public void LoadBlessings_WithNullOptionalFields_UsesDefaults()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": ""Test"",
                    ""kind"": ""Player"",
                    ""category"": ""Utility""
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        var blessing = result[0];
        Assert.Equal(string.Empty, blessing.Description);
        Assert.Equal(string.Empty, blessing.IconName);
        Assert.Equal(0, blessing.RequiredFavorRank);
        Assert.Equal(0, blessing.RequiredPrestigeRank);
        Assert.Empty(blessing.PrerequisiteBlessings!);
        Assert.Empty(blessing.StatModifiers);
        Assert.Empty(blessing.SpecialEffects!);
    }

    [Fact]
    public void LoadBlessings_WithPrerequisites_ParsesCorrectly()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": ""Test"",
                    ""kind"": ""Player"",
                    ""category"": ""Utility"",
                    ""prerequisiteBlessings"": [""prereq_1"", ""prereq_2""]
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].PrerequisiteBlessings!.Count);
        Assert.Contains("prereq_1", result[0].PrerequisiteBlessings);
        Assert.Contains("prereq_2", result[0].PrerequisiteBlessings);
    }

    [Fact]
    public void LoadBlessings_WithSpecialEffects_ParsesCorrectly()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_id"",
                    ""name"": ""Test"",
                    ""kind"": ""Player"",
                    ""category"": ""Utility"",
                    ""specialEffects"": [""effect_1"", ""effect_2""]
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].SpecialEffects!.Count);
        Assert.Contains("effect_1", result[0].SpecialEffects);
        Assert.Contains("effect_2", result[0].SpecialEffects);
    }

    #endregion

    #region LoadBlessings Tests - Religion Blessings

    [Fact]
    public void LoadBlessings_WithReligionKind_ParsesCorrectly()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_religion_blessing"",
                    ""name"": ""Test Religion Blessing"",
                    ""kind"": ""Religion"",
                    ""category"": ""Utility"",
                    ""requiredPrestigeRank"": 2
                }
            ]
        }";
        SetupMockAsset("config/blessings/craft.json", json);

        var loader = new BlessingLoader(_mockApi.Object, _mockLogger.Object);

        // Act
        var result = loader.LoadBlessings();

        // Assert
        Assert.Single(result);
        Assert.Equal(BlessingKind.Religion, result[0].Kind);
        Assert.Equal(2, result[0].RequiredPrestigeRank);
    }

    #endregion

    #region Helper Methods

    private void SetupMockAsset(string path, string content)
    {
        var mockAsset = new Mock<IAsset>();
        mockAsset.Setup(a => a.Data).Returns(Encoding.UTF8.GetBytes(content));

        _mockAssetManager
            .Setup(a => a.Get(It.Is<AssetLocation>(loc =>
                loc.Domain == "divineascension" && loc.Path == path)))
            .Returns(mockAsset.Object);
    }

    private static string CreateValidCraftJson()
    {
        return @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""khoras_craftsmans_touch"",
                    ""name"": ""Craftsman's Touch"",
                    ""description"": ""+10% chance for tools to take no damage, +10% ore yield."",
                    ""kind"": ""Player"",
                    ""category"": ""Utility"",
                    ""iconName"": ""hammer-drop"",
                    ""requiredFavorRank"": 0,
                    ""requiredPrestigeRank"": 0,
                    ""prerequisiteBlessings"": [],
                    ""statModifiers"": {
                        ""toolDurability"": 0.10,
                        ""oreDropRate"": 0.10
                    },
                    ""specialEffects"": []
                }
            ]
        }";
    }

    private static string CreateValidWildJson()
    {
        return @"{
            ""domain"": ""Wild"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""lysa_hunters_instinct"",
                    ""name"": ""Hunter's Instinct"",
                    ""description"": ""+15% animal and forage drops, +5% movement speed."",
                    ""kind"": ""Player"",
                    ""category"": ""Utility"",
                    ""iconName"": ""paw"",
                    ""requiredFavorRank"": 0,
                    ""requiredPrestigeRank"": 0,
                    ""prerequisiteBlessings"": [],
                    ""statModifiers"": {
                        ""animalLootDropRate"": 0.15,
                        ""forageDropRate"": 0.15,
                        ""walkspeed"": 0.05
                    },
                    ""specialEffects"": []
                }
            ]
        }";
    }

    private static string CreateSingleBlessingJson()
    {
        return @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""blessings"": [
                {
                    ""blessingId"": ""test_blessing"",
                    ""name"": ""Test Blessing"",
                    ""description"": ""A test description."",
                    ""kind"": ""Player"",
                    ""category"": ""Utility"",
                    ""iconName"": ""hammer-drop"",
                    ""requiredFavorRank"": 0,
                    ""requiredPrestigeRank"": 0,
                    ""prerequisiteBlessings"": [],
                    ""statModifiers"": {
                        ""toolDurability"": 0.10
                    },
                    ""specialEffects"": []
                }
            ]
        }";
    }

    #endregion
}
