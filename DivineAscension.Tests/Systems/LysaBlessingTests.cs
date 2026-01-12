using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;

namespace DivineAscension.Tests.Systems;

/// <summary>
/// Tests for Lysa (Hunt & Wild) blessing definitions
/// Verifies all 10 blessings (6 player + 4 religion) have correct:
/// - Stat modifiers
/// - Prerequisites
/// - Favor/Prestige rank requirements
/// - Special effects
/// </summary>
[ExcludeFromCodeCoverage]
public class LysaBlessingTests
{
    private static IEnumerable<Blessing> GetLysa() =>
        BlessingDefinitions.GetAllBlessings().Where(b => b.Domain == DeityDomain.Wild);

    #region Tier 1 Tests

    [Fact]
    public void T1_HuntersInstinct_AppliesCorrectModifiers()
    {
        var t1 = GetLysa().First(b => b.BlessingId == BlessingIds.WildHuntersInstinct);

        Assert.Equal(0, t1.RequiredFavorRank); // Initiate
        Assert.Equal(BlessingKind.Player, t1.Kind);
        Assert.Equal(DeityDomain.Wild, t1.Domain);

        // Verify stat modifiers
        Assert.Equal(0.15f, t1.StatModifiers[VintageStoryStats.AnimalDrops], 3);
        Assert.Equal(0.15f, t1.StatModifiers[VintageStoryStats.ForageDropRate], 3);
        Assert.Equal(0.05f, t1.StatModifiers[VintageStoryStats.WalkSpeed], 3);

        // T1 should have no prerequisites
        Assert.True(t1.PrerequisiteBlessings == null || t1.PrerequisiteBlessings.Count == 0);
    }

    #endregion

    #region Tier 2 Tests

    [Fact]
    public void T2A_MasterForager_AppliesCorrectModifiers()
    {
        var t2a = GetLysa().First(b => b.BlessingId == BlessingIds.WildMasterForager);

        Assert.Equal(1, t2a.RequiredFavorRank); // Disciple
        Assert.Equal(BlessingKind.Player, t2a.Kind);

        // Verify stat modifiers
        Assert.Equal(0.20f, t2a.StatModifiers[VintageStoryStats.ForageDropRate], 3);
        Assert.Equal(0.20f, t2a.StatModifiers[VintageStoryStats.WildCropYield], 3);
        Assert.Equal(0.15f, t2a.StatModifiers[VintageStoryStats.FoodSpoilage], 3);

        // Verify requires T1
        Assert.NotNull(t2a.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildHuntersInstinct, t2a.PrerequisiteBlessings);

        // Verify special effect
        Assert.Contains(SpecialEffects.FoodSpoilageReduction, t2a.SpecialEffects);
    }

    [Fact]
    public void T2B_ApexPredator_AppliesCorrectModifiers()
    {
        var t2b = GetLysa().First(b => b.BlessingId == BlessingIds.WildApexPredator);

        Assert.Equal(1, t2b.RequiredFavorRank); // Disciple
        Assert.Equal(BlessingKind.Player, t2b.Kind);

        // Verify stat modifiers
        Assert.Equal(0.20f, t2b.StatModifiers[VintageStoryStats.AnimalDrops], 3);
        Assert.Equal(0.10f, t2b.StatModifiers[VintageStoryStats.AnimalHarvestTime], 3);

        // Verify requires T1
        Assert.NotNull(t2b.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildHuntersInstinct, t2b.PrerequisiteBlessings);
    }

    [Fact]
    public void T2_BothPaths_RequireT1()
    {
        var t2a = GetLysa().First(b => b.BlessingId == BlessingIds.WildMasterForager);
        var t2b = GetLysa().First(b => b.BlessingId == BlessingIds.WildApexPredator);

        Assert.Contains(BlessingIds.WildHuntersInstinct, t2a.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.WildHuntersInstinct, t2b.PrerequisiteBlessings!);
    }

    #endregion

    #region Tier 3 Tests

