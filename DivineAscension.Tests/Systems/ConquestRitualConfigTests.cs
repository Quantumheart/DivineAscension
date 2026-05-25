using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using DivineAscension.Models;
using DivineAscension.Systems;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Guards the item-code globs in the shipped conquest ritual config (#245). VS armor
///     codes are <c>armor-{bodypart}-{type}-{material}</c> (e.g. <c>armor-body-plate-leather</c>),
///     so a pattern like <c>game:armor-plate-*</c> — missing the bodypart segment — matches
///     nothing and makes the Ancient Armor requirement impossible to satisfy.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConquestRitualConfigTests
{
    // Representative real VS armor item codes (bodypart × type × material).
    [Theory]
    [InlineData("game:armor-body-plate-blackguard-rusty")]
    [InlineData("game:armor-legs-plate-leather")]
    [InlineData("game:armor-head-plate-forlorn-rusty")]
    [InlineData("game:armor-body-brigandine-linen")]
    [InlineData("game:armor-legs-brigandine-gold")]
    public void AncientArmorRequirement_MatchesRealVsArmorCodes(string armorCode)
    {
        var requirement = LoadRequirement("ancient_armor");
        var matcher = new RitualMatcher();

        var matched = matcher.DoesItemMatchRequirement(CreateItemStack(armorCode), requirement);

        Assert.True(matched, $"Ancient Armor requirement should match '{armorCode}'.");
    }

    [Theory]
    [InlineData("game:ingot-copper")]
    [InlineData("game:armor-body-chain-leather")] // chain/scale are not "ancient" armor types
    public void AncientArmorRequirement_DoesNotMatchUnrelatedItems(string code)
    {
        var requirement = LoadRequirement("ancient_armor");
        var matcher = new RitualMatcher();

        Assert.False(matcher.DoesItemMatchRequirement(CreateItemStack(code), requirement));
    }

    // VS melee blades are blade-{type}-{metal}; there is no "sword" item (#467).
    [Theory]
    [InlineData("game:blade-longsword-steel")]
    [InlineData("game:blade-arming-iron")]
    [InlineData("game:blade-gladius-copper")]
    [InlineData("game:spear-generic-copper")]
    [InlineData("game:bow-recurve")]
    public void WeaponsRequirement_MatchesRealVsWeaponCodes(string code)
    {
        var requirement = LoadRequirement("weapons");
        var matched = new RitualMatcher().DoesItemMatchRequirement(CreateItemStack(code), requirement);

        Assert.True(matched, $"Weapons requirement should match '{code}'.");
    }

    [Theory]
    [InlineData("game:blade-longsword-steel")]
    [InlineData("game:spear-boar-steel")]
    [InlineData("game:bow-recurve")]
    public void EliteWeaponsRequirement_MatchesSteelTierWeapons(string code)
    {
        var requirement = LoadRequirement("elite_weapons");
        var matched = new RitualMatcher().DoesItemMatchRequirement(CreateItemStack(code), requirement);

        Assert.True(matched, $"Elite weapons requirement should match '{code}'.");
    }

    [Theory]
    [InlineData("game:blade-longsword-copper")] // not steel tier
    [InlineData("game:spear-generic-iron")]      // not steel tier
    public void EliteWeaponsRequirement_DoesNotMatchSubSteelWeapons(string code)
    {
        var requirement = LoadRequirement("elite_weapons");
        Assert.False(new RitualMatcher().DoesItemMatchRequirement(CreateItemStack(code), requirement));
    }

    private static RitualRequirement LoadRequirement(string requirementId)
    {
        var path = Path.Combine(AppContext.BaseDirectory,
            "assets", "divineascension", "config", "rituals", "conquest.json");
        Assert.True(File.Exists(path), $"Missing ritual config: {path}");

        var fileDto = JsonSerializer.Deserialize<RitualFileDto>(
            File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(fileDto);

        var dto = fileDto!.Rituals
            .SelectMany(r => r.Steps)
            .SelectMany(s => s.Requirements)
            .FirstOrDefault(req => req.RequirementId == requirementId);
        Assert.NotNull(dto);

        Assert.True(Enum.TryParse<RequirementType>(dto!.Type, true, out var type));
        return new RitualRequirement(
            RequirementId: dto.RequirementId,
            DisplayName: dto.DisplayName,
            Quantity: dto.Quantity,
            Type: type,
            ItemCodes: dto.ItemCodes);
    }

    private static ItemStack CreateItemStack(string code)
    {
        var parts = code.Split(':');
        var domain = parts.Length > 1 ? parts[0] : "game";
        var path = parts.Length > 1 ? parts[1] : parts[0];
        return new ItemStack(new Item { Code = new AssetLocation(domain, path) });
    }
}
