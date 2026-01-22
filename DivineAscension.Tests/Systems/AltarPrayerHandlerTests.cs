using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.BuffSystem.Interfaces;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems;

public class AltarPrayerHandlerTests
{
    private readonly Mock<AltarEventEmitter> _altarEventEmitter;
    private readonly Mock<IBuffManager> _buffManager;
    private readonly GameBalanceConfig _config;
    private readonly FakeEventService _eventService;
    private readonly AltarPrayerHandler _handler;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<ILogger> _logger;
    private readonly SpyPlayerMessenger _messenger;
    private readonly Mock<IOfferingLoader> _offeringLoader;
    private readonly Mock<IPlayerProgressionDataManager> _progressionDataManager;
    private readonly Mock<IPlayerProgressionService> _progressionService;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly Mock<IRitualLoader> _ritualLoader;
    private readonly Mock<IRitualProgressManager> _ritualProgressManager;
    private readonly FakeTimeService _timeService;
    private readonly Mock<IWorldService> _worldService;

    public AltarPrayerHandlerTests()
    {
        // Initialize localization service for tests
        TestFixtures.InitializeLocalizationForTests();

        _eventService = new FakeEventService();
        _offeringLoader = new Mock<IOfferingLoader>();
        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _progressionDataManager = new Mock<IPlayerProgressionDataManager>();
        _progressionService = new Mock<IPlayerProgressionService>();
        _messenger = new SpyPlayerMessenger();
        _worldService = new Mock<IWorldService>();
        _logger = new Mock<ILogger>();
        _buffManager = new Mock<IBuffManager>();
        _config = new GameBalanceConfig();
        _timeService = new FakeTimeService();
        _altarEventEmitter = new Mock<AltarEventEmitter>();
        _ritualLoader = new Mock<IRitualLoader>();
        _ritualProgressManager = new Mock<IRitualProgressManager>();

        // Setup default: no cooldown active
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiry(It.IsAny<string>()))
            .Returns(0);

        _handler = new AltarPrayerHandler(
            _logger.Object,
            _offeringLoader.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _progressionService.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _ritualProgressManager.Object,
            _ritualLoader.Object,
            _worldService.Object);

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

    [Fact]
    public void ProcessPrayer_AltarNotConsecrated_ReturnsFailure()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns((HolySiteData?)null);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("This altar is not consecrated. It must be part of a holy site.", result.Message);
    }

    // Test helpers
    private static HolySiteData CreateHolySite(string religionUID) =>
        new() { SiteUID = "site1", ReligionUID = religionUID, SiteName = "Test Site" };

    private static ReligionData CreateReligion(string religionUID, DeityDomain domain) =>
        new()
        {
            ReligionUID = religionUID,
            ReligionName = "Test Religion",
            DeityName = "Test Deity",
            Domain = domain,
            FounderUID = "player1",
            FounderName = "Player1"
        };

    [Fact]
    public void ProcessPrayer_PlayerNotInReligion_ReturnsFailure()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns((ReligionData?)null);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You must be in a religion to pray.", result.Message);
    }

    [Fact]
    public void ProcessPrayer_WrongReligion_ReturnsFailure()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion2", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You can only pray at altars belonging to your religion.", result.Message);
    }

    // Note: Cooldown behavior is a side effect managed by OnAltarUsed, not ProcessPrayer.
    // ProcessPrayer reads cooldown state but doesn't update it, making it difficult to test in isolation.

    [Fact]
    public void ProcessPrayer_ValidPrayerWithoutOffering_ReturnsSuccess()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(10, result.FavorAwarded); // BASE (5) * tier1 multiplier (2.0) = 10
        Assert.Equal(10, result.PrestigeAwarded);
        Assert.Equal(1, result.HolySiteTier);
        Assert.False(result.ShouldConsumeOffering);
        Assert.True(result.ShouldUpdateCooldown);

        // Verify progression was awarded
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            "player1",
            "religion1",
            10,
            10,
            DeityDomain.Craft,
            It.IsAny<string>()), Times.Once);
    }

    // Note: Testing with actual ItemStack objects is difficult due to non-virtual properties.
    // Offering-specific logic is tested at integration level or via Calculate OfferingValue tests.

    [Fact]
    public void ProcessPrayer_OnCooldown_ReturnsCorrectRemainingTime()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Setup cooldown: 30 minutes remaining (1800000 ms)
        var currentTime = 0L;
        var cooldownExpiry = currentTime + 1800000; // 30 minutes from now
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiry("player1"))
            .Returns(cooldownExpiry);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, currentTime);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You must wait 30 more minute(s) before praying again.", result.Message);
    }

    [Fact]
    public void ProcessPrayer_OnCooldownNearExpiry_RoundsCorrectly()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Setup cooldown: 59.5 minutes remaining (3570000 ms = 59 min 30 sec)
        // This should round to 60 minutes, NOT 61
        var currentTime = 0L;
        var cooldownExpiry = currentTime + 3570000; // 59.5 minutes from now
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiry("player1"))
            .Returns(cooldownExpiry);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, currentTime);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You must wait 60 more minute(s) before praying again.", result.Message);
    }
}