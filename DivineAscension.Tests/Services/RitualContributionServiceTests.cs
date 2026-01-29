using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Services;

public class RitualContributionServiceTests
{
    private readonly Mock<IRitualProgressManager> _ritualProgressManager;
    private readonly Mock<IRitualLoader> _ritualLoader;
    private readonly FakeOfferingEvaluator _offeringEvaluator;
    private readonly Mock<IPlayerProgressionService> _progressionService;
    private readonly Mock<IWorldService> _worldService;
    private readonly Mock<ILoggerWrapper> _logger;
    private readonly RitualContributionService _service;

    public RitualContributionServiceTests()
    {
        TestFixtures.InitializeLocalizationForTests();

        _ritualProgressManager = new Mock<IRitualProgressManager>();
        _ritualLoader = new Mock<IRitualLoader>();
        _offeringEvaluator = new FakeOfferingEvaluator();
        _progressionService = new Mock<IPlayerProgressionService>();
        _worldService = new Mock<IWorldService>();
        _logger = new Mock<ILoggerWrapper>();

        _service = new RitualContributionService(
            _ritualProgressManager.Object,
            _ritualLoader.Object,
            _offeringEvaluator,
            _progressionService.Object,
            _worldService.Object,
            _logger.Object);
    }

    [Fact]
    public void Constructor_NullRitualProgressManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RitualContributionService(
            null!,
            _ritualLoader.Object,
            _offeringEvaluator,
            _progressionService.Object,
            _worldService.Object,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullRitualLoader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RitualContributionService(
            _ritualProgressManager.Object,
            null!,
            _offeringEvaluator,
            _progressionService.Object,
            _worldService.Object,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullOfferingEvaluator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RitualContributionService(
            _ritualProgressManager.Object,
            _ritualLoader.Object,
            null!,
            _progressionService.Object,
            _worldService.Object,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullProgressionService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RitualContributionService(
            _ritualProgressManager.Object,
            _ritualLoader.Object,
            _offeringEvaluator,
            null!,
            _worldService.Object,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullWorldService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RitualContributionService(
            _ritualProgressManager.Object,
            _ritualLoader.Object,
            _offeringEvaluator,
            _progressionService.Object,
            null!,
            _logger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RitualContributionService(
            _ritualProgressManager.Object,
            _ritualLoader.Object,
            _offeringEvaluator,
            _progressionService.Object,
            _worldService.Object,
            null!));
    }

    [Fact]
    public void TryContributeToRitual_NoActiveRitual_NoMatchingRitual_ReturnsFailure()
    {
        // Arrange
        var holySite = CreateHolySite(activeRitual: null);
        var religion = CreateReligion(DeityDomain.Craft);
        var offering = new Mock<ItemStack>(MockBehavior.Loose).Object;

        _ritualLoader.Setup(x => x.GetRitualForTierUpgrade(DeityDomain.Craft, 1, 2))
            .Returns((Ritual?)null);

        // Act
        var result = _service.TryContributeToRitual(holySite, offering, religion, "player1", "Player1");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.RitualStarted);
        Assert.False(result.RitualCompleted);
    }

    [Fact]
    public void TryContributeToRitual_ActiveRitual_ContributionSucceeds_ReturnsSuccess()
    {
        // Arrange
        var holySite = CreateHolySite(activeRitual: new RitualProgressData { RitualId = "craft_tier2" });
        var religion = CreateReligion(DeityDomain.Craft);
        var offering = new Mock<ItemStack>(MockBehavior.Loose).Object;

        _offeringEvaluator.DefaultReturnValue = 20;

        _ritualProgressManager.Setup(x => x.ContributeToRitual("site1", offering, "player1"))
            .Returns(new RitualContributionResult(
                Success: true,
                Message: "Contribution accepted",
                StepId: "step1",
                StepDiscovered: false,
                StepCompleted: false,
                RitualCompleted: false));

        // Act
        var result = _service.TryContributeToRitual(holySite, offering, religion, "player1", "Player1");

        // Assert
        Assert.True(result.Success);
        Assert.False(result.RitualStarted);
        Assert.False(result.RitualCompleted);
        Assert.Equal(10, result.FavorAwarded); // 20 * 0.5 = 10
        Assert.True(result.ShouldConsumeOffering);
    }

    [Fact]
    public void TryContributeToRitual_ActiveRitual_ContributionFails_ReturnsFailure()
    {
        // Arrange
        var holySite = CreateHolySite(activeRitual: new RitualProgressData { RitualId = "craft_tier2" });
        var religion = CreateReligion(DeityDomain.Craft);
        var offering = new Mock<ItemStack>(MockBehavior.Loose).Object;

        _ritualProgressManager.Setup(x => x.ContributeToRitual("site1", offering, "player1"))
            .Returns(new RitualContributionResult(
                Success: false,
                Message: "Item does not match any ritual requirement"));

        // Act
        var result = _service.TryContributeToRitual(holySite, offering, religion, "player1", "Player1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Item does not match any ritual requirement", result.Message);
    }

    [Fact]
    public void TryContributeToRitual_RitualCompleted_AwardsProgressionWithCompletionMessage()
    {
        // Arrange
        var holySite = CreateHolySite(activeRitual: new RitualProgressData { RitualId = "craft_tier2" });
        var religion = CreateReligion(DeityDomain.Craft);
        var offering = new Mock<ItemStack>(MockBehavior.Loose).Object;

        _offeringEvaluator.DefaultReturnValue = 50;

        _ritualProgressManager.Setup(x => x.ContributeToRitual("site1", offering, "player1"))
            .Returns(new RitualContributionResult(
                Success: true,
                Message: "Ritual completed!",
                RitualCompleted: true));

        // Act
        var result = _service.TryContributeToRitual(holySite, offering, religion, "player1", "Player1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.RitualCompleted);
        Assert.Equal(25, result.FavorAwarded); // 50 * 0.5 = 25

        // Verify progression was awarded with completion message
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            "player1",
            "religion1",
            25,
            25,
            DeityDomain.Craft,
            It.Is<string>(s => s.Contains("completed"))), Times.Once);
    }

    [Fact]
    public void TryContributeToRitual_MaxTier_DoesNotAttemptAutoStart()
    {
        // Arrange - holy site at tier 3 (max)
        var holySite = CreateHolySite(tier: 3, activeRitual: null);
        var religion = CreateReligion(DeityDomain.Craft);
        var offering = new Mock<ItemStack>(MockBehavior.Loose).Object;

        // Act
        var result = _service.TryContributeToRitual(holySite, offering, religion, "player1", "Player1");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.RitualStarted);

        // Verify no ritual was started
        _ritualProgressManager.Verify(x => x.StartRitual(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void TryContributeToRitual_ZeroFavorOffering_NoProgressionAwarded()
    {
        // Arrange
        var holySite = CreateHolySite(activeRitual: new RitualProgressData { RitualId = "craft_tier2" });
        var religion = CreateReligion(DeityDomain.Craft);
        var offering = new Mock<ItemStack>(MockBehavior.Loose).Object;

        _offeringEvaluator.DefaultReturnValue = 0; // No favor value

        _ritualProgressManager.Setup(x => x.ContributeToRitual("site1", offering, "player1"))
            .Returns(new RitualContributionResult(
                Success: true,
                Message: "Contribution accepted"));

        // Act
        var result = _service.TryContributeToRitual(holySite, offering, religion, "player1", "Player1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.FavorAwarded);

        // Verify progression was NOT awarded (zero favor)
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.IsAny<string>()), Times.Never);
    }

    private static HolySiteData CreateHolySite(
        string siteUID = "site1",
        string religionUID = "religion1",
        int tier = 1,
        RitualProgressData? activeRitual = null)
    {
        return new HolySiteData
        {
            SiteUID = siteUID,
            ReligionUID = religionUID,
            SiteName = "Test Site",
            RitualTier = tier,
            ActiveRitual = activeRitual
        };
    }

    private static ReligionData CreateReligion(DeityDomain domain)
    {
        return new ReligionData
        {
            ReligionUID = "religion1",
            ReligionName = "Test Religion",
            Domain = domain,
            FounderUID = "founder1",
            FounderName = "Founder"
        };
    }
}
