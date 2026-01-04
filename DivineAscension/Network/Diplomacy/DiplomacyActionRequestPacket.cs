using ProtoBuf;

namespace DivineAscension.Network.Diplomacy;

/// <summary>
///     Client requests a diplomacy action between civilizations
/// </summary>
[ProtoContract]
public class DiplomacyActionRequestPacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public DiplomacyActionRequestPacket()
    {
    }

    /// <summary>
    ///     Creates a new diplomacy action request
    /// </summary>
    public DiplomacyActionRequestPacket(
        string action,
        string civId = "",
        string targetCivId = "",
        string proposalId = "",
        string proposedStatus = "",
        int? duration = null)
    {
        Action = action;
        CivId = civId;
        TargetCivId = targetCivId;
        ProposalId = proposalId;
        ProposedStatus = proposedStatus;
        Duration = duration;
    }

    /// <summary>
    ///     Action type: "propose", "accept", "decline", "schedulebreak", "cancelbreak", "declarewar", "declarepeace"
    /// </summary>
    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization ID of the requester
    /// </summary>
    [ProtoMember(2)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Target civilization ID (for propose, schedulebreak, cancelbreak, declarewar, declarepeace)
    /// </summary>
    [ProtoMember(3)]
    public string TargetCivId { get; set; } = string.Empty;

    /// <summary>
    ///     Proposal ID (for accept, decline actions)
    /// </summary>
    [ProtoMember(4)]
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    ///     Proposed diplomatic status (for propose action): "NonAggressionPact", "Alliance"
    /// </summary>
    [ProtoMember(5)]
    public string ProposedStatus { get; set; } = string.Empty;

    /// <summary>
    ///     Duration in days (optional, for propose action)
    /// </summary>
    [ProtoMember(6)]
    public int? Duration { get; set; }
}
