using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Models.Civilization.Browse;
using DivineAscension.GUI.Models.Civilization.Create;
using DivineAscension.GUI.Models.Civilization.Detail;
using DivineAscension.GUI.Models.Civilization.Edit;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.Models.Civilization.Invites;
using DivineAscension.GUI.Models.Civilization.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Adapters.Civilizations;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.Civilization;
using DivineAscension.Systems.Interfaces;
using ImGuiNET;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.Managers;

public class CivilizationStateManager(ICoreClientAPI coreClientApi, IUiService uiService, ISoundManager soundManager)
{
    private readonly ICoreClientAPI _coreClientApi =
        coreClientApi ?? throw new ArgumentNullException(nameof(coreClientApi));

    private readonly ISoundManager
        _soundManager = soundManager ?? throw new ArgumentNullException(nameof(soundManager));

    private readonly IUiService _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

    // UI-only adapter (fake or real). Null when not used.
    internal ICivilizationProvider? CivilizationProvider { get; private set; }

    private CivilizationTabState State { get; } = new();

    public string CurrentCivilizationId { get; set; } = string.Empty;

    public List<CivilizationInfoResponsePacket.MemberReligion>? CivilizationMemberReligions { get; set; } = new();

    public string CivilizationFounderReligionUID { get; set; } = string.Empty;

    public string CurrentCivilizationName { get; set; } = string.Empty;

    public string CivilizationIcon { get; set; } = string.Empty;

    // Religion state (updated by GuiDialogManager)
    public bool UserHasReligion { get; set; }
    public bool UserIsReligionFounder { get; set; }

    public void Reset()
    {
        State.Reset();
        CurrentCivilizationId = string.Empty;
        CurrentCivilizationName = string.Empty;
        CivilizationFounderReligionUID = string.Empty;
        if (CivilizationMemberReligions != null) CivilizationMemberReligions.Clear();
    }


    /// <summary>
    ///     Check if player's religion is in a civilization
    /// </summary>
    public bool HasCivilization()
    {
        return !string.IsNullOrEmpty(CurrentCivilizationId);
    }

    /// <summary>
    ///     Update civilization state from response packet
    /// </summary>
    public void UpdateCivilizationState(CivilizationInfoResponsePacket.CivilizationDetails? details)
    {
        if (details == null)
        {
            // Clear civilization state
            CurrentCivilizationId = string.Empty;
            CurrentCivilizationName = string.Empty;
            CivilizationIcon = string.Empty;
            CivilizationFounderReligionUID = string.Empty;
            CivilizationMemberReligions?.Clear();
            State.InfoState.Info = null;
            State.InviteState.MyInvites.Clear();
            return;
        }

        // Check if this is for a civilization we're viewing (from "View Details")
        if (!string.IsNullOrEmpty(State.DetailState.ViewingCivilizationId) &&
            details.CivId == State.DetailState.ViewingCivilizationId)
        {
            // Update viewing details
            State.DetailState.ViewingCivilizationDetails = details;
        }
        else
        {
            // Update player's own civilization (or just invites if not in a civilization)
            if (string.IsNullOrEmpty(details.CivId))
            {
                // Player has no civilization; only update invites and keep civ info cleared
                CurrentCivilizationId = string.Empty;
                CurrentCivilizationName = string.Empty;
                CivilizationIcon = string.Empty;
                CivilizationFounderReligionUID = string.Empty;
                CivilizationMemberReligions?.Clear();
                State.InfoState.Info = null;
                State.InviteState.MyInvites = new List<CivilizationInfoResponsePacket.PendingInvite>(
                    details.PendingInvites ??
                    []);
            }
            else
            {
                CurrentCivilizationId = details.CivId;
                CurrentCivilizationName = details.Name;
                CivilizationIcon = details.Icon ?? "default";
                CivilizationFounderReligionUID = details.FounderReligionUID;
                CivilizationMemberReligions =
                    new List<CivilizationInfoResponsePacket.MemberReligion>(details.MemberReligions ?? []);
                State.InfoState.Info = details;
                State.InviteState.MyInvites =
                    new List<CivilizationInfoResponsePacket.PendingInvite>(details.PendingInvites ?? []);
            }
        }
    }


