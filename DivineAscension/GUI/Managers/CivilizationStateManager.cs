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
using DivineAscension.GUI.Models.Civilization.HolySites;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.Models.Civilization.Invites;
using DivineAscension.GUI.Models.Civilization.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.State.Civilization;
using DivineAscension.GUI.UI.Adapters.Civilizations;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
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

    // UI-only detail adapter (fake or real). Null when not used.
    internal ICivilizationDetailProvider? CivilizationDetailProvider { get; set; }

    private CivilizationTabState State { get; } = new();

    /// <summary>
    ///     Public accessor for diplomacy state (for network client access)
    /// </summary>
    public DiplomacyState DiplomacyState => State.DiplomacyState;

    /// <summary>
    ///     Public accessor for current sub-tab (for network client access)
    /// </summary>
    public CivilizationSubTab CurrentSubTab => State.CurrentSubTab;

    public string CurrentCivilizationId { get; set; } = string.Empty;

    public List<CivilizationInfoResponsePacket.MemberReligion>? CivilizationMemberReligions { get; set; } = new();

    public string CivilizationFounderReligionUID { get; set; } = string.Empty;

    public string CivilizationFounderUID { get; set; } = string.Empty;

    public string CurrentCivilizationName { get; set; } = string.Empty;

    public string CivilizationIcon { get; set; } = string.Empty;

    // Religion state (updated by GuiDialogManager)
    public bool UserHasReligion { get; set; }
    public bool UserIsReligionFounder { get; set; }
    public int UserPrestigeRank { get; set; }
    public bool UserIsCivilizationFounder { get; set; }

    public void Reset()
    {
        State.Reset();
        CurrentCivilizationId = string.Empty;
        CurrentCivilizationName = string.Empty;
        CivilizationFounderReligionUID = string.Empty;
        CivilizationFounderUID = string.Empty;
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
    ///     Configure a UI-only civilization detail provider (fake or real). When set, RequestCivilizationInfo()
    ///     uses it instead of performing a network call for detail views.
    /// </summary>
    internal void UseCivilizationDetailProvider(ICivilizationDetailProvider provider)
    {
        CivilizationDetailProvider = provider;
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
            CivilizationFounderUID = string.Empty;
            UserIsCivilizationFounder = false;
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
                CivilizationFounderUID = string.Empty;
                UserIsCivilizationFounder = false;
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
                CivilizationFounderUID = details.FounderUID;
                UserIsCivilizationFounder = details.IsFounder;
                CivilizationMemberReligions =
                    new List<CivilizationInfoResponsePacket.MemberReligion>(details.MemberReligions ?? []);
                State.InfoState.Info = details;
                // Sync DescriptionText with server description when receiving new info
                State.InfoState.DescriptionText = details.Description ?? string.Empty;
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
                Icon = c.icon,
                Description = c.description
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

        // Adapter short-circuit: if detail provider is configured and we have a specific civId, use it
        if (CivilizationDetailProvider != null && !string.IsNullOrEmpty(civIdOrEmpty))
        {
            var detail = CivilizationDetailProvider.GetCivilizationDetail(civIdOrEmpty);
            if (detail != null)
            {
                // Map CivilizationDetailVM → CivilizationInfoResponsePacket
                var packet = new CivilizationInfoResponsePacket
                {
                    Details = new CivilizationInfoResponsePacket.CivilizationDetails
                    {
                        CivId = detail.CivId,
                        Name = detail.Name,
                        FounderUID = detail.FounderUID,
                        FounderName = detail.FounderName,
                        FounderReligionUID = detail.FounderReligionUID,
                        FounderReligionName = detail.FounderReligionName,
                        MemberReligions = detail.MemberReligions.Select(m =>
                            new CivilizationInfoResponsePacket.MemberReligion
                            {
                                ReligionId = m.ReligionId,
                                ReligionName = m.ReligionName,
                                Domain = m.Domain,
                                FounderUID = m.FounderUID,
                                FounderName = m.FounderName,
                                MemberCount = m.MemberCount,
                                DeityName = m.DeityName
                            }).ToList(),
                        CreatedDate = detail.CreatedDate,
                        Icon = detail.Icon,
                        Description = detail.Description,
                        IsFounder = false // Adapter doesn't track current player
                    }
                };
                UpdateCivilizationState(packet.Details);
                State.DetailState.IsLoading = false;
                return;
            }
        }

        // Default: request from server
        _uiService.RequestCivilizationInfo(civIdOrEmpty);
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband, updateicon, setdescription)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "",
        string icon = "", string description = "")
    {
        // Clear transient action error; some actions will trigger refreshes
        State.LastActionError = null;
        _uiService.RequestCivilizationAction(action, civId, targetId, name, icon, description);
    }

    /// <summary>
    ///     Request diplomacy information for the current civilization
    /// </summary>
    public void RequestDiplomacyInfo()
    {
        if (!HasCivilization())
        {
            State.DiplomacyState.ErrorMessage = "Not in a civilization";
            return;
        }

        State.DiplomacyState.IsLoading = true;
        State.DiplomacyState.ErrorMessage = null;

        _uiService.RequestDiplomacyInfo(CurrentCivilizationId);
        _coreClientApi.Logger.Debug(
            $"[DivineAscension:Diplomacy] Requested diplomacy info for civ: {CurrentCivilizationId}");
    }

    /// <summary>
    ///     Request a diplomacy action (propose, accept, decline, schedulebreak, cancelbreak, declarewar, declarepeace)
    /// </summary>
    internal void RequestDiplomacyAction(string action, string targetCivId = "", string proposalOrRelationshipId = "",
        string proposedStatus = "")
    {
        if (!HasCivilization())
        {
            State.DiplomacyState.ErrorMessage = "Not in a civilization";
            _soundManager.PlayError();
            return;
        }

        State.DiplomacyState.ErrorMessage = null;

        _uiService.RequestDiplomacyAction(action, targetCivId, proposalOrRelationshipId, proposedStatus);
        _coreClientApi.Logger.Debug(
            $"[DivineAscension:Diplomacy] Requested diplomacy action: action={action}, targetCivId={targetCivId}, id={proposalOrRelationshipId}, status={proposedStatus}");

        _soundManager.PlayClick();
    }

    /// <summary>
    ///     Request holy sites for the current civilization
    /// </summary>
    public void RequestCivilizationHolySites()
    {
        if (!HasCivilization())
        {
            State.HolySitesState.ErrorMsg = "Not in a civilization";
            return;
        }

        State.HolySitesState.IsLoading = true;
        State.HolySitesState.ErrorMsg = null;

        // Request all sites - we'll filter client-side
        _uiService.RequestHolySiteList("");
    }

    /// <summary>
    ///     Update holy sites state from network response
    /// </summary>
    public void UpdateHolySiteList(List<DivineAscension.Network.HolySite.HolySiteResponsePacket.HolySiteInfo> allSites)
    {
        State.HolySitesState.AllSites = allSites;
        State.HolySitesState.IsLoading = false;

        // Filter to only member religions and group
        FilterAndGroupHolySites();
    }

    /// <summary>
    ///     Filter sites to member religions and group by religion
    /// </summary>
    private void FilterAndGroupHolySites()
    {
        State.HolySitesState.SitesByReligion.Clear();

        if (CivilizationMemberReligions == null) return;

        var memberReligionUIDs = new HashSet<string>(
            CivilizationMemberReligions.Select(r => r.ReligionId));

        foreach (var site in State.HolySitesState.AllSites)
        {
            if (memberReligionUIDs.Contains(site.ReligionUID))
            {
                if (!State.HolySitesState.SitesByReligion.ContainsKey(site.ReligionUID))
                {
                    State.HolySitesState.SitesByReligion[site.ReligionUID] = new();
                }
                State.HolySitesState.SitesByReligion[site.ReligionUID].Add(site);
            }
        }
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
            CivilizationSubTab.Diplomacy => tabVm.ShowDiplomacyTab,
            CivilizationSubTab.HolySites => tabVm.ShowHolySitesTab,
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
            case CivilizationSubTab.Diplomacy:
                DrawCivilizationDiplomacy(x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.HolySites:
                DrawCivilizationHolySites(x, contentY, width, contentHeight);
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
        var deityNames = DomainHelper.DeityNames;
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
            State.BrowseState.SelectedCivId,
            UserHasReligion,
            HasCivilization(),
            x,
            y,
            width,
            height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationBrowseRenderer.Draw(vm, State.BrowseState.IsDeityFilterOpen, drawList);

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
            details?.Description ?? string.Empty,
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
            civ?.Description ?? string.Empty,
            State.InfoState.DescriptionText,
            civ?.FounderName ?? string.Empty,
            UserIsCivilizationFounder,
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
        // Check for profanity in civilization name
        string? profanityWord = null;
        if (!string.IsNullOrWhiteSpace(State.CreateState.CreateCivName))
        {
            ProfanityFilterService.Instance.ContainsProfanity(State.CreateState.CreateCivName, out profanityWord);
        }

        // Check for profanity in description
        string? profanityWordInDescription = null;
        if (!string.IsNullOrWhiteSpace(State.CreateState.CreateDescription))
        {
            ProfanityFilterService.Instance.ContainsProfanity(State.CreateState.CreateDescription,
                out profanityWordInDescription);
        }

        // Build ViewModel
        var vm = new CivilizationCreateViewModel(
            State.CreateState.CreateCivName,
            State.CreateState.SelectedIcon,
            State.CreateState.CreateDescription,
            State.CreateError,
            UserIsReligionFounder,
            HasCivilization(),
            profanityWord,
            profanityWordInDescription,
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

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationDiplomacy(float x, float y, float width, float height)
    {
        // Auto-refresh: Check if data is stale (> 30 seconds old or null)
        var isStale = State.DiplomacyState.LastRefresh == null ||
                      (DateTime.UtcNow - State.DiplomacyState.LastRefresh.Value).TotalSeconds > 30;

        if (isStale && !State.DiplomacyState.IsLoading && HasCivilization())
        {
            _coreClientApi.Logger.Debug("[DivineAscension:Diplomacy] Auto-refreshing stale diplomacy data");
            RequestDiplomacyInfo();
        }

        // Get list of available civilizations (exclude player's own civilization)
        var availableCivs = State.BrowseState.AllCivilizations
            .Where(c => c.CivId != CurrentCivilizationId)
            .Select(c => new CivilizationInfo(c.CivId, c.Name))
            .ToList();

        // Get current prestige rank from user's religion
        var currentRank = UserPrestigeRank;

        // Build ViewModel
        var vm = new DiplomacyTabViewModel(
            x,
            y,
            width,
            height,
            State.DiplomacyState.IsLoading,
            HasCivilization(),
            State.DiplomacyState.ErrorMessage,
            State.DiplomacyState.ActiveRelationships,
            State.DiplomacyState.IncomingProposals,
            State.DiplomacyState.OutgoingProposals,
            availableCivs,
            State.DiplomacyState.SelectedCivId,
            State.DiplomacyState.SelectedProposalType,
            currentRank,
            State.DiplomacyState.ConfirmWarCivId,
            State.DiplomacyState.IsCivDropdownOpen,
            State.DiplomacyState.IsTypeDropdownOpen);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = DiplomacyTabRenderer.Draw(vm, drawList);

        // Process events
        ProcessDiplomacyEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    private void DrawCivilizationHolySites(float x, float y, float width, float height)
    {
        // Build religion name and domain maps
        var religionNames = CivilizationMemberReligions?
            .ToDictionary(r => r.ReligionId, r => r.ReligionName)
            ?? new Dictionary<string, string>();

        var religionDomains = CivilizationMemberReligions?
            .ToDictionary(r => r.ReligionId, r => r.Domain)
            ?? new Dictionary<string, string>();

        // Build ViewModel
        var vm = new DivineAscension.GUI.Models.Civilization.HolySites.CivilizationHolySitesViewModel(
            State.HolySitesState.SitesByReligion,
            religionNames,
            religionDomains,
            State.HolySitesState.ExpandedReligions,
            State.HolySitesState.IsLoading,
            State.HolySitesState.ErrorMsg,
            State.HolySitesState.ScrollY,
            x, y, width, height);

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationHolySitesRenderer.Draw(vm, drawList);

        // Process events
        ProcessHolySitesEvents(result.Events);
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
                        CivilizationSubTab.Diplomacy => HasCivilization(),
                        CivilizationSubTab.HolySites => HasCivilization(),
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
                        case CivilizationSubTab.Diplomacy:
                            State.DiplomacyState.ErrorMessage = null;
                            RequestDiplomacyInfo();
                            break;
                        case CivilizationSubTab.HolySites:
                            State.HolySitesState.ErrorMsg = null;
                            RequestCivilizationHolySites();
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
                        case CivilizationSubTab.Diplomacy:
                            State.DiplomacyState.ErrorMessage = null;
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
                        case CivilizationSubTab.Diplomacy:
                            RequestDiplomacyInfo();
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

                case BrowseEvent.Selected selected:
                    // Update selection state
                    State.BrowseState.SelectedCivId = selected.CivId;
                    State.BrowseState.BrowseScrollY = selected.ScrollY;

                    // Auto-navigate to detail view
                    State.DetailState.ViewingCivilizationId = selected.CivId;
                    State.DetailState.MemberScrollY = 0f;
                    RequestCivilizationInfo(selected.CivId);
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

                case InfoEvent.DescriptionChanged dc:
                    State.InfoState.DescriptionText = dc.newDescription;
                    break;

                case InfoEvent.SaveDescriptionClicked:
                    if (!string.IsNullOrWhiteSpace(civId))
                    {
                        RequestCivilizationAction("setdescription", civId, "", "", "", State.InfoState.DescriptionText);
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

                case InvitesEvent.DeclineInviteClicked dic:
                    RequestCivilizationAction("decline", "", dic.inviteId);
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

                case CreateEvent.DescriptionChanged dc:
                    State.CreateState.CreateDescription = dc.newDescription;
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
                            State.CreateState.SelectedIcon, State.CreateState.CreateDescription);
                        State.CreateState.CreateCivName = string.Empty;
                        State.CreateState.SelectedIcon = "default";
                        State.CreateState.CreateDescription = string.Empty;
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
                    State.CreateState.CreateDescription = string.Empty;
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

    internal void ProcessDiplomacyEvents(IReadOnlyList<DiplomacyEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case DiplomacyEvent.ProposeRelationship pr:
                    if (!string.IsNullOrEmpty(pr.TargetCivId) && !string.IsNullOrEmpty(CurrentCivilizationId))
                    {
                        RequestDiplomacyAction("propose", pr.TargetCivId, "", pr.ProposedStatus.ToString());
                        State.DiplomacyState.SelectedCivId = string.Empty;
                    }

                    break;

                case DiplomacyEvent.AcceptProposal ap:
                    if (!string.IsNullOrEmpty(ap.ProposalId))
                        RequestDiplomacyAction("accept", "", ap.ProposalId);
                    break;

                case DiplomacyEvent.DeclineProposal dp:
                    if (!string.IsNullOrEmpty(dp.ProposalId))
                        RequestDiplomacyAction("decline", "", dp.ProposalId);
                    break;

                case DiplomacyEvent.ScheduleBreak sb:
                    if (!string.IsNullOrEmpty(sb.TargetCivId))
                    {
                        RequestDiplomacyAction("schedulebreak", sb.TargetCivId);
                        State.DiplomacyState.ConfirmBreakRelationshipId = null;
                    }

                    break;

                case DiplomacyEvent.CancelBreak cb:
                    if (!string.IsNullOrEmpty(cb.TargetCivId))
                        RequestDiplomacyAction("cancelbreak", cb.TargetCivId);
                    break;

                case DiplomacyEvent.DeclareWar dw:
                    if (!string.IsNullOrEmpty(dw.TargetCivId))
                    {
                        RequestDiplomacyAction("declarewar", dw.TargetCivId);
                        State.DiplomacyState.ConfirmWarCivId = null;
                    }

                    break;

                case DiplomacyEvent.DeclarePeace dp:
                    if (!string.IsNullOrEmpty(dp.TargetCivId))
                        RequestDiplomacyAction("declarepeace", dp.TargetCivId);
                    break;

                case DiplomacyEvent.SelectCivilization sc:
                    State.DiplomacyState.SelectedCivId = sc.CivId;
                    break;

                case DiplomacyEvent.SelectProposalType spt:
                    State.DiplomacyState.SelectedProposalType = spt.ProposalType;
                    break;

                case DiplomacyEvent.ShowWarConfirmation swc:
                    State.DiplomacyState.ConfirmWarCivId = swc.CivId;
                    break;

                case DiplomacyEvent.CancelWarConfirmation:
                    State.DiplomacyState.ConfirmWarCivId = null;
                    break;

                case DiplomacyEvent.ToggleCivDropdown tcd:
                    State.DiplomacyState.IsCivDropdownOpen = tcd.IsOpen;
                    // Close the other dropdown
                    if (tcd.IsOpen) State.DiplomacyState.IsTypeDropdownOpen = false;
                    break;

                case DiplomacyEvent.ToggleTypeDropdown ttd:
                    State.DiplomacyState.IsTypeDropdownOpen = ttd.IsOpen;
                    // Close the other dropdown
                    if (ttd.IsOpen) State.DiplomacyState.IsCivDropdownOpen = false;
                    break;

                case DiplomacyEvent.DismissError:
                    State.DiplomacyState.ErrorMessage = null;
                    break;
            }
    }

    private void ProcessHolySitesEvents(IReadOnlyList<HolySitesEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case HolySitesEvent.RefreshClicked:
                    RequestCivilizationHolySites();
                    break;
                case HolySitesEvent.ScrollChanged e:
                    State.HolySitesState.ScrollY = e.NewScrollY;
                    break;
                case HolySitesEvent.ReligionToggled e:
                    // Toggle expanded state
                    if (State.HolySitesState.ExpandedReligions.Contains(e.ReligionUID))
                        State.HolySitesState.ExpandedReligions.Remove(e.ReligionUID);
                    else
                        State.HolySitesState.ExpandedReligions.Add(e.ReligionUID);
                    break;
                case HolySitesEvent.SiteSelected e:
                    // Could open detail view (future enhancement)
                    _coreClientApi.Logger.Debug($"[DivineAscension:HolySites] Site selected: {e.SiteUID}");
                    break;
            }
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