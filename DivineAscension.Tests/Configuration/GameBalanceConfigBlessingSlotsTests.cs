using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;

namespace DivineAscension.Tests.Configuration;

[ExcludeFromCodeCoverage]
public class GameBalanceConfigBlessingSlotsTests
{
    [Fact]
    public void Defaults_MatchSpec()
    {
        var config = new GameBalanceConfig();

        Assert.Equal(1, config.InitiateActiveBlessingSlots);
        Assert.Equal(2, config.DiscipleActiveBlessingSlots);
        Assert.Equal(3, config.ZealotActiveBlessingSlots);
        Assert.Equal(4, config.ChampionActiveBlessingSlots);
        Assert.Equal(5, config.AvatarActiveBlessingSlots);

        Assert.Equal(0, config.FledglingBonusSlots);
        Assert.Equal(0, config.EstablishedBonusSlots);
        Assert.Equal(1, config.RenownedBonusSlots);
        Assert.Equal(1, config.LegendaryBonusSlots);
        Assert.Equal(2, config.MythicBonusSlots);
    }

    [Fact]
    public void Validate_AcceptsDefaults()
    {
        var config = new GameBalanceConfig();

        var ex = Record.Exception(() => config.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void UnlearnDefaults_MatchSpec()
    {
        var config = new GameBalanceConfig();

        Assert.Equal(0.5f, config.UnlearnRefundPercent);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_RejectsOutOfRangeUnlearnRefundPercent(float percent)
    {
        var config = new GameBalanceConfig { UnlearnRefundPercent = percent };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Theory]
    [InlineData(nameof(GameBalanceConfig.InitiateActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.DiscipleActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.ZealotActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.ChampionActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.AvatarActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.FledglingBonusSlots))]
    [InlineData(nameof(GameBalanceConfig.EstablishedBonusSlots))]
    [InlineData(nameof(GameBalanceConfig.RenownedBonusSlots))]
    [InlineData(nameof(GameBalanceConfig.LegendaryBonusSlots))]
    [InlineData(nameof(GameBalanceConfig.MythicBonusSlots))]
    public void Validate_RejectsNegativeSlotValues(string propertyName)
    {
        var config = new GameBalanceConfig();
        typeof(GameBalanceConfig).GetProperty(propertyName)!.SetValue(config, -1);

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void Validate_RejectsTotalAboveHardCap()
    {
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 7,
            MythicBonusSlots = 2
        };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void Validate_AcceptsTotalAtHardCap()
    {
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 6,
            MythicBonusSlots = 2
        };

        var ex = Record.Exception(() => config.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void Validate_RejectsCrossPairAboveHardCap()
    {
        var config = new GameBalanceConfig
        {
            ChampionActiveBlessingSlots = 6,
            RenownedBonusSlots = 3
        };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void HardCapConstant_Is8()
    {
        Assert.Equal(8, GameBalanceConfig.MaxTotalBlessingSlots);
    }
}