    [Fact]
    public void T3A_AbundanceOfWild_AppliesCorrectModifiers()
    {
        var t3a = GetLysa().First(b => b.BlessingId == BlessingIds.WildAbundanceOfWild);

        Assert.Equal(2, t3a.RequiredFavorRank); // Zealot
        Assert.Equal(BlessingKind.Player, t3a.Kind);

        // Verify stat modifiers
        Assert.Equal(0.25f, t3a.StatModifiers[VintageStoryStats.ForageDropRate], 3);
        Assert.Equal(0.25f, t3a.StatModifiers[VintageStoryStats.FoodSpoilage], 3);

        // Verify requires T2A
        Assert.NotNull(t3a.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildMasterForager, t3a.PrerequisiteBlessings);

        // Verify special effect
        Assert.Contains(SpecialEffects.FoodSpoilageReduction, t3a.SpecialEffects);
    }

    [Fact]
    public void T3B_SilentDeath_AppliesCorrectModifiers()
    {
        var t3b = GetLysa().First(b => b.BlessingId == BlessingIds.WildSilentDeath);

        Assert.Equal(2, t3b.RequiredFavorRank); // Zealot
        Assert.Equal(BlessingKind.Player, t3b.Kind);

        // Verify stat modifiers
        Assert.Equal(0.15f, t3b.StatModifiers[VintageStoryStats.RangedWeaponsAccuracy], 3);
        Assert.Equal(0.15f, t3b.StatModifiers[VintageStoryStats.RangedWeaponsDamage], 3);

        // Verify requires T2B
        Assert.NotNull(t3b.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildApexPredator, t3b.PrerequisiteBlessings);
    }

    [Fact]
    public void T3_BothPaths_FormCorrectChain()
    {
        var t3a = GetLysa().First(b => b.BlessingId == BlessingIds.WildAbundanceOfWild);
        var t3b = GetLysa().First(b => b.BlessingId == BlessingIds.WildSilentDeath);

        // T3A requires T2A (Foraging path)
        Assert.Contains(BlessingIds.WildMasterForager, t3a.PrerequisiteBlessings!);

        // T3B requires T2B (Combat path)
        Assert.Contains(BlessingIds.WildApexPredator, t3b.PrerequisiteBlessings!);
    }

    #endregion

    #region Tier 4 (Capstone) Tests

    [Fact]
    public void T4_AvatarOfWild_RequiresBothT3Paths()
    {
        var t4 = GetLysa().First(b => b.BlessingId == BlessingIds.WildAvatarOfWild);

        Assert.Equal(3, t4.RequiredFavorRank); // Champion
        Assert.Equal(BlessingKind.Player, t4.Kind);

        // Capstone requires BOTH T3 paths
        Assert.NotNull(t4.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildAbundanceOfWild, t4.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildSilentDeath, t4.PrerequisiteBlessings);
        Assert.Equal(2, t4.PrerequisiteBlessings.Count);
    }

    [Fact]
    public void T4_AvatarOfWild_AppliesCorrectModifiers()
    {
        var t4 = GetLysa().First(b => b.BlessingId == BlessingIds.WildAvatarOfWild);

        // Verify stat modifiers
        Assert.Equal(0.20f, t4.StatModifiers[VintageStoryStats.RangedWeaponsRange], 3);
        Assert.Equal(0.20f, t4.StatModifiers[VintageStoryStats.AnimalSeekingRange], 3);
    }

    #endregion

    #region Religion Blessing Tests

    [Fact]
    public void ReligionBlessings_AllMarkedAsReligionKind()
    {
        var ids = new[]
        {
            BlessingIds.WildHuntingParty,
            BlessingIds.WildWildernessTribe,
            BlessingIds.WildChildrenOfForest,
            BlessingIds.WildPantheonOfHunt
        };

        var religionBlessings = GetLysa().Where(b => ids.Contains(b.BlessingId)).ToList();

        Assert.Equal(4, religionBlessings.Count);
        Assert.All(religionBlessings, b => Assert.Equal(BlessingKind.Religion, b.Kind));
    }

