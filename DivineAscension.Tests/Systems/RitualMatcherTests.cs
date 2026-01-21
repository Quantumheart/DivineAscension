using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Systems;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
/// Tests for RitualMatcher item matching and glob pattern support.
/// </summary>
[ExcludeFromCodeCoverage]
public class RitualMatcherTests
{
    private readonly RitualMatcher _matcher;

    public RitualMatcherTests()
    {
        _matcher = new RitualMatcher();
    }

    #region DoesItemMatchRequirement Tests

    [Fact]
    public void DoesItemMatchRequirement_NullOffering_ReturnsFalse()
    {
        // Arrange
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper");

        // Act
        var result = _matcher.DoesItemMatchRequirement(null, requirement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_NullCollectible_ReturnsFalse()
    {
        // Arrange
        var offering = new ItemStack(); // No collectible set
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_ExactMatch_SingleCode_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_ExactMatch_MultipleCodesFirstMatch_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper", "game:ingot-bronze");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_ExactMatch_MultipleCodesSecondMatch_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-bronze");
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper", "game:ingot-bronze");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_ExactMatch_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:INGOT-COPPER");
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_ExactMatch_NoMatch_ReturnsFalse()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-iron");
        var requirement = CreateRequirement(RequirementType.Exact, "game:ingot-copper");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_SimpleGlob_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        var requirement = CreateRequirement(RequirementType.Category, "game:ingot-*");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_MultipleIngots_ReturnsTrue()
    {
        // Arrange - Test multiple different ingot types
        var copperOffering = CreateItemStack("game:ingot-copper");
        var bronzeOffering = CreateItemStack("game:ingot-bronze");
        var ironOffering = CreateItemStack("game:ingot-iron");
        var steelOffering = CreateItemStack("game:ingot-steel");
        var requirement = CreateRequirement(RequirementType.Category, "game:ingot-*");

        // Act & Assert
        Assert.True(_matcher.DoesItemMatchRequirement(copperOffering, requirement));
        Assert.True(_matcher.DoesItemMatchRequirement(bronzeOffering, requirement));
        Assert.True(_matcher.DoesItemMatchRequirement(ironOffering, requirement));
        Assert.True(_matcher.DoesItemMatchRequirement(steelOffering, requirement));
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_MultiplePatterns_ReturnsTrue()
    {
        // Arrange
        var pickaxeOffering = CreateItemStack("game:pickaxe-steel");
        var axeOffering = CreateItemStack("game:axe-steel");
        var requirement = CreateRequirement(RequirementType.Category, "game:pickaxe-*", "game:axe-*");

        // Act & Assert
        Assert.True(_matcher.DoesItemMatchRequirement(pickaxeOffering, requirement));
        Assert.True(_matcher.DoesItemMatchRequirement(axeOffering, requirement));
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_NoMatch_ReturnsFalse()
    {
        // Arrange
        var offering = CreateItemStack("game:ore-copper");
        var requirement = CreateRequirement(RequirementType.Category, "game:ingot-*");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_PartialMatch_ReturnsFalse()
    {
        // Arrange - Pattern requires "ingot-" prefix, "copper" alone shouldn't match
        var offering = CreateItemStack("game:copper");
        var requirement = CreateRequirement(RequirementType.Category, "game:ingot-*");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_WildcardInMiddle_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:tool-pickaxe-steel");
        var requirement = CreateRequirement(RequirementType.Category, "game:tool-*-steel");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_MultipleWildcards_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:tool-pickaxe-steel-reinforced");
        var requirement = CreateRequirement(RequirementType.Category, "game:*-pickaxe-*");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesItemMatchRequirement_CategoryMatch_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var offering = CreateItemStack("game:INGOT-COPPER");
        var requirement = CreateRequirement(RequirementType.Category, "game:ingot-*");

        // Act
        var result = _matcher.DoesItemMatchRequirement(offering, requirement);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region FindMatchingRequirement Tests

    [Fact]
    public void FindMatchingRequirement_NoMatch_ReturnsNull()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-iron");
        var requirements = new List<RitualRequirement>
        {
            CreateRequirement(RequirementType.Exact, "game:ingot-copper"),
            CreateRequirement(RequirementType.Exact, "game:ingot-bronze")
        };

        // Act
        var result = _matcher.FindMatchingRequirement(offering, requirements);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingRequirement_FirstMatch_ReturnsFirstRequirement()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        var req1 = CreateRequirement(RequirementType.Exact, "game:ingot-copper");
        var req2 = CreateRequirement(RequirementType.Exact, "game:ingot-bronze");
        var requirements = new List<RitualRequirement> { req1, req2 };

        // Act
        var result = _matcher.FindMatchingRequirement(offering, requirements);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("game:ingot-copper", result.ItemCodes[0]);
    }

    [Fact]
    public void FindMatchingRequirement_MultipleMatches_ReturnsFirstMatch()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        var req1 = CreateRequirement(RequirementType.Category, "game:ingot-*");
        var req2 = CreateRequirement(RequirementType.Exact, "game:ingot-copper");
        var requirements = new List<RitualRequirement> { req1, req2 };

        // Act
        var result = _matcher.FindMatchingRequirement(offering, requirements);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(RequirementType.Category, result.Type); // Should match first requirement
    }

    [Fact]
    public void FindMatchingRequirement_EmptyList_ReturnsNull()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        var requirements = new List<RitualRequirement>();

        // Act
        var result = _matcher.FindMatchingRequirement(offering, requirements);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingRequirement_MatchInMiddle_ReturnsMatchedRequirement()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-bronze");
        var req1 = CreateRequirement(RequirementType.Exact, "game:ingot-copper");
        var req2 = CreateRequirement(RequirementType.Exact, "game:ingot-bronze");
        var req3 = CreateRequirement(RequirementType.Exact, "game:ingot-iron");
        var requirements = new List<RitualRequirement> { req1, req2, req3 };

        // Act
        var result = _matcher.FindMatchingRequirement(offering, requirements);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("game:ingot-bronze", result.ItemCodes[0]);
    }

    #endregion

    #region Helper Methods

    private static ItemStack CreateItemStack(string code)
    {
        var parts = code.Split(':');
        var domain = parts.Length > 1 ? parts[0] : "game";
        var path = parts.Length > 1 ? parts[1] : parts[0];

        var item = new Item
        {
            Code = new AssetLocation(domain, path)
        };
        return new ItemStack(item);
    }

    private static RitualRequirement CreateRequirement(RequirementType type, params string[] itemCodes)
    {
        return new RitualRequirement(
            RequirementId: "test_requirement",
            DisplayName: "Test Requirement",
            Quantity: 10,
            Type: type,
            ItemCodes: itemCodes);
    }

    #endregion
}
