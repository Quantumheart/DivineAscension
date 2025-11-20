using System.Diagnostics.CodeAnalysis;
using PantheonWars.Systems.SpecialEffects.Handlers;
using Vintagestory.API.Common;
using Xunit;

namespace PantheonWars.Tests.Systems.SpecialEffects;

/// <summary>
/// Tests for the DamageReductionHandler special effect
/// </summary>
[ExcludeFromCodeCoverage]
public class DamageReductionHandlerTests
{
    [Fact]
    public void Constructor_SetsEffectIdAndReductionPercent()
    {
        // Arrange & Act
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);

        // Assert
        Assert.Equal("damage_reduction_10", handler.EffectId);
    }

    [Fact]
    public void OnDamageReceived_ReducesDamageByCorrectPercentage()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f); // 10% reduction
        var damageSource = new DamageSource
        {
            Type = EnumDamageType.BluntAttack,
            Source = EnumDamageSource.Entity
        };
        float damage = 100f;

        // Act
        handler.OnDamageReceived(null!, null, damageSource, ref damage);

        // Assert
        Assert.Equal(90f, damage); // 10% reduction: 100 * 0.9 = 90
    }

    [Fact]
    public void OnDamageReceived_WithHigherReduction_CalculatesCorrectly()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction25", 0.25f); // 25% reduction
        var damageSource = new DamageSource { Type = EnumDamageType.SlashingAttack };
        float damage = 80f;

        // Act
        handler.OnDamageReceived(null!, null, damageSource, ref damage);

        // Assert
        Assert.Equal(60f, damage); // 25% reduction: 80 * 0.75 = 60
    }

    [Fact]
    public void OnDamageReceived_DoesNotReduceHealingDamage()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var healSource = new DamageSource
        {
            Type = EnumDamageType.Heal,
            Source = EnumDamageSource.Revive
        };
        float healing = 50f;

        // Act
        handler.OnDamageReceived(null!, null, healSource, ref healing);

        // Assert
        Assert.Equal(50f, healing); // Healing should NOT be reduced
    }

    [Fact]
    public void OnDamageReceived_DoesNotReduceReviveDamage()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var reviveSource = new DamageSource
        {
            Type = EnumDamageType.Heal,
            Source = EnumDamageSource.Revive
        };
        float reviveDamage = 9999f;

        // Act
        handler.OnDamageReceived(null!, null, reviveSource, ref reviveDamage);

        // Assert
        Assert.Equal(9999f, reviveDamage); // Revive "damage" (healing) should NOT be reduced
    }

    [Theory]
    [InlineData(EnumDamageType.BluntAttack)]
    [InlineData(EnumDamageType.SlashingAttack)]
    [InlineData(EnumDamageType.PiercingAttack)]
    [InlineData(EnumDamageType.Fire)]
    [InlineData(EnumDamageType.Poison)]
    [InlineData(EnumDamageType.Frost)]
    [InlineData(EnumDamageType.Crushing)]
    public void OnDamageReceived_ReducesAllDamageTypes(EnumDamageType damageType)
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var damageSource = new DamageSource { Type = damageType };
        float damage = 100f;

        // Act
        handler.OnDamageReceived(null!, null, damageSource, ref damage);

        // Assert
        Assert.Equal(90f, damage); // All damage types should be reduced
    }

    [Fact]
    public void OnDamageReceived_WithZeroDamage_DoesNothing()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var damageSource = new DamageSource { Type = EnumDamageType.BluntAttack };
        float damage = 0f;

        // Act
        handler.OnDamageReceived(null!, null, damageSource, ref damage);

        // Assert
        Assert.Equal(0f, damage);
    }

    [Fact]
    public void OnDamageReceived_WithSmallDamage_StillReduces()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var damageSource = new DamageSource { Type = EnumDamageType.BluntAttack };
        float damage = 1f;

        // Act
        handler.OnDamageReceived(null!, null, damageSource, ref damage);

        // Assert
        Assert.Equal(0.9f, damage); // Even small damage is reduced
    }

    [Fact]
    public void OnDamageReceived_With50PercentReduction_HalvesDamage()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction50", 0.50f); // 50% reduction
        var damageSource = new DamageSource { Type = EnumDamageType.BluntAttack };
        float damage = 200f;

        // Act
        handler.OnDamageReceived(null!, null, damageSource, ref damage);

        // Assert
        Assert.Equal(100f, damage); // 50% reduction: 200 * 0.5 = 100
    }

    [Fact]
    public void OnDamageDealt_DoesNothing()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var damageSource = new DamageSource();
        float damage = 100f;

        // Act
        handler.OnDamageDealt(null!, null!, damageSource, ref damage);

        // Assert
        Assert.Equal(100f, damage); // OnDamageDealt should not modify damage
    }

    [Fact]
    public void OnTick_DoesNothing()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);

        // Act & Assert - should not throw
        handler.OnTick(null!, 0.016f);
    }

    [Fact]
    public void OnKill_DoesNothing()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);
        var damageSource = new DamageSource();

        // Act & Assert - should not throw
        handler.OnKill(null!, null!, damageSource);
    }

    [Fact]
    public void OnActivate_DoesNothing()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);

        // Act & Assert - should not throw
        handler.OnActivate(null!);
    }

    [Fact]
    public void OnDeactivate_DoesNothing()
    {
        // Arrange
        var handler = new DamageReductionHandler("damage_reduction_10", 0.10f);

        // Act & Assert - should not throw
        handler.OnDeactivate(null!);
    }
}
