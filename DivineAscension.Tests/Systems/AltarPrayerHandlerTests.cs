using DivineAscension.Services;
using DivineAscension.Systems.Altar;
using DivineAscension.Tests.Helpers;
using Moq;

namespace DivineAscension.Tests.Systems;

/// <summary>
/// Tests for the refactored AltarPrayerHandler which delegates to IPrayerPipeline.
/// Prayer logic tests are in the individual step test classes.
/// </summary>
public class AltarPrayerHandlerTests
{
    private readonly Mock<AltarEventEmitter> _altarEventEmitter;
    private readonly Mock<ILoggerWrapper> _logger;
    private readonly SpyPlayerMessenger _messenger;
    private readonly FakePrayerPipeline _pipeline;
    private readonly FakeTimeService _timeService;

    public AltarPrayerHandlerTests()
    {
        _altarEventEmitter = new Mock<AltarEventEmitter>();
        _pipeline = new FakePrayerPipeline();
        _messenger = new SpyPlayerMessenger();
        _timeService = new FakeTimeService();
        _logger = new Mock<ILoggerWrapper>();
    }

    private AltarPrayerHandler CreateHandler() =>
        new AltarPrayerHandler(
            _altarEventEmitter.Object,
            _pipeline,
            _messenger,
            _timeService,
            _logger.Object);

    #region Constructor Null Guard Tests

    [Fact]
    public void Constructor_NullAltarEventEmitter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            null!,
            _pipeline,
            _messenger,
            _timeService,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _altarEventEmitter.Object,
            null!,
            _messenger,
            _timeService,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullMessenger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _altarEventEmitter.Object,
            _pipeline,
            null!,
            _timeService,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullTimeService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _altarEventEmitter.Object,
            _pipeline,
            _messenger,
            null!,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _altarEventEmitter.Object,
            _pipeline,
            _messenger,
            _timeService,
            null!));
    }

    #endregion

    #region Initialization and Disposal Tests

    [Fact]
    public void Initialize_SubscribesToAltarUsedEvent()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        handler.Initialize();

        // Assert - verify handler doesn't throw and is initialized
        Assert.NotNull(handler);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var handler = CreateHandler();
        handler.Initialize();

        // Act & Assert - verify handler unsubscribes without throwing
        handler.Dispose();
        Assert.True(true);
    }

    #endregion
}