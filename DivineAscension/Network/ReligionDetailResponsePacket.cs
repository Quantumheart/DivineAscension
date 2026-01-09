using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server sends detailed religion information including member list with favor ranks
/// </summary>
[ProtoContract]
public class ReligionDetailResponsePacket
{
    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(2)] public string ReligionName { get; set; } = string.Empty;

    [ProtoMember(3)] public string Deity { get; set; } = string.Empty;

    [ProtoMember(4)] public string Description { get; set; } = string.Empty;

    [ProtoMember(5)] public int Prestige { get; set; }

    [ProtoMember(6)] public string PrestigeRank { get; set; } = string.Empty;

    [ProtoMember(7)] public bool IsPublic { get; set; }

    [ProtoMember(8)] public string FounderUID { get; set; } = string.Empty;

    [ProtoMember(9)] public string FounderName { get; set; } = string.Empty;

    [ProtoMember(10)] public List<MemberInfo> Members { get; set; } = new();

    [ProtoContract]
    public class MemberInfo
    {
        [ProtoMember(1)] public string PlayerUID { get; set; } = string.Empty;

        [ProtoMember(2)] public string PlayerName { get; set; } = string.Empty;

        [ProtoMember(3)] public string FavorRank { get; set; } = string.Empty;

        [ProtoMember(4)] public int Favor { get; set; }
    }
}