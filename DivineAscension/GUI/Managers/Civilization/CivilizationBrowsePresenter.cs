using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Browse;
using DivineAscension.GUI.Models.Civilization.Detail;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the civilization browse table (and the detail overlay it opens) and
///     reduces their events. Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationBrowsePresenter(CivilizationStateManager owner)
{
    [ExcludeFromCodeCoverage]
    public void Draw(float x, float y, float width, float height)
    {
        // Check if viewing details (overlay mode)
        if (!string.IsNullOrEmpty(owner.State.DetailState.ViewingCivilizationId))
        {
            DrawDetail(x, y, width, height);
            return;
        }

        // Build deity filters
        var deityNames = DomainHelper.DeityNames;
        var deities = new string[deityNames.Length + 1];
        deities[0] = "All";
        Array.Copy(deityNames, 0, deities, 1, deityNames.Length);

        var effectiveFilter = string.IsNullOrEmpty(owner.State.BrowseState.DeityFilter)
            ? "All"
            : owner.State.BrowseState.DeityFilter;

        // Accord-status sub-index (#324). Bucket: All / Allies (Alliance+NAP) /
        // Neutral / AtWar. Filter applied client-side against StatusToViewer
        // already carried on each CivilizationInfo.
        var accordFilters = new[] { "All", "Allies", "Neutral", "AtWar" };
        var currentAccord = string.IsNullOrEmpty(owner.State.BrowseState.AccordFilter)
            ? "All"
            : owner.State.BrowseState.AccordFilter;
        var filteredCivs = ApplyBrowseFilters(
            owner.State.BrowseState.AllCivilizations, currentAccord, owner.State.BrowseState.SearchText);

        var vm = new CivilizationBrowseViewModel(
            deities,
            effectiveFilter,
            accordFilters,
            currentAccord,
            owner.State.BrowseState.SearchText,
            filteredCivs,
            owner.State.BrowseState.IsLoading,
            owner.State.BrowseState.BrowseScrollY,
            owner.State.BrowseState.SelectedCivId,
            owner.UserHasReligion,
            owner.HasCivilization(),
            x,
            y,
            width,
            height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationBrowseRenderer.Draw(vm, drawList);

        ProcessEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    private void DrawDetail(float x, float y, float width, float height)
    {
        var details = owner.State.DetailState.ViewingCivilizationDetails;

        var vm = new CivilizationDetailViewModel(
            owner.State.DetailState.IsLoading,
            owner.State.DetailState.ViewingCivilizationId ?? string.Empty,
            details?.Name ?? string.Empty,
            details?.FounderName ?? string.Empty,
            details?.FounderReligionName ?? string.Empty,
            details?.Rank ?? 0,
            details?.Ethos ?? 0,
            details?.FounderEpithet ?? string.Empty,
            details?.MemberReligions ?? new List<CivilizationInfoResponsePacket.MemberReligion>(),
            details?.CreatedDate ?? DateTime.MinValue,
            details?.Description ?? string.Empty,
            details?.CapitalName ?? string.Empty,
            details?.CapitalHolySiteId ?? string.Empty,
            details?.Bonuses ?? new CivilizationBonusesDto(),
            owner.State.DetailState.MemberScrollY,
            !owner.HasCivilization() && (details?.MemberReligions?.Count ?? 0) < 4,
            x,
            y,
            width,
            height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationDetailRenderer.Draw(vm, drawList);

        ProcessDetailEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<BrowseEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case BrowseEvent.DeityFilterChanged dfc:
                    owner.State.BrowseState.DeityFilter = dfc.newFilter == "All" ? string.Empty : dfc.newFilter;
                    owner.RequestCivilizationList(owner.State.BrowseState.DeityFilter);
                    owner.SoundManager.PlayClick();
                    break;

                case BrowseEvent.AccordFilterChanged afc:
                    // Client-side filter — StatusToViewer rides on the existing list packet.
                    owner.State.BrowseState.AccordFilter = afc.newFilter == "All" ? string.Empty : afc.newFilter;
                    owner.State.BrowseState.BrowseScrollY = 0f;
                    owner.SoundManager.PlayClick();
                    break;

                case BrowseEvent.SearchTextChanged stc:
                    if (owner.State.BrowseState.SearchText != stc.newText)
                    {
                        owner.State.BrowseState.SearchText = stc.newText;
                        owner.State.BrowseState.BrowseScrollY = 0f;
                    }
                    break;

                case BrowseEvent.ScrollChanged sc:
                    owner.State.BrowseState.BrowseScrollY = sc.y;
                    break;

                case BrowseEvent.ViewDetailedsClicked vdc:
                    owner.State.DetailState.ViewingCivilizationId = vdc.civId;
                    owner.State.DetailState.MemberScrollY = 0f;
                    owner.RequestCivilizationInfo(vdc.civId);
                    break;

                case BrowseEvent.Selected selected:
                    owner.State.BrowseState.SelectedCivId = selected.CivId;
                    owner.State.BrowseState.BrowseScrollY = selected.ScrollY;

                    // Auto-navigate to detail view
                    owner.State.DetailState.ViewingCivilizationId = selected.CivId;
                    owner.State.DetailState.MemberScrollY = 0f;
                    owner.RequestCivilizationInfo(selected.CivId);
                    break;

                case BrowseEvent.RefreshClicked:
                    owner.RequestCivilizationList(owner.State.BrowseState.DeityFilter);
                    break;

                case BrowseEvent.DeityDropDownToggled ddt:
                    owner.State.BrowseState.IsDeityFilterOpen = ddt.isOpen;
                    break;
            }
    }

    public void ProcessDetailEvents(IReadOnlyList<DetailEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case DetailEvent.BackToBrowseClicked:
                    owner.State.DetailState.ViewingCivilizationId = null;
                    owner.State.DetailState.ViewingCivilizationDetails = null;
                    break;

                case DetailEvent.MemberScrollChanged msc:
                    owner.State.DetailState.MemberScrollY = msc.NewScrollY;
                    break;

                case DetailEvent.RequestToJoinClicked rtjc:
                    // Note: Request to join is not implemented in the current system
                    // Would need to add this action to the server
                    break;
            }
    }

    private static IReadOnlyList<CivilizationListResponsePacket.CivilizationInfo> ApplyBrowseFilters(
        IReadOnlyList<CivilizationListResponsePacket.CivilizationInfo> source,
        string accord,
        string searchText)
    {
        var accordActive = !(accord == "All" || string.IsNullOrEmpty(accord));
        var query = (searchText ?? string.Empty).Trim();
        var searchActive = query.Length > 0;
        if (!accordActive && !searchActive) return source;
        if (source.Count == 0) return source;

        // Status codes mirror DiplomaticStatus: 0=Neutral, 1=NAP, 2=Alliance, 3=War, -1=self/no-civ.
        var result = new List<CivilizationListResponsePacket.CivilizationInfo>(source.Count);
        foreach (var civ in source)
        {
            if (accordActive)
            {
                var s = civ.StatusToViewer;
                var keep = accord switch
                {
                    "Allies" => s == 1 || s == 2,
                    "Neutral" => s == 0,
                    "AtWar" => s == 3,
                    _ => true
                };
                if (!keep) continue;
            }
            if (searchActive &&
                civ.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            result.Add(civ);
        }
        return result;
    }
}
