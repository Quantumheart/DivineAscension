using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class ReligionActionResponsePacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new ReligionActionResponsePacket();
        Assert.False(packet.Success);
        Assert.Equal(string.Empty, packet.Message);
        Assert.Equal(string.Empty, packet.Action);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new ReligionActionResponsePacket(true, "Success message", "join");

        Assert.True(packet.Success);
        Assert.Equal("Success message", packet.Message);
        Assert.Equal("join", packet.Action);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new ReligionActionResponsePacket(true, "Operation successful", "update");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<ReligionActionResponsePacket>(ms);

        Assert.Equal(original.Success, deserialized.Success);
        Assert.Equal(original.Message, deserialized.Message);
        Assert.Equal(original.Action, deserialized.Action);
    }
}