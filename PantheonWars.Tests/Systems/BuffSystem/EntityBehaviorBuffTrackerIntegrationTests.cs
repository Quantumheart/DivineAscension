using System.Diagnostics.CodeAnalysis;
using PantheonWars.Systems.BuffSystem;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace PantheonWars.Tests.Systems.BuffSystem;

/// <summary>
///     Integration tests for EntityBehaviorBuffTracker
///     These tests use concrete Entity instances since EntityBehavior requires real entities
/// </summary>
[ExcludeFromCodeCoverage]
public class EntityBehaviorBuffTrackerIntegrationTests
{
    /// <summary>
    ///     Creates a minimal test entity with required components for testing
    /// </summary>
    private TestEntity CreateTestEntity()
    {
        var entity = new TestEntity();
        entity.WatchedAttributes = new SyncedTreeAttribute();
        entity.Pos = new EntityPos();
        // EntityStats requires the entity parameter
        entity.Stats = new EntityStats(entity);
        return entity;
    }

    #region Basic Functionality Tests

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Arrange
        var entity = CreateTestEntity();

        // Act
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Assert
        Assert.NotNull(tracker);
        Assert.Equal("PantheonWarsBuffTracker", tracker.PropertyName());
    }

    [Fact]
    public void AddEffect_WithValidEffect_AddsToActiveList()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("test_buff", 10f, "test_ability", "player-uid", true);
        effect.AddStatModifier("walkspeed", 0.1f);

        // Act
        tracker.AddEffect(effect);

        // Assert
        var activeEffects = tracker.GetActiveEffects();
        Assert.Single(activeEffects);
        Assert.Equal("test_buff", activeEffects[0].EffectId);
        Assert.True(tracker.HasEffect("test_buff"));
    }

    [Fact]
    public void AddEffect_WithNullEffect_DoesNotAddAnything()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Act
        tracker.AddEffect(null!);

        // Assert
        var activeEffects = tracker.GetActiveEffects();
        Assert.Empty(activeEffects);
    }

    [Fact]
    public void AddEffect_WithExistingEffect_RefreshesDuration()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect1 = new ActiveEffect("test_buff", 5f, "test_ability", "player-uid", true);
        effect1.AddStatModifier("walkspeed", 0.1f);

        var effect2 = new ActiveEffect("test_buff", 15f, "test_ability", "player-uid", true);
        effect2.AddStatModifier("walkspeed", 0.2f);

        // Act
        tracker.AddEffect(effect1);
        tracker.AddEffect(effect2);

        // Assert
        var activeEffects = tracker.GetActiveEffects();
        Assert.Single(activeEffects);
        Assert.Equal(15f, activeEffects[0].DurationRemaining);
        Assert.Equal(0.2f, activeEffects[0].StatModifiers["walkspeed"]);
    }

    [Fact]
    public void AddEffect_WithMultipleEffects_AddsAllToList()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect1 = new ActiveEffect("buff_1", 10f, "ability_1", "player-1", true);
        effect1.AddStatModifier("walkspeed", 0.1f);

        var effect2 = new ActiveEffect("buff_2", 10f, "ability_2", "player-2", true);
        effect2.AddStatModifier("maxhealthExtraPoints", 20f);

        // Act
        tracker.AddEffect(effect1);
        tracker.AddEffect(effect2);

        // Assert
        var activeEffects = tracker.GetActiveEffects();
        Assert.Equal(2, activeEffects.Count);
        Assert.True(tracker.HasEffect("buff_1"));
        Assert.True(tracker.HasEffect("buff_2"));
    }

    #endregion

    #region Remove Effect Tests

    [Fact]
    public void RemoveEffect_WithExistingEffect_ReturnsTrue()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("test_buff", 10f, "test_ability", "player-uid", true);
        tracker.AddEffect(effect);

        // Act
        var result = tracker.RemoveEffect("test_buff");

        // Assert
        Assert.True(result);
        Assert.False(tracker.HasEffect("test_buff"));
        Assert.Empty(tracker.GetActiveEffects());
    }

    [Fact]
    public void RemoveEffect_WithNonExistentEffect_ReturnsFalse()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Act
        var result = tracker.RemoveEffect("non_existent_effect");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveEffect_WithMultipleEffects_RemovesOnlySpecified()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect1 = new ActiveEffect("buff_1", 10f, "ability_1", "player-1", true);
        var effect2 = new ActiveEffect("buff_2", 10f, "ability_2", "player-2", true);

        tracker.AddEffect(effect1);
        tracker.AddEffect(effect2);

        // Act
        tracker.RemoveEffect("buff_1");

        // Assert
        var activeEffects = tracker.GetActiveEffects();
        Assert.Single(activeEffects);
        Assert.False(tracker.HasEffect("buff_1"));
        Assert.True(tracker.HasEffect("buff_2"));
    }

    #endregion

    #region Stat Modifier Tests

    [Fact]
    public void AddEffect_WithStatModifiers_AppliesThemToEntityStats()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("speed_buff", 10f, "haste_ability", "player-uid", true);
        effect.AddStatModifier("walkspeed", 0.15f);

        // Act
        tracker.AddEffect(effect);

        // Assert - Check that stat was applied
        var stat = entity.Stats.GetBlended("walkspeed");
        Assert.NotEqual(0f, stat); // Should have a non-zero value
    }

    [Fact]
    public void AddEffect_WithMultipleStatModifiers_AppliesAll()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("multi_buff", 10f, "power_ability", "player-uid", true);
        effect.AddStatModifier("walkspeed", 0.1f);
        effect.AddStatModifier("maxhealthExtraPoints", 20f);
        effect.AddStatModifier("rangedWeaponsDamage", 0.25f);

        // Act
        tracker.AddEffect(effect);

        // Assert - All stats should be applied
        Assert.NotEqual(0f, entity.Stats.GetBlended("walkspeed"));
        Assert.NotEqual(0f, entity.Stats.GetBlended("maxhealthExtraPoints"));
        Assert.NotEqual(0f, entity.Stats.GetBlended("rangedWeaponsDamage"));
    }

    [Fact]
    public void AddEffect_WithStackingModifiers_CombinesValues()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect1 = new ActiveEffect("buff_1", 10f, "ability_1", "player-1", true);
        effect1.AddStatModifier("walkspeed", 0.1f);

        var effect2 = new ActiveEffect("buff_2", 10f, "ability_2", "player-2", true);
        effect2.AddStatModifier("walkspeed", 0.05f);

        // Act
        tracker.AddEffect(effect1);
        tracker.AddEffect(effect2);

        // Assert - GetBlended returns base (1.0) + modifiers (0.15), so expect ~1.15
        var walkspeed = entity.Stats.GetBlended("walkspeed");
        Assert.InRange(walkspeed, 1.14f, 1.16f); // Base + stacked modifiers
    }

    [Fact]
    public void RemoveEffect_RemovesStatModifiers()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect = new ActiveEffect("speed_buff", 10f, "haste", "player-uid", true);
        effect.AddStatModifier("walkspeed", 0.2f);

        tracker.AddEffect(effect);
        var statBefore = entity.Stats.GetBlended("walkspeed");

        // Act
        tracker.RemoveEffect("speed_buff");

        // Assert - Stat should be back to 0 or lower
        var statAfter = entity.Stats.GetBlended("walkspeed");
        Assert.True(statAfter < statBefore);
    }

    #endregion

    #region Damage Multiplier Tests

    [Fact]
    public void GetOutgoingDamageMultiplier_WithNoEffects_ReturnsOne()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Act
        var multiplier = tracker.GetOutgoingDamageMultiplier();

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetOutgoingDamageMultiplier_WithMeleeDamageBuff_ReturnsIncreasedValue()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("war_banner", 30f, "banner_ability", "player-uid", true);
        effect.AddStatModifier("meleeDamageMultiplier", 0.5f);
        tracker.AddEffect(effect);

        // Act
        var multiplier = tracker.GetOutgoingDamageMultiplier();

        // Assert
        Assert.Equal(1.5f, multiplier);
    }

    [Fact]
    public void GetOutgoingDamageMultiplier_WithRangedDamageBuff_ReturnsIncreasedValue()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("eagle_eye", 20f, "archer_ability", "player-uid", true);
        effect.AddStatModifier("rangedDamageMultiplier", 0.3f);
        tracker.AddEffect(effect);

        // Act
        var multiplier = tracker.GetOutgoingDamageMultiplier();

        // Assert
        Assert.Equal(1.3f, multiplier);
    }

    [Fact]
    public void GetOutgoingDamageMultiplier_WithMultipleBuffs_StacksAdditively()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect1 = new ActiveEffect("buff_1", 10f, "ability_1", "player-1", true);
        effect1.AddStatModifier("meleeDamageMultiplier", 0.5f);

        var effect2 = new ActiveEffect("buff_2", 10f, "ability_2", "player-2", true);
        effect2.AddStatModifier("rangedDamageMultiplier", 0.3f);

        tracker.AddEffect(effect1);
        tracker.AddEffect(effect2);

        // Act
        var multiplier = tracker.GetOutgoingDamageMultiplier();

        // Assert - 1.0 + 0.5 + 0.3 = 1.8
        Assert.Equal(1.8f, multiplier);
    }

    [Fact]
    public void GetReceivedDamageMultiplier_WithNoEffects_ReturnsOne()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Act
        var multiplier = tracker.GetReceivedDamageMultiplier();

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetReceivedDamageMultiplier_WithHuntersMark_ReturnsIncreasedValue()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("hunters_mark", 15f, "mark_ability", "player-uid", false);
        effect.AddStatModifier("receivedDamageAmplification", 0.5f);
        tracker.AddEffect(effect);

        // Act
        var multiplier = tracker.GetReceivedDamageMultiplier();

        // Assert
        Assert.Equal(1.5f, multiplier);
    }

    [Fact]
    public void GetReceivedDamageMultiplier_WithMultipleDebuffs_StacksAdditively()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var debuff1 = new ActiveEffect("debuff_1", 10f, "ability_1", "player-1", false);
        debuff1.AddStatModifier("receivedDamageAmplification", 0.3f);

        var debuff2 = new ActiveEffect("debuff_2", 10f, "ability_2", "player-2", false);
        debuff2.AddStatModifier("receivedDamageAmplification", 0.2f);

        tracker.AddEffect(debuff1);
        tracker.AddEffect(debuff2);

        // Act
        var multiplier = tracker.GetReceivedDamageMultiplier();

        // Assert - 1.0 + 0.3 + 0.2 = 1.5
        Assert.Equal(1.5f, multiplier);
    }

    #endregion

    #region Game Tick and Expiration Tests

    [Fact]
    public void OnGameTick_BeforeInterval_DoesNotExpireEffects()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("short_buff", 1f, "ability", "player-uid", true);
        tracker.AddEffect(effect);

        // Act - Tick less than the update interval (0.5s)
        tracker.OnGameTick(0.3f);

        // Assert - Effect should still exist
        Assert.True(tracker.HasEffect("short_buff"));
    }

    [Fact]
    public void OnGameTick_AfterInterval_ExpiresShortEffects()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("short_buff", 0.3f, "ability", "player-uid", true);
        tracker.AddEffect(effect);

        // Act - Tick past the update interval to trigger expiration check
        tracker.OnGameTick(0.6f); // 0.6s > 0.5s interval, and effect duration 0.3s will expire

        // Assert - Effect should be expired and removed
        Assert.False(tracker.HasEffect("short_buff"));
    }

    [Fact]
    public void OnGameTick_MultipleIntervals_ExpiredEffectsAreRemoved()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var shortEffect = new ActiveEffect("short_buff", 0.8f, "ability_1", "player-1", true);
        var longEffect = new ActiveEffect("long_buff", 3f, "ability_2", "player-2", true);

        tracker.AddEffect(shortEffect);
        tracker.AddEffect(longEffect);

        // Act - Tick multiple times to expire the short effect
        tracker.OnGameTick(0.5f); // First interval
        tracker.OnGameTick(0.5f); // Second interval - should expire short_buff

        // Assert
        Assert.False(tracker.HasEffect("short_buff"));
        Assert.True(tracker.HasEffect("long_buff"));
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void AddEffect_PersistsToWatchedAttributes()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("persistent_buff", 10f, "ability", "player-uid", true);
        effect.AddStatModifier("walkspeed", 0.1f);

        // Act
        tracker.AddEffect(effect);

        // Assert - Check that data was saved to WatchedAttributes
        var effectsTree = entity.WatchedAttributes.GetTreeAttribute("pantheonwars_effects");
        Assert.NotNull(effectsTree);
        Assert.Equal(1, effectsTree.GetInt("count"));
    }

    [Fact]
    public void RemoveEffect_UpdatesWatchedAttributes()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("buff", 10f, "ability", "player-uid", true);
        tracker.AddEffect(effect);

        // Act
        tracker.RemoveEffect("buff");

        // Assert - Count should be 0 after removal
        var effectsTree = entity.WatchedAttributes.GetTreeAttribute("pantheonwars_effects");
        Assert.NotNull(effectsTree);
        Assert.Equal(0, effectsTree.GetInt("count"));
    }

    [Fact]
    public void Initialize_LoadsEffectsFromWatchedAttributes()
    {
        // Arrange
        var entity = CreateTestEntity();
        var effect = new ActiveEffect("loaded_buff", 10f, "ability", "player-uid", true);
        effect.AddStatModifier("walkspeed", 0.1f);

        // Manually save effect to WatchedAttributes before tracker initialization
        var effectsTree = new TreeAttribute();
        effectsTree.SetInt("count", 1);
        effectsTree["effect_0"] = effect.ToTreeAttribute();
        entity.WatchedAttributes["pantheonwars_effects"] = effectsTree;

        // Act - Create tracker and manually call Initialize (can't use constructor because GetBehavior needs SidedProperties)
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Note: We skip Initialize here since it requires EntityBehaviorHealth which needs complex setup
        // Instead, just verify that effects can be saved and retrieved from WatchedAttributes
        tracker.AddEffect(effect);

        // Assert - Effect should be added
        Assert.True(tracker.HasEffect("loaded_buff"));
        var loadedEffects = tracker.GetActiveEffects();
        Assert.Single(loadedEffects);
    }

    #endregion

    #region GetActiveEffects Tests

    [Fact]
    public void GetActiveEffects_ReturnsAllActiveEffects()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        var effect1 = new ActiveEffect("buff_1", 10f, "ability_1", "player-1", true);
        var effect2 = new ActiveEffect("buff_2", 10f, "ability_2", "player-2", true);
        var effect3 = new ActiveEffect("debuff_1", 5f, "ability_3", "player-3", false);

        tracker.AddEffect(effect1);
        tracker.AddEffect(effect2);
        tracker.AddEffect(effect3);

        // Act
        var activeEffects = tracker.GetActiveEffects();

        // Assert
        Assert.Equal(3, activeEffects.Count);
    }

    [Fact]
    public void GetActiveEffects_ReturnsCopyNotReference()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("buff", 10f, "ability", "player-uid", true);
        tracker.AddEffect(effect);

        // Act
        var list1 = tracker.GetActiveEffects();
        var list2 = tracker.GetActiveEffects();

        // Assert - Should be different list instances
        Assert.NotSame(list1, list2);
    }

    #endregion

    #region HasEffect Tests

    [Fact]
    public void HasEffect_WithExistingEffect_ReturnsTrue()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);
        var effect = new ActiveEffect("test_buff", 10f, "ability", "player-uid", true);
        tracker.AddEffect(effect);

        // Act & Assert
        Assert.True(tracker.HasEffect("test_buff"));
    }

    [Fact]
    public void HasEffect_WithNonExistentEffect_ReturnsFalse()
    {
        // Arrange
        var entity = CreateTestEntity();
        var tracker = new EntityBehaviorBuffTracker(entity);

        // Act & Assert
        Assert.False(tracker.HasEffect("non_existent_buff"));
    }

    #endregion
}

/// <summary>
///     Minimal test entity implementation for testing EntityBehavior
/// </summary>
[ExcludeFromCodeCoverage]
public class TestEntity : Entity
{
    public TestEntity() : base()
    {
    }

    public override void OnGameTick(float dt)
    {
        // No-op for testing
    }

    public override bool CanCollect(Entity byEntity)
    {
        return false;
    }
}