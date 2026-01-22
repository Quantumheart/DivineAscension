using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.BlessingEffects;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems.BlessingEffects;

/// <summary>
///     Unit tests for SpecialEffectRegistry
///     Tests handler registration, effect activation/deactivation, and game tick integration
/// </summary>
[ExcludeFromCodeCoverage]
public class SpecialEffectRegistryTests
{
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly Mock<IWorldService> _mockWorldService;

    public SpecialEffectRegistryTests()
    {
        _mockLogger = new Mock<ILoggerWrapper>();
        _mockEventService = new Mock<IEventService>();
        _mockWorldService = new Mock<IWorldService>();
    }

    private SpecialEffectRegistry CreateRegistry()
    {
        return new SpecialEffectRegistry(
            _mockLogger.Object,
            _mockEventService.Object,
            _mockWorldService.Object);
    }

    #region Dispose Tests

    [Fact]
    public void Dispose_UnregistersGameTickListener()
    {
        // Arrange
        var registry = CreateRegistry();
        long callbackId = 12345;
        _mockEventService.Setup(e => e.RegisterGameTickListener(It.IsAny<Action<float>>(), It.IsAny<int>()))
            .Returns(callbackId);
        registry.Initialize();

        // Act
        registry.Dispose();

        // Assert
        _mockEventService.Verify(e => e.UnregisterCallback(callbackId), Times.Once());
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void RegisterHandler_UsesLogger()
    {
        // Arrange
        var registry = CreateRegistry();
        var mockHandler = new Mock<ISpecialEffectHandler>();
        mockHandler.Setup(h => h.EffectId).Returns("test_effect");

        // Act
        registry.RegisterHandler(mockHandler.Object);

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("test_effect"))),
            Times.Once());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SpecialEffectRegistry(null!, _mockEventService.Object, _mockWorldService.Object));
    }

    [Fact]
    public void Constructor_WithNullEventService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SpecialEffectRegistry(_mockLogger.Object, null!, _mockWorldService.Object));
    }

    [Fact]
    public void Constructor_WithNullWorldService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SpecialEffectRegistry(_mockLogger.Object, _mockEventService.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => CreateRegistry());
        Assert.Null(exception);
    }

    #endregion

    #region Initialize Tests

    [Fact]
    public void Initialize_RegistersGameTickListener()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        registry.Initialize();

        // Assert
        _mockEventService.Verify(
            e => e.RegisterGameTickListener(It.IsAny<Action<float>>(), It.IsAny<int>()),
            Times.Once());
    }

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        registry.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Initializing") && s.Contains("Special Effect Registry"))),
            Times.Once());
    }

    #endregion
}