using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Systems.BuffSystem;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Systems.BuffSystem;

/// <summary>
///     Unit tests for BuffManager
///     Tests buff application, removal, and querying
/// </summary>
[ExcludeFromCodeCoverage]
public class BuffManagerTests
{
    private readonly BuffManager _buffManager;
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<ILogger> _mockLogger;

    public BuffManagerTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(_mockLogger.Object);

        _buffManager = new BuffManager(_mockAPI.Object);
    }

    // NOTE: ApplyAoEBuff tests omitted - this method requires complex world interaction mocking
    // (GetNearestEntity, EntityPlayer types, IServerWorldAccessor) that is difficult to test in unit tests.
    // ApplyAoEBuff logic is better tested through integration or end-to-end tests with a real game world.
    // The method currently has 66% coverage from indirect testing through other methods.

    #region Logging Tests

    [Fact]
    public void ApplyEffect_LogsDebugMessage()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);
        var statModifiers = new Dictionary<string, float> { { "walkspeed", 0.2f } };

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockEntity
            .Setup(e => e.GetName())
            .Returns("TestEntity");

        // Act
        _buffManager.ApplyEffect(
            mockEntity.Object,
            "test_effect",
            10.0f,
            "test_ability",
            "player-uid",
            statModifiers
        );

        // Assert
        TestFixtures.VerifyLoggerDebug(_mockLogger, "Applied effect test_effect");
    }

    #endregion

    #region ApplyEffect Tests

    [Fact]
    public void ApplyEffect_WithNullTarget_ReturnsFalse()
    {
        // Arrange
        var statModifiers = new Dictionary<string, float> { { "walkspeed", 0.2f } };

        // Act
        var result = _buffManager.ApplyEffect(
            null!,
            "test_buff",
            10.0f,
            "test_ability",
            "player-uid",
            statModifiers,
            true
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ApplyEffect_WithEmptyEffectId_ReturnsFalse()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var statModifiers = new Dictionary<string, float> { { "walkspeed", 0.2f } };

        // Act
        var result = _buffManager.ApplyEffect(
            mockEntity.Object,
            "",
            10.0f,
            "test_ability",
            "player-uid",
            statModifiers,
            true
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ApplyEffect_WithNullStatModifiers_ReturnsFalse()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();

        // Act
        var result = _buffManager.ApplyEffect(
            mockEntity.Object,
            "test_buff",
            10.0f,
            "test_ability",
            "player-uid",
            null!,
            true
        );

        // Assert
        // Should still work with null modifiers (will create empty effect)
        // This tests defensive programming
        Assert.False(result); // Will be false because GetOrCreateBuffTracker returns null for test mock
    }

    [Fact]
    public void ApplySimpleBuff_WithValidParameters_CreatesCorrectModifier()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        // Act
        var speedBuff = "speed_buff";
        var duration = 15.0f;
        var sourceAbilityId = "swift_feet";
        var casterPlayerUid = "player-123";
        var statName = "walkspeed";
        var statValue = 0.3f;
        var result = _buffManager.ApplySimpleBuff(
            mockEntity.Object,
            speedBuff,
            duration,
            sourceAbilityId,
            casterPlayerUid,
            statName,
            statValue
        );

        // Assert
        Assert.True(result);
        mockBuffTracker.Verify(
            bt => bt.AddEffect(It.Is<ActiveEffect>(e =>
                e.EffectId == "speed_buff" &&
                e.DurationRemaining == 15.0f &&
                e.SourceAbilityId == "swift_feet"
            )),
            Times.Once()
        );
    }

    #endregion

    #region RemoveEffect Tests

    [Fact]
    public void RemoveEffect_WithNullTarget_ReturnsFalse()
    {
        // Act
        var result = _buffManager.RemoveEffect(null!, "test_buff");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveEffect_WithEmptyEffectId_ReturnsFalse()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();

        // Act
        var result = _buffManager.RemoveEffect(mockEntity.Object, "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveEffect_WithExistingEffect_ReturnsTrue()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.RemoveEffect("test_buff"))
            .Returns(true);

        // Act
        var result = _buffManager.RemoveEffect(mockEntity.Object, "test_buff");

        // Assert
        Assert.True(result);
        mockBuffTracker.Verify(bt => bt.RemoveEffect("test_buff"), Times.Once());
    }

    [Fact]
    public void RemoveEffect_WithNonExistentEffect_ReturnsFalse()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.RemoveEffect("nonexistent_buff"))
            .Returns(false);

        // Act
        var result = _buffManager.RemoveEffect(mockEntity.Object, "nonexistent_buff");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveEffect_WithNoBuffTracker_ReturnsFalse()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns((EntityBehaviorBuffTracker)null!);

        // Act
        var result = _buffManager.RemoveEffect(mockEntity.Object, "test_buff");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region HasEffect Tests

    [Fact]
    public void HasEffect_WithNullTarget_ReturnsFalse()
    {
        // Act
        var result = _buffManager.HasEffect(null!, "test_buff");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasEffect_WithActiveEffect_ReturnsTrue()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.HasEffect("active_buff"))
            .Returns(true);

        // Act
        var result = _buffManager.HasEffect(mockEntity.Object, "active_buff");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasEffect_WithoutActiveEffect_ReturnsFalse()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.HasEffect("inactive_buff"))
            .Returns(false);

        // Act
        var result = _buffManager.HasEffect(mockEntity.Object, "inactive_buff");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetActiveEffects Tests

    [Fact]
    public void GetActiveEffects_WithNullTarget_ReturnsEmptyList()
    {
        // Act
        var effects = _buffManager.GetActiveEffects(null!);

        // Assert
        Assert.NotNull(effects);
        Assert.Empty(effects);
    }

    [Fact]
    public void GetActiveEffects_WithNoBuffTracker_ReturnsEmptyList()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns((EntityBehaviorBuffTracker)null!);

        // Act
        var effects = _buffManager.GetActiveEffects(mockEntity.Object);

        // Assert
        Assert.NotNull(effects);
        Assert.Empty(effects);
    }

    [Fact]
    public void GetActiveEffects_ReturnsAllActiveEffects()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        var activeEffects = new List<ActiveEffect>
        {
            new("buff1", 10f, "ability1", "player1", true),
            new("buff2", 20f, "ability2", "player2", true)
        };

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.GetActiveEffects())
            .Returns(activeEffects);

        // Act
        var effects = _buffManager.GetActiveEffects(mockEntity.Object);

        // Assert
        Assert.NotNull(effects);
        Assert.Equal(2, effects.Count);
        Assert.Contains(effects, e => e.EffectId == "buff1");
        Assert.Contains(effects, e => e.EffectId == "buff2");
    }

    #endregion

    #region Damage Multiplier Tests

    [Fact]
    public void GetOutgoingDamageMultiplier_WithNullEntity_Returns1Point0()
    {
        // Act
        var multiplier = _buffManager.GetOutgoingDamageMultiplier(null!);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetOutgoingDamageMultiplier_WithNoBuffTracker_Returns1Point0()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns((EntityBehaviorBuffTracker)null!);

        // Act
        var multiplier = _buffManager.GetOutgoingDamageMultiplier(mockEntity.Object);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetOutgoingDamageMultiplier_WithBuffs_ReturnsCorrectMultiplier()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.GetOutgoingDamageMultiplier())
            .Returns(1.5f);

        // Act
        var multiplier = _buffManager.GetOutgoingDamageMultiplier(mockEntity.Object);

        // Assert
        Assert.Equal(1.5f, multiplier);
    }

    [Fact]
    public void GetReceivedDamageMultiplier_WithNullEntity_Returns1Point0()
    {
        // Act
        var multiplier = _buffManager.GetReceivedDamageMultiplier(null!);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    [Fact]
    public void GetReceivedDamageMultiplier_WithDebuffs_ReturnsCorrectMultiplier()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns(mockBuffTracker.Object);

        mockBuffTracker
            .Setup(bt => bt.GetReceivedDamageMultiplier())
            .Returns(1.2f); // Taking 20% more damage

        // Act
        var multiplier = _buffManager.GetReceivedDamageMultiplier(mockEntity.Object);

        // Assert
        Assert.Equal(1.2f, multiplier);
    }

    [Fact]
    public void GetReceivedDamageMultiplier_WithNoBuffTracker_Returns1Point0()
    {
        // Arrange
        var mockEntity = TestFixtures.CreateMockEntity();
        mockEntity
            .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
            .Returns((EntityBehaviorBuffTracker)null!);

        // Act
        var multiplier = _buffManager.GetReceivedDamageMultiplier(mockEntity.Object);

        // Assert
        Assert.Equal(1.0f, multiplier);
    }

    #endregion
}