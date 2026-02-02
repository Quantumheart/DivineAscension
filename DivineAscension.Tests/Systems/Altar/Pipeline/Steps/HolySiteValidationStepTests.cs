using DivineAscension.Data;
using DivineAscension.Systems.Altar.Pipeline;
using DivineAscension.Systems.Altar.Pipeline.Steps;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems.Altar.Pipeline.Steps;

public class HolySiteValidationStepTests
{
    private readonly Mock<IHolySiteManager> _holySiteManager;
    private readonly HolySiteValidationStep _step;

    public HolySiteValidationStepTests()
    {
        TestFixtures.InitializeLocalizationForTests();
        _holySiteManager = new Mock<IHolySiteManager>();
        _step = new HolySiteValidationStep(_holySiteManager.Object);
    }

    private static PrayerContext CreateContext(BlockPos? altarPos = null) =>
        new()
        {
            PlayerUID = "player1",
            PlayerName = "TestPlayer",
            AltarPosition = altarPos ?? new BlockPos(100, 50, 100),
            Offering = null,
            CurrentTime = 0,
            Player = null!
        };

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        Assert.Equal("HolySiteValidation", _step.Name);
    }

    [Fact]
    public void Execute_AltarNotConsecrated_SetsFailureAndCompletes()
    {
        // Arrange
        var context = CreateContext();
        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(context.AltarPosition))
            .Returns((HolySiteData?)null);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.Equal("This altar is not consecrated. It must be part of a holy site.", context.Message);
        Assert.Null(context.HolySite);
    }

    [Fact]
    public void Execute_ValidHolySite_SetsHolySiteAndTierInfo()
    {
        // Arrange
        var context = CreateContext();
        var holySite = new HolySiteData
        {
            SiteUID = "site1",
            ReligionUID = "religion1",
            SiteName = "Test Site",
            RitualTier = 2 // Temple tier
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(context.AltarPosition))
            .Returns(holySite);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.IsComplete); // Should continue to next step
        Assert.Equal(holySite, context.HolySite);
        Assert.Equal(2, context.HolySiteTier);
        Assert.Equal(2.5, context.PrayerMultiplier); // Tier 2 multiplier
    }

    [Theory]
    [InlineData(1, 2.0)]
    [InlineData(2, 2.5)]
    [InlineData(3, 3.0)]
    public void Execute_SetsPrayerMultiplierBasedOnTier(int tier, double expectedMultiplier)
    {
        // Arrange
        var context = CreateContext();
        var holySite = new HolySiteData
        {
            SiteUID = "site1",
            ReligionUID = "religion1",
            SiteName = "Test Site",
            RitualTier = tier
        };

        _holySiteManager.Setup(x => x.GetHolySiteByAltarPosition(context.AltarPosition))
            .Returns(holySite);

        // Act
        _step.Execute(context);

        // Assert
        Assert.Equal(expectedMultiplier, context.PrayerMultiplier);
    }
}