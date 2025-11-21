using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class CreateReligionRequestPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesPropertiesCorrectly()
    {
        var packet = new CreateReligionRequestPacket();
        Assert.Equal(string.Empty, packet.ReligionName);
        Assert.False(packet.IsPublic);
    }

    [Fact]
    public void ParameterizedConstructor_SetsPropertiesCorrectly()
    {
        var packet = new CreateReligionRequestPacket("Test Guild", true);
        Assert.Equal("Test Guild", packet.ReligionName);
        Assert.True(packet.IsPublic);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripCorrectness()
    {
        var original = new CreateReligionRequestPacket("Test Guild", true);

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;

        var deserialized = Serializer.Deserialize<CreateReligionRequestPacket>(ms);

        Assert.Equal(original.ReligionName, deserialized.ReligionName);
        Assert.Equal(original.IsPublic, deserialized.IsPublic);
    }
}