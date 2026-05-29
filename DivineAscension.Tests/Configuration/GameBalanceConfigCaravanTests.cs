using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;

namespace DivineAscension.Tests.Configuration;

[ExcludeFromCodeCoverage]
public class GameBalanceConfigCaravanTests
{
    [Fact]
    public void Defaults_MatchTrackerHardcodedValues()
    {
        var config = new GameBalanceConfig();

        // Defaults must match the compiled-in tracker constants so legacy configs
        // (or no-config scenarios) behave identically to pre-knob behaviour.
        Assert.Equal(1.0f, config.CaravanTradeFavorMultiplier);
        Assert.Equal(1.0f, config.CaravanExplorationFavorMultiplier);
        Assert.Equal(1.0f, config.CaravanTradeTableFavorMultiplier);
        Assert.Equal(20, config.CaravanPerTradeFavorCap);
    }

    [Fact]
    public void Validate_AcceptsDefaults()
    {
        var ex = Record.Exception(() => new GameBalanceConfig().Validate());
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.5f)]
    public void Validate_RejectsNonPositiveTradeMultiplier(float value)
    {
        var config = new GameBalanceConfig { CaravanTradeFavorMultiplier = value };
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void Validate_RejectsNonPositiveExplorationMultiplier(float value)
    {
        var config = new GameBalanceConfig { CaravanExplorationFavorMultiplier = value };
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    public void Validate_RejectsOutOfRangePerTradeCap(int value)
    {
        var config = new GameBalanceConfig { CaravanPerTradeFavorCap = value };
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void Validate_AcceptsRebalancedValues()
    {
        var config = new GameBalanceConfig
        {
            CaravanTradeFavorMultiplier = 0.5f,
            CaravanExplorationFavorMultiplier = 2.0f,
            CaravanTradeTableFavorMultiplier = 1.5f,
            CaravanPerTradeFavorCap = 50
        };

        var ex = Record.Exception(() => config.Validate());
        Assert.Null(ex);
    }
}
