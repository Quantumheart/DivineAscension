using System.Diagnostics.CodeAnalysis;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Systems.Patches;

/// <summary>
///     Regression coverage for the firepit duplication exploit: the cookingYield
///     bonus must only ever scale genuine food outputs, and must never mint extra
///     items when there is no bonus.
/// </summary>
[ExcludeFromCodeCoverage]
public class CookingPatchesTests
{
    private static ItemStack CreateStack(string code, int stackSize, bool isFood)
    {
        var item = new Item
        {
            Code = new AssetLocation("game", code)
        };
        if (isFood) item.NutritionProps = new FoodNutritionProperties();

        return new ItemStack(item) { StackSize = stackSize };
    }

    [Fact]
    public void IsScalableFoodOutput_WithCookwareContainer_ReturnsFalse()
    {
        // A single cooking pot / cauldron / crucible carries no NutritionProps and
        // must never be eligible for scaling -- this is the dupe at its root.
        var pot = CreateStack("claypot-cooked", 1, isFood: false);

        Assert.False(CookingPatches.IsScalableFoodOutput(pot));
    }

    [Fact]
    public void IsScalableFoodOutput_WithFoodStack_ReturnsTrue()
    {
        var cookedMeat = CreateStack("bushmeat-cooked", 1, isFood: true);

        Assert.True(CookingPatches.IsScalableFoodOutput(cookedMeat));
    }

    [Fact]
    public void IsScalableFoodOutput_WithNullOrEmpty_ReturnsFalse()
    {
        Assert.False(CookingPatches.IsScalableFoodOutput(null));
        Assert.False(CookingPatches.IsScalableFoodOutput(CreateStack("bushmeat-cooked", 0, isFood: true)));
    }

    [Theory]
    [InlineData(1, 1.0f, 1)] // no bonus, single item stays single (never minted)
    [InlineData(1, 0.5f, 1)] // sub-1.0 multiplier never shrinks the stack
    [InlineData(4, 1.0f, 4)] // no bonus, stack unchanged
    public void ComputeScaledStackSize_WithoutBonus_DoesNotIncrease(int stackSize, float yield, int expected)
    {
        Assert.Equal(expected, CookingPatches.ComputeScaledStackSize(stackSize, yield));
    }

    [Theory]
    [InlineData(1, 1.6f, 2)] // maxed Harvest: a single cooked food becomes two -- intended reward
    [InlineData(2, 1.6f, 3)]
    [InlineData(10, 1.25f, 13)] // Baker's Touch only
    public void ComputeScaledStackSize_WithBonus_ScalesFood(int stackSize, float yield, int expected)
    {
        Assert.Equal(expected, CookingPatches.ComputeScaledStackSize(stackSize, yield));
    }
}
