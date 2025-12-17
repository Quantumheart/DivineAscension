using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.Tests.Models;

[ExcludeFromCodeCoverage]
public class BlessingTests
{
    [Fact]
    public void TestParameterizedConstructor()
    {
        var blessing = new Blessing("khoras_warriors_resolve", "Warrior's Resolve", DeityType.Khoras);
        Assert.Equal("khoras_warriors_resolve", blessing.BlessingId);
        Assert.Equal("Warrior's Resolve", blessing.Name);
        Assert.Equal(DeityType.Khoras, blessing.Deity);
        Assert.Equal(BlessingKind.Player, blessing.Kind);
        Assert.Equal(BlessingCategory.Combat, blessing.Category);
        Assert.Equal(0, blessing.RequiredFavorRank);
        Assert.Equal(0, blessing.RequiredPrestigeRank);
        Assert.Empty(blessing.PrerequisiteBlessings);
        Assert.Empty(blessing.StatModifiers);
        Assert.Empty(blessing.SpecialEffects);
    }

    [Fact]
    public void TestParameterlessConstructor()
    {
        var blessing = new Blessing();
        Assert.Empty(blessing.BlessingId);
        Assert.Empty(blessing.Name);
        Assert.Equal(DeityType.None, blessing.Deity);
        Assert.Equal(0, blessing.RequiredFavorRank);
        Assert.Equal(0, blessing.RequiredPrestigeRank);
        Assert.Empty(blessing.PrerequisiteBlessings);
        Assert.Empty(blessing.StatModifiers);
        Assert.Empty(blessing.SpecialEffects);
    }

    [Fact]
    public void TestCollectionProperties()
    {
        var blessing = new Blessing("test-blessing", "Test Blessing", DeityType.None);
        blessing.StatModifiers["walkspeed"] = 0.2f;
        blessing.SpecialEffects.Add("effect1");
        Assert.Equal(0.2f, blessing.StatModifiers["walkspeed"]);
        Assert.Contains("effect1", blessing.SpecialEffects);
    }

    [Fact]
    public void TestEnumValues()
    {
        var blessing = new Blessing("test-blessing", "Test Blessing", DeityType.None);
        Assert.Equal(BlessingKind.Player, blessing.Kind);
        Assert.Equal(BlessingCategory.Combat, blessing.Category);
        Assert.Equal(DeityType.None, blessing.Deity);
    }

    [Fact]
    public void TestPropertyValidation()
    {
        var blessing = new Blessing("test-blessing", "Test Blessing", DeityType.None);
        Assert.False(string.IsNullOrEmpty(blessing.BlessingId));
        Assert.False(string.IsNullOrEmpty(blessing.Name));
        Assert.Equal(DeityType.None, blessing.Deity);
    }
}