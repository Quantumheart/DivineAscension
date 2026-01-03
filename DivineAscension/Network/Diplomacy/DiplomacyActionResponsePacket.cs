using ProtoBuf;

namespace DivineAscension.Network.Diplomacy;

/// <summary>
///     Server response to a diplomacy action request
/// </summary>
[ProtoContract]
public class DiplomacyActionResponsePacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public DiplomacyActionResponsePacket()
    {
    }

    /// <summary>
    ///     Creates a new diplomacy action response
    /// </summary>
    public DiplomacyActionResponsePacket(
        bool success,
        string message,
        string action,
        string? relationshipId = null,
        string? proposalId = null,
        int? violationCount = null)
    {
        Success = success;
        Message = message;
        Action = action;
        RelationshipId = relationshipId ?? string.Empty;
        ProposalId = proposalId ?? string.Empty;
        ViolationCount = violationCount;
    }

    /// <summary>
    ///     Whether the action succeeded
    /// </summary>
    [ProtoMember(1)]
    public bool Success { get; set; }

    /// <summary>
    ///     Response message to display to the user
    /// </summary>
    [ProtoMember(2)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     The action that was performed
    /// </summary>
    [ProtoMember(3)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     Relationship ID (if a relationship was created/modified)
    /// </summary>
    [ProtoMember(4)]
    public string RelationshipId { get; set; } = string.Empty;

    /// <summary>
    ///     Proposal ID (if a proposal was created)
    /// </summary>
    [ProtoMember(5)]
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    ///     Current violation count (if relevant to the action)
    /// </summary>
    [ProtoMember(6)]
    public int? ViolationCount { get; set; }
}
