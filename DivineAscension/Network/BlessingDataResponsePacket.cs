using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server sends blessing data for the player's current deity
/// </summary>
[ProtoContract]
public class BlessingDataResponsePacket
{
    [ProtoMember(1)] public bool HasReligion { get; set; }

    [ProtoMember(2)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     The domain (Craft, Wild, Harvest, Stone)
    /// </summary>
    [ProtoMember(4)]
    public string Domain { get; set; } = string.Empty;

    [ProtoMember(5)] public int FavorRank { get; set; }

    /// <summary>
    ///     The custom deity name for this religion
    /// </summary>
    [ProtoMember(14)]
    public string DeityName { get; set; } = string.Empty;

    [ProtoMember(6)] public int PrestigeRank { get; set; }

    [ProtoMember(7)] public int CurrentFavor { get; set; }

    [ProtoMember(8)] public int CurrentPrestige { get; set; }

    [ProtoMember(9)] public int TotalFavorEarned { get; set; }

    [ProtoMember(10)] public List<BlessingInfo> PlayerBlessings { get; set; } = new();

    [ProtoMember(11)] public List<BlessingInfo> ReligionBlessings { get; set; } = new();

    [ProtoMember(12)] public List<string> UnlockedPlayerBlessings { get; set; } = new();

    [ProtoMember(13)] public List<string> UnlockedReligionBlessings { get; set; } = new();

    /// <summary>
    ///     Basic blessing information needed for UI display
    /// </summary>
    [ProtoContract]
    public class BlessingInfo
    {
        [ProtoMember(1)] public string BlessingId { get; set; } = string.Empty;

        [ProtoMember(2)] public string Name { get; set; } = string.Empty;

        [ProtoMember(3)] public string Description { get; set; } = string.Empty;

        [ProtoMember(4)] public int RequiredFavorRank { get; set; }

        [ProtoMember(5)] public int RequiredPrestigeRank { get; set; }

        [ProtoMember(6)] public List<string> PrerequisiteBlessings { get; set; } = new();

        [ProtoMember(7)] public int Category { get; set; } // BlessingCategory as int

        [ProtoMember(8)] public Dictionary<string, float> StatModifiers { get; set; } = new();

        [ProtoMember(9)] public string IconName { get; set; } = string.Empty;

        [ProtoMember(10)] public int Cost { get; set; }
    }
}