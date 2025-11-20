using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moq;
using PantheonWars.Systems.SpecialEffects;
using PantheonWars.Systems.SpecialEffects.Handlers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Xunit;

namespace PantheonWars.Tests.Systems.SpecialEffects;

/// <summary>
/// Tests for the SpecialEffectHandlerRegistry system
/// </summary>
[ExcludeFromCodeCoverage]
public class SpecialEffectHandlerRegistryTests
{
    private readonly Mock<ICoreServerAPI> _mockApi;
    private readonly Mock<ILogger> _mockLogger;

    public SpecialEffectHandlerRegistryTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockApi = new Mock<ICoreServerAPI>();
        _mockApi.Setup(api => api.Logger).Returns(_mockLogger.Object);
    }

    [Fact]
    public void Initialize_RegistersHandlers()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);

        // Act
        registry.Initialize();

        // Assert
        var allHandlers = registry.GetAllHandlers().ToList();
        Assert.NotEmpty(allHandlers); // Should have at least damage_reduction10
    }

    [Fact]
    public void Initialize_RegistersDamageReduction10Handler()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);

        // Act
        registry.Initialize();

        // Assert
        var handler = registry.GetHandler("damage_reduction_10");
        Assert.NotNull(handler);
        Assert.IsType<DamageReductionHandler>(handler);
        Assert.Equal("damage_reduction_10", handler.EffectId);
    }

    [Fact]
    public void GetHandler_WithUnknownEffectId_ReturnsNull()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        var handler = registry.GetHandler("unknown_effect");

        // Assert
        Assert.Null(handler);
    }

    [Fact]
    public void GetHandler_WithValidEffectId_ReturnsCorrectHandler()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        var handler = registry.GetHandler("damage_reduction_10");

        // Assert
        Assert.NotNull(handler);
        Assert.Equal("damage_reduction_10", handler.EffectId);
    }

    [Fact]
    public void HasHandler_WithRegisteredEffect_ReturnsTrue()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        bool hasHandler = registry.HasHandler("damage_reduction_10");

        // Assert
        Assert.True(hasHandler);
    }

    [Fact]
    public void HasHandler_WithUnregisteredEffect_ReturnsFalse()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        bool hasHandler = registry.HasHandler("nonexistent_effect");

        // Assert
        Assert.False(hasHandler);
    }

    [Fact]
    public void GetHandlers_WithMultipleEffectIds_ReturnsAllRegistered()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();
        var effectIds = new[] { "damage_reduction_10", "unknown_effect", "another_unknown" };

        // Act
        var handlers = registry.GetHandlers(effectIds);

        // Assert
        Assert.Single(handlers); // Only damage_reduction_10 is registered
        Assert.Equal("damage_reduction_10", handlers[0].EffectId);
    }

    [Fact]
    public void GetHandlers_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        var handlers = registry.GetHandlers(new string[] { });

        // Assert
        Assert.Empty(handlers);
    }

    [Fact]
    public void GetHandlers_WithAllUnknownEffects_ReturnsEmptyList()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();
        var effectIds = new[] { "unknown1", "unknown2", "unknown3" };

        // Act
        var handlers = registry.GetHandlers(effectIds);

        // Assert
        Assert.Empty(handlers);
    }

    [Fact]
    public void GetAllHandlers_ReturnsAllRegisteredHandlers()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        var allHandlers = registry.GetAllHandlers().ToList();

        // Assert
        Assert.NotEmpty(allHandlers);
        Assert.Contains(allHandlers, h => h.EffectId == "damage_reduction_10");
    }

    [Fact]
    public void Initialize_LogsSuccessMessage()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);

        // Act
        registry.Initialize();

        // Assert
        _mockLogger.Verify(
            logger => logger.Notification(It.Is<string>(msg => msg.Contains("Registered") && msg.Contains("special effect handlers"))),
            Times.Once);
    }

    [Fact]
    public void GetHandlers_WithUnknownEffect_LogsDebugMessage()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();
        var effectIds = new[] { "unknown_effect" };

        // Act
        registry.GetHandlers(effectIds);

        // Assert
        _mockLogger.Verify(
            logger => logger.Debug(It.Is<string>(msg => msg.Contains("No handler found") && msg.Contains("unknown_effect"))),
            Times.Once);
    }

    [Fact]
    public void GetHandler_ReturnsSameInstanceOnMultipleCalls()
    {
        // Arrange
        var registry = new SpecialEffectHandlerRegistry(_mockApi.Object);
        registry.Initialize();

        // Act
        var handler1 = registry.GetHandler("damage_reduction10");
        var handler2 = registry.GetHandler("damage_reduction10");

        // Assert
        Assert.Same(handler1, handler2); // Should be the same instance (singleton pattern)
    }
}
