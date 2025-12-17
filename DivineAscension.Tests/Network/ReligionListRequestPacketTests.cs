using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class ReligionListRequestPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new ReligionListRequestPacket();
        Assert.Equal(string.Empty, packet.FilterDeity);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new ReligionListRequestPacket("deityFilter");
        Assert.Equal("deityFilter", packet.FilterDeity);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new ReligionListRequestPacket("dragon deity");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<ReligionListRequestPacket>(ms);

        Assert.Equal(original.FilterDeity, deserialized.FilterDeity);
    }
}