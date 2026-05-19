using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using ProtoBuf;

namespace DivineAscension.Tests.Network;

[ExcludeFromCodeCoverage]
public class PlayerReligionDataPacketTests
{
    private static readonly DeityDomain[] AllDeities =
    {
        DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest,
        DeityDomain.Harvest, DeityDomain.Stone
    };

    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new PlayerReligionDataPacket();
        Assert.Equal(string.Empty, packet.ReligionName);
        Assert.Equal(DeityDomain.None, packet.PatronDomain);
        Assert.Equal(string.Empty, packet.PatronName);
        Assert.Empty(packet.FavorByDeity);
        Assert.Empty(packet.FavorRanksByDeity);
        Assert.Empty(packet.TotalFavorEarnedByDeity);
        Assert.Equal(0, packet.Prestige);
        Assert.Null(packet.PrestigeRank);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var favor = new Dictionary<DeityDomain, int> { [DeityDomain.Craft] = 100 };
        var ranks = new Dictionary<DeityDomain, string> { [DeityDomain.Craft] = "Disciple" };
        var totals = new Dictionary<DeityDomain, int> { [DeityDomain.Craft] = 1234 };

        var packet = new PlayerReligionDataPacket(
            "Test Religion",
            DeityDomain.Craft,
            "Test Deity",
            favor,
            ranks,
            totals,
            500,
            "Elite");

        Assert.Equal("Test Religion", packet.ReligionName);
        Assert.Equal(DeityDomain.Craft, packet.PatronDomain);
        Assert.Equal("Test Deity", packet.PatronName);
        Assert.Equal(100, packet.FavorByDeity[DeityDomain.Craft]);
        Assert.Equal("Disciple", packet.FavorRanksByDeity[DeityDomain.Craft]);
        Assert.Equal(1234, packet.TotalFavorEarnedByDeity[DeityDomain.Craft]);
        Assert.Equal(500, packet.Prestige);
        Assert.Equal("Elite", packet.PrestigeRank);
    }

    [Fact]
    public void Serialize_Deserialize_AllFiveDeitiesRoundTrip()
    {
        var favor = new Dictionary<DeityDomain, int>();
        var ranks = new Dictionary<DeityDomain, string>();
        var totals = new Dictionary<DeityDomain, int>();
        for (var i = 0; i < AllDeities.Length; i++)
        {
            favor[AllDeities[i]] = 100 * (i + 1);
            ranks[AllDeities[i]] = $"Rank{i}";
            totals[AllDeities[i]] = 1000 * (i + 1);
        }

        var original = new PlayerReligionDataPacket(
            "Test Religion",
            DeityDomain.Wild,
            "Patron Of Wild",
            favor,
            ranks,
            totals,
            500,
            "Elite");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<PlayerReligionDataPacket>(ms);

        Assert.Equal("Test Religion", deserialized.ReligionName);
        Assert.Equal(DeityDomain.Wild, deserialized.PatronDomain);
        Assert.Equal("Patron Of Wild", deserialized.PatronName);
        Assert.Equal(500, deserialized.Prestige);
        Assert.Equal("Elite", deserialized.PrestigeRank);

        foreach (var d in AllDeities)
        {
            Assert.Equal(favor[d], deserialized.FavorByDeity[d]);
            Assert.Equal(ranks[d], deserialized.FavorRanksByDeity[d]);
            Assert.Equal(totals[d], deserialized.TotalFavorEarnedByDeity[d]);
        }
    }
}
