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
        Assert.NotNull(packet.Religions);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var guild1 = new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = "guild1",
            ReligionName = "Ancient Guild",
            MemberCount = 15,
            IsPublic = true,
            FounderUID = "founder001",
            Description = "A powerful guild"
        };

        var guild2 = new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = "guild2",
            ReligionName = "Order of the Sun",
            MemberCount = 8,
            IsPublic = false,
            FounderUID = "founder002",
            Description = "A mysterious guild"
        };

        var packet = new ReligionListResponsePacket(new List<ReligionListResponsePacket.ReligionInfo>
            { guild1, guild2 });
        Assert.Equal(2, packet.Religions.Count);
        Assert.Equal(guild1, packet.Religions[0]);
        Assert.Equal(guild2, packet.Religions[1]);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var guild = new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = "guild123",
            ReligionName = "Celestial Order",
            MemberCount = 20,
            IsPublic = true,
            FounderUID = "founder456",
            Description = "A celestial guild"
        };

        var packet = new ReligionListResponsePacket(new List<ReligionListResponsePacket.ReligionInfo> { guild });

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, packet);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<ReligionListResponsePacket>(ms);

        Assert.Equal(packet.Religions.Count, deserialized.Religions.Count);
        Assert.Equal(guild.ReligionUID, deserialized.Religions[0].ReligionUID);
        Assert.Equal(guild.ReligionName, deserialized.Religions[0].ReligionName);
        Assert.Equal(guild.MemberCount, deserialized.Religions[0].MemberCount);
        Assert.Equal(guild.IsPublic, deserialized.Religions[0].IsPublic);
        Assert.Equal(guild.FounderUID, deserialized.Religions[0].FounderUID);
        Assert.Equal(guild.Description, deserialized.Religions[0].Description);
    }
}