    /// <summary>
    ///     Request the list of civilizations from the server (filtered by deity when provided)
    /// </summary>
    public void RequestCivilizationList(string deityFilter = "")
    {
        // Adapter short-circuit: if a UI-only provider is configured, use it instead of network
        if (CivilizationProvider != null)
        {
            State.BrowseState.IsLoading = true;
            State.BrowseState.ErrorMsg = null;

            var items = CivilizationProvider.GetCivilizations();
            var filter = deityFilter?.Trim();

            // Apply deity filter if provided
            if (!string.IsNullOrEmpty(filter))
                items = items.Where(c => c.memberDeities.Any(d =>
                    string.Equals(d, filter, StringComparison.OrdinalIgnoreCase))).ToList();

            // Map adapter VM → Network DTO
            var mapped = items.Select(c => new CivilizationListResponsePacket.CivilizationInfo
            {
                CivId = c.civId,
                Name = c.name,
                FounderUID = c.founderUID,
                FounderReligionUID = c.founderReligionUID,
                MemberCount = c.memberCount,
                MemberDeities = c.memberDeities,
                MemberReligionNames = c.memberReligionNames,
                Icon = c.icon
            }).ToList();

            State.BrowseState.AllCivilizations = mapped;
            State.BrowseState.IsLoading = false;
            State.BrowseState.ErrorMsg = null;
            return;
        }

        // Default: request from server
        State.BrowseState.IsLoading = true;
        State.BrowseState.ErrorMsg = null;
        _uiService.RequestCivilizationList(deityFilter);
    }

