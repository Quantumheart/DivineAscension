using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.Civilization;

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

        [ProtoMember(11)] public string Description { get; set; } = string.Empty; // Civilization description

        /// <summary>
        ///     Whether the requesting player is the civilization founder
        /// </summary>
        [ProtoMember(12)]
        public bool IsFounder { get; set; }

        /// <summary>
        ///     The civilization's current rank based on milestone completion
        /// </summary>
        [ProtoMember(13)]
        public int Rank { get; set; }

        /// <summary>
        ///     Narrative ethos derived at founding. Stored as int for proto-compat with
        ///     <see cref="DivineAscension.Models.Enum.CivilizationEthos" />.
        /// </summary>
        [ProtoMember(14)]
        public int Ethos { get; set; }

        /// <summary>
        ///     Localized founder epithet (e.g. "the Forge-Crowned"), resolved at founding
        ///     and persisted on the civilization.
        /// </summary>
        [ProtoMember(15)]
        public string FounderEpithet { get; set; } = string.Empty;

        /// <summary>
        ///     Civilization capital display name.
        /// </summary>
        [ProtoMember(16)]
        public string CapitalName { get; set; } = string.Empty;

        /// <summary>
        ///     Optional UID of the holy site bound as capital. Empty when unbound.
        /// </summary>
        [ProtoMember(17)]
        public string CapitalHolySiteId { get; set; } = string.Empty;

        /// <summary>
        ///     Active civic boons (favor / prestige / conquest multipliers and
        ///     bonus holy-site slots) earned by the civilization's milestones.
        /// </summary>
        [ProtoMember(18)]
        public CivilizationBonusesDto Bonuses { get; set; } = new();
    }

    /// <summary>
    ///     Information about a member religion
    /// </summary>
    [ProtoContract]
    public class MemberReligion
    {
        [ProtoMember(1)] public string ReligionId { get; set; } = string.Empty;

        [ProtoMember(2)] public string ReligionName { get; set; } = string.Empty;

        /// <summary>
        ///     The domain (Craft, Wild, Harvest, Stone)
        /// </summary>
        [ProtoMember(3)]
        public string Domain { get; set; } = string.Empty;

        [ProtoMember(4)] public string FounderUID { get; set; } = string.Empty;

        [ProtoMember(5)] public string FounderReligionUID { get; set; } = string.Empty;

        [ProtoMember(6)] public int MemberCount { get; set; }

        [ProtoMember(7)]
        public string FounderName { get; set; } = string.Empty; // Player name who founded this religion

        /// <summary>
        ///     The custom deity name for this religion
        /// </summary>
        [ProtoMember(8)]
        public string DeityName { get; set; } = string.Empty;
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