    [Fact]
    public void R1_HuntingParty_AppliesCorrectModifiers()
    {
        var r1 = GetLysa().First(b => b.BlessingId == BlessingIds.WildHuntingParty);

        Assert.Equal(0, r1.RequiredPrestigeRank); // Fledgling
        Assert.Equal(BlessingKind.Religion, r1.Kind);

        // Verify stat modifiers
        Assert.Equal(0.15f, r1.StatModifiers[VintageStoryStats.AnimalDrops], 3);
        Assert.Equal(0.15f, r1.StatModifiers[VintageStoryStats.ForageDropRate], 3);

        // R1 should have no prerequisites
        Assert.True(r1.PrerequisiteBlessings == null || r1.PrerequisiteBlessings.Count == 0);
    }

    [Fact]
    public void R2_WildernessTribe_AppliesCorrectModifiers()
    {
        var r2 = GetLysa().First(b => b.BlessingId == BlessingIds.WildWildernessTribe);

        Assert.Equal(1, r2.RequiredPrestigeRank); // Established
        Assert.Equal(BlessingKind.Religion, r2.Kind);

        // Verify stat modifiers
        Assert.Equal(0.20f, r2.StatModifiers[VintageStoryStats.AnimalDrops], 3);
        Assert.Equal(0.20f, r2.StatModifiers[VintageStoryStats.ForageDropRate], 3);
        Assert.Equal(0.15f, r2.StatModifiers[VintageStoryStats.FoodSpoilage], 3);

        // Verify requires R1
        Assert.NotNull(r2.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildHuntingParty, r2.PrerequisiteBlessings);
    }

    [Fact]
    public void R3_ChildrenOfForest_AppliesCorrectModifiers()
    {
        var r3 = GetLysa().First(b => b.BlessingId == BlessingIds.WildChildrenOfForest);

        Assert.Equal(2, r3.RequiredPrestigeRank); // Renowned
        Assert.Equal(BlessingKind.Religion, r3.Kind);

        // Verify stat modifiers
        Assert.Equal(0.25f, r3.StatModifiers[VintageStoryStats.AnimalDrops], 3);
        Assert.Equal(0.25f, r3.StatModifiers[VintageStoryStats.ForageDropRate], 3);
        Assert.Equal(0.05f, r3.StatModifiers[VintageStoryStats.WalkSpeed], 3);

        // Verify requires R2
        Assert.NotNull(r3.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildWildernessTribe, r3.PrerequisiteBlessings);
    }

    [Fact]
    public void R4_PantheonOfHunt_AppliesCorrectModifiers()
    {
        var r4 = GetLysa().First(b => b.BlessingId == BlessingIds.WildPantheonOfHunt);

        Assert.Equal(3, r4.RequiredPrestigeRank); // Legendary
        Assert.Equal(BlessingKind.Religion, r4.Kind);

        // Verify stat modifiers
        Assert.Equal(5.0f, r4.StatModifiers[VintageStoryStats.TemperatureResistance], 3);

        // Verify requires R3
        Assert.NotNull(r4.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildChildrenOfForest, r4.PrerequisiteBlessings);
    }

    [Fact]
    public void ReligionChain_Prerequisites_AreLinear()
    {
        var r1 = GetLysa().First(b => b.BlessingId == BlessingIds.WildHuntingParty);
        var r2 = GetLysa().First(b => b.BlessingId == BlessingIds.WildWildernessTribe);
        var r3 = GetLysa().First(b => b.BlessingId == BlessingIds.WildChildrenOfForest);
        var r4 = GetLysa().First(b => b.BlessingId == BlessingIds.WildPantheonOfHunt);

        // R1 has no prerequisites
        Assert.True(r1.PrerequisiteBlessings == null || r1.PrerequisiteBlessings.Count == 0);

        // R2 requires R1
        Assert.NotNull(r2.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildHuntingParty, r2.PrerequisiteBlessings);

        // R3 requires R2
        Assert.NotNull(r3.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildWildernessTribe, r3.PrerequisiteBlessings);

        // R4 requires R3
        Assert.NotNull(r4.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.WildChildrenOfForest, r4.PrerequisiteBlessings);
    }

    #endregion

    #region Stat Stacking Tests

