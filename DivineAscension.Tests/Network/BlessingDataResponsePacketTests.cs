using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using ProtoBuf;

namespace DivineAscension.Tests.Network;

[ExcludeFromCodeCoverage]
public class BlessingDataResponsePacketTests
{
    private static readonly DeityDomain[] AllDeities =
    {
        DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest,
        DeityDomain.Harvest, DeityDomain.Stone
    };

    [Fact]
    public void DefaultConstructor_HasEmptyState()
    {
        var packet = new BlessingDataResponsePacket();
        Assert.False(packet.HasReligion);
        Assert.Equal(DeityDomain.None, packet.PatronDomain);
        Assert.Equal(string.Empty, packet.PatronName);
        Assert.Empty(packet.FavorByDeity);
        Assert.Empty(packet.FavorRanksByDeity);
        Assert.Empty(packet.TotalFavorEarnedByDeity);
        Assert.Empty(packet.PlayerBlessings);
        Assert.Empty(packet.ReligionBlessings);
    }

    [Fact]
    public void RoundTrip_CarriesAllFiveDeitiesAndPerBlessingDomain()
    {
        var packet = new BlessingDataResponsePacket
        {
            HasReligion = true,
            ReligionUID = "uid",
            ReligionName = "Test",
            PatronDomain = DeityDomain.Harvest,
            PatronName = "PatronH",
            PrestigeRank = 3,
            CurrentPrestige = 12345,
            ReligionBlessingSlotCap = 5,
            ReligionBlessingSlotUsed = 3
        };

        for (var i = 0; i < AllDeities.Length; i++)
        {
            var d = AllDeities[i];
            packet.FavorByDeity[d] = (i + 1) * 100;
            packet.FavorRanksByDeity[d] = i;
            packet.TotalFavorEarnedByDeity[d] = (i + 1) * 1000;

            packet.PlayerBlessings.Add(new BlessingDataResponsePacket.BlessingInfo
            {
                BlessingId = $"p_{d}",
                Name = $"Player {d}",
                Cost = 10,
                Domain = d,
                RequiresPatron = false
            });
            packet.ReligionBlessings.Add(new BlessingDataResponsePacket.BlessingInfo
            {
                BlessingId = $"r_{d}",
                Name = $"Religion {d}",
                Cost = 100,
                Domain = d,
                // Capstones flagged as patron-only
                RequiresPatron = true
            });
        }

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, packet);
        ms.Position = 0;
        var rt = Serializer.Deserialize<BlessingDataResponsePacket>(ms);

        Assert.True(rt.HasReligion);
        Assert.Equal(DeityDomain.Harvest, rt.PatronDomain);
        Assert.Equal("PatronH", rt.PatronName);
        Assert.Equal(3, rt.PrestigeRank);
        Assert.Equal(5, rt.ReligionBlessingSlotCap);
        Assert.Equal(3, rt.ReligionBlessingSlotUsed);

        foreach (var d in AllDeities)
        {
            Assert.Equal(packet.FavorByDeity[d], rt.FavorByDeity[d]);
            Assert.Equal(packet.FavorRanksByDeity[d], rt.FavorRanksByDeity[d]);
            Assert.Equal(packet.TotalFavorEarnedByDeity[d], rt.TotalFavorEarnedByDeity[d]);

            var pb = rt.PlayerBlessings.Single(b => b.BlessingId == $"p_{d}");
            Assert.Equal(d, pb.Domain);
            Assert.False(pb.RequiresPatron);

            var rb = rt.ReligionBlessings.Single(b => b.BlessingId == $"r_{d}");
            Assert.Equal(d, rb.Domain);
            Assert.True(rb.RequiresPatron);
        }
    }

    [Fact]
    public void RoundTrip_NoReligion_PreservesEmptyState()
    {
        var packet = new BlessingDataResponsePacket { HasReligion = false };

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, packet);
        ms.Position = 0;
        var rt = Serializer.Deserialize<BlessingDataResponsePacket>(ms);

        Assert.False(rt.HasReligion);
        Assert.Equal(DeityDomain.None, rt.PatronDomain);
    }
}
