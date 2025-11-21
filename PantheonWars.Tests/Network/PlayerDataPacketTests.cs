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
    
}