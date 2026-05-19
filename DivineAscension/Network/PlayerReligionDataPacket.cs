using System.Collections.Generic;
using DivineAscension.Models.Enum;
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
        DeityDomain patronDomain,
        string patronName,
        Dictionary<DeityDomain, int> favorByDeity,
        Dictionary<DeityDomain, string> favorRanksByDeity,
        Dictionary<DeityDomain, int> totalFavorEarnedByDeity,
        int prestige,
        string prestigeRank)
    {
        ReligionName = religionName;
        PatronDomain = patronDomain;
        PatronName = patronName;
        FavorByDeity = favorByDeity;
        FavorRanksByDeity = favorRanksByDeity;
        TotalFavorEarnedByDeity = totalFavorEarnedByDeity;
        Prestige = prestige;
        PrestigeRank = prestigeRank;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     Patron deity of the religion the player belongs to.
    /// </summary>
    [ProtoMember(2)]
    public DeityDomain PatronDomain { get; set; } = DeityDomain.None;

    // PM 3, 4, 7 retired (single-deity Favor, FavorRank, TotalFavorEarned). Do not reuse.

    [ProtoMember(5)] public int Prestige { get; set; }

    [ProtoMember(6)] public string? PrestigeRank { get; set; }

    /// <summary>
    ///     Patron name (custom display name for the religion's patron deity).
    /// </summary>
    [ProtoMember(8)]
    public string PatronName { get; set; } = string.Empty;

    // Config thresholds (sent from server so client UI displays correct values)
    [ProtoMember(9)] public int DiscipleThreshold { get; set; } = 500;
    [ProtoMember(10)] public int ZealotThreshold { get; set; } = 2000;
    [ProtoMember(11)] public int ChampionThreshold { get; set; } = 5000;
    [ProtoMember(12)] public int AvatarThreshold { get; set; } = 10000;
    [ProtoMember(13)] public int EstablishedThreshold { get; set; } = 2500;
    [ProtoMember(14)] public int RenownedThreshold { get; set; } = 10000;
    [ProtoMember(15)] public int LegendaryThreshold { get; set; } = 25000;
    [ProtoMember(16)] public int MythicThreshold { get; set; } = 50000;

    /// <summary>
    ///     Per-deity current favor (all five domains).
    /// </summary>
    [ProtoMember(17)]
    public Dictionary<DeityDomain, int> FavorByDeity { get; set; } = new();

    /// <summary>
    ///     Per-deity favor rank, as enum name (e.g. "Initiate", "Disciple") for UI display.
    /// </summary>
    [ProtoMember(18)]
    public Dictionary<DeityDomain, string> FavorRanksByDeity { get; set; } = new();

    /// <summary>
    ///     Per-deity total favor earned (for rank progression bars).
    /// </summary>
    [ProtoMember(19)]
    public Dictionary<DeityDomain, int> TotalFavorEarnedByDeity { get; set; } = new();
}
