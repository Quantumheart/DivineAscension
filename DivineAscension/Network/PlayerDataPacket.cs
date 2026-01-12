using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Network packet for syncing player deity data from server to client
/// </summary>
[ProtoContract]
public class PlayerDataPacket
{
    public PlayerDataPacket()
    {
    }

    public PlayerDataPacket(DeityDomain deityDomain, int favor, DevotionRank rank, string deityName)
    {
        DeityTypeId = (int)deityDomain;
        DivineFavor = favor;
        DevotionRankId = (int)rank;
        DeityName = deityName;
    }

    [ProtoMember(1)] public int DeityTypeId { get; set; }

    [ProtoMember(2)] public int DivineFavor { get; set; }

    [ProtoMember(3)] public int DevotionRankId { get; set; }

    [ProtoMember(4)] public string DeityName { get; set; } = string.Empty;

    public DeityDomain GetDeityType()
    {
        return (DeityDomain)DeityTypeId;
    }

    public DevotionRank GetDevotionRank()
    {
        return (DevotionRank)DevotionRankId;
    }
}