    [Fact]
    public void ForageDrops_T1ThroughT3A_StackAdditively()
    {
        var t1 = GetLysa().First(b => b.BlessingId == BlessingIds.WildHuntersInstinct);
        var t2a = GetLysa().First(b => b.BlessingId == BlessingIds.WildMasterForager);
        var t3a = GetLysa().First(b => b.BlessingId == BlessingIds.WildAbundanceOfWild);

        // Calculate total forage drop bonus: T1 (15%) + T2A (20%) + T3A (25%) = 60%
        float totalForageDrop = 0f;
        if (t1.StatModifiers.TryGetValue(VintageStoryStats.ForageDropRate, out var t1Val))
            totalForageDrop += t1Val;
        if (t2a.StatModifiers.TryGetValue(VintageStoryStats.ForageDropRate, out var t2aVal))
            totalForageDrop += t2aVal;
        if (t3a.StatModifiers.TryGetValue(VintageStoryStats.ForageDropRate, out var t3aVal))
            totalForageDrop += t3aVal;

        Assert.Equal(0.60f, totalForageDrop, precision: 3);
    }

    [Fact]
    public void AnimalDrops_T1ThroughT2B_StackAdditively()
    {
        var t1 = GetLysa().First(b => b.BlessingId == BlessingIds.WildHuntersInstinct);
        var t2b = GetLysa().First(b => b.BlessingId == BlessingIds.WildApexPredator);

        // Calculate total animal drop bonus: T1 (15%) + T2B (20%) = 35%
        float totalAnimalDrop = 0f;
        if (t1.StatModifiers.TryGetValue(VintageStoryStats.AnimalDrops, out var t1Val))
            totalAnimalDrop += t1Val;
        if (t2b.StatModifiers.TryGetValue(VintageStoryStats.AnimalDrops, out var t2bVal))
            totalAnimalDrop += t2bVal;

        Assert.Equal(0.35f, totalAnimalDrop, precision: 3);
    }

    [Fact]
    public void FoodSpoilage_T2AThroughT3A_StackAdditively()
    {
        var t2a = GetLysa().First(b => b.BlessingId == BlessingIds.WildMasterForager);
        var t3a = GetLysa().First(b => b.BlessingId == BlessingIds.WildAbundanceOfWild);

        // Calculate total food spoilage reduction: T2A (15%) + T3A (25%) = 40%
        float totalSpoilage = 0f;
        if (t2a.StatModifiers.TryGetValue(VintageStoryStats.FoodSpoilage, out var t2aVal))
            totalSpoilage += t2aVal;
        if (t3a.StatModifiers.TryGetValue(VintageStoryStats.FoodSpoilage, out var t3aVal))
            totalSpoilage += t3aVal;

        Assert.Equal(0.40f, totalSpoilage, precision: 3);
    }

    #endregion

    #region Count and Completeness Tests

    [Fact]
    public void AllLysaBlessings_HaveCorrectDeity()
    {
        var lysaBlessings = GetLysa().ToList();

        // Should have exactly 10 blessings (6 player + 4 religion)
        Assert.Equal(10, lysaBlessings.Count);

        // All should be Lysa deity
        Assert.All(lysaBlessings, b => Assert.Equal(DeityDomain.Wild, b.Domain));
    }

    [Fact]
    public void LysaBlessings_PlayerVsReligion_CorrectCounts()
    {
        var lysaBlessings = GetLysa().ToList();

        // Should have 6 player blessings and 4 religion blessings
        var playerBlessings = lysaBlessings.Where(b => b.Kind == BlessingKind.Player).ToList();
        var religionBlessings = lysaBlessings.Where(b => b.Kind == BlessingKind.Religion).ToList();

        Assert.Equal(6, playerBlessings.Count);
        Assert.Equal(4, religionBlessings.Count);
    }

    [Fact]
    public void AllLysaBlessings_HaveStatModifiers()
    {
        var lysaBlessings = GetLysa().ToList();

        // All Lysa blessings should have at least one stat modifier
        Assert.All(lysaBlessings, b => Assert.NotEmpty(b.StatModifiers));
    }

    [Fact]
    public void AllLysaBlessings_HaveUniqueIds()
    {
        var lysaBlessings = GetLysa().ToList();

        // All blessing IDs should be unique
        var blessingIds = lysaBlessings.Select(b => b.BlessingId).ToList();
        var distinctIds = blessingIds.Distinct().ToList();

        Assert.Equal(blessingIds.Count, distinctIds.Count);
    }

    #endregion
}