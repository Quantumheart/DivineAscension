using System.Diagnostics.CodeAnalysis;
using PantheonWars.Data;
using PantheonWars.Models.Enum;

namespace PantheonWars.Tests.Data;

[ExcludeFromCodeCoverage]
public class PlayerReligionDataTests
{
    [Fact]
    public void Constructor_Should_SetPlayerId()
    {
        var playerData = new PlayerReligionData("abc-1234acc");
        Assert.NotNull(playerData.PlayerUID);
    }

    [Fact]
    public void HasReligion_ShouldReturnFalse()
    {
        var playerData = new PlayerReligionData();
        Assert.False(playerData.HasReligion());
    }

    [Theory]
    [InlineData(0, FavorRank.Initiate)]
    [InlineData(500, FavorRank.Disciple)]
    [InlineData(2000, FavorRank.Zealot)]
    [InlineData(5000, FavorRank.Champion)]
    [InlineData(10000, FavorRank.Avatar)]
    public void AddFavor_ShouldUpgradeRankInThreshold(int favorAmount, FavorRank rank)
    {
        var playerData = new PlayerReligionData();
        playerData.AddFavor(favorAmount);
        Assert.Equal(favorAmount, playerData.Favor);
        Assert.Equal(favorAmount, playerData.TotalFavorEarned);
        Assert.Equal(playerData.FavorRank, rank);
    }

    [Fact]
    public void RemoveFavor_ShouldReturnTrueWhenEnoughFavor()
    {
        var playerData = new PlayerReligionData();
        playerData.Favor = 1000;
        var result = playerData.RemoveFavor(500);
        Assert.True(result);
        Assert.Equal(500, playerData.Favor);
    }

    [Fact]
    public void RemoveFavor_ShouldReturnFalseWhenNotEnoughFavor()
    {
        var playerData = new PlayerReligionData();
        playerData.Favor = 500;
        var result = playerData.RemoveFavor(1000);
        Assert.False(result);
        Assert.Equal(500, playerData.Favor);
    }

    [Fact]
    public void UnlockBlessing_ShouldSetBlessingToUnlocked()
    {
        var data = new PlayerReligionData();
        var blessingId = "Blessing123";
        data.UnlockBlessing(blessingId);
        Assert.True(data.UnlockedBlessings[blessingId]);
        Assert.Contains(blessingId, data.UnlockedBlessings.Keys);
    }

    // New tests for missing methods

    [Fact]
    public void UpdateFavorRank_ShouldSetCorrectRankBasedOnFavor()
    {
        var playerData = new PlayerReligionData();

        // Test Initiate (0-499)
        playerData.Favor = 0;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Initiate, playerData.FavorRank);

        // Test Disciple (500-1999)
        playerData.TotalFavorEarned = 500;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Disciple, playerData.FavorRank);

        // Test Zealot (2000-4999)
        playerData.TotalFavorEarned = 2000;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Zealot, playerData.FavorRank);

        // Test Champion (5000-9999)
        playerData.TotalFavorEarned = 5000;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Champion, playerData.FavorRank);

        // Test Avatar (10000+)
        playerData.TotalFavorEarned = 10000;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Avatar, playerData.FavorRank);

        // Test edge case: 499
        playerData.TotalFavorEarned = 499;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Initiate, playerData.FavorRank);

        // Test edge case: 5000
        playerData.TotalFavorEarned = 5000;
        playerData.UpdateFavorRank();
        Assert.Equal(FavorRank.Champion, playerData.FavorRank);
    }

    [Fact]
    public void ApplySwitchPenalty_ShouldResetFavorAndRank()
    {
        var playerData = new PlayerReligionData();
        playerData.Favor = 1000;
        playerData.FavorRank = FavorRank.Champion;
        playerData.UnlockedBlessings.Add("Blessing123", true);

        playerData.ApplySwitchPenalty();

        Assert.Equal(0, playerData.Favor);
        Assert.Equal(FavorRank.Initiate, playerData.FavorRank);
        Assert.Empty(playerData.UnlockedBlessings);
    }

    [Fact]
    public void IsBlessingUnlocked_ShouldReturnCorrectValue()
    {
        var playerData = new PlayerReligionData();
        var blessingId = "Blessing123";

        // Test when blessing is not unlocked
        Assert.False(playerData.IsBlessingUnlocked(blessingId));

        // Test when blessing is unlocked
        playerData.UnlockBlessing(blessingId);
        Assert.True(playerData.IsBlessingUnlocked(blessingId));

        // Test when blessing is not present
        Assert.False(playerData.IsBlessingUnlocked("Blessing456"));
    }
}