using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class ReligionActionRequestPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new ReligionActionRequestPacket();
        Assert.Equal(string.Empty, packet.Action);
        Assert.Equal(string.Empty, packet.ReligionUID);
        Assert.Equal(string.Empty, packet.TargetPlayerUID);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new ReligionActionRequestPacket(
            "join",
            "religion123",
            "player456");

        Assert.Equal("join", packet.Action);
        Assert.Equal("religion123", packet.ReligionUID);
        Assert.Equal("player456", packet.TargetPlayerUID);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new ReligionActionRequestPacket(
            "kick",
            "religion789",
            "player012");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<ReligionActionRequestPacket>(ms);

        Assert.Equal(original.Action, deserialized.Action);
        Assert.Equal(original.ReligionUID, deserialized.ReligionUID);
        Assert.Equal(original.TargetPlayerUID, deserialized.TargetPlayerUID);
    }
}