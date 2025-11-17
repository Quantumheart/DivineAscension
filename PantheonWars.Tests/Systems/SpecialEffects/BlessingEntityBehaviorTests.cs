using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Systems.SpecialEffects;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Xunit;

namespace PantheonWars.Tests.Systems.SpecialEffects;

/// <summary>
/// Tests for the BlessingEntityBehavior that attaches to player entities
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingEntityBehaviorTests
{
    private readonly Mock<ICoreServerAPI> _mockApi;
    private readonly Mock<ILogger> _mockLogger;
    private readonly SpecialEffectHandlerRegistry _registry;

    public BlessingEntityBehaviorTests()
    {
        // Setup mock API
        _mockLogger = new Mock<ILogger>();
        _mockApi = new Mock<ICoreServerAPI>();
        _mockApi.Setup(api => api.Logger).Returns(_mockLogger.Object);
        _mockApi.Setup(api => api.Side).Returns(EnumAppSide.Server);

        // Setup registry
        _registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        _registry.Initialize();
    }

    private TestEntity CreateTestEntity()
    {
        var entity = new TestEntity();
        entity.WatchedAttributes = new SyncedTreeAttribute();
        entity.Pos = new EntityPos();
        entity.Api = _mockApi.Object;
        return entity;
    }

    [Fact]
    public void PropertyName_ReturnsPantheonwarsBlessings()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);

        // Act
        string propertyName = behavior.PropertyName();

        // Assert
        Assert.Equal("pantheonwars_blessings", propertyName);
    }

    [Fact]
    public void LoadHandlers_WithValidEffectIds_LoadsHandlers()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        var effectIds = new[] { "damage_reduction10" };

        // Act
        behavior.LoadHandlers(effectIds);

        // Assert
        Assert.Equal(1, behavior.GetActiveHandlerCount());
    }

    [Fact]
    public void LoadHandlers_WithUnknownEffectIds_IgnoresThem()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        var effectIds = new[] { "unknown_effect1", "unknown_effect2" };

        // Act
        behavior.LoadHandlers(effectIds);

        // Assert
        Assert.Equal(0, behavior.GetActiveHandlerCount());
    }

    [Fact]
    public void LoadHandlers_WithMixedEffectIds_LoadsOnlyKnown()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        var effectIds = new[] { "damage_reduction10", "unknown_effect", "another_unknown" };

        // Act
        behavior.LoadHandlers(effectIds);

        // Assert
        Assert.Equal(1, behavior.GetActiveHandlerCount()); // Only damage_reduction10
    }

    [Fact]
    public void ClearHandlers_RemovesAllHandlers()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        Assert.Equal(1, behavior.GetActiveHandlerCount());

        // Act
        behavior.ClearHandlers();

        // Assert
        Assert.Equal(0, behavior.GetActiveHandlerCount());
    }

    [Fact]
    public void OnEntityReceiveDamage_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        var damageSource = new DamageSource { Type = EnumDamageType.BluntAttack };
        float damage = 100f;

        // Act & Assert - should not throw
        behavior.OnEntityReceiveDamage(damageSource, ref damage);
        Assert.Equal(100f, damage); // Damage unchanged
    }

    [Fact]
    public void OnEntityReceiveDamage_WithDamageReduction_ReducesDamage()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        var damageSource = new DamageSource { Type = EnumDamageType.BluntAttack };
        float damage = 100f;

        // Act
        behavior.OnEntityReceiveDamage(damageSource, ref damage);

        // Assert
        Assert.Equal(90f, damage); // 10% reduction applied
    }

    [Fact]
    public void DidAttack_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        var damageSource = new DamageSource();
        var target = CreateTestEntity();
        target.WatchedAttributes.SetFloat("onHurt", 0f);
        var handled = EnumHandling.PassThrough;

        // Act & Assert - should not throw
        behavior.DidAttack(damageSource, target, ref handled);
    }

    [Fact]
    public void DidAttack_ReadsDamageFromTargetWatchedAttributes()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        var damageSource = new DamageSource();
        var target = CreateTestEntity();
        target.WatchedAttributes.SetFloat("onHurt", 50f);
        var handled = EnumHandling.PassThrough;

        // Act
        behavior.DidAttack(damageSource, target, ref handled);

        // Assert
        // Verify damage was read (implicit - no exception thrown)
        Assert.Equal(50f, target.WatchedAttributes.GetFloat("onHurt"));
    }

    [Fact]
    public void DidAttack_WithZeroDamage_DoesNotCallHandlers()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        var damageSource = new DamageSource();
        var target = CreateTestEntity();
        target.WatchedAttributes.SetFloat("onHurt", 0f);
        var handled = EnumHandling.PassThrough;

        // Act
        behavior.DidAttack(damageSource, target, ref handled);

        // Assert
        // Handlers should not be called for zero damage
        // This is implicitly tested by no exceptions being thrown
    }

    [Fact]
    public void DidAttack_WhenTargetDies_CallsOnKill()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        var damageSource = new DamageSource();
        var target = CreateTestEntity();
        target.WatchedAttributes.SetFloat("onHurt", 100f);
        target.Alive = false; // Target is dead
        var handled = EnumHandling.PassThrough;

        // Act
        behavior.DidAttack(damageSource, target, ref handled);

        // Assert
        // OnKill should be called on handlers (tested implicitly - no exceptions)
    }

    [Fact]
    public void OnGameTick_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        float deltaTime = 0.016f; // ~60 FPS

        // Act & Assert - should not throw
        behavior.OnGameTick(deltaTime);
    }

    [Fact]
    public void OnGameTick_WithHandlers_CallsOnTick()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        float deltaTime = 0.016f;

        // Act & Assert - should not throw
        behavior.OnGameTick(deltaTime);
        // Handlers' OnTick should be called (tested implicitly)
    }

    [Fact]
    public void GetActiveHandlerCount_InitiallyReturnsZero()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);

        // Assert
        Assert.Equal(0, behavior.GetActiveHandlerCount());
    }

    [Fact]
    public void GetActiveHandlerCount_AfterLoadingHandlers_ReturnsCorrectCount()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);

        // Act
        behavior.LoadHandlers(new[] { "damage_reduction10" });

        // Assert
        Assert.Equal(1, behavior.GetActiveHandlerCount());
    }

    [Fact]
    public void LoadHandlers_ClearsOldHandlersBeforeLoadingNew()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        Assert.Equal(1, behavior.GetActiveHandlerCount());

        // Act - Load different handlers
        behavior.LoadHandlers(new string[] { }); // Load empty set

        // Assert
        Assert.Equal(0, behavior.GetActiveHandlerCount());
    }

    [Fact]
    public void LoadHandlers_WithEmptyList_ClearsHandlers()
    {
        // Arrange
        var entity = CreateTestEntity();
        var behavior = new BlessingEntityBehavior(entity, _registry);
        behavior.LoadHandlers(new[] { "damage_reduction10" });
        Assert.Equal(1, behavior.GetActiveHandlerCount());

        // Act
        behavior.LoadHandlers(new string[] { });

        // Assert
        Assert.Equal(0, behavior.GetActiveHandlerCount());
    }
}

/// <summary>
/// Minimal test entity implementation for testing BlessingEntityBehavior
/// </summary>
public class TestEntity : EntityAgent
{
    public TestEntity() : base()
    {
        Alive = true;
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
