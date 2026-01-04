using System;
using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
/// Interface for managing diplomatic relationships between civilizations
/// </summary>
public interface IDiplomacyManager
{
    /// <summary>
    /// Event fired when a diplomatic relationship is established
    /// </summary>
    event Action<string, string, DiplomaticStatus>? OnRelationshipEstablished;

    /// <summary>
    /// Event fired when a diplomatic relationship ends
    /// </summary>
    event Action<string, string, DiplomaticStatus>? OnRelationshipEnded;

    /// <summary>
    /// Event fired when war is declared
    /// </summary>
    event Action<string, string>? OnWarDeclared;

    /// <summary>
    /// Initializes the diplomacy manager
    /// </summary>
    void Initialize();

    /// <summary>
    /// Proposes a diplomatic relationship between two civilizations
    /// </summary>
    /// <param name="proposerCivId">The civilization proposing the relationship</param>
    /// <param name="targetCivId">The target civilization</param>
    /// <param name="proposedStatus">The proposed diplomatic status</param>
    /// <param name="proposerFounderUID">The UID of the proposer's founder</param>
    /// <param name="duration">Optional duration in days for the relationship</param>
    /// <returns>Success status and message</returns>
    (bool success, string message, string? proposalId) ProposeRelationship(
        string proposerCivId,
        string targetCivId,
        DiplomaticStatus proposedStatus,
        string proposerFounderUID,
        int? duration = null);

    /// <summary>
    /// Accepts a diplomatic proposal
    /// </summary>
    /// <param name="proposalId">The proposal ID to accept</param>
    /// <param name="acceptorFounderUID">The UID of the acceptor's founder</param>
    /// <returns>Success status and message</returns>
    (bool success, string message, string? relationshipId) AcceptProposal(string proposalId, string acceptorFounderUID);

    /// <summary>
    /// Declines a diplomatic proposal
    /// </summary>
    /// <param name="proposalId">The proposal ID to decline</param>
    /// <param name="declinerFounderUID">The UID of the decliner's founder</param>
    /// <returns>Success status and message</returns>
    (bool success, string message) DeclineProposal(string proposalId, string declinerFounderUID);

    /// <summary>
    /// Schedules a treaty break with 24-hour warning
    /// </summary>
    /// <param name="civId">The civilization scheduling the break</param>
    /// <param name="otherCivId">The other civilization in the relationship</param>
    /// <param name="founderUID">The UID of the founder requesting the break</param>
    /// <returns>Success status and message</returns>
    (bool success, string message) ScheduleBreak(string civId, string otherCivId, string founderUID);

    /// <summary>
    /// Cancels a scheduled treaty break
    /// </summary>
    /// <param name="civId">The civilization canceling the break</param>
    /// <param name="otherCivId">The other civilization in the relationship</param>
    /// <param name="founderUID">The UID of the founder canceling the break</param>
    /// <returns>Success status and message</returns>
    (bool success, string message) CancelScheduledBreak(string civId, string otherCivId, string founderUID);

    /// <summary>
    /// Declares war on another civilization (unilateral)
    /// </summary>
    /// <param name="declarerCivId">The civilization declaring war</param>
    /// <param name="targetCivId">The target civilization</param>
    /// <param name="founderUID">The UID of the declarer's founder</param>
    /// <returns>Success status and message</returns>
    (bool success, string message) DeclareWar(string declarerCivId, string targetCivId, string founderUID);

    /// <summary>
    /// Declares peace (ends war status)
    /// </summary>
    /// <param name="civId">The civilization declaring peace</param>
    /// <param name="otherCivId">The other civilization</param>
    /// <param name="founderUID">The UID of the founder declaring peace</param>
    /// <returns>Success status and message</returns>
    (bool success, string message) DeclarePeace(string civId, string otherCivId, string founderUID);

    /// <summary>
    /// Gets the diplomatic status between two civilizations
    /// </summary>
    /// <param name="civId1">First civilization ID</param>
    /// <param name="civId2">Second civilization ID</param>
    /// <returns>The diplomatic status</returns>
    DiplomaticStatus GetDiplomaticStatus(string civId1, string civId2);

    /// <summary>
    /// Records a PvP violation (attacking allied/NAP civilization)
    /// </summary>
    /// <param name="attackerCivId">The attacking civilization</param>
    /// <param name="victimCivId">The victim civilization</param>
    /// <returns>Violation count after recording</returns>
    int RecordPvPViolation(string attackerCivId, string victimCivId);

    /// <summary>
    /// Gets the favor multiplier for PvP based on diplomatic status
    /// </summary>
    /// <param name="attackerCivId">The attacking civilization</param>
    /// <param name="victimCivId">The victim civilization</param>
    /// <returns>The favor multiplier (1.0 for neutral, 1.5 for war, 0.0 for allies)</returns>
    double GetFavorMultiplier(string attackerCivId, string victimCivId);

    /// <summary>
    /// Gets all diplomatic relationships for a civilization
    /// </summary>
    /// <param name="civId">The civilization ID</param>
    /// <returns>List of diplomatic relationships</returns>
    List<DiplomaticRelationship> GetRelationshipsForCiv(string civId);

    /// <summary>
    /// Gets all proposals for a civilization (both incoming and outgoing)
    /// </summary>
    /// <param name="civId">The civilization ID</param>
    /// <returns>List of diplomatic proposals</returns>
    List<DiplomaticProposal> GetProposalsForCiv(string civId);

    /// <summary>
    /// Gets a specific relationship by ID
    /// </summary>
    /// <param name="relationshipId">The relationship ID</param>
    /// <returns>The diplomatic relationship or null</returns>
    DiplomaticRelationship? GetRelationship(string relationshipId);

    /// <summary>
    /// Disposes the diplomacy manager and cleanup resources
    /// </summary>
    void Dispose();
}
