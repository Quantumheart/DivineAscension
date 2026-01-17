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
        var t1 = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftCraftsmansTouch);

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
        var t1 = blessings.First(b => b.BlessingId == BlessingIds.CraftCraftsmansTouch);
        var t2a = blessings.First(b => b.BlessingId == BlessingIds.CraftMasterworkTools);
        var t3a = blessings.First(b => b.BlessingId == BlessingIds.CraftLegendarySmith);

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
        var t2b = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftForgebornEndurance);

        Assert.True(t2b.StatModifiers.TryGetValue(VintageStoryStats.MeleeWeaponsDamage, out var melee),
            "Forgeborn Endurance must define MeleeWeaponsDamage modifier");
        Assert.Equal(0.10f, melee, 3);

        // MaxHealth uses percentage-based multiplier stat (0.10f means +10%)
        Assert.True(t2b.StatModifiers.TryGetValue(VintageStoryStats.MaxHealthExtraMultiplier, out var hp),
            "Forgeborn Endurance must define MaxHealthExtraMultiplier modifier");
        Assert.Equal(0.10f, hp, 3);
    }

    [Fact]
    public void T3A_LegendarySmith_Requires_T2A_MasterworkTools()
    {
        var t3a = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftLegendarySmith);
        Assert.Contains(BlessingIds.CraftMasterworkTools, t3a.PrerequisiteBlessings!);
    }

    [Fact]
    public void T3B_Unyielding_Requires_T2B_ForgebornEndurance()
    {
        var t3b = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftUnyielding);
        Assert.Contains(BlessingIds.CraftForgebornEndurance, t3b.PrerequisiteBlessings!);
    }

    [Fact]
    public void T4_AvatarOfTheForge_Requires_Both_T3_Paths()
    {
        var t4 = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftAvatarOfForge);
        Assert.Contains(BlessingIds.CraftLegendarySmith, t4.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.CraftUnyielding, t4.PrerequisiteBlessings!);
    }

    [Fact]
    public void ReligionBlessings_AllMarked_AsReligionKind()
    {
        var ids = new[]
        {
            BlessingIds.CraftSharedWorkshop,
            BlessingIds.CraftGuildOfSmiths,
            BlessingIds.CraftMasterCraftsmen,
            BlessingIds.CraftPantheonOfCreation
        };

        var blessings = GetKhoras().Where(b => ids.Contains(b.BlessingId)).ToList();
        Assert.Equal(4, blessings.Count);
        Assert.All(blessings, b => Assert.Equal(BlessingKind.Religion, b.Kind));
    }

    [Fact]
    public void SpecificStats_Match_Implementation()
    {
        var blessings = GetKhoras().ToList();

        var unyielding = blessings.First(b => b.BlessingId == BlessingIds.CraftUnyielding);
        Assert.Equal(-0.10f, unyielding.StatModifiers[VintageStoryStats.ArmorDurabilityLoss], 3);
        Assert.Equal(0.15f, unyielding.StatModifiers[VintageStoryStats.MaxHealthExtraMultiplier], 3);

        var avatar = blessings.First(b => b.BlessingId == BlessingIds.CraftAvatarOfForge);
        Assert.Equal(-0.10f, avatar.StatModifiers[VintageStoryStats.ArmorWalkSpeedAffectedness], 3);

        var masterCraftsmen = blessings.First(b => b.BlessingId == BlessingIds.CraftMasterCraftsmen);
        Assert.Equal(-0.10f, masterCraftsmen.StatModifiers[VintageStoryStats.ArmorWalkSpeedAffectedness], 3);
    }

    [Fact]
    public void ReligionChain_Prerequisites_AreLinear()
    {
        var shared = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftSharedWorkshop);
        var guild = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftGuildOfSmiths);
        var master = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftMasterCraftsmen);
        var pantheon = GetKhoras().First(b => b.BlessingId == BlessingIds.CraftPantheonOfCreation);

        Assert.True(shared.PrerequisiteBlessings == null || shared.PrerequisiteBlessings.Count == 0);
        Assert.Contains(BlessingIds.CraftSharedWorkshop, guild.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.CraftGuildOfSmiths, master.PrerequisiteBlessings!);
        Assert.Contains(BlessingIds.CraftMasterCraftsmen, pantheon.PrerequisiteBlessings!);
    }
}