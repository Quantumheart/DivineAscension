using System;
using System.Collections.Generic;
using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Server sends player's religion details including member list
/// </summary>
[ProtoContract]
public class PlayerReligionInfoResponsePacket
{
    [ProtoMember(1)] public bool HasReligion { get; set; }

    [ProtoMember(2)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;

    [ProtoMember(4)] public string Deity { get; set; } = string.Empty;

    [ProtoMember(5)] public string FounderUID { get; set; } = string.Empty;

    [ProtoMember(6)] public int Prestige { get; set; }

    [ProtoMember(7)] public string PrestigeRank { get; set; } = string.Empty;

    [ProtoMember(8)] public bool IsPublic { get; set; }

    [ProtoMember(9)] public string Description { get; set; } = string.Empty;

    [ProtoMember(10)] public List<MemberInfo> Members { get; set; } = new();

    [ProtoMember(11)] public bool IsFounder { get; set; }

    [ProtoMember(12)] public List<BanInfo> BannedPlayers { get; set; } = new();

    [ProtoMember(13)] public List<ReligionInviteInfo> PendingInvites { get; set; } = new();

    [ProtoContract]
    public class MemberInfo
    {
        [ProtoMember(1)] public string PlayerUID { get; set; } = string.Empty;

        [ProtoMember(2)] public string PlayerName { get; set; } = string.Empty;

        [ProtoMember(3)] public string FavorRank { get; set; } = string.Empty;

        [ProtoMember(4)] public int Favor { get; set; }

        [ProtoMember(5)] public bool IsFounder { get; set; }

        [ProtoMember(6)] public string RoleName { get; set; } = string.Empty;

        [ProtoMember(7)] public string RoleId { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class BanInfo
    {
        [ProtoMember(1)] public string PlayerUID { get; set; } = string.Empty;

        [ProtoMember(2)] public string PlayerName { get; set; } = string.Empty;

        [ProtoMember(3)] public string Reason { get; set; } = string.Empty;

        [ProtoMember(4)] public string BannedAt { get; set; } = string.Empty;

        [ProtoMember(5)] public string ExpiresAt { get; set; } = string.Empty;

        [ProtoMember(6)] public bool IsPermanent { get; set; }
    }

    [ProtoContract]
    public class ReligionInviteInfo
    {
        [ProtoMember(1)] public string InviteId { get; set; } = string.Empty;

        [ProtoMember(2)] public string ReligionId { get; set; } = string.Empty;

        [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;

        [ProtoMember(4)] public DateTime ExpiresAt { get; set; }
    }
}