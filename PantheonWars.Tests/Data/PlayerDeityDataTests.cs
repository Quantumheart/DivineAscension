using System.Diagnostics.CodeAnalysis;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using Vintagestory.API.Util;

namespace PantheonWars.Tests.Data;

[ExcludeFromCodeCoverage]
public class PlayerDeityDataTests
{
    #region Existing Tests (Devotion Rank, Basic Operations)

    [Fact]
    public void Constructor_ShouldSetPlayerUID()
    {
        // Given: A player UID
        var playerUID = "test-player-123";

        // When: Create new PlayerDeityData
        var playerData = new PlayerDeityData(playerUID);

        // Then: PlayerUID should be set
        Assert.Equal(playerUID, playerData.PlayerUID);
        Assert.Equal(DeityType.None, playerData.DeityType);
        Assert.Equal(0, playerData.DivineFavor);
    }

    [Fact]
    public void HasDeity_ShouldReturnFalse_WhenNoDeity()
    {
        // Given: Player with no deity
        var playerData = new PlayerDeityData();

        // When: Check has deity
        var result = playerData.HasDeity();

        // Then: Should return false
        Assert.False(result);
    }

    [Fact]
    public void HasDeity_ShouldReturnTrue_WhenHasDeity()
    {
        // Given: Player with deity
        var playerData = new PlayerDeityData("test-player");
        playerData.DeityType = DeityType.Aethra;

        // When: Check has deity
        var result = playerData.HasDeity();

        // Then: Should return true
        Assert.True(result);
    }

    [Theory]
    [InlineData(0, DevotionRank.Initiate)]
    [InlineData(499, DevotionRank.Initiate)]
    [InlineData(500, DevotionRank.Disciple)]
    [InlineData(1999, DevotionRank.Disciple)]
    [InlineData(2000, DevotionRank.Zealot)]
    [InlineData(4999, DevotionRank.Zealot)]
    [InlineData(5000, DevotionRank.Champion)]
    [InlineData(9999, DevotionRank.Champion)]
    [InlineData(10000, DevotionRank.Avatar)]
    [InlineData(15000, DevotionRank.Avatar)]
    public void UpdateDevotionRank_ShouldSetCorrectRank_BasedOnTotalFavor(int totalFavor, DevotionRank expectedRank)
    {
        // Given: Player with specific total favor
        var playerData = new PlayerDeityData("test-player");
        playerData.TotalFavorEarned = totalFavor;

        // When: Update devotion rank
        playerData.UpdateDevotionRank();

        // Then: Rank should match expected
        Assert.Equal(expectedRank, playerData.DevotionRank);
    }

    [Fact]
    public void AddFavor_ShouldIncreaseFavor_AndUpdateRank()
    {
        // Given: New player
        var playerData = new PlayerDeityData("test-player");

        // When: Add 500 favor
        playerData.AddFavor(500);

        // Then: Favor updated and rank promoted
        Assert.Equal(500, playerData.DivineFavor);
        Assert.Equal(500, playerData.TotalFavorEarned);
        Assert.Equal(DevotionRank.Disciple, playerData.DevotionRank);
    }

    [Fact]
    public void RemoveFavor_ShouldDecreaseFavor_WhenSufficient()
    {
        // Given: Player with 100 favor
        var playerData = new PlayerDeityData("test-player");
        playerData.DivineFavor = 100;

        // When: Remove 50 favor
        var result = playerData.RemoveFavor(50);

        // Then: Should succeed
        Assert.True(result);
        Assert.Equal(50, playerData.DivineFavor);
    }

    [Fact]
    public void RemoveFavor_ShouldFail_WhenInsufficient()
    {
        // Given: Player with 50 favor
        var playerData = new PlayerDeityData("test-player");
        playerData.DivineFavor = 50;

        // When: Try to remove 100 favor
        var result = playerData.RemoveFavor(100);

        // Then: Should fail and favor unchanged
        Assert.False(result);
        Assert.Equal(50, playerData.DivineFavor);
    }

    #endregion

    #region Fractional Favor Tests (Phase 1)

