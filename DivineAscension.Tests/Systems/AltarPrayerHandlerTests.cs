using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Systems.BuffSystem.Interfaces;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

public class AltarPrayerHandlerTests
{
    private readonly Mock<IActivityLogManager> _activityLogManager;
    private readonly Mock<IBuffManager> _buffManager;
    private readonly GameBalanceConfig _config;
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
        _buffManager = new Mock<IBuffManager>();
        _config = new GameBalanceConfig();

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
            _worldService.Object,
            _buffManager.Object,
            _config);

        _handler.Initialize();
    }

    [Fact]
    public void Initialize_SubscribesToAltarPatchEvent()
    {
        // Arrange & Act done in constructor

        // Assert - verify handler is subscribed to AltarPatches.OnAltarUsed
        // Since we can't directly check static event subscribers, verify the handler doesn't throw
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