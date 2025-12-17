using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using ProtoBuf;

namespace PantheonWars.Tests.Network;

[ExcludeFromCodeCoverage]
public class PlayerDataPacketTests
{
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new PlayerDataPacket();
        Assert.Equal(0, packet.DeityTypeId);
        Assert.Equal(0, packet.DivineFavor);
        Assert.Equal(0, packet.DevotionRankId);
        Assert.Equal(string.Empty, packet.DeityName);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        var packet = new PlayerDataPacket(DeityType.Aethra, 100, DevotionRank.Avatar, "Solar Deity");
        Assert.Equal((int)DeityType.Aethra, packet.DeityTypeId);
        Assert.Equal(100, packet.DivineFavor);
        Assert.Equal((int)DevotionRank.Avatar, packet.DevotionRankId);
        Assert.Equal("Solar Deity", packet.DeityName);
    }

    [Fact]
    public void GetDeityType_ReturnsCorrectEnum()
    {
        var packet = new PlayerDataPacket(DeityType.Aethra, 100, DevotionRank.Avatar, "Solar Deity");
        Assert.Equal(DeityType.Aethra, packet.GetDeityType());
    }

    [Fact]
    public void GetDevotionRank_ReturnsCorrectEnum()
    {
        var packet = new PlayerDataPacket(DeityType.Aethra, 100, DevotionRank.Avatar, "Solar Deity");
        Assert.Equal(DevotionRank.Avatar, packet.GetDevotionRank());
    }

    [Fact]
    public void Serialize_Deserialize_ValuesArePreserved()
    {
        var original = new PlayerDataPacket(DeityType.Aethra, 100, DevotionRank.Avatar, "Solar Deity");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<PlayerDataPacket>(ms);

        Assert.Equal(original.DeityTypeId, deserialized.DeityTypeId);
        Assert.Equal(original.DivineFavor, deserialized.DivineFavor);
        Assert.Equal(original.DevotionRankId, deserialized.DevotionRankId);
        Assert.Equal(original.DeityName, deserialized.DeityName);
    }
}