using ProtoBuf;

namespace DivineAscension.Network;

[ProtoContract]
public class PlayerReligionDataPacket
{
    public PlayerReligionDataPacket()
    {
    }

    public PlayerReligionDataPacket(
        string religionName,
        string domain,
        string deityName,
        int favor,
        string favorRank,
        int prestige,
        string prestigeRank,
        int totalFavorEarned = 0)
    {
        ReligionName = religionName;
        Domain = domain;
        DeityName = deityName;
        Favor = favor;
        FavorRank = favorRank;
        Prestige = prestige;
        PrestigeRank = prestigeRank;
        TotalFavorEarned = totalFavorEarned;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     The domain (Craft, Wild, Harvest, Stone)
    /// </summary>
    [ProtoMember(2)]
    public string Domain { get; set; } = string.Empty;

    [ProtoMember(3)] public int Favor { get; set; }

    [ProtoMember(4)] public string? FavorRank { get; set; }

    [ProtoMember(5)] public int Prestige { get; set; }

    [ProtoMember(6)] public string? PrestigeRank { get; set; }

    [ProtoMember(7)] public int TotalFavorEarned { get; set; }

    /// <summary>
    ///     The custom deity name for this religion
    /// </summary>
    [ProtoMember(8)]
    public string DeityName { get; set; } = string.Empty;

    // Config thresholds (sent from server so client UI displays correct values)
    [ProtoMember(9)] public int DiscipleThreshold { get; set; } = 500;
    [ProtoMember(10)] public int ZealotThreshold { get; set; } = 2000;
    [ProtoMember(11)] public int ChampionThreshold { get; set; } = 5000;
    [ProtoMember(12)] public int AvatarThreshold { get; set; } = 10000;
    [ProtoMember(13)] public int EstablishedThreshold { get; set; } = 2500;
    [ProtoMember(14)] public int RenownedThreshold { get; set; } = 10000;
    [ProtoMember(15)] public int LegendaryThreshold { get; set; } = 25000;
    [ProtoMember(16)] public int MythicThreshold { get; set; } = 50000;
}