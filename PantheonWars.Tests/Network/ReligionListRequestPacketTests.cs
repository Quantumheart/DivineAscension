using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class ReligionListRequestPacketTests
{
    [Fact]
    public void Serialize_Deserialize_EmptyPacketIsPreserved()
    {
        var original = new ReligionListRequestPacket();

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<ReligionListRequestPacket>(ms);

        Assert.NotNull(deserialized);
    }
}
