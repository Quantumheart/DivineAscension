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
        Assert.NotNull(packet.SelectedBlessings);
        Assert.Empty(packet.SelectedBlessings);
        Assert.False(packet.IsPublic);
    }

    [Fact]
    public void ParameterizedConstructor_SetsPropertiesCorrectly()
    {
        var blessings = new List<string> { "efficient_miner", "swift_traveler" };
        var packet = new CreateReligionRequestPacket("Test Religion", blessings, true);
        Assert.Equal("Test Religion", packet.ReligionName);
        Assert.Equal(blessings, packet.SelectedBlessings);
        Assert.True(packet.IsPublic);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripCorrectness()
    {
        var blessings = new List<string> { "efficient_miner", "swift_traveler" };
        var original = new CreateReligionRequestPacket("Test Religion", blessings, true);

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;

        var deserialized = Serializer.Deserialize<CreateReligionRequestPacket>(ms);

        Assert.Equal(original.ReligionName, deserialized.ReligionName);
        Assert.Equal(original.SelectedBlessings, deserialized.SelectedBlessings);
        Assert.Equal(original.IsPublic, deserialized.IsPublic);
    }
}
