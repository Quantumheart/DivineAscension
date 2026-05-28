using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server sends player's religion details including member list
/// </summary>
[ProtoContract]
public class PlayerReligionInfoResponsePacket
{
    [ProtoMember(1)] public bool HasReligion { get; set; }

    [ProtoMember(2)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     The domain (Craft, Wild, Harvest, Stone)
    /// </summary>
    [ProtoMember(4)]
    public string Domain { get; set; } = string.Empty;

    [ProtoMember(5)] public string FounderUID { get; set; } = string.Empty;

    /// <summary>
    ///     The custom deity name for this religion
    /// </summary>
    [ProtoMember(15)]
    public string DeityName { get; set; } = string.Empty;

    [ProtoMember(6)] public int Prestige { get; set; }

    [ProtoMember(14)] public string FounderName { get; set; } = string.Empty;

    [ProtoMember(7)] public string PrestigeRank { get; set; } = string.Empty;

    [ProtoMember(8)] public bool IsPublic { get; set; }

    [ProtoMember(9)] public string Description { get; set; } = string.Empty;

    [ProtoMember(10)] public List<MemberInfo> Members { get; set; } = new();

    [ProtoMember(11)] public bool IsFounder { get; set; }

    [ProtoMember(12)] public List<BanInfo> BannedPlayers { get; set; } = new();

    [ProtoMember(13)] public List<ReligionInviteInfo> PendingInvites { get; set; } = new();

    [ProtoMember(16)] public string Motto { get; set; } = string.Empty;

    [ProtoMember(17)] public string FoundingMyth { get; set; } = string.Empty;

    /// <summary>
    ///     Chronicle of significant events, oldest-first, for the ledger-chapter
    ///     chronicle section (#373).
    /// </summary>
    [ProtoMember(18)]
    public List<ChronicleEntryDto> Chronicle { get; set; } = new();

    /// <summary>
    ///     A single chronicle entry sent to the client for display.
    /// </summary>
    [ProtoContract]
    public class ChronicleEntryDto
    {
        /// <summary>In-game day the event occurred, for "Day N" presentation.</summary>
        [ProtoMember(1)] public int InGameDay { get; set; }

        /// <summary>
        ///     Event category. Stored as int for proto-compat with
        ///     <see cref="DivineAscension.Models.Enum.ChronicleKind" />.
        /// </summary>
        [ProtoMember(2)] public int Kind { get; set; }

        /// <summary>The chronicle voice line, already resolved server-side.</summary>
        [ProtoMember(3)] public string Line { get; set; } = string.Empty;

        /// <summary>In-game calendar year; 0 for pre-capture entries (fall back to "Day N").</summary>
        [ProtoMember(4)] public int Year { get; set; }

        /// <summary>In-game month (1..12); 0 when unknown.</summary>
        [ProtoMember(5)] public int Month { get; set; }

        /// <summary>In-game day of month (1-based); 0 when unknown.</summary>
        [ProtoMember(6)] public int DayOfMonth { get; set; }
    }

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

        [ProtoMember(5)] public string DeityDomain { get; set; } = string.Empty;

        [ProtoMember(6)] public string Description { get; set; } = string.Empty;
    }
}