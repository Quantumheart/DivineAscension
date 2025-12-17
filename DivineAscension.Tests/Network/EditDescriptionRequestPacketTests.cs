using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class EditDescriptionRequestPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesPropertiesCorrectly()
    {
        var packet = new EditDescriptionRequestPacket();
        Assert.Equal(string.Empty, packet.ReligionUID);
        Assert.Equal(string.Empty, packet.Description);
    }

    [Fact]
    public void ParameterizedConstructor_SetsPropertiesCorrectly()
    {
        var packet = new EditDescriptionRequestPacket("religion-123", "New description");
        Assert.Equal("religion-123", packet.ReligionUID);
        Assert.Equal("New description", packet.Description);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripCorrectness()
    {
        var original = new EditDescriptionRequestPacket("religion-123", "New description");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;

        var deserialized = Serializer.Deserialize<EditDescriptionRequestPacket>(ms);

        Assert.Equal(original.ReligionUID, deserialized.ReligionUID);
        Assert.Equal(original.Description, deserialized.Description);
    }
}