    [Fact]
    public void AddFractionalFavor_ShouldAccumulate_WithoutAwardingUntilOne()
    {
        // Given: New player data
        var playerData = new PlayerDeityData("test-player");

        // When: Add fractional favor < 1.0
        playerData.AddFractionalFavor(0.04f);
        playerData.AddFractionalFavor(0.04f);
        playerData.AddFractionalFavor(0.04f);

        // Then: No favor awarded, fractional accumulator increased
        Assert.Equal(0, playerData.DivineFavor);
        Assert.Equal(0, playerData.TotalFavorEarned);
        Assert.Equal(0.12f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_ShouldAwardFavor_WhenReachingOne()
    {
        // Given: Player data with accumulated fractional favor
        var playerData = new PlayerDeityData("test-player");
        playerData.AccumulatedFractionalFavor = 0.96f;

        // When: Add fractional favor pushing total over 1.0
        playerData.AddFractionalFavor(0.08f); // Total: 1.04

        // Then: 1 favor awarded, remainder kept
        Assert.Equal(1, playerData.DivineFavor);
        Assert.Equal(1, playerData.TotalFavorEarned);
        Assert.Equal(0.04f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_ShouldAwardMultipleFavor_WhenLargeAmount()
    {
        // Given: New player data
        var playerData = new PlayerDeityData("test-player");

        // When: Add large fractional amount
        playerData.AddFractionalFavor(3.75f);

        // Then: Multiple favor awarded, remainder kept
        Assert.Equal(3, playerData.DivineFavor);
        Assert.Equal(3, playerData.TotalFavorEarned);
        Assert.Equal(0.75f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Theory]
    [InlineData(0.04f, 25, 1)]  // 25 ticks should award 1 favor
    [InlineData(0.08f, 13, 1)]  // 13 ticks should award 1 favor
    [InlineData(0.01f, 100, 1)] // 100 ticks should award 1 favor
    [InlineData(0.04f, 50, 2)]  // 50 ticks should award 2 favor
    public void AddFractionalFavor_ShouldPreservePrecision_OverMultipleTicks(float amount, int ticks, int expectedFavor)
    {
        // Given: New player data
        var playerData = new PlayerDeityData("test-player");

        // When: Add fractional favor multiple times
        for (int i = 0; i < ticks; i++)
        {
            playerData.AddFractionalFavor(amount);
        }

        // Then: Should award exactly the expected favor
        Assert.InRange(playerData.DivineFavor, 0,expectedFavor);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.5f)]
    public void AddFractionalFavor_ShouldIgnore_ZeroOrNegativeAmounts(float amount)
    {
        // Given: New player data with some accumulated favor
        var playerData = new PlayerDeityData("test-player");
        playerData.AccumulatedFractionalFavor = 0.5f;

        // When: Add zero or negative amount
        playerData.AddFractionalFavor(amount);

        // Then: No change
        Assert.Equal(0, playerData.DivineFavor);
        Assert.Equal(0.5f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_ShouldUpdateDevotionRank_WhenThresholdReached()
    {
        // Given: Player data near Disciple threshold (500 favor)
        var playerData = new PlayerDeityData("test-player");
        playerData.DivineFavor = 499;
        playerData.TotalFavorEarned = 499;
        playerData.DevotionRank = DevotionRank.Initiate;
        playerData.AccumulatedFractionalFavor = 0.9f;

        // When: Add fractional favor to reach threshold
        playerData.AddFractionalFavor(0.15f); // Total: 1.05, awards 1, new total = 500

        // Then: Rank upgraded
        Assert.Equal(500, playerData.DivineFavor);
        Assert.Equal(500, playerData.TotalFavorEarned);
        Assert.Equal(DevotionRank.Disciple, playerData.DevotionRank);
        Assert.Equal(0.05f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AccumulatedFractionalFavor_ShouldPersist_ThroughSerialization()
    {
        // Given: Player data with fractional favor
        var original = new PlayerDeityData("test-player");
        original.DeityType = DeityType.Aethra;
        original.DivineFavor = 100;
        original.TotalFavorEarned = 100;
        original.AddFractionalFavor(0.66f);

        // When: Serialize and deserialize (simulating save/load)
        var bytes = SerializerUtil.Serialize(original);
        var deserialized = SerializerUtil.Deserialize<PlayerDeityData>(bytes);

        // Then: All data preserved including fractional favor
        Assert.Equal("test-player", deserialized.PlayerUID);
        Assert.Equal(DeityType.Aethra, deserialized.DeityType);
        Assert.Equal(100, deserialized.DivineFavor);
        Assert.Equal(100, deserialized.TotalFavorEarned);
        Assert.Equal(0.66f, deserialized.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_ShouldHandleMultipleAwards_InSingleCall()
    {
        // Given: Player with 0.8 accumulated
        var playerData = new PlayerDeityData("test-player");
        playerData.AccumulatedFractionalFavor = 0.8f;

        // When: Add 2.5 favor (should award 3 total, keep 0.3)
        playerData.AddFractionalFavor(2.5f); // 0.8 + 2.5 = 3.3

        // Then: 3 favor awarded, 0.3 remainder
        Assert.Equal(3, playerData.DivineFavor);
        Assert.Equal(3, playerData.TotalFavorEarned);
        Assert.Equal(0.3f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    #endregion
}
