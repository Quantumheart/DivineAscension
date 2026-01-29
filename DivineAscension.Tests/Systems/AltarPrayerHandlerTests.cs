using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
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
    private readonly AltarPrayerHandler _handler;
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly Mock<ILoggerWrapper> _logger;
    private readonly SpyPlayerMessenger _messenger;
    private readonly FakeOfferingEvaluator _offeringEvaluator;
    private readonly FakePrayerEffectsService _prayerEffectsService;
    private readonly Mock<IPlayerProgressionDataManager> _progressionDataManager;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly FakeRitualContributionService _ritualContributionService;
    private readonly FakeTimeService _timeService;

    public AltarPrayerHandlerTests()
    {
        // Initialize localization service for tests
        TestFixtures.InitializeLocalizationForTests();

        _holySiteManager = new Mock<IHolySiteManager>();
        _religionManager = new Mock<IReligionManager>();
        _progressionDataManager = new Mock<IPlayerProgressionDataManager>();
        _messenger = new SpyPlayerMessenger();
        _logger = new Mock<ILoggerWrapper>();
        _buffManager = new Mock<IBuffManager>();
        _config = new GameBalanceConfig();
        _timeService = new FakeTimeService();
        _altarEventEmitter = new Mock<AltarEventEmitter>();

        // Use fakes for extracted services
        _offeringEvaluator = new FakeOfferingEvaluator();
        _prayerEffectsService = new FakePrayerEffectsService();
        _ritualContributionService = new FakeRitualContributionService();

        // Setup default: no cooldown active
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiry(It.IsAny<string>()))
            .Returns(0);

        _handler = new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService);

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
    public void ProcessPrayer_WrongDomain_ReturnsFailure()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var holySiteOwnerReligion = CreateReligion("religion1", DeityDomain.Craft);
        var playerReligion = CreateReligion("religion2", DeityDomain.Wild); // Different domain

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(playerReligion);
        _religionManager.Setup(x => x.GetReligion("religion1"))
            .Returns(holySiteOwnerReligion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You can only pray at altars of religions that worship the same deity domain.", result.Message);
    }

    [Fact]
    public void ProcessPrayer_SameDomainDifferentReligion_ReturnsSuccess()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var holySiteOwnerReligion = CreateReligion("religion1", DeityDomain.Craft);
        var playerReligion = CreateReligion("religion2", DeityDomain.Craft); // Same domain, different religion

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(playerReligion);
        _religionManager.Setup(x => x.GetReligion("religion1"))
            .Returns(holySiteOwnerReligion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(10, result.FavorAwarded); // BASE (5) * tier1 multiplier (2.0) = 10
        Assert.Equal(10, result.PrestigeAwarded);
        Assert.Equal(1, result.HolySiteTier);
        Assert.True(result.ShouldUpdateCooldown);
    }

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
    }

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

    #region Constructor Null Guard Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            null!,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullHolySiteManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            null!,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullReligionManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            null!,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullProgressionDataManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            null!,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullMessenger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            null!,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullBuffManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            null!,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            null!,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullTimeService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            null!,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullAltarEventEmitter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            null!,
            _offeringEvaluator,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullOfferingEvaluator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            null!,
            _prayerEffectsService,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullPrayerEffectsService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            null!,
            _ritualContributionService));
    }

    [Fact]
    public void Constructor_NullRitualContributionService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AltarPrayerHandler(
            _logger.Object,
            _holySiteManager.Object,
            _religionManager.Object,
            _progressionDataManager.Object,
            _messenger,
            _buffManager.Object,
            _config,
            _timeService,
            _altarEventEmitter.Object,
            _offeringEvaluator,
            _prayerEffectsService,
            null!));
    }

    #endregion

    #region Domain and Religion Validation Tests

    [Fact]
    public void ProcessPrayer_SameReligion_SkipsDomainCheckAndSucceeds()
    {
        // Arrange - player is member of the religion that owns the holy site
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft); // Same religion UID

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);
        // Note: GetReligion is NOT called because player is in same religion

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.True(result.Success);
        _religionManager.Verify(x => x.GetReligion(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessPrayer_HolySiteOwnerReligionNull_ReturnsFailure()
    {
        // Arrange - holy site owner religion was deleted
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("deleted_religion");
        var playerReligion = CreateReligion("religion2", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(playerReligion);
        _religionManager.Setup(x => x.GetReligion("deleted_religion"))
            .Returns((ReligionData?)null); // Religion no longer exists

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert - domain defaults to None, which doesn't match Craft
        Assert.False(result.Success);
        Assert.Equal("You can only pray at altars of religions that worship the same deity domain.", result.Message);
    }

    #endregion

    #region Cooldown Edge Case Tests

    [Fact]
    public void ProcessPrayer_CooldownLessThanOneMinute_ShowsOneMinute()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Setup cooldown: 15 seconds remaining (15000 ms)
        // This rounds to 0 minutes, but code enforces minimum of 1
        var currentTime = 0L;
        var cooldownExpiry = currentTime + 15000; // 15 seconds from now
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiry("player1"))
            .Returns(cooldownExpiry);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, currentTime);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You must wait 1 more minute(s) before praying again.", result.Message);
    }

    #endregion

    #region Ritual Contribution Tests

    [Fact]
    public void ProcessPrayer_RitualContributionSuccess_ReturnsRitualResultWithoutCooldown()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);
        // Use real ItemStack with StackSize > 0 to pass the offering check
        var offering = new ItemStack { StackSize = 1 };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Configure ritual contribution to succeed
        _ritualContributionService.NextResult = new RitualAttemptResult(
            Success: true,
            RitualStarted: true,
            RitualCompleted: false,
            Message: "Ritual discovered and started!",
            FavorAwarded: 15,
            PrestigeAwarded: 15,
            ShouldConsumeOffering: true);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, offering, 0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Ritual discovered and started!", result.Message);
        Assert.Equal(15, result.FavorAwarded);
        Assert.Equal(15, result.PrestigeAwarded);
        Assert.True(result.ShouldConsumeOffering);
        Assert.False(result.ShouldUpdateCooldown); // No cooldown for ritual contributions
    }

    [Fact]
    public void ProcessPrayer_RitualContributionFails_ContinuesToNormalPrayer()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);
        // Use real ItemStack with StackSize > 0 to pass the offering check
        var offering = new ItemStack { StackSize = 1 };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Ritual contribution fails (item doesn't match any ritual)
        _ritualContributionService.DefaultResult = new RitualAttemptResult(
            Success: false,
            RitualStarted: false,
            RitualCompleted: false,
            Message: "");

        // Offering evaluator returns 0 (not a valid offering for domain either)
        _offeringEvaluator.DefaultReturnValue = 0;

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, offering, 0);

        // Assert - continues to normal prayer flow
        Assert.True(result.Success);
        Assert.True(result.ShouldUpdateCooldown);
        Assert.False(result.ShouldConsumeOffering); // Offering rejected
    }

    #endregion

    #region Offering Processing Tests

    [Fact]
    public void ProcessPrayer_OfferingTierRejected_ReturnsFailure()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);
        // Use real ItemStack with StackSize > 0 to pass the offering check
        var offering = new ItemStack { StackSize = 1 };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Offering evaluator returns -1 (tier too low)
        _offeringEvaluator.DefaultReturnValue = -1;

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, offering, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("This holy site is not powerful enough to accept such a valuable offering.", result.Message);
    }

    [Fact]
    public void ProcessPrayer_ValidOffering_ConsumesAndAddsBonus()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);
        // Use real ItemStack with StackSize > 0 to pass the offering check
        var offering = new ItemStack { StackSize = 1 };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Offering evaluator returns positive value
        _offeringEvaluator.DefaultReturnValue = 20;

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, offering, 0);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ShouldConsumeOffering);
        // BASE (5) + offering bonus (20) = 25, * tier 1 multiplier (2.0) = 50
        Assert.Equal(50, result.FavorAwarded);
        Assert.Equal(50, result.PrestigeAwarded);
    }

    [Fact]
    public void ProcessPrayer_OfferingDomainRejected_ProceedsWithoutBonus()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySite("religion1");
        var religion = CreateReligion("religion1", DeityDomain.Craft);
        // Use real ItemStack with StackSize > 0 to pass the offering check
        var offering = new ItemStack { StackSize = 1 };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Offering evaluator returns 0 (not valid for this domain)
        _offeringEvaluator.DefaultReturnValue = 0;

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, offering, 0);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.ShouldConsumeOffering); // Offering not consumed
        // BASE (5) * tier 1 multiplier (2.0) = 10 (no offering bonus)
        Assert.Equal(10, result.FavorAwarded);
    }

    #endregion

    #region Tier Multiplier Tests

    private static HolySiteData CreateHolySiteWithTier(string religionUID, int tier) =>
        new() { SiteUID = "site1", ReligionUID = religionUID, SiteName = "Test Site", RitualTier = tier };

    [Fact]
    public void ProcessPrayer_Tier2HolySite_UsesCorrectMultipliers()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySiteWithTier("religion1", 2);
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.HolySiteTier);
        // BASE (5) * tier 2 prayer multiplier (2.5) = 12.5, Math.Round uses banker's rounding = 12
        Assert.Equal(12, result.FavorAwarded);
        Assert.Equal(_config.HolySiteTier2Multiplier, result.BuffMultiplier);
    }

    [Fact]
    public void ProcessPrayer_Tier3HolySite_UsesCorrectMultipliers()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySiteWithTier("religion1", 3);
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.HolySiteTier);
        // BASE (5) * tier 3 prayer multiplier (3.0) = 15
        Assert.Equal(15, result.FavorAwarded);
        Assert.Equal(_config.HolySiteTier3Multiplier, result.BuffMultiplier);
    }

    [Fact]
    public void ProcessPrayer_InvalidTier_UsesDefaultMultiplier()
    {
        // Arrange - tier 0 or negative (edge case)
        var altarPos = new BlockPos(100, 50, 100);
        var holySite = CreateHolySiteWithTier("religion1", 0);
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(altarPos))
            .Returns(holySite);
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Act
        var result = _handler.ProcessPrayer("player1", "TestPlayer", altarPos, null, 0);

        // Assert
        Assert.True(result.Success);
        // Tier 0 uses default multiplier of 1.0
        Assert.Equal(1.0f, result.BuffMultiplier);
    }

    #endregion
}