    /// <summary>
    ///     Request details for the current civilization (empty string means player religion's civ)
    /// </summary>
    public void RequestCivilizationInfo(string civIdOrEmpty = "")
    {
        // Toggle loading depending on details vs my civ
        if (string.IsNullOrEmpty(civIdOrEmpty))
        {
            State.InfoState.IsLoading = true;
            State.InviteState.IsLoading = true;
            State.InfoState.ErrorMsg = null;
            State.InviteState.ErrorMsg = null;
        }
        else
        {
            State.DetailState.IsLoading = true;
            State.DetailState.ErrorMsg = null;
        }

        _uiService.RequestCivilizationInfo(civIdOrEmpty);
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband, updateicon)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "",
        string icon = "")
    {
        // Clear transient action error; some actions will trigger refreshes
        State.LastActionError = null;
        _uiService.RequestCivilizationAction(action, civId, targetId, name, icon);
    }

    /// <summary>
    ///     Main EDA orchestrator for Civilization tab: builds ViewModels, calls renderers, processes events
    /// </summary>
    internal void DrawCivilizationTab(float x, float y, float width, float height)
    {
        // Build tab ViewModel from state
        var tabVm = new CivilizationTabViewModel(
            State.CurrentSubTab,
            State.LastActionError,
            State.BrowseState.ErrorMsg,
            State.InfoState.ErrorMsg,
            State.InviteState.ErrorMsg,
            !string.IsNullOrEmpty(State.DetailState.ViewingCivilizationId),
            UserHasReligion,
            HasCivilization(),
            x,
            y,
            width,
            height);

        // AUTO-CORRECTION: Ensure active tab is valid for current religion/civilization state
        var isCurrentTabValid = State.CurrentSubTab switch
        {
            CivilizationSubTab.Browse => true, // Always visible
            CivilizationSubTab.Info => tabVm.ShowInfoTab,
            CivilizationSubTab.Invites => tabVm.ShowInvitesTab,
            CivilizationSubTab.Create => tabVm.ShowCreateTab,
            _ => false
        };

        if (!isCurrentTabValid)
        {
            _coreClientApi.Logger.Debug(
                $"[DivineAscension] Auto-switching from {State.CurrentSubTab} to Browse (tab now hidden for HasReligion={UserHasReligion}, HasCivilization={HasCivilization()})");
            State.CurrentSubTab = CivilizationSubTab.Browse;

            // Rebuild ViewModel with corrected tab
            tabVm = new CivilizationTabViewModel(
                State.CurrentSubTab,
                State.LastActionError,
                State.BrowseState.ErrorMsg,
                State.InfoState.ErrorMsg,
                State.InviteState.ErrorMsg,
                !string.IsNullOrEmpty(State.DetailState.ViewingCivilizationId),
                UserHasReligion,
                HasCivilization(),
                x, y, width, height);
        }

        var drawList = ImGui.GetWindowDrawList();
        var tabResult = CivilizationTabRenderer.Draw(tabVm, drawList);

        // Process tab events
        ProcessTabEvents(tabResult.Events);

        // Route to sub-renderers
        var contentY = y + tabResult.RendererHeight;
        var contentHeight = height - tabResult.RendererHeight;

        switch (State.CurrentSubTab)
        {
            case CivilizationSubTab.Browse:
                DrawCivilizationBrowse(x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.Info:
                DrawCivilizationInfo(x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.Invites:
                DrawCivilizationInvites(x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.Create:
                DrawCivilizationCreate(x, contentY, width, contentHeight);
                break;
        }
    }

    #region Sub-Renderer Orchestrators

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationBrowse(float x, float y, float width, float height)
    {
        // Check if viewing details (overlay mode)
        if (!string.IsNullOrEmpty(State.DetailState.ViewingCivilizationId))
        {
            DrawCivilizationDetail(x, y, width, height);
            return;
        }

        // Build deity filters
        var deityNames = DeityHelper.DeityNames;
        var deities = new string[deityNames.Length + 1];
        deities[0] = "All";
        Array.Copy(deityNames, 0, deities, 1, deityNames.Length);

        var effectiveFilter = string.IsNullOrEmpty(State.BrowseState.DeityFilter)
            ? "All"
            : State.BrowseState.DeityFilter;

        // Build ViewModel
        var vm = new CivilizationBrowseViewModel(
            deities,
            effectiveFilter,
            State.BrowseState.AllCivilizations,
            State.BrowseState.IsLoading,
            State.BrowseState.BrowseScrollY,
            State.BrowseState.IsDeityFilterOpen,
            UserHasReligion,
            HasCivilization(),
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationBrowseRenderer.Draw(vm, drawList);

        // Process events
        ProcessBrowseEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationDetail(float x, float y, float width, float height)
    {
        var details = State.DetailState.ViewingCivilizationDetails;

        // Build ViewModel
        var vm = new CivilizationDetailViewModel(
            State.DetailState.IsLoading,
            State.DetailState.ViewingCivilizationId ?? string.Empty,
            details?.Name ?? string.Empty,
            details?.FounderName ?? string.Empty,
            details?.FounderReligionName ?? string.Empty,
            details?.MemberReligions ?? new List<CivilizationInfoResponsePacket.MemberReligion>(),
            details?.CreatedDate ?? DateTime.MinValue,
            State.DetailState.MemberScrollY,
            !HasCivilization() && (details?.MemberReligions?.Count ?? 0) < 4,
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationDetailRenderer.Draw(vm, drawList);

        // Process events
        ProcessDetailEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationInfo(float x, float y, float width, float height)
    {
        var civ = State.InfoState.Info;

        // Build ViewModel
        var vm = new CivilizationInfoViewModel(
            State.InfoState.IsLoading,
            civ != null,
            civ?.CivId ?? string.Empty,
            civ?.Name ?? string.Empty,
            civ?.Icon ?? "default",
            civ?.FounderName ?? string.Empty,
            !string.IsNullOrEmpty(CivilizationFounderReligionUID) &&
            civ?.FounderReligionUID == CivilizationFounderReligionUID,
            civ?.MemberReligions ?? new List<CivilizationInfoResponsePacket.MemberReligion>(),
            State.InfoState.InviteReligionName ?? string.Empty,
            State.ShowDisbandConfirm,
            State.KickConfirmReligionId,
            State.InfoState.ScrollY,
            State.InfoState.MemberScrollY,
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationInfoRenderer.Draw(vm, drawList, civ?.PendingInvites,
            CivilizationFounderReligionUID, civ?.CreatedDate ?? DateTime.MinValue);

        // Process events
        ProcessInfoEvents(result.Events);

        // Draw edit dialog overlay if open
        if (State.EditState.IsOpen) DrawCivilizationEditDialog(x, y, width, height);
    }

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationEditDialog(float x, float y, float width, float height)
    {
        var civ = State.InfoState.Info;
        if (civ == null) return;

        // Build ViewModel
        var vm = new CivilizationEditViewModel(
            civ.CivId,
            civ.Name,
            civ.Icon,
            State.EditState.EditingIcon,
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationEditRenderer.Draw(vm, drawList);

        // Process events
        ProcessEditEvents(result.Events);
    }

    private void DrawCivilizationInvites(float x, float y, float width, float height)
    {
        // Build ViewModel
        var vm = new CivilizationInvitesViewModel(
            State.InviteState.MyInvites,
            State.InviteState.IsLoading,
            State.InviteState.InvitesScrollY,
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationInvitesRenderer.Draw(vm, drawList);

        // Process events
        ProcessInvitesEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationCreate(float x, float y, float width, float height)
    {
        // Build ViewModel
        var vm = new CivilizationCreateViewModel(
            State.CreateState.CreateCivName,
            State.CreateState.SelectedIcon,
            State.CreateError,
            UserIsReligionFounder,
            HasCivilization(),
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationCreateRenderer.Draw(vm, drawList);

        // Process events
        ProcessCreateEvents(result.Events);
    }

    #endregion

    #region Event Processors

    internal void ProcessTabEvents(IReadOnlyList<SubTabEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case SubTabEvent.TabChanged tc:
                    // Validate that the requested tab is visible for current religion/civilization state
                    var isTabVisible = tc.NewSubTab switch
                    {
                        CivilizationSubTab.Browse => true,
                        CivilizationSubTab.Info => HasCivilization(),
                        CivilizationSubTab.Invites => UserHasReligion && !HasCivilization(),
                        CivilizationSubTab.Create => UserHasReligion && !HasCivilization(),
                        _ => false
                    };

                    if (!isTabVisible)
                    {
                        _coreClientApi.Logger.Warning(
                            $"[DivineAscension] Attempted to switch to hidden tab {tc.NewSubTab} (HasReligion={UserHasReligion}, HasCivilization={HasCivilization()}). Ignoring.");
                        break; // Don't process the tab change
                    }

                    State.CurrentSubTab = tc.NewSubTab;
                    // Clear transient action error on tab change
                    State.LastActionError = null;

                    // Clear context-specific errors when switching into a tab
                    switch (tc.NewSubTab)
                    {
                        case CivilizationSubTab.Browse:
                            if (State.DetailState.ViewingCivilizationId != null)
                                State.DetailState.ErrorMsg = null;
                            else
                                State.BrowseState.ErrorMsg = null;
                            break;
                        case CivilizationSubTab.Info:
                            State.InfoState.ErrorMsg = null;
                            RequestCivilizationInfo();
                            break;
                        case CivilizationSubTab.Invites:
                            State.InviteState.ErrorMsg = null;
                            RequestCivilizationInfo();
                            break;
                    }

                    break;

                case SubTabEvent.DismissActionError:
                    State.LastActionError = null;
                    break;

                case SubTabEvent.DismissContextError dce:
                    switch (dce.SubTab)
                    {
                        case CivilizationSubTab.Browse:
                            if (State.DetailState.ViewingCivilizationId != null)
                                State.DetailState.ErrorMsg = null;
                            else
                                State.BrowseState.ErrorMsg = null;
                            break;
                        case CivilizationSubTab.Info:
                            State.InfoState.ErrorMsg = null;
                            break;
                        case CivilizationSubTab.Invites:
                            State.InviteState.ErrorMsg = null;
                            break;
                    }

                    break;

                case SubTabEvent.RetryRequested rr:
                    switch (rr.SubTab)
                    {
                        case CivilizationSubTab.Browse:
                            if (State.DetailState.ViewingCivilizationId != null)
                                RequestCivilizationInfo(State.DetailState.ViewingCivilizationId);
                            else
                                RequestCivilizationList(State.BrowseState.DeityFilter);
                            break;
                        case CivilizationSubTab.Info:
                        case CivilizationSubTab.Invites:
                            RequestCivilizationInfo();
                            break;
                    }

                    break;
            }
    }

    internal void ProcessBrowseEvents(IReadOnlyList<BrowseEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case BrowseEvent.DeityFilterChanged dfc:
                    State.BrowseState.DeityFilter = dfc.newFilter == "All" ? string.Empty : dfc.newFilter;
                    RequestCivilizationList(State.BrowseState.DeityFilter);
                    _soundManager.PlayClick();
                    break;

                case BrowseEvent.ScrollChanged sc:
                    State.BrowseState.BrowseScrollY = sc.y;
                    break;

                case BrowseEvent.ViewDetailedsClicked vdc:
                    State.DetailState.ViewingCivilizationId = vdc.civId;
                    State.DetailState.MemberScrollY = 0f;
                    RequestCivilizationInfo(vdc.civId);
                    break;

                case BrowseEvent.RefreshClicked:
                    RequestCivilizationList(State.BrowseState.DeityFilter);
                    break;

                case BrowseEvent.DeityDropDownToggled ddt:
                    State.BrowseState.IsDeityFilterOpen = ddt.isOpen;
                    break;
            }
    }

    internal void ProcessDetailEvents(IReadOnlyList<DetailEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case DetailEvent.BackToBrowseClicked:
                    State.DetailState.ViewingCivilizationId = null;
                    State.DetailState.ViewingCivilizationDetails = null;
                    break;

                case DetailEvent.MemberScrollChanged msc:
                    State.DetailState.MemberScrollY = msc.NewScrollY;
                    break;

                case DetailEvent.RequestToJoinClicked rtjc:
                    // Note: Request to join is not implemented in the current system
                    // Would need to add this action to the server
                    break;
            }
    }

    internal void ProcessInfoEvents(IReadOnlyList<InfoEvent> events)
    {
        var civ = State.InfoState.Info;
        var civId = civ?.CivId ?? string.Empty;

        foreach (var evt in events)
            switch (evt)
            {
                case InfoEvent.ScrollChanged sc:
                    State.InfoState.ScrollY = sc.y;
                    break;

                case InfoEvent.MemberScrollChanged msc:
                    State.InfoState.MemberScrollY = msc.y;
                    break;

                case InfoEvent.InviteReligionNameChanged irnc:
                    State.InfoState.InviteReligionName = irnc.text;
                    break;

                case InfoEvent.InviteReligionClicked irc:
                    if (!string.IsNullOrWhiteSpace(irc.religionName) && !string.IsNullOrWhiteSpace(civId))
                    {
                        RequestCivilizationAction("invite", civId, irc.religionName);
                        State.InfoState.InviteReligionName = string.Empty;
                    }

                    break;

                case InfoEvent.LeaveClicked:
                    if (!string.IsNullOrWhiteSpace(civId))
                        RequestCivilizationAction("leave");
                    break;

                case InfoEvent.EditIconClicked:
                    State.EditState.IsOpen = true;
                    State.EditState.CivId = civId;
                    State.EditState.EditingIcon = civ?.Icon ?? "default";
                    _soundManager.PlayClick();
                    break;

                case InfoEvent.DisbandOpened:
                    State.ShowDisbandConfirm = true;
                    break;

                case InfoEvent.DisbandCancel:
                    State.ShowDisbandConfirm = false;
                    break;

                case InfoEvent.DisbandConfirmed:
                    if (!string.IsNullOrWhiteSpace(civId))
                    {
                        RequestCivilizationAction("disband", civId);
                        State.ShowDisbandConfirm = false;
                    }

                    break;

                case InfoEvent.KickOpen ko:
                    State.KickConfirmReligionId = ko.religionId;
                    break;

                case InfoEvent.KickCancel:
                    State.KickConfirmReligionId = null;
                    break;

                case InfoEvent.KickConfirm kc:
                    if (!string.IsNullOrWhiteSpace(civId) && !string.IsNullOrWhiteSpace(State.KickConfirmReligionId))
                    {
                        RequestCivilizationAction("kick", civId, State.KickConfirmReligionId);
                        State.KickConfirmReligionId = null;
                    }

                    break;
            }
    }

    internal void ProcessInvitesEvents(IReadOnlyList<InvitesEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case InvitesEvent.ScrollChanged sc:
                    State.InviteState.InvitesScrollY = sc.y;
                    break;

                case InvitesEvent.AcceptInviteClicked aic:
                    RequestCivilizationAction("accept", "", aic.inviteId);
                    break;

                case InvitesEvent.AcceptInviteDeclined aid:
                    // Decline is not implemented yet
                    _coreClientApi.ShowChatMessage("Decline functionality coming soon!");
                    break;
            }
    }

    internal void ProcessCreateEvents(IReadOnlyList<CreateEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case CreateEvent.NameChanged nc:
                    State.CreateState.CreateCivName = nc.newName;
                    break;

                case CreateEvent.IconSelected iconSelected:
                    State.CreateState.SelectedIcon = iconSelected.icon;
                    _soundManager.PlayClick();
                    break;

                case CreateEvent.SubmitClicked:
                    if (!string.IsNullOrWhiteSpace(State.CreateState.CreateCivName) &&
                        State.CreateState.CreateCivName.Length >= 3 &&
                        State.CreateState.CreateCivName.Length <= 32)
                    {
                        RequestCivilizationAction("create", "", "", State.CreateState.CreateCivName,
                            State.CreateState.SelectedIcon);
                        State.CreateState.CreateCivName = string.Empty;
                        State.CreateState.SelectedIcon = "default";
                    }
                    else
                    {
                        _coreClientApi.ShowChatMessage("Civilization name must be 3-32 characters.");
                        _soundManager.PlayError();
                    }

                    break;

                case CreateEvent.ClearClicked:
                    State.CreateState.CreateCivName = string.Empty;
                    State.CreateState.SelectedIcon = "default";
                    break;
            }
    }

    internal void ProcessEditEvents(IReadOnlyList<EditEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case EditEvent.IconSelected iconSelected:
                    State.EditState.EditingIcon = iconSelected.icon;
                    _soundManager.PlayClick();
                    break;

                case EditEvent.SubmitClicked:
                    if (!string.IsNullOrWhiteSpace(State.EditState.CivId) &&
                        !string.IsNullOrWhiteSpace(State.EditState.EditingIcon))
                    {
                        RequestCivilizationAction("updateicon", State.EditState.CivId, "", "",
                            State.EditState.EditingIcon);
                        State.EditState.IsOpen = false;
                        State.EditState.Reset();
                    }

                    break;

                case EditEvent.CancelClicked:
                    State.EditState.IsOpen = false;
                    State.EditState.Reset();
                    _soundManager.PlayClick();
                    break;
            }
    }

    #endregion

    #region Network Event Handlers

    public void OnCivilizationListReceived(CivilizationListResponsePacket packet)
    {
        State.BrowseState.AllCivilizations = packet.Civilizations;
        State.BrowseState.IsLoading = false;
        State.BrowseState.ErrorMsg = null;
    }

    public void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        UpdateCivilizationState(packet.Details);

        // Update loading flags based on context
        if (State.DetailState.ViewingCivilizationId == packet.Details?.CivId)
        {
            State.DetailState.IsLoading = false;
        }
        else
        {
            State.InfoState.IsLoading = false;
            State.InviteState.IsLoading = false;
        }
    }

    public void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
    {
        if (packet.Success)
        {
            _soundManager.PlayClick();

            // AUTO-CORRECT TAB: Switch to appropriate tab based on action
            var currentTab = State.CurrentSubTab;
            switch (packet.Action?.ToLowerInvariant())
            {
                case "leave" or "disband":
                    // Leaving civilization - switch to Browse
                    if (currentTab is CivilizationSubTab.Info)
                    {
                        _coreClientApi.Logger.Debug(
                            $"[DivineAscension] Switching tab from {currentTab} to Browse after leaving civilization");
                        State.CurrentSubTab = CivilizationSubTab.Browse;
                    }
                    break;

                case "join" or "create" or "accept":
                    // Joining/creating civilization - switch to Info to show new civilization
                    if (currentTab is CivilizationSubTab.Invites or CivilizationSubTab.Create)
                    {
                        _coreClientApi.Logger.Debug(
                            $"[DivineAscension] Switching tab from {currentTab} to Info after joining civilization");
                        State.CurrentSubTab = CivilizationSubTab.Info;
                    }
                    break;
            }

            RequestCivilizationList(State.BrowseState.DeityFilter);
            RequestCivilizationInfo();
        }
        else
        {
            _soundManager.PlayError();
            State.LastActionError = packet.Message;
        }
    }

    #endregion

    #region Civilization Provider (Adapter Pattern)

    /// <summary>
    ///     Configure a UI-only civilization data provider (fake or real). When set,
    ///     RequestCivilizationList() uses it instead of performing a network call.
    /// </summary>
    internal void UseCivilizationProvider(ICivilizationProvider provider)
    {
        CivilizationProvider = provider;
    }

    /// <summary>
    ///     Refresh the current civilization list from the configured provider (if any).
    /// </summary>
    public void RefreshCivilizationsFromProvider()
    {
        CivilizationProvider?.Refresh();
        RequestCivilizationList(State.BrowseState.DeityFilter);
    }

    #endregion
}