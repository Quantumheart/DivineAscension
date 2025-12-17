using System;
using System.Collections.Generic;
using ProtoBuf;

namespace PantheonWars.Network.Civilization;

/// <summary>
///     Server sends detailed information about a specific civilization
/// </summary>
[ProtoContract]
public class CivilizationInfoResponsePacket
{
    public CivilizationInfoResponsePacket()
    {
    }

    public CivilizationInfoResponsePacket(CivilizationDetails details)
    {
        Details = details;
    }

    [ProtoMember(1)] public CivilizationDetails? Details { get; set; }

    /// <summary>
    ///     Detailed information about a civilization
    /// </summary>
    [ProtoContract]
    public class CivilizationDetails
    {
        [ProtoMember(1)] public string CivId { get; set; } = string.Empty;

        [ProtoMember(2)] public string Name { get; set; } = string.Empty;

        [ProtoMember(3)] public string FounderUID { get; set; } = string.Empty;

        [ProtoMember(4)] public string FounderReligionUID { get; set; } = string.Empty;

        [ProtoMember(5)] public List<MemberReligion>? MemberReligions { get; set; } = new();

        [ProtoMember(6)] public List<PendingInvite>? PendingInvites { get; set; } = new();

        [ProtoMember(7)] public DateTime CreatedDate { get; set; }

        [ProtoMember(8)]
        public string FounderName { get; set; } = string.Empty; // Player name who founded the civilization

        [ProtoMember(9)]
        public string FounderReligionName { get; set; } = string.Empty; // Religion name that founded the civilization

        [ProtoMember(10)] public string Icon { get; set; } = "default"; // Icon identifier for the civilization
    }

    /// <summary>
    ///     Information about a member religion
    /// </summary>
    [ProtoContract]
    public class MemberReligion
    {
        [ProtoMember(1)] public string ReligionId { get; set; } = string.Empty;

        [ProtoMember(2)] public string ReligionName { get; set; } = string.Empty;

        [ProtoMember(3)] public string Deity { get; set; } = string.Empty;

        [ProtoMember(4)] public string FounderUID { get; set; } = string.Empty;

        [ProtoMember(5)] public string FounderReligionUID { get; set; } = string.Empty;

        [ProtoMember(6)] public int MemberCount { get; set; }

        [ProtoMember(7)]
        public string FounderName { get; set; } = string.Empty; // Player name who founded this religion
    }

    /// <summary>
    ///     Information about a pending invite
    /// </summary>
    [ProtoContract]
    public class PendingInvite
    {
        [ProtoMember(1)] public string InviteId { get; set; } = string.Empty;

        [ProtoMember(2)] public string ReligionId { get; set; } = string.Empty;

        [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;

        [ProtoMember(4)] public DateTime ExpiresAt { get; set; }
    }
}