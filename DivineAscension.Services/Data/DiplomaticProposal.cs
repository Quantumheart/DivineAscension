using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// Represents a proposal for a diplomatic relationship between two civilizations
/// </summary>
[ProtoContract]
public class DiplomaticProposal
{
    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public DiplomaticProposal()
    {
    }

    /// <summary>
    /// Creates a new diplomatic proposal
    /// </summary>
    public DiplomaticProposal(
        string proposalId,
        string proposerCivId,
        string targetCivId,
        DiplomaticStatus proposedStatus,
        string proposerFounderUID,
        int? duration = null)
    {
        ProposalId = proposalId;
        ProposerCivId = proposerCivId;
        TargetCivId = targetCivId;
        ProposedStatus = proposedStatus;
        ProposerFounderUID = proposerFounderUID;
        SentDate = DateTime.UtcNow;
        ExpiresDate = DateTime.UtcNow.AddDays(7); // 7-day expiration
        Duration = duration;
    }

    /// <summary>
    /// Unique identifier for the proposal
    /// </summary>
    [ProtoMember(1)]
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Civilization ID of the proposer
    /// </summary>
    [ProtoMember(2)]
    public string ProposerCivId { get; set; } = string.Empty;

    /// <summary>
    /// Civilization ID of the target
    /// </summary>
    [ProtoMember(3)]
    public string TargetCivId { get; set; } = string.Empty;

    /// <summary>
    /// The proposed diplomatic status
    /// </summary>
    [ProtoMember(4)]
    public DiplomaticStatus ProposedStatus { get; set; }

    /// <summary>
    /// When the proposal was sent
    /// </summary>
    [ProtoMember(5)]
    public DateTime SentDate { get; set; }

    /// <summary>
    /// When the proposal expires (7 days from sent date)
    /// </summary>
    [ProtoMember(6)]
    public DateTime ExpiresDate { get; set; }

    /// <summary>
    /// Player UID of the proposer's civilization founder
    /// </summary>
    [ProtoMember(7)]
    public string ProposerFounderUID { get; set; } = string.Empty;

    /// <summary>
    /// Duration in days for the relationship (null for permanent)
    /// </summary>
    [ProtoMember(8)]
    public int? Duration { get; set; }

    /// <summary>
    /// Checks if the proposal is still valid (not expired)
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresDate;
}