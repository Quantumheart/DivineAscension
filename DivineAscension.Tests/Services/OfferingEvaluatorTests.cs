using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Services;

public class OfferingEvaluatorTests
{
    private readonly Mock<IOfferingLoader> _offeringLoader;
    private readonly OfferingEvaluator _evaluator;

    public OfferingEvaluatorTests()
    {
        _offeringLoader = new Mock<IOfferingLoader>();
        _evaluator = new OfferingEvaluator(_offeringLoader.Object);
    }

    [Fact]
    public void Constructor_NullOfferingLoader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OfferingEvaluator(null!));
    }

    [Fact]
    public void CalculateOfferingValue_ValidOffering_ReturnsValue()
    {
        // Arrange
        // Use a loose mock that won't throw on non-virtual properties
        var mockItemStack = new Mock<ItemStack>(MockBehavior.Loose);
        var offering = new Offering(
            Name: "Copper Ingot",
            ItemCodes: new List<string> { "game:ingot-copper" },
            Tier: 1,
            Value: 10,
            MinHolySiteTier: 1,
            Description: "A copper ingot");

        // The evaluator calls offering.Collectible?.Code?.ToString() which returns empty string for mock
        // So we setup the loader to return the offering for empty string
        _offeringLoader.Setup(x => x.FindOfferingByItemCode("", DeityDomain.Craft))
            .Returns(offering);

        // Act
        var result = _evaluator.CalculateOfferingValue(mockItemStack.Object, DeityDomain.Craft, 1);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void CalculateOfferingValue_InvalidOfferingForDomain_ReturnsZero()
    {
        // Arrange
        var mockItemStack = new Mock<ItemStack>(MockBehavior.Loose);

        _offeringLoader.Setup(x => x.FindOfferingByItemCode("", DeityDomain.Wild))
            .Returns((Offering?)null);

        // Act
        var result = _evaluator.CalculateOfferingValue(mockItemStack.Object, DeityDomain.Wild, 1);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateOfferingValue_TierTooLow_ReturnsMinusOne()
    {
        // Arrange
        var mockItemStack = new Mock<ItemStack>(MockBehavior.Loose);
        var offering = new Offering(
            Name: "Gold Ingot",
            ItemCodes: new List<string> { "game:ingot-gold" },
            Tier: 3,
            Value: 50,
            MinHolySiteTier: 3, // Requires tier 3
            Description: "A gold ingot");

        _offeringLoader.Setup(x => x.FindOfferingByItemCode("", DeityDomain.Craft))
            .Returns(offering);

        // Act
        var result = _evaluator.CalculateOfferingValue(mockItemStack.Object, DeityDomain.Craft, 1); // Tier 1

        // Assert
        Assert.Equal(-1, result);
    }

    [Theory]
    [InlineData(1, 1, 10)] // Tier 1 holy site, tier 1 requirement = valid
    [InlineData(2, 1, 10)] // Tier 2 holy site, tier 1 requirement = valid
    [InlineData(3, 2, 25)] // Tier 3 holy site, tier 2 requirement = valid
    [InlineData(2, 2, 25)] // Tier 2 holy site, tier 2 requirement = valid
    public void CalculateOfferingValue_SufficientTier_ReturnsValue(int holySiteTier, int minRequiredTier, int expectedValue)
    {
        // Arrange
        var mockItemStack = new Mock<ItemStack>(MockBehavior.Loose);
        var offering = new Offering(
            Name: "Test Ingot",
            ItemCodes: new List<string> { "game:ingot-test" },
            Tier: minRequiredTier,
            Value: expectedValue,
            MinHolySiteTier: minRequiredTier,
            Description: "A test ingot");

        _offeringLoader.Setup(x => x.FindOfferingByItemCode("", DeityDomain.Craft))
            .Returns(offering);

        // Act
        var result = _evaluator.CalculateOfferingValue(mockItemStack.Object, DeityDomain.Craft, holySiteTier);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void CalculateOfferingValue_NullCollectible_ReturnsZero()
    {
        // Arrange
        var mockItemStack = new Mock<ItemStack>(MockBehavior.Loose);
        // Default loose mock behavior: Collectible returns null

        _offeringLoader.Setup(x => x.FindOfferingByItemCode("", DeityDomain.Craft))
            .Returns((Offering?)null);

        // Act
        var result = _evaluator.CalculateOfferingValue(mockItemStack.Object, DeityDomain.Craft, 1);

        // Assert
        Assert.Equal(0, result);
    }
}
