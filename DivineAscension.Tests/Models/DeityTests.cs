using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.Tests.Models;

[ExcludeFromCodeCoverage]
public class DeityTests
{
    [Fact]
    public void TestPropertyInitialization()
    {
        var deity = new Deity(DeityType.Khoras, "Khoras", "War");
        Assert.Equal(DeityType.Khoras, deity.Type);
        Assert.Equal("Khoras", deity.Name);
        Assert.Equal("War", deity.Domain);
        Assert.Equal(string.Empty, deity.Playstyle);
        Assert.Equal("#FFFFFF", deity.PrimaryColor);
        Assert.Equal("#CCCCCC", deity.SecondaryColor);
        Assert.Empty(deity.AbilityIds);
        Assert.Empty(deity.Relationships);
    }

    [Fact]
    public void TestRelationshipsDictionary()
    {
        var deity = new Deity(DeityType.Khoras, "Khoras", "War");
        deity.Relationships[DeityType.Aethra] = DeityRelationshipType.Allied;
        Assert.Equal(DeityRelationshipType.Allied, deity.Relationships[DeityType.Aethra]);
        Assert.Contains(DeityType.Aethra, deity.Relationships.Keys);
    }

    [Fact]
    public void TestEnumValues()
    {
        var deity = new Deity(DeityType.Khoras, "Khoras", "War");
        deity.Relationships.Add(DeityType.Aethra, DeityRelationshipType.Allied);
        Assert.Equal(DeityType.Khoras, deity.Type);
        Assert.Equal(DeityRelationshipType.Allied, deity.Relationships[DeityType.Aethra]);
    }

    [Fact]
    public void TestStringProperties()
    {
        var deity = new Deity(DeityType.Khoras, "Khoras", "War");
        deity.Name = null!;
        deity.Domain = "Battle";
        deity.Playstyle = "Aggressive";
        Assert.Null(deity.Name);
        Assert.Equal("Battle", deity.Domain);
        Assert.Equal("Aggressive", deity.Playstyle);
    }

    [Fact]
    public void TestListProperties()
    {
        var deity = new Deity(DeityType.Khoras, "Khoras", "War");
        deity.AbilityIds.Add("ability1");
        deity.AbilityIds.Add("ability2");
        Assert.Contains("ability1", deity.AbilityIds);
        Assert.Contains("ability2", deity.AbilityIds);
        Assert.Equal(2, deity.AbilityIds.Count);
    }
}