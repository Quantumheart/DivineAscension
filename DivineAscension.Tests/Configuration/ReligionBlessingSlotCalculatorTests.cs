using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Configuration;

[ExcludeFromCodeCoverage]
public class ReligionBlessingSlotCalculatorTests
{
    [Theory]
    [InlineData(PrestigeRank.Fledgling, 2)]
    [InlineData(PrestigeRank.Established, 3)]
    [InlineData(PrestigeRank.Renowned, 4)]
    [InlineData(PrestigeRank.Legendary, 5)]
    [InlineData(PrestigeRank.Mythic, 6)]
    public void GetMaxUnlocks_WithDefaults_ReturnsConfiguredSlots(PrestigeRank rank, int expectedSlots)
    {
        var config = new GameBalanceConfig();

        var slots = ReligionBlessingSlotCalculator.GetMaxUnlocks(config, rank);

        Assert.Equal(expectedSlots, slots);
    }

    [Fact]
    public void GetMaxUnlocks_HonoursOverriddenConfigValues()
    {
        var config = new GameBalanceConfig
        {
            RenownedReligionBlessingSlots = 9
        };

        var slots = ReligionBlessingSlotCalculator.GetMaxUnlocks(config, PrestigeRank.Renowned);

        Assert.Equal(9, slots);
    }

    [Fact]
    public void GetMaxUnlocks_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ReligionBlessingSlotCalculator.GetMaxUnlocks(null!, PrestigeRank.Fledgling));
    }

    [Fact]
    public void GetMaxUnlocks_UnknownRank_Throws()
    {
        var config = new GameBalanceConfig();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReligionBlessingSlotCalculator.GetMaxUnlocks(config, (PrestigeRank)999));
    }
}
