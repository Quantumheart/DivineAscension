using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Altar.Pipeline;
using DivineAscension.Systems.Altar.Pipeline.Steps;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems.Altar.Pipeline.Steps;

public class ProgressionAwardStepTests
{
    private readonly Mock<IPlayerProgressionService> _progressionService;
    private readonly ProgressionAwardStep _step;

    public ProgressionAwardStepTests()
    {
        _progressionService = new Mock<IPlayerProgressionService>();
        _step = new ProgressionAwardStep(_progressionService.Object);
    }

    private static PrayerContext CreateSuccessfulContext(
        int favorAwarded = 10,
        int prestigeAwarded = 10,
        int offeringBonus = 0,
        bool offeringRejected = false) =>
        new()
        {
            PlayerUID = "player1",
            PlayerName = "TestPlayer",
            AltarPosition = new BlockPos(100, 50, 100),
            Offering = null,
            CurrentTime = 0,
            Player = null!,
            Success = true,
            FavorAwarded = favorAwarded,
            PrestigeAwarded = prestigeAwarded,
            OfferingBonus = offeringBonus,
            OfferingRejectedDomain = offeringRejected,
            Domain = DeityDomain.Craft,
            HolySite = new HolySiteData
            {
                SiteUID = "site1",
                ReligionUID = "religion1",
                SiteName = "Test Site"
            }
        };

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        Assert.Equal("ProgressionAward", _step.Name);
    }

    [Fact]
    public void Execute_SuccessfulPrayer_AwardsProgression()
    {
        // Arrange
        var context = CreateSuccessfulContext(favorAwarded: 15, prestigeAwarded: 15);

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            "player1",
            "religion1",
            15,
            15,
            DeityDomain.Craft,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Execute_FailedPrayer_DoesNotAwardProgression()
    {
        // Arrange
        var context = CreateSuccessfulContext();
        context.Success = false;

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Execute_RitualContribution_DoesNotAwardProgression()
    {
        // Arrange - ritual contributions handle their own progression
        var context = CreateSuccessfulContext();
        context.IsRitualContribution = true;

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Execute_WithOffering_IncludesBonusInMessage()
    {
        // Arrange
        var context = CreateSuccessfulContext(offeringBonus: 20);

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.Is<string>(msg => msg.Contains("offering") && msg.Contains("+20"))), Times.Once);
    }

    [Fact]
    public void Execute_RejectedOffering_IncludesRejectionInMessage()
    {
        // Arrange
        var context = CreateSuccessfulContext(offeringRejected: true);

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.Is<string>(msg => msg.Contains("rejected"))), Times.Once);
    }

    [Fact]
    public void Execute_NoOffering_BasicMessage()
    {
        // Arrange
        var context = CreateSuccessfulContext();

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.Is<string>(msg => msg.Contains("prayed at holy site"))), Times.Once);
    }

    [Fact]
    public void Execute_UsesCorrectReligionUID()
    {
        // Arrange
        var context = CreateSuccessfulContext();
        context.HolySite = new HolySiteData
        {
            SiteUID = "site1",
            ReligionUID = "specific_religion_uid",
            SiteName = "Test"
        };

        // Act
        _step.Execute(context);

        // Assert
        _progressionService.Verify(x => x.AwardProgressionForPrayer(
            "player1",
            "specific_religion_uid",
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DeityDomain>(),
            It.IsAny<string>()), Times.Once);
    }
}