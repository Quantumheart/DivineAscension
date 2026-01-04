using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Network.Diplomacy;

/// <summary>
///     Server response containing diplomacy information for a civilization
/// </summary>
[ProtoContract]
public class DiplomacyInfoResponsePacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public DiplomacyInfoResponsePacket()
    {
        Relationships = new List<RelationshipInfo>();
        IncomingProposals = new List<ProposalInfo>();
        OutgoingProposals = new List<ProposalInfo>();
    }

    /// <summary>
    ///     Creates a new diplomacy info response
    /// </summary>
    public DiplomacyInfoResponsePacket(
        string civId,
        List<RelationshipInfo> relationships,
        List<ProposalInfo> incomingProposals,
        List<ProposalInfo> outgoingProposals)
    {
        CivId = civId;
        Relationships = relationships;
        IncomingProposals = incomingProposals;
        OutgoingProposals = outgoingProposals;
    }

    /// <summary>
    ///     Civilization ID this response is for
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     All active relationships for the civilization
    /// </summary>
    [ProtoMember(2)]
    public List<RelationshipInfo> Relationships { get; set; }

    /// <summary>
    ///     Incoming proposals (where this civ is the target)
    /// </summary>
    [ProtoMember(3)]
    public List<ProposalInfo> IncomingProposals { get; set; }

    /// <summary>
    ///     Outgoing proposals (where this civ is the proposer)
    /// </summary>
    [ProtoMember(4)]
    public List<ProposalInfo> OutgoingProposals { get; set; }

    /// <summary>
    ///     Serializable relationship information
    /// </summary>
    [ProtoContract]
    public class RelationshipInfo
    {
        /// <summary>
        ///     Parameterless constructor for serialization
        /// </summary>
        public RelationshipInfo()
        {
        }

        /// <summary>
        ///     Creates a new relationship info
        /// </summary>
        public RelationshipInfo(
            string relationshipId,
            string otherCivId,
            string otherCivName,
            DiplomaticStatus status,
            DateTime establishedDate,
            DateTime? expiresDate,
            int violationCount,
            DateTime? breakScheduledDate)
        {
            RelationshipId = relationshipId;
            OtherCivId = otherCivId;
            OtherCivName = otherCivName;
            Status = status;
            EstablishedDate = establishedDate;
            ExpiresDate = expiresDate;
            ViolationCount = violationCount;
            BreakScheduledDate = breakScheduledDate;
        }

        [ProtoMember(1)] public string RelationshipId { get; set; } = string.Empty;
        [ProtoMember(2)] public string OtherCivId { get; set; } = string.Empty;
        [ProtoMember(3)] public string OtherCivName { get; set; } = string.Empty;
        [ProtoMember(4)] public DiplomaticStatus Status { get; set; }
        [ProtoMember(5)] public DateTime EstablishedDate { get; set; }
        [ProtoMember(6)] public DateTime? ExpiresDate { get; set; }
        [ProtoMember(7)] public int ViolationCount { get; set; }
        [ProtoMember(8)] public DateTime? BreakScheduledDate { get; set; }
    }

    /// <summary>
    ///     Serializable proposal information
    /// </summary>
    [ProtoContract]
    public class ProposalInfo
    {
        /// <summary>
        ///     Parameterless constructor for serialization
        /// </summary>
        public ProposalInfo()
        {
        }

        /// <summary>
        ///     Creates a new proposal info
        /// </summary>
        public ProposalInfo(
            string proposalId,
            string otherCivId,
            string otherCivName,
            DiplomaticStatus proposedStatus,
            DateTime sentDate,
            DateTime expiresDate,
            int? duration)
        {
            ProposalId = proposalId;
            OtherCivId = otherCivId;
            OtherCivName = otherCivName;
            ProposedStatus = proposedStatus;
            SentDate = sentDate;
            ExpiresDate = expiresDate;
            Duration = duration;
        }

        [ProtoMember(1)] public string ProposalId { get; set; } = string.Empty;
        [ProtoMember(2)] public string OtherCivId { get; set; } = string.Empty;
        [ProtoMember(3)] public string OtherCivName { get; set; } = string.Empty;
        [ProtoMember(4)] public DiplomaticStatus ProposedStatus { get; set; }
        [ProtoMember(5)] public DateTime SentDate { get; set; }
        [ProtoMember(6)] public DateTime ExpiresDate { get; set; }
        [ProtoMember(7)] public int? Duration { get; set; }
    }
}
