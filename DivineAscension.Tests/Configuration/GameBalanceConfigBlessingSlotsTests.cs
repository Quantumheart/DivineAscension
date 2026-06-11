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
    [InlineData(nameof(GameBalanceConfig.FledglingReligionBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.MythicReligionBlessingSlots))]
    public void Validate_AcceptsNegativeSlotValues_WithoutThrowing(string propertyName)
    {
        // A negative slot field must NOT cause Validate() to throw — that would reset the whole
        // config to defaults (#616). Repair is the job of ClampBlessingSlots instead.
        var config = new GameBalanceConfig();
        typeof(GameBalanceConfig).GetProperty(propertyName)!.SetValue(config, -1);

        var ex = Record.Exception(() => config.Validate());

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(nameof(GameBalanceConfig.InitiateActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.AvatarActiveBlessingSlots))]
    [InlineData(nameof(GameBalanceConfig.MythicBonusSlots))]
    [InlineData(nameof(GameBalanceConfig.MythicReligionBlessingSlots))]
    public void ClampBlessingSlots_ClampsNegativeToZero_AndReports(string propertyName)
    {
        var config = new GameBalanceConfig();
        var property = typeof(GameBalanceConfig).GetProperty(propertyName)!;
        property.SetValue(config, -5);

        var adjustments = config.ClampBlessingSlots();

        Assert.Equal(0, (int)property.GetValue(config)!);
        Assert.Contains(adjustments, m => m.Contains(propertyName));
    }

    [Fact]
    public void ClampBlessingSlots_LeavesValidConfigUntouched()
    {
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 7,
            MythicBonusSlots = 2
        };

        var adjustments = config.ClampBlessingSlots();

        Assert.Empty(adjustments);
        // High-but-non-negative values survive — the cap is enforced at runtime, not by reset.
        Assert.Equal(7, config.AvatarActiveBlessingSlots);
        Assert.Equal(2, config.MythicBonusSlots);
    }

    [Theory]
    [InlineData(0, GameBalanceConfig.MinAllowedMaxTotalActiveBlessingSlots)]
    [InlineData(-3, GameBalanceConfig.MinAllowedMaxTotalActiveBlessingSlots)]
    [InlineData(9999, GameBalanceConfig.MaxAllowedMaxTotalActiveBlessingSlots)]
    public void ClampBlessingSlots_ClampsCapToAllowedRange(int input, int expected)
    {
        var config = new GameBalanceConfig { MaxTotalActiveBlessingSlots = input };

        var adjustments = config.ClampBlessingSlots();

        Assert.Equal(expected, config.MaxTotalActiveBlessingSlots);
        Assert.Contains(adjustments, m => m.Contains(nameof(GameBalanceConfig.MaxTotalActiveBlessingSlots)));
    }

    [Fact]
    public void Validate_AcceptsTotalAboveDefaultCap_WithoutThrowing()
    {
        // Previously this reset the entire config; now an over-cap total is tolerated and clamped
        // at runtime instead of rejected at load (#616).
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 7,
            MythicBonusSlots = 2
        };

        var ex = Record.Exception(() => config.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void MaxTotalActiveBlessingSlots_DefaultsTo8()
    {
        Assert.Equal(8, new GameBalanceConfig().MaxTotalActiveBlessingSlots);
    }

    [Fact]
    public void MaxTotalActiveBlessingSlots_IsConfigurable()
    {
        var config = new GameBalanceConfig { MaxTotalActiveBlessingSlots = 12 };

        var adjustments = config.ClampBlessingSlots();

        Assert.Empty(adjustments);
        Assert.Equal(12, config.MaxTotalActiveBlessingSlots);
    }
}
