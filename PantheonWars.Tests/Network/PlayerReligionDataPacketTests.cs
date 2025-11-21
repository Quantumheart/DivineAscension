using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class PlayerReligionDataPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new PlayerReligionDataPacket();
        Assert.Equal(string.Empty, packet.ReligionName);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new PlayerReligionDataPacket("Test Guild");

        Assert.Equal("Test Guild", packet.ReligionName);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new PlayerReligionDataPacket("Test Guild");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<PlayerReligionDataPacket>(ms);

        Assert.Equal(original.ReligionName, deserialized.ReligionName);
    }
}
