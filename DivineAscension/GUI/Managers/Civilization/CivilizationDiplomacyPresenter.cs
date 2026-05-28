using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the diplomacy chapter and the propose-accord page and reduces their
///     events. Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationDiplomacyPresenter(CivilizationStateManager owner)
{
    [ExcludeFromCodeCoverage]
    public void DrawDiplomacy(float x, float y, float width, float height)
    {
        // Auto-refresh: Check if data is stale (> 30 seconds old or null)
        var isStale = owner.State.DiplomacyState.LastRefresh == null ||
                      (DateTime.UtcNow - owner.State.DiplomacyState.LastRefresh.Value).TotalSeconds > 30;

        if (isStale && !owner.State.DiplomacyState.IsLoading && owner.HasCivilization())
        {
            owner.ClientApi.Logger.Debug("[DivineAscension:Diplomacy] Auto-refreshing stale diplomacy data");
            owner.RequestDiplomacyInfo();
        }

        // Get list of available civilizations (exclude player's own civilization)
        var availableCivs = owner.State.BrowseState.AllCivilizations
            .Where(c => c.CivId != owner.CurrentCivilizationId)
            .Select(c => new CivilizationInfo(c.CivId, c.Name))
            .ToList();

        var currentRank = owner.UserPrestigeRank;

        var vm = new DiplomacyTabViewModel(
            x,
            y,
            width,
            height,
            owner.State.DiplomacyState.IsLoading,
            owner.HasCivilization(),
            owner.State.DiplomacyState.ErrorMessage,
            owner.State.DiplomacyState.ActiveRelationships,
            owner.State.DiplomacyState.IncomingProposals,
            owner.State.DiplomacyState.OutgoingProposals,
            availableCivs,
            owner.State.DiplomacyState.SelectedCivId,
            owner.State.DiplomacyState.SelectedProposalType,
            currentRank,
            owner.State.DiplomacyState.ConfirmWarCivId,
            owner.State.DiplomacyState.IsCivDropdownOpen,
            owner.State.DiplomacyState.IsTypeDropdownOpen,
            owner.CurrentCivilizationName,
            owner.State.DiplomacyState.RelationshipsScrollY);

        var drawList = ImGui.GetWindowDrawList();
        var result = DiplomacyTabRenderer.Draw(vm, drawList);

        ProcessEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    public void DrawProposeAccord(float x, float y, float width, float height)
    {
        var selectedType = owner.State.DiplomacyState.SelectedProposalType;

        // Recipient filter depends on the selected manner. War can target any
        // civ we aren't already at war with (existing alliances/NAPs are valid
        // targets — declaring war breaks them on the server). Pacts exclude
        // any standing accord, since you can't sign two with the same realm.
        var existingPartnerIds = owner.State.DiplomacyState.ActiveRelationships
            .Where(r => selectedType == DiplomaticStatus.War
                ? r.Status == DiplomaticStatus.War
                : true)
            .Select(r => r.OtherCivId)
            .ToHashSet();
        var availableCivs = owner.State.BrowseState.AllCivilizations
            .Where(c => c.CivId != owner.CurrentCivilizationId && !existingPartnerIds.Contains(c.CivId))
            .Select(c => new CivilizationInfo(c.CivId, c.Name))
            .ToList();

        var requiredRank = selectedType == DiplomaticStatus.NonAggressionPact
            ? DiplomacyConstants.NonAggressionPactRequiredRank
            : DiplomacyConstants.AllianceRequiredRank;
        var requiredRankName = selectedType == DiplomaticStatus.NonAggressionPact
            ? DiplomacyConstants.NonAggressionPactRankName
            : DiplomacyConstants.AllianceRankName;

        var vm = new ProposeAccordViewModel(
            x, y, width, height,
            owner.HasCivilization(),
            owner.UserIsCivilizationFounder,
            owner.State.DiplomacyState.ErrorMessage,
            availableCivs,
            owner.State.DiplomacyState.SelectedCivId,
            selectedType,
            owner.UserPrestigeRank,
            requiredRank,
            requiredRankName,
            owner.State.DiplomacyState.IsCivDropdownOpen,
            owner.State.DiplomacyState.IsTypeDropdownOpen,
            owner.State.DiplomacyState.ConfirmWarCivId);

        var drawList = ImGui.GetWindowDrawList();
        var result = ProposeAccordRenderer.Draw(vm, drawList);
        ProcessEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<DiplomacyEvent> events)
    {
        // Block diplomacy background interaction behind an open declare-war confirm (#455).
        events = ModalInputGuard.FilterBackground(events);

        foreach (var evt in events)
            switch (evt)
            {
                case DiplomacyEvent.ProposeRelationship pr:
                    if (!string.IsNullOrEmpty(pr.TargetCivId) && !string.IsNullOrEmpty(owner.CurrentCivilizationId))
                    {
                        owner.RequestDiplomacyAction("propose", pr.TargetCivId, "", pr.ProposedStatus.ToString());
                        owner.State.DiplomacyState.SelectedCivId = string.Empty;
                    }

                    break;

                case DiplomacyEvent.AcceptProposal ap:
                    if (!string.IsNullOrEmpty(ap.ProposalId))
                        owner.RequestDiplomacyAction("accept", "", ap.ProposalId);
                    break;

                case DiplomacyEvent.DeclineProposal dp:
                    if (!string.IsNullOrEmpty(dp.ProposalId))
                        owner.RequestDiplomacyAction("decline", "", dp.ProposalId);
                    break;

                case DiplomacyEvent.ScheduleBreak sb:
                    if (!string.IsNullOrEmpty(sb.TargetCivId))
                    {
                        owner.RequestDiplomacyAction("schedulebreak", sb.TargetCivId);
                        owner.State.DiplomacyState.ConfirmBreakRelationshipId = null;
                    }

                    break;

                case DiplomacyEvent.CancelBreak cb:
                    if (!string.IsNullOrEmpty(cb.TargetCivId))
                        owner.RequestDiplomacyAction("cancelbreak", cb.TargetCivId);
                    break;

                case DiplomacyEvent.DeclareWar dw:
                    if (!string.IsNullOrEmpty(dw.TargetCivId))
                    {
                        owner.RequestDiplomacyAction("declarewar", dw.TargetCivId);
                        owner.State.DiplomacyState.ConfirmWarCivId = null;
                    }

                    break;

                case DiplomacyEvent.DeclarePeace dp:
                    if (!string.IsNullOrEmpty(dp.TargetCivId))
                        owner.RequestDiplomacyAction("declarepeace", dp.TargetCivId);
                    break;

                case DiplomacyEvent.SelectCivilization sc:
                    owner.State.DiplomacyState.SelectedCivId = sc.CivId;
                    break;

                case DiplomacyEvent.SelectProposalType spt:
                    owner.State.DiplomacyState.SelectedProposalType = spt.ProposalType;
                    break;

                case DiplomacyEvent.ShowWarConfirmation swc:
                    owner.State.DiplomacyState.ConfirmWarCivId = swc.CivId;
                    break;

                case DiplomacyEvent.CancelWarConfirmation:
                    owner.State.DiplomacyState.ConfirmWarCivId = null;
                    break;

                case DiplomacyEvent.ToggleCivDropdown tcd:
                    owner.State.DiplomacyState.IsCivDropdownOpen = tcd.IsOpen;
                    // Close the other dropdown
                    if (tcd.IsOpen) owner.State.DiplomacyState.IsTypeDropdownOpen = false;
                    break;

                case DiplomacyEvent.ToggleTypeDropdown ttd:
                    owner.State.DiplomacyState.IsTypeDropdownOpen = ttd.IsOpen;
                    // Close the other dropdown
                    if (ttd.IsOpen) owner.State.DiplomacyState.IsCivDropdownOpen = false;
                    break;

                case DiplomacyEvent.DismissError:
                    owner.State.DiplomacyState.ErrorMessage = null;
                    break;

                case DiplomacyEvent.ScrollChanged sc:
                    owner.State.DiplomacyState.RelationshipsScrollY = sc.NewScrollY;
                    break;
            }
    }
}
