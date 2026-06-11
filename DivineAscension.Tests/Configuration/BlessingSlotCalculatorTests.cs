using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Configuration;

[ExcludeFromCodeCoverage]
public class BlessingSlotCalculatorTests
{
    [Theory]
    [InlineData(FavorRank.Initiate, PrestigeRank.Fledgling, 1)]
    [InlineData(FavorRank.Initiate, PrestigeRank.Established, 1)]
    [InlineData(FavorRank.Initiate, PrestigeRank.Renowned, 2)]
    [InlineData(FavorRank.Initiate, PrestigeRank.Legendary, 2)]
    [InlineData(FavorRank.Initiate, PrestigeRank.Mythic, 3)]
    [InlineData(FavorRank.Disciple, PrestigeRank.Fledgling, 2)]
    [InlineData(FavorRank.Disciple, PrestigeRank.Established, 2)]
    [InlineData(FavorRank.Disciple, PrestigeRank.Renowned, 3)]
    [InlineData(FavorRank.Disciple, PrestigeRank.Legendary, 3)]
    [InlineData(FavorRank.Disciple, PrestigeRank.Mythic, 4)]
    [InlineData(FavorRank.Zealot, PrestigeRank.Fledgling, 3)]
    [InlineData(FavorRank.Zealot, PrestigeRank.Established, 3)]
    [InlineData(FavorRank.Zealot, PrestigeRank.Renowned, 4)]
    [InlineData(FavorRank.Zealot, PrestigeRank.Legendary, 4)]
    [InlineData(FavorRank.Zealot, PrestigeRank.Mythic, 5)]
    [InlineData(FavorRank.Champion, PrestigeRank.Fledgling, 4)]
    [InlineData(FavorRank.Champion, PrestigeRank.Established, 4)]
    [InlineData(FavorRank.Champion, PrestigeRank.Renowned, 5)]
    [InlineData(FavorRank.Champion, PrestigeRank.Legendary, 5)]
    [InlineData(FavorRank.Champion, PrestigeRank.Mythic, 6)]
    [InlineData(FavorRank.Avatar, PrestigeRank.Fledgling, 5)]
    [InlineData(FavorRank.Avatar, PrestigeRank.Established, 5)]
    [InlineData(FavorRank.Avatar, PrestigeRank.Renowned, 6)]
    [InlineData(FavorRank.Avatar, PrestigeRank.Legendary, 6)]
    [InlineData(FavorRank.Avatar, PrestigeRank.Mythic, 7)]
    public void GetMaxUnlocks_WithDefaults_ReturnsExpectedMatrix(
        FavorRank favorRank, PrestigeRank prestigeRank, int expectedSlots)
    {
        var config = new GameBalanceConfig();

        var slots = BlessingSlotCalculator.GetMaxUnlocks(config, favorRank, prestigeRank);

        Assert.Equal(expectedSlots, slots);
    }

    [Theory]
    [InlineData(FavorRank.Initiate, 1)]
    [InlineData(FavorRank.Disciple, 2)]
    [InlineData(FavorRank.Zealot, 3)]
    [InlineData(FavorRank.Champion, 4)]
    [InlineData(FavorRank.Avatar, 5)]
    public void GetMaxUnlocks_WithNullPrestige_ReturnsFavorOnlyCount(
        FavorRank favorRank, int expectedSlots)
    {
        var config = new GameBalanceConfig();

        var slots = BlessingSlotCalculator.GetMaxUnlocks(config, favorRank, null);

        Assert.Equal(expectedSlots, slots);
    }

    [Fact]
    public void GetMaxUnlocks_HonoursOverriddenConfigValues()
    {
        var config = new GameBalanceConfig
        {
            ZealotActiveBlessingSlots = 2,
            RenownedBonusSlots = 3
        };

        var slots = BlessingSlotCalculator.GetMaxUnlocks(config, FavorRank.Zealot, PrestigeRank.Renowned);

        Assert.Equal(5, slots);
    }

    [Fact]
    public void GetMaxUnlocks_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BlessingSlotCalculator.GetMaxUnlocks(null!, FavorRank.Initiate, null));
    }

    [Fact]
    public void GetMaxUnlocks_ClampsTotalToConfiguredCap()
    {
        // favor 7 + bonus 4 = 11, but the cap binds at 8.
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 7,
            MythicBonusSlots = 4,
            MaxTotalActiveBlessingSlots = 8
        };

        var slots = BlessingSlotCalculator.GetMaxUnlocks(config, FavorRank.Avatar, PrestigeRank.Mythic);

        Assert.Equal(8, slots);
    }

    [Fact]
    public void GetMaxUnlocks_HonoursRaisedCap()
    {
        // Raising the cap lets the same dials yield more slots — the wall moves with the config.
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 7,
            MythicBonusSlots = 4,
            MaxTotalActiveBlessingSlots = 16
        };

        var slots = BlessingSlotCalculator.GetMaxUnlocks(config, FavorRank.Avatar, PrestigeRank.Mythic);

        Assert.Equal(11, slots);
    }

    [Fact]
    public void GetMaxUnlocks_ClampsFavorOnlyTotalToCap()
    {
        var config = new GameBalanceConfig
        {
            AvatarActiveBlessingSlots = 20,
            MaxTotalActiveBlessingSlots = 8
        };

        var slots = BlessingSlotCalculator.GetMaxUnlocks(config, FavorRank.Avatar, null);

        Assert.Equal(8, slots);
    }
}
