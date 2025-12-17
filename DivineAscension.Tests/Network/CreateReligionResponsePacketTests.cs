using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class CreateReligionResponsePacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesPropertiesCorrectly()
    {
        var packet = new CreateReligionResponsePacket();
        Assert.False(packet.Success);
        Assert.Equal(string.Empty, packet.Message);
        Assert.Equal(string.Empty, packet.ReligionUID);
    }

    [Fact]
    public void ParameterizedConstructor_SetsPropertiesCorrectly()
    {
        var packet = new CreateReligionResponsePacket(true, "Religion created successfully", "religion-123");
        Assert.True(packet.Success);
        Assert.Equal("Religion created successfully", packet.Message);
        Assert.Equal("religion-123", packet.ReligionUID);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripCorrectness()
    {
        var original = new CreateReligionResponsePacket(true, "Religion created successfully", "religion-123");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;

        var deserialized = Serializer.Deserialize<CreateReligionResponsePacket>(ms);

        Assert.Equal(original.Success, deserialized.Success);
        Assert.Equal(original.Message, deserialized.Message);
        Assert.Equal(original.ReligionUID, deserialized.ReligionUID);
    }
}