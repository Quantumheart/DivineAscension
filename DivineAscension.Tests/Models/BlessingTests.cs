using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Helpers;

namespace DivineAscension.Tests.Models;

[ExcludeFromCodeCoverage]
public class BlessingTests
{
    public BlessingTests()
    {
        TestFixtures.InitializeLocalizationForTests();
    }

    [Fact]
    public void TestParameterizedConstructor()
    {
        var blessing = new Blessing("khoras_warriors_resolve", "Warrior's Resolve", DeityDomain.Craft);
        Assert.Equal("khoras_warriors_resolve", blessing.BlessingId);
        Assert.Equal("Warrior's Resolve", blessing.Name);
        Assert.Equal(DeityDomain.Craft, blessing.Domain);
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
        Assert.Equal(DeityDomain.None, blessing.Domain);
        Assert.Equal(0, blessing.RequiredFavorRank);
        Assert.Equal(0, blessing.RequiredPrestigeRank);
        Assert.Empty(blessing.PrerequisiteBlessings);
        Assert.Empty(blessing.StatModifiers);
        Assert.Empty(blessing.SpecialEffects);
    }

    [Fact]
    public void TestCollectionProperties()
    {
        var blessing = new Blessing("test-blessing", "Test Blessing", DeityDomain.None);
        blessing.StatModifiers["walkspeed"] = 0.2f;
        blessing.SpecialEffects.Add("effect1");
        Assert.Equal(0.2f, blessing.StatModifiers["walkspeed"]);
        Assert.Contains("effect1", blessing.SpecialEffects);
    }

    [Fact]
    public void TestEnumValues()
    {
        var blessing = new Blessing("test-blessing", "Test Blessing", DeityDomain.None);
        Assert.Equal(BlessingKind.Player, blessing.Kind);
        Assert.Equal(BlessingCategory.Combat, blessing.Category);
        Assert.Equal(DeityDomain.None, blessing.Domain);
    }

    [Fact]
    public void TestPropertyValidation()
    {
        var blessing = new Blessing("test-blessing", "Test Blessing", DeityDomain.None);
        Assert.False(string.IsNullOrEmpty(blessing.BlessingId));
        Assert.False(string.IsNullOrEmpty(blessing.Name));
        Assert.Equal(DeityDomain.None, blessing.Domain);
    }
}