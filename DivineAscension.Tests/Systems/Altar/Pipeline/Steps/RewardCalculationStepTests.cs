using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Altar.Pipeline;
using DivineAscension.Systems.Altar.Pipeline.Steps;
using DivineAscension.Tests.Helpers;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems.Altar.Pipeline.Steps;

public class RewardCalculationStepTests
{
    private readonly GameBalanceConfig _config;
    private readonly RewardCalculationStep _step;

    public RewardCalculationStepTests()
    {
        TestFixtures.InitializeLocalizationForTests();
        _config = new GameBalanceConfig();
        _step = new RewardCalculationStep(_config);
    }

    private static PrayerContext CreateContext(int tier = 1, double prayerMultiplier = 2.0, int offeringBonus = 0) =>
        new()
        {
            PlayerUID = "player1",
            PlayerName = "TestPlayer",
            AltarPosition = new BlockPos(100, 50, 100),
            Offering = null,
            CurrentTime = 0,
            Player = null!,
            HolySiteTier = tier,
            PrayerMultiplier = prayerMultiplier,
            OfferingBonus = offeringBonus,
            Religion = new ReligionData
            {
                ReligionUID = "religion1",
                ReligionName = "Test",
                DeityName = "Deity",
                Domain = DeityDomain.Craft,
                FounderUID = "founder1",
                FounderName = "Founder"
            }
        };

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        Assert.Equal("RewardCalculation", _step.Name);
    }

    [Fact]
    public void Execute_BasePrayerNoOffering_CalculatesCorrectRewards()
    {
        // Arrange - Tier 1, no offering
        var context = CreateContext(tier: 1, prayerMultiplier: 2.0);

        // Act
        _step.Execute(context);

        // Assert
        Assert.True(context.Success);
        // BASE (5) * tier 1 multiplier (2.0) = 10
        Assert.Equal(10, context.FavorAwarded);
        Assert.Equal(10, context.PrestigeAwarded);
        Assert.Equal(_config.HolySiteTier1Multiplier, context.BuffMultiplier);
    }

    [Fact]
    public void Execute_WithOfferingBonus_AddsToRewards()
    {
        // Arrange - Tier 1, with offering bonus
        var context = CreateContext(tier: 1, prayerMultiplier: 2.0, offeringBonus: 20);

        // Act
        _step.Execute(context);

        // Assert
        // BASE (5) + offering (20) = 25, * tier 1 multiplier (2.0) = 50
        Assert.Equal(50, context.FavorAwarded);
        Assert.Equal(50, context.PrestigeAwarded);
    }

    [Theory]
    [InlineData(1, 1.25f)]
    [InlineData(2, 1.5f)]
    [InlineData(3, 1.75f)]
    public void Execute_SetsBuffMultiplierByTier(int tier, float expectedBuffMultiplier)
    {
        // Arrange
        var context = CreateContext(tier: tier);

        // Act
        _step.Execute(context);

        // Assert
        Assert.Equal(expectedBuffMultiplier, context.BuffMultiplier);
    }

    [Fact]
    public void Execute_Tier2_CalculatesCorrectRewards()
    {
        // Arrange
        var context = CreateContext(tier: 2, prayerMultiplier: 2.5);

        // Act
        _step.Execute(context);

        // Assert
        // BASE (5) * tier 2 multiplier (2.5) = 12.5, rounds to 12
        Assert.Equal(12, context.FavorAwarded);
        Assert.Equal(_config.HolySiteTier2Multiplier, context.BuffMultiplier);
    }

    [Fact]
    public void Execute_Tier3_CalculatesCorrectRewards()
    {
        // Arrange
        var context = CreateContext(tier: 3, prayerMultiplier: 3.0);

        // Act
        _step.Execute(context);

        // Assert
        // BASE (5) * tier 3 multiplier (3.0) = 15
        Assert.Equal(15, context.FavorAwarded);
        Assert.Equal(_config.HolySiteTier3Multiplier, context.BuffMultiplier);
    }

    [Fact]
    public void Execute_InvalidTier_UsesDefaultMultiplier()
    {
        // Arrange - tier 0 (edge case)
        var context = CreateContext(tier: 0, prayerMultiplier: 1.0);

        // Act
        _step.Execute(context);

        // Assert
        Assert.Equal(1.0f, context.BuffMultiplier);
    }

    [Fact]
    public void Execute_SetsSuccessMessageWithOffering()
    {
        // Arrange
        var context = CreateContext(tier: 1, prayerMultiplier: 2.0, offeringBonus: 10);

        // Act
        _step.Execute(context);

        // Assert
        Assert.True(context.Success);
        Assert.NotNull(context.Message);
        Assert.Contains("offering", context.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_OfferingRejectedDomain_SetsRejectionMessage()
    {
        // Arrange
        var context = CreateContext();
        context.OfferingRejectedDomain = true;

        // Act
        _step.Execute(context);

        // Assert
        Assert.NotNull(context.Message);
        // Message should mention rejection
    }
}