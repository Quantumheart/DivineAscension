using System;
using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;

namespace DivineAscension.GUI.State.Civilization;

/// <summary>
///     State for the Diplomacy tab in the Civilization UI
/// </summary>
public class DiplomacyState : IState
{
    /// <summary>
    ///     All active diplomatic relationships for the current civilization
    /// </summary>
    public List<DiplomacyInfoResponsePacket.RelationshipInfo> ActiveRelationships { get; set; } = new();

    /// <summary>
    ///     Incoming diplomatic proposals (where current civ is the target)
    /// </summary>
    public List<DiplomacyInfoResponsePacket.ProposalInfo> IncomingProposals { get; set; } = new();

    /// <summary>
    ///     Outgoing diplomatic proposals (where current civ is the proposer)
    /// </summary>
    public List<DiplomacyInfoResponsePacket.ProposalInfo> OutgoingProposals { get; set; } = new();

    /// <summary>
    ///     Selected civilization ID for proposing a new relationship
    /// </summary>
    public string SelectedCivId { get; set; } = string.Empty;

    /// <summary>
    ///     Selected diplomatic status for the proposal (NAP or Alliance)
    /// </summary>
    public DiplomaticStatus SelectedProposalType { get; set; } = DiplomaticStatus.NonAggressionPact;

    /// <summary>
    ///     Selected duration in days for the proposal (optional)
    /// </summary>
    public int? SelectedDuration { get; set; }

    /// <summary>
    ///     Relationship ID pending break confirmation
    /// </summary>
    public string? ConfirmBreakRelationshipId { get; set; }

    /// <summary>
    ///     Civilization ID pending war declaration confirmation
    /// </summary>
    public string? ConfirmWarCivId { get; set; }

    /// <summary>
    ///     Error message to display to the user
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Whether the UI is currently loading data
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    ///     Timestamp of the last data refresh
    /// </summary>
    public DateTime? LastRefresh { get; set; }

    /// <summary>
    ///     Scroll position for the relationships panel
    /// </summary>
    public float RelationshipsScrollY { get; set; }

    /// <summary>
    ///     Scroll position for the proposals panel
    /// </summary>
    public float ProposalsScrollY { get; set; }

    /// <summary>
    ///     Whether the propose relationship panel is expanded
    /// </summary>
    public bool IsProposeExpanded { get; set; } = true;

    /// <summary>
    ///     Whether the target civilization dropdown is open
    /// </summary>
    public bool IsCivDropdownOpen { get; set; }

    /// <summary>
    ///     Whether the relationship type dropdown is open
    /// </summary>
    public bool IsTypeDropdownOpen { get; set; }

    public void Reset()
    {
        ActiveRelationships.Clear();
        IncomingProposals.Clear();
        OutgoingProposals.Clear();
        SelectedCivId = string.Empty;
        SelectedProposalType = DiplomaticStatus.NonAggressionPact;
        SelectedDuration = null;
        ConfirmBreakRelationshipId = null;
        ConfirmWarCivId = null;
        ErrorMessage = null;
        IsLoading = false;
        LastRefresh = null;
        RelationshipsScrollY = 0f;
        ProposalsScrollY = 0f;
        IsProposeExpanded = true;
        IsCivDropdownOpen = false;
        IsTypeDropdownOpen = false;
    }
}
