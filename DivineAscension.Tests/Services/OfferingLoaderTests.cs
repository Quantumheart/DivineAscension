using System.Text;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Services;

public class OfferingLoaderTests
{
    private readonly Mock<IAssetManager> _mockAssetManager;
    private readonly Mock<ILoggerWrapper> _mockLogger;

    public OfferingLoaderTests()
    {
        _mockLogger = new Mock<ILoggerWrapper>();
        _mockAssetManager = new Mock<IAssetManager>();
    }

    #region LoadOfferings Tests - No Files

    [Fact]
    public void LoadOfferings_WhenNoFilesExist_DoesNotCrash()
    {
        // Arrange
        _mockAssetManager
            .Setup(a => a.Get(It.IsAny<AssetLocation>()))
            .Returns((IAsset?)null);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert - should not throw
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Empty(offerings);
    }

    #endregion

    #region LoadOfferings Tests - Multiple Files

    [Fact]
    public void LoadOfferings_WithMultipleFiles_LoadsAllDomains()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateValidCraftJson());
        SetupMockAsset("config/offerings/wild.json", CreateValidWildJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var craftOfferings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        var wildOfferings = loader.GetOfferingsForDomain(DeityDomain.Wild);
        Assert.NotEmpty(craftOfferings);
        Assert.NotEmpty(wildOfferings);
    }

    #endregion

    #region LoadOfferings Tests - Optional Fields

    [Fact]
    public void LoadOfferings_WithNullDescription_UsesEmptyString()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 1
                }
            ]
        }";
        SetupMockAsset("config/offerings/craft.json", json);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Single(offerings);
        Assert.Equal(string.Empty, offerings[0].Description);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfferingLoader(null!, _mockAssetManager.Object));
    }

    [Fact]
    public void Constructor_WithNullAssetManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OfferingLoader(_mockLogger.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Assert
        Assert.NotNull(loader);
    }

    #endregion

    #region LoadOfferings Tests - Single File

    [Fact]
    public void LoadOfferings_WithValidCraftFile_LoadsOfferings()
    {
        // Arrange
        var craftJson = CreateValidCraftJson();
        SetupMockAsset("config/offerings/craft.json", craftJson);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.NotEmpty(offerings);
    }

    [Fact]
    public void LoadOfferings_WithValidOffering_MapsAllFieldsCorrectly()
    {
        // Arrange
        var craftJson = CreateSingleOfferingJson();
        SetupMockAsset("config/offerings/craft.json", craftJson);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Single(offerings);
        var offering = offerings[0];
        Assert.Equal("Copper Ingot", offering.Name);
        Assert.Single(offering.ItemCodes);
        Assert.Equal("game:ingot-copper", offering.ItemCodes[0]);
        Assert.Equal(1, offering.Tier);
        Assert.Equal(2, offering.Value);
        Assert.Equal(1, offering.MinHolySiteTier);
        Assert.Equal("Basic metalworking material", offering.Description);
    }

    #endregion

    #region LoadOfferings Tests - Error Handling

    [Fact]
    public void LoadOfferings_WithMalformedJson_SkipsFileAndContinues()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", "{ invalid json }");
        SetupMockAsset("config/offerings/wild.json", CreateValidWildJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var craftOfferings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        var wildOfferings = loader.GetOfferingsForDomain(DeityDomain.Wild);
        Assert.Empty(craftOfferings);
        Assert.NotEmpty(wildOfferings);
    }

    [Fact]
    public void LoadOfferings_WithInvalidDomain_SkipsFile()
    {
        // Arrange
        var invalidDomainJson = @"{
            ""domain"": ""InvalidDomain"",
            ""version"": 1,
            ""offerings"": []
        }";
        SetupMockAsset("config/offerings/craft.json", invalidDomainJson);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Empty(offerings);
    }

    [Fact]
    public void LoadOfferings_WithMissingName_SkipsOffering()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": """",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 1
                }
            ]
        }";
        SetupMockAsset("config/offerings/craft.json", json);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Empty(offerings);
    }

    [Fact]
    public void LoadOfferings_WithMissingItemCodes_SkipsOffering()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 1
                }
            ]
        }";
        SetupMockAsset("config/offerings/craft.json", json);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Empty(offerings);
    }

    [Fact]
    public void LoadOfferings_WithInvalidTier_SkipsOffering()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 5,
                    ""value"": 2,
                    ""minHolySiteTier"": 1
                }
            ]
        }";
        SetupMockAsset("config/offerings/craft.json", json);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Empty(offerings);
    }

    [Fact]
    public void LoadOfferings_WithInvalidMinHolySiteTier_UsesDefault()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 5
                }
            ]
        }";
        SetupMockAsset("config/offerings/craft.json", json);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Single(offerings);
        Assert.Equal(1, offerings[0].MinHolySiteTier); // Should use default
    }

    [Fact]
    public void LoadOfferings_WithInvalidValue_SkipsOffering()
    {
        // Arrange
        var json = @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 1,
                    ""value"": 0,
                    ""minHolySiteTier"": 1
                }
            ]
        }";
        SetupMockAsset("config/offerings/craft.json", json);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);

        // Act
        loader.LoadOfferings();

        // Assert
        var offerings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        Assert.Empty(offerings);
    }

    #endregion

    #region FindOfferingByItemCode Tests

    [Fact]
    public void FindOfferingByItemCode_WithValidCode_ReturnsOffering()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateSingleOfferingJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.FindOfferingByItemCode("game:ingot-copper", DeityDomain.Craft);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Copper Ingot", result.Name);
    }

    [Fact]
    public void FindOfferingByItemCode_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateSingleOfferingJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.FindOfferingByItemCode("game:nonexistent", DeityDomain.Craft);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindOfferingByItemCode_WithWrongDomain_ReturnsNull()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateSingleOfferingJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.FindOfferingByItemCode("game:ingot-copper", DeityDomain.Wild);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindOfferingByItemCode_WithNullItemCode_ReturnsNull()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateSingleOfferingJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.FindOfferingByItemCode(null!, DeityDomain.Craft);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindOfferingByItemCode_WithEmptyItemCode_ReturnsNull()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateSingleOfferingJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.FindOfferingByItemCode("", DeityDomain.Craft);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindOfferingByItemCode_CaseInsensitive_ReturnsOffering()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateSingleOfferingJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.FindOfferingByItemCode("GAME:INGOT-COPPER", DeityDomain.Craft);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Copper Ingot", result.Name);
    }

    #endregion

    #region GetOfferingsForDomain Tests

    [Fact]
    public void GetOfferingsForDomain_WithNoLoadedOfferings_ReturnsEmpty()
    {
        // Arrange
        _mockAssetManager
            .Setup(a => a.Get(It.IsAny<AssetLocation>()))
            .Returns((IAsset?)null);

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var result = loader.GetOfferingsForDomain(DeityDomain.Craft);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetOfferingsForDomain_WithLoadedOfferings_ReturnsMatchingDomain()
    {
        // Arrange
        SetupMockAsset("config/offerings/craft.json", CreateValidCraftJson());
        SetupMockAsset("config/offerings/wild.json", CreateValidWildJson());

        var loader = new OfferingLoader(_mockLogger.Object, _mockAssetManager.Object);
        loader.LoadOfferings();

        // Act
        var craftOfferings = loader.GetOfferingsForDomain(DeityDomain.Craft);
        var wildOfferings = loader.GetOfferingsForDomain(DeityDomain.Wild);

        // Assert
        Assert.NotEmpty(craftOfferings);
        Assert.NotEmpty(wildOfferings);
        Assert.NotEqual(craftOfferings.Count, wildOfferings.Count);
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
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 1,
                    ""description"": ""Basic metalworking material""
                },
                {
                    ""name"": ""Iron Ingot"",
                    ""itemCodes"": [""game:ingot-iron""],
                    ""tier"": 2,
                    ""value"": 5,
                    ""minHolySiteTier"": 2,
                    ""description"": ""Common metalworking material""
                }
            ]
        }";
    }

    private static string CreateValidWildJson()
    {
        return @"{
            ""domain"": ""Wild"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Bushmeat"",
                    ""itemCodes"": [""game:bushmeat""],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 1,
                    ""description"": ""Small game meat""
                }
            ]
        }";
    }

    private static string CreateSingleOfferingJson()
    {
        return @"{
            ""domain"": ""Craft"",
            ""version"": 1,
            ""offerings"": [
                {
                    ""name"": ""Copper Ingot"",
                    ""itemCodes"": [""game:ingot-copper""],
                    ""tier"": 1,
                    ""value"": 2,
                    ""minHolySiteTier"": 1,
                    ""description"": ""Basic metalworking material""
                }
            ]
        }";
    }

    #endregion
}