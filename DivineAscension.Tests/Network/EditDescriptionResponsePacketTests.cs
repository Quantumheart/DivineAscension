using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class EditDescriptionResponsePacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new EditDescriptionResponsePacket();
        Assert.False(packet.Success);
        Assert.Equal(string.Empty, packet.Message);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new EditDescriptionResponsePacket(true, "Test message");
        Assert.True(packet.Success);
        Assert.Equal("Test message", packet.Message);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new EditDescriptionResponsePacket(true, "Test message");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<EditDescriptionResponsePacket>(ms);

        Assert.Equal(original.Success, deserialized.Success);
        Assert.Equal(original.Message, deserialized.Message);
    }
}