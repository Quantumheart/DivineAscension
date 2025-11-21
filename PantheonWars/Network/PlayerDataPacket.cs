using PantheonWars.Models.Enum;
using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Network packet for syncing player deity data from server to client
/// </summary>
[ProtoContract]
public class PlayerDataPacket
{
    public PlayerDataPacket()
    {
    }

    public PlayerDataPacket(DeityType deityType, int favor, int rank, string deityName)
    {
        DeityTypeId = (int)deityType;
        DivineFavor = favor;
        DevotionRankId = rank;
        DeityName = deityName;
    }

    [ProtoMember(1)] public int DeityTypeId { get; set; }

    [ProtoMember(2)] public int DivineFavor { get; set; }

    [ProtoMember(3)] public int DevotionRankId { get; set; }

    [ProtoMember(4)] public string DeityName { get; set; } = string.Empty;

    public DeityType GetDeityType()
    {
        return (DeityType)DeityTypeId;
    }

    public int GetDevotionRank()
    {
        return DevotionRankId;
    }
}