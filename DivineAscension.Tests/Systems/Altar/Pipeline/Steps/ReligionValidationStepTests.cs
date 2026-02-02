using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Altar.Pipeline;
using DivineAscension.Systems.Altar.Pipeline.Steps;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems.Altar.Pipeline.Steps;

public class ReligionValidationStepTests
{
    private readonly Mock<IReligionManager> _religionManager;
    private readonly ReligionValidationStep _step;

    public ReligionValidationStepTests()
    {
        TestFixtures.InitializeLocalizationForTests();
        _religionManager = new Mock<IReligionManager>();
        _step = new ReligionValidationStep(_religionManager.Object);
    }

    private static PrayerContext CreateContext(HolySiteData? holySite = null) =>
        new()
        {
            PlayerUID = "player1",
            PlayerName = "TestPlayer",
            AltarPosition = new BlockPos(100, 50, 100),
            Offering = null,
            CurrentTime = 0,
            Player = null!,
            HolySite = holySite ?? new HolySiteData { SiteUID = "site1", ReligionUID = "religion1" }
        };

    private static ReligionData CreateReligion(string uid, DeityDomain domain) =>
        new()
        {
            ReligionUID = uid,
            ReligionName = "Test Religion",
            DeityName = "Test Deity",
            Domain = domain,
            FounderUID = "founder1",
            FounderName = "Founder"
        };

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        Assert.Equal("ReligionValidation", _step.Name);
    }

    [Fact]
    public void Execute_PlayerNotInReligion_SetsFailureAndCompletes()
    {
        // Arrange
        var context = CreateContext();
        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns((ReligionData?)null);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.Equal("You must be in a religion to pray.", context.Message);
    }

    [Fact]
    public void Execute_SameReligion_SetsReligionAndContinues()
    {
        // Arrange - player is in same religion as holy site owner
        var holySite = new HolySiteData { SiteUID = "site1", ReligionUID = "religion1" };
        var context = CreateContext(holySite);
        var religion = CreateReligion("religion1", DeityDomain.Craft);

        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(religion);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.IsComplete);
        Assert.Equal(religion, context.Religion);
        Assert.Equal(DeityDomain.Craft, context.Domain);
        // Should NOT call GetReligion since player is in same religion
        _religionManager.Verify(x => x.GetReligion(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Execute_DifferentReligionSameDomain_Continues()
    {
        // Arrange
        var holySite = new HolySiteData { SiteUID = "site1", ReligionUID = "religion1" };
        var context = CreateContext(holySite);
        var playerReligion = CreateReligion("religion2", DeityDomain.Craft);
        var ownerReligion = CreateReligion("religion1", DeityDomain.Craft);

        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(playerReligion);
        _religionManager.Setup(x => x.GetReligion("religion1"))
            .Returns(ownerReligion);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.IsComplete);
        Assert.Equal(playerReligion, context.Religion);
    }

    [Fact]
    public void Execute_DifferentDomain_SetsFailureAndCompletes()
    {
        // Arrange
        var holySite = new HolySiteData { SiteUID = "site1", ReligionUID = "religion1" };
        var context = CreateContext(holySite);
        var playerReligion = CreateReligion("religion2", DeityDomain.Wild);
        var ownerReligion = CreateReligion("religion1", DeityDomain.Craft);

        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(playerReligion);
        _religionManager.Setup(x => x.GetReligion("religion1"))
            .Returns(ownerReligion);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.Equal("You can only pray at altars of religions that worship the same deity domain.", context.Message);
    }

    [Fact]
    public void Execute_OwnerReligionDeleted_SetsFailure()
    {
        // Arrange - holy site owner religion was deleted
        var holySite = new HolySiteData { SiteUID = "site1", ReligionUID = "deleted_religion" };
        var context = CreateContext(holySite);
        var playerReligion = CreateReligion("religion2", DeityDomain.Craft);

        _religionManager.Setup(x => x.GetPlayerReligion("player1"))
            .Returns(playerReligion);
        _religionManager.Setup(x => x.GetReligion("deleted_religion"))
            .Returns((ReligionData?)null);

        // Act
        _step.Execute(context);

        // Assert - domain defaults to None, which doesn't match Craft
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
    }
}