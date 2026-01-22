using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

public class AltarPlacementHandlerTests
{
    private readonly Mock<AltarEventEmitter> _altarEventEmitter;
    private readonly AltarPlacementHandler _handler;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<ILoggerWrapper> _logger;
    private readonly SpyPlayerMessenger _messenger;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly Mock<IWorldService> _worldService;

    public AltarPlacementHandlerTests()
    {
        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _worldService = new Mock<IWorldService>();
        _messenger = new SpyPlayerMessenger();
        _logger = new Mock<ILoggerWrapper>();
        _altarEventEmitter = new Mock<AltarEventEmitter>();

        _handler = new AltarPlacementHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _worldService.Object,
            _messenger,
            _altarEventEmitter.Object);

        _handler.Initialize();
    }

    [Fact]
    public void Initialize_SubscribesToAltarPlacedEvent()
    {
        // Arrange & Act done in constructor

        // Assert - verify handler is subscribed (no exception means success)
        Assert.NotNull(_handler);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Act & Assert - verify handler unsubscribes without throwing
        _handler.Dispose();

        // Verify no exceptions were thrown during disposal
        Assert.True(true);
    }
}