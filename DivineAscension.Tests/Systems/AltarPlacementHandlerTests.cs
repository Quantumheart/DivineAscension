using DivineAscension.API.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Systems;

public class AltarPlacementHandlerTests
{
    private readonly FakeEventService _eventService;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly Mock<IWorldService> _worldService;
    private readonly SpyPlayerMessenger _messenger;
    private readonly Mock<ILogger> _logger;
    private readonly AltarPlacementHandler _handler;

    public AltarPlacementHandlerTests()
    {
        _eventService = new FakeEventService();
        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _worldService = new Mock<IWorldService>();
        _messenger = new SpyPlayerMessenger();
        _logger = new Mock<ILogger>();

        _handler = new AltarPlacementHandler(
            _logger.Object,
            _eventService,
            _holySiteManager.Object,
            _religionManager.Object,
            _worldService.Object,
            _messenger);

        _handler.Initialize();
    }

    [Fact]
    public void Initialize_SubscribesToDidPlaceBlockEvent()
    {
        // Arrange & Act done in constructor

        // Assert
        Assert.True(_eventService.HasDidPlaceBlockSubscribers());
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Act
        _handler.Dispose();

        // Assert
        Assert.False(_eventService.HasDidPlaceBlockSubscribers());
    }
}
