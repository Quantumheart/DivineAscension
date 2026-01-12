using System.Diagnostics.CodeAnalysis;
using DivineAscension.Network;
using ProtoBuf;

namespace DivineAscension.Tests.Network;

[ExcludeFromCodeCoverage]
public class PlayerReligionDataPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new PlayerReligionDataPacket();
        Assert.Equal(string.Empty, packet.ReligionName);
        Assert.Equal(string.Empty, packet.Domain);
        Assert.Equal(string.Empty, packet.DeityName);
        Assert.Equal(0, packet.Favor);
        Assert.Null(packet.FavorRank);
        Assert.Equal(0, packet.Prestige);
        Assert.Null(packet.PrestigeRank);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new PlayerReligionDataPacket(
            "Test Religion",
            "Craft",
            "Test Deity",
            100,
            "High",
            500,
            "Elite");

        Assert.Equal("Test Religion", packet.ReligionName);
        Assert.Equal("Craft", packet.Domain);
        Assert.Equal("Test Deity", packet.DeityName);
        Assert.Equal(100, packet.Favor);
        Assert.Equal("High", packet.FavorRank);
        Assert.Equal(500, packet.Prestige);
        Assert.Equal("Elite", packet.PrestigeRank);
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new PlayerReligionDataPacket(
            "Test Religion",
            "Craft",
            "Test Deity",
            100,
            "High",
            500,
            "Elite");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<PlayerReligionDataPacket>(ms);

        Assert.Equal(original.ReligionName, deserialized.ReligionName);
        Assert.Equal(original.Domain, deserialized.Domain);
        Assert.Equal(original.DeityName, deserialized.DeityName);
        Assert.Equal(original.Favor, deserialized.Favor);
        Assert.Equal(original.FavorRank, deserialized.FavorRank);
        Assert.Equal(original.Prestige, deserialized.Prestige);
        Assert.Equal(original.PrestigeRank, deserialized.PrestigeRank);
    }
}