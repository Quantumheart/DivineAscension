using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;

namespace DivineAscension.Tests.Systems;

[ExcludeFromCodeCoverage]
public class KhorasBlessingTests
{
    private static IQueryable<Blessing> GetKhoras()
        => BlessingDefinitions.GetAllBlessings().Where(b => b.Domain == DeityDomain.Craft).AsQueryable();

    [Fact]
    public void Sanity_Khoras_Has10Blessings_AndUniqueIds()
    {
        var blessings = GetKhoras().ToList();
        Assert.Equal(10, blessings.Count);

        var distinctIds = blessings.Select(b => b.BlessingId).Distinct().Count();
        Assert.Equal(10, distinctIds);
    }

    [Fact]
    public void T1_CraftsmansTouch_AppliesToolDurability_And_OreYield()
    {
        var t1 = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasCraftsmansTouch);

        Assert.True(t1.StatModifiers.TryGetValue(VintageStoryStats.ToolDurability, out var toolDur),
            "Craftsman's Touch must define ToolDurability modifier");
        Assert.Equal(0.10f, toolDur, 3);

        // Implementation uses OreDropRate as the ore yield stat code
        Assert.True(t1.StatModifiers.TryGetValue(VintageStoryStats.OreDropRate, out var oreYield),
            "Craftsman's Touch must define OreDropRate (ore yield) modifier");
        Assert.Equal(0.10f, oreYield, 3);
    }

    [Fact]
    public void T2A_MasterworkTools_StacksAdditivelyWith_T1_ForToolDurability()
    {
        var blessings = GetKhoras().ToList();
        var t1 = blessings.First(b => b.BlessingId == BlessingIds.KhorasCraftsmansTouch);
        var t2a = blessings.First(b => b.BlessingId == BlessingIds.KhorasMasterworkTools);
        var t3a = blessings.First(b => b.BlessingId == BlessingIds.KhorasLegendarySmith);

        float GetOrZero(Blessing b, string stat)
            => b.StatModifiers.TryGetValue(stat, out var v) ? v : 0f;

        var totalToolDur = GetOrZero(t1, VintageStoryStats.ToolDurability)
                           + GetOrZero(t2a, VintageStoryStats.ToolDurability)
                           + GetOrZero(t3a, VintageStoryStats.ToolDurability);

        // Expect 0.10 + 0.15 + 0.20 = 0.45 (i.e., +45% total)
        Assert.Equal(0.45f, totalToolDur, 3);
    }

    [Fact]
    public void T2B_ForgebornEndurance_AppliesMeleeDamage_And_MaxHealth()
    {
        var t2b = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasForgebornEndurance);

        Assert.True(t2b.StatModifiers.TryGetValue(VintageStoryStats.MeleeWeaponsDamage, out var melee),
            "Forgeborn Endurance must define MeleeWeaponsDamage modifier");
        Assert.Equal(0.10f, melee, 3);

        // MaxHealth is represented as an extra points multiplier (1.10f means +10%) in current implementation
        Assert.True(t2b.StatModifiers.TryGetValue(VintageStoryStats.MaxHealthExtraPoints, out var hp),
            "Forgeborn Endurance must define MaxHealthExtraPoints modifier");
        Assert.Equal(1.10f, hp, 3);
    }

    [Fact]
    public void T3A_LegendarySmith_Requires_T2A_MasterworkTools()
    {
        var t3a = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasLegendarySmith);
        Assert.Contains(BlessingIds.KhorasMasterworkTools, t3a.PrerequisiteBlessings!);
    }

    [Fact]
    public void T3B_Unyielding_Requires_T2B_ForgebornEndurance()
    {
        var t3b = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasUnyielding);
        Assert.Contains(BlessingIds.KhorasForgebornEndurance, t3b.PrerequisiteBlessings!);
    }

    [Fact]
    public void T4_AvatarOfTheForge_Requires_Both_T3_Paths()
    {
        var t4 = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasAvatarOfForge);
        Assert.Contains(BlessingIds.KhorasLegendarySmith, t4.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.KhorasUnyielding, t4.PrerequisiteBlessings!);
    }

    [Fact]
    public void ReligionBlessings_AllMarked_AsReligionKind()
    {
        var ids = new[]
        {
            BlessingIds.KhorasSharedWorkshop,
            BlessingIds.KhorasGuildOfSmiths,
            BlessingIds.KhorasMasterCraftsmen,
            BlessingIds.KhorasPantheonOfCreation
        };

        var blessings = GetKhoras().Where(b => ids.Contains(b.BlessingId)).ToList();
        Assert.Equal(4, blessings.Count);
        Assert.All(blessings, b => Assert.Equal(BlessingKind.Religion, b.Kind));
    }

    [Fact]
    public void SpecificStats_Match_Implementation()
    {
        var blessings = GetKhoras().ToList();

        var unyielding = blessings.First(b => b.BlessingId == BlessingIds.KhorasUnyielding);
        Assert.Equal(-0.10f, unyielding.StatModifiers[VintageStoryStats.ArmorDurabilityLoss], 3);
        Assert.Equal(1.15f, unyielding.StatModifiers[VintageStoryStats.MaxHealthExtraPoints], 3);

        var avatar = blessings.First(b => b.BlessingId == BlessingIds.KhorasAvatarOfForge);
        Assert.Equal(-0.10f, avatar.StatModifiers[VintageStoryStats.ArmorWalkSpeedAffectedness], 3);

        var masterCraftsmen = blessings.First(b => b.BlessingId == BlessingIds.KhorasMasterCraftsmen);
        Assert.Equal(-0.10f, masterCraftsmen.StatModifiers[VintageStoryStats.ArmorWalkSpeedAffectedness], 3);
    }

    [Fact]
    public void ReligionChain_Prerequisites_AreLinear()
    {
        var shared = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasSharedWorkshop);
        var guild = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasGuildOfSmiths);
        var master = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasMasterCraftsmen);
        var pantheon = GetKhoras().First(b => b.BlessingId == BlessingIds.KhorasPantheonOfCreation);

        Assert.True(shared.PrerequisiteBlessings == null || shared.PrerequisiteBlessings.Count == 0);
        Assert.Contains(BlessingIds.KhorasSharedWorkshop, guild.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.KhorasGuildOfSmiths, master.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.KhorasMasterCraftsmen, pantheon.PrerequisiteBlessings!);
    }
}