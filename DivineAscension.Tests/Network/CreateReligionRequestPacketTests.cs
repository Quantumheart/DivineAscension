using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using ProtoBuf;

namespace DivineAscension.Tests.Network;

[ExcludeFromCodeCoverage]
public class CreateReligionRequestPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesPropertiesCorrectly()
    {
        var packet = new CreateReligionRequestPacket();
        Assert.Equal(string.Empty, packet.ReligionName);
        Assert.Equal(string.Empty, packet.Domain);
        Assert.False(packet.IsPublic);
    }

    [Fact]
    public void ParameterizedConstructor_SetsPropertiesCorrectly()
    {
        var packet = new CreateReligionRequestPacket("Test Religion", DeityDomain.Craft.ToString(), "Craft", true);
        Assert.Equal("Test Religion", packet.ReligionName);
        Assert.Equal("Craft", packet.Domain);
        Assert.Equal("Craft", packet.DeityName);
        Assert.True(packet.IsPublic);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripCorrectness()
    {
        var original = new CreateReligionRequestPacket("Test Religion", DeityDomain.Craft.ToString(), "Craft", true);

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;

        var deserialized = Serializer.Deserialize<CreateReligionRequestPacket>(ms);

        Assert.Equal(original.ReligionName, deserialized.ReligionName);
        Assert.Equal(original.Domain, deserialized.Domain);
        Assert.Equal(original.IsPublic, deserialized.IsPublic);
    }
}