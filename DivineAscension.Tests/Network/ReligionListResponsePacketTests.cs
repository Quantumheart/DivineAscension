using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class ReligionListResponsePacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new ReligionListResponsePacket();
        Assert.Empty(packet.Religions);
        Assert.NotNull(packet.Religions); // Verify null reference behavior
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var religion1 = new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = "religion1",
            ReligionName = "Ancient Faith",
            Deity = "God of Storms",
            MemberCount = 1234,
            Prestige = 850,
            PrestigeRank = "Epic",
            IsPublic = true,
            FounderUID = "founder001",
            Description = "A powerful faith of the storm"
        };

        var religion2 = new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = "religion2",
            ReligionName = "Order of the Sun",
            Deity = "Solar Deity",
            MemberCount = 567,
            Prestige = 420,
            PrestigeRank = "Legendary",
            IsPublic = false,
            FounderUID = "founder002",
            Description = "A mysterious sun cult"
        };

        var packet = new ReligionListResponsePacket(new List<ReligionListResponsePacket.ReligionInfo>
            { religion1, religion2 });
        Assert.Equal(2, packet.Religions.Count);
        Assert.Equal(religion1, packet.Religions[0]);
        Assert.Equal(religion2, packet.Religions[1]);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var religion = new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = "religion123",
            ReligionName = "Celestial Order",
            Deity = "Star God",
            MemberCount = 9876,
            Prestige = 1500,
            PrestigeRank = "Divine",
            IsPublic = true,
            FounderUID = "founder456",
            Description = "A celestial faith of stars"
        };

        var packet = new ReligionListResponsePacket(new List<ReligionListResponsePacket.ReligionInfo> { religion });

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, packet);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<ReligionListResponsePacket>(ms);

        Assert.Equal(packet.Religions.Count, deserialized.Religions.Count);
        Assert.Equal(religion.ReligionUID, deserialized.Religions[0].ReligionUID);
        Assert.Equal(religion.ReligionName, deserialized.Religions[0].ReligionName);
        Assert.Equal(religion.Deity, deserialized.Religions[0].Deity);
        Assert.Equal(religion.MemberCount, deserialized.Religions[0].MemberCount);
        Assert.Equal(religion.Prestige, deserialized.Religions[0].Prestige);
        Assert.Equal(religion.PrestigeRank, deserialized.Religions[0].PrestigeRank);
        Assert.Equal(religion.IsPublic, deserialized.Religions[0].IsPublic);
        Assert.Equal(religion.FounderUID, deserialized.Religions[0].FounderUID);
        Assert.Equal(religion.Description, deserialized.Religions[0].Description);
    }
}