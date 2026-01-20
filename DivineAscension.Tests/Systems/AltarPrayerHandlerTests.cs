using DivineAscension.API.Interfaces;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

public class AltarPrayerHandlerTests
{
    private readonly Mock<IActivityLogManager> _activityLogManager;
    private readonly FakeEventService _eventService;
    private readonly Mock<IFavorSystem> _favorSystem;
    private readonly AltarPrayerHandler _handler;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<ILogger> _logger;
    private readonly SpyPlayerMessenger _messenger;
    private readonly Mock<IOfferingLoader> _offeringLoader;
    private readonly Mock<IReligionPrestigeManager> _prestigeManager;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly Mock<IWorldService> _worldService;

    public AltarPrayerHandlerTests()
    {
        _eventService = new FakeEventService();
        _offeringLoader = new Mock<IOfferingLoader>();
        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _favorSystem = new Mock<IFavorSystem>();
        _prestigeManager = new Mock<IReligionPrestigeManager>();
        _activityLogManager = new Mock<IActivityLogManager>();
        _messenger = new SpyPlayerMessenger();
        _worldService = new Mock<IWorldService>();
        _logger = new Mock<ILogger>();

        _worldService.Setup(x => x.ElapsedMilliseconds).Returns(0);

        _handler = new AltarPrayerHandler(
            _logger.Object,
            _eventService,
            _offeringLoader.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _favorSystem.Object,
            _prestigeManager.Object,
            _activityLogManager.Object,
            _messenger,
            _worldService.Object);

        _handler.Initialize();
    }

    [Fact]
    public void Initialize_SubscribesToDidUseBlockEvent()
    {
        // Arrange & Act done in constructor

        // Assert
        Assert.True(_eventService.HasDidUseBlockSubscribers());
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Act
        _handler.Dispose();

        // Assert
        Assert.False(_eventService.HasDidUseBlockSubscribers());
    }
}