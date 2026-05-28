using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Models.Civilization.Browse;
using DivineAscension.GUI.Models.Civilization.Chronicle;
using DivineAscension.GUI.Models.Civilization.Create;
using DivineAscension.GUI.Models.Civilization.Detail;
using DivineAscension.GUI.Models.Civilization.Edit;
using DivineAscension.GUI.Models.Civilization.HolySites;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.Models.Civilization.Leaderboard;
using DivineAscension.GUI.Models.Civilization.Milestones;
using DivineAscension.GUI.Models.Civilization.Invites;
using DivineAscension.GUI.Models.Civilization.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.State.Civilization;
using DivineAscension.GUI.UI.Adapters.Civilizations;
using DivineAscension.GUI.UI.Adapters.Diplomacy;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Renderers.HolySites;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
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

    // Internal accessors so the per-chapter presenters can reach shared services
    // without each taking its own constructor dependencies.
    internal ISoundManager SoundManager => _soundManager;
    internal ICoreClientAPI ClientApi => _coreClientApi;
    internal IUiService UiService => _uiService;

    // Per-chapter presenters: each owns the Draw + event-reduction logic for one
    // sidebar destination. Lazily built (field initializers can't reference `this`).
    private Civilization.CivilizationBrowsePresenter? _browse;
    private Civilization.CivilizationInfoPresenter? _info;
    private Civilization.CivilizationInvitesPresenter? _invites;
    private Civilization.CivilizationCreatePresenter? _create;
    private Civilization.CivilizationDiplomacyPresenter? _diplomacy;
    private Civilization.CivilizationHolySitesPresenter? _holySites;
    private Civilization.CivilizationMilestonesPresenter? _milestones;
    private Civilization.CivilizationChroniclePresenter? _chronicle;
    private Civilization.CivilizationLeaderboardPresenter? _leaderboard;

    private Civilization.CivilizationBrowsePresenter Browse => _browse ??= new(this);
    private Civilization.CivilizationInfoPresenter Info => _info ??= new(this);
    private Civilization.CivilizationInvitesPresenter Invites => _invites ??= new(this);
    private Civilization.CivilizationCreatePresenter Create => _create ??= new(this);
    private Civilization.CivilizationDiplomacyPresenter Diplomacy => _diplomacy ??= new(this);
    private Civilization.CivilizationHolySitesPresenter HolySites => _holySites ??= new(this);
    private Civilization.CivilizationMilestonesPresenter Milestones => _milestones ??= new(this);
    private Civilization.CivilizationChroniclePresenter Chronicle => _chronicle ??= new(this);
    private Civilization.CivilizationLeaderboardPresenter Leaderboard => _leaderboard ??= new(this);

    // UI-only adapter (fake or real). Null when not used.
    internal ICivilizationProvider? CivilizationProvider { get; private set; }

    // UI-only detail adapter (fake or real). Null when not used.
    internal ICivilizationDetailProvider? CivilizationDetailProvider { get; set; }

    // UI-only diplomacy adapter (fake in dev). Null in release.
    internal IDiplomacyProvider? DiplomacyProvider { get; private set; }

    // UI-only leaderboard adapter (fake in dev). Null in release.
    internal ILeaderboardProvider? LeaderboardProvider { get; private set; }

    public CivilizationTabState State { get; } = new();

    /// <summary>
    ///     Public accessor for diplomacy state (for network client access)
    /// </summary>
    public DiplomacyState DiplomacyState => State.DiplomacyState;

    /// <summary>
    ///     Public accessor for invite state (used by sidebar nav badge counts).
    /// </summary>
    public InviteState InviteState => State.InviteState;

    public string CurrentCivilizationId { get; set; } = string.Empty;

    public List<CivilizationInfoResponsePacket.MemberReligion>? CivilizationMemberReligions { get; set; } = new();

    public string CivilizationFounderReligionUID { get; set; } = string.Empty;

    public string CivilizationFounderUID { get; set; } = string.Empty;

    public string CurrentCivilizationName { get; set; } = string.Empty;

    public string CivilizationIcon { get; set; } = string.Empty;

    public int CivilizationRank { get; set; }

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
        CivilizationIcon = string.Empty;
        CivilizationRank = 0;
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
    ///     Configure a UI-only diplomacy provider (fake in dev). When set,
    ///     <see cref="RequestDiplomacyInfo" /> populates DiplomacyState from
    ///     the provider instead of dispatching a packet to the server.
    /// </summary>
    internal void UseDiplomacyProvider(IDiplomacyProvider provider)
    {
        DiplomacyProvider = provider;
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
            CivilizationRank = 0;
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
                CivilizationRank = 0;
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
                CivilizationRank = details.Rank;
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
        string icon = "", string description = "", int ethos = -1,
        string capitalName = "", string holySiteId = "")
    {
        // Clear transient action error; some actions will trigger refreshes
        State.LastActionError = null;
        _uiService.RequestCivilizationAction(action, civId, targetId, name, icon, description, ethos, capitalName,
            holySiteId);
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

        // Adapter short-circuit: dev fake provider populates state in-process
        // so the Accords / Propose pages render seeded data without a server.
        if (DiplomacyProvider != null)
        {
            var roster = State.BrowseState.AllCivilizations
                .Where(c => c.CivId != CurrentCivilizationId)
                .Select(c => (c.CivId, c.Name))
                .ToList();
            var packet = DiplomacyProvider.GetDiplomacyInfo(CurrentCivilizationId, roster);
            State.DiplomacyState.ActiveRelationships = packet.Relationships;
            State.DiplomacyState.IncomingProposals = packet.IncomingProposals;
            State.DiplomacyState.OutgoingProposals = packet.OutgoingProposals;
            State.DiplomacyState.IsLoading = false;
            State.DiplomacyState.LastRefresh = DateTime.UtcNow;
            return;
        }

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
            State.HolySitesState.Browse.ErrorMsg = "Not in a civilization";
            return;
        }

        State.HolySitesState.Browse.IsLoading = true;
        State.HolySitesState.Browse.ErrorMsg = null;

        // Request all sites - we'll filter client-side
        _uiService.RequestHolySiteList("");
    }

    /// <summary>
    ///     Update holy sites state from network response
    /// </summary>
    public void UpdateHolySiteList(List<DivineAscension.Network.HolySite.HolySiteResponsePacket.HolySiteInfo> allSites)
    {
        State.HolySitesState.Browse.AllSites = allSites;
        State.HolySitesState.Browse.IsLoading = false;

        // Filter to only member religions and group
        FilterAndGroupHolySites();
    }

    /// <summary>
    ///     Update holy site detail state from network response
    /// </summary>
    public void UpdateHolySiteDetail(DivineAscension.Network.HolySite.HolySiteResponsePacket.HolySiteDetailInfo detailInfo)
    {
        State.HolySitesState.Detail.ViewingSiteDetails = detailInfo;
        State.HolySitesState.Detail.IsLoading = false;
        State.HolySitesState.Detail.ErrorMsg = null;
    }

    /// <summary>
    ///     Request milestone progress for the current civilization
    /// </summary>
    public void RequestMilestoneProgress()
    {
        if (!HasCivilization())
        {
            State.MilestoneState.ErrorMsg = "Not in a civilization";
            return;
        }

        State.MilestoneState.IsLoading = true;
        State.MilestoneState.ErrorMsg = null;

        _uiService.RequestMilestoneProgress(CurrentCivilizationId);
    }

    /// <summary>
    ///     Update milestone state from network response
    /// </summary>
    public void UpdateMilestoneProgress(DivineAscension.Network.MilestoneProgressResponsePacket packet)
    {
        State.MilestoneState.UpdateFromPacket(packet);
    }

    /// <summary>
    ///     Request the Standing of Realms leaderboard. Available to all players,
    ///     including those with no civilization.
    /// </summary>
    public void RequestLeaderboard()
    {
        State.LeaderboardState.IsLoading = true;
        State.LeaderboardState.ErrorMsg = null;

        // Adapter short-circuit: dev fake provider feeds the chapter in-process,
        // so the boards render (with glyphs/bars) without a server. In Release the
        // provider is null and the request falls through to the network.
        if (LeaderboardProvider != null)
        {
            State.LeaderboardState.UpdateFromPacket(BuildLeaderboardPacket(LeaderboardProvider.GetLeaderboards()));
            return;
        }

        _uiService.RequestLeaderboard();
    }

    /// <summary>
    ///     Map adapter board VMs → the network response packet so the fake
    ///     provider and the real server source feed the renderer identically.
    /// </summary>
    private static LeaderboardResponsePacket BuildLeaderboardPacket(IReadOnlyList<LeaderboardBoardVM> boards)
    {
        var packetBoards = boards.Select(b => new LeaderboardResponsePacket.Board
        {
            Metric = (int)b.board,
            ViewerPosition = b.viewerPosition,
            Entries = b.entries.Select(e => new LeaderboardResponsePacket.LeaderboardEntry
            {
                Position = e.position,
                CivId = e.civId,
                Name = e.name,
                TierLabel = e.tierLabel,
                Score = (int)e.score,
                Ethos = (int)e.ethos
            }).ToList()
        }).ToList();

        return new LeaderboardResponsePacket(packetBoards)
        {
            TotalRealms = boards.FirstOrDefault()?.totalCount ?? 0
        };
    }

    /// <summary>
    ///     Update leaderboard state from network response
    /// </summary>
    public void UpdateLeaderboard(DivineAscension.Network.Civilization.LeaderboardResponsePacket packet)
    {
        State.LeaderboardState.UpdateFromPacket(packet);
    }

    /// <summary>
    ///     Handle successful holy site update - exit editing mode and refresh detail view
    /// </summary>
    public void OnHolySiteUpdateSuccess(string siteUID)
    {
        // Exit editing mode
        State.HolySitesState.Detail.IsEditingName = false;
        State.HolySitesState.Detail.IsEditingDescription = false;
        State.HolySitesState.Detail.EditingNameValue = null;
        State.HolySitesState.Detail.EditingDescriptionValue = null;

        // Re-request detail info to refresh the view with updated data
        if (!string.IsNullOrEmpty(siteUID))
        {
            _uiService.RequestHolySiteDetail(siteUID);
        }
    }

    /// <summary>
    ///     Filter sites to member religions and group by religion
    /// </summary>
    private void FilterAndGroupHolySites()
    {
        State.HolySitesState.Browse.SitesByReligion.Clear();

        if (CivilizationMemberReligions == null) return;

        var memberReligionUIDs = new HashSet<string>(
            CivilizationMemberReligions.Select(r => r.ReligionId));

        foreach (var site in State.HolySitesState.Browse.AllSites)
        {
            if (memberReligionUIDs.Contains(site.ReligionUID))
            {
                if (!State.HolySitesState.Browse.SitesByReligion.ContainsKey(site.ReligionUID))
                {
                    State.HolySitesState.Browse.SitesByReligion[site.ReligionUID] = new();
                }
                State.HolySitesState.Browse.SitesByReligion[site.ReligionUID].Add(site);
            }
        }
    }

    /// <summary>
    ///     Main EDA orchestrator for Civilization tab: builds ViewModels, calls
    ///     renderers, processes events. Nav state is owned by the sidebar;
    ///     <paramref name="nav"/> is one of the Civilization* values from
    ///     <see cref="SidebarNavId"/>.
    /// </summary>
    internal void DrawCivilizationTab(SidebarNavId nav, float x, float y, float width, float height)
    {
        var tabVm = new CivilizationTabViewModel(
            nav,
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

        var drawList = ImGui.GetWindowDrawList();
        var tabResult = CivilizationTabRenderer.Draw(tabVm, drawList);

        ProcessTabEvents(tabResult.Events);

        // Route to sub-renderers
        var contentY = y + tabResult.RendererHeight;
        var contentHeight = height - tabResult.RendererHeight;

        switch (nav)
        {
            case SidebarNavId.CivilizationBrowse:
                Browse.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationInfo:
                Info.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationInvites:
                Invites.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationCreate:
                Create.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationDiplomacy:
                Diplomacy.DrawDiplomacy(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationProposeAccord:
                Diplomacy.DrawProposeAccord(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationHolySites:
                HolySites.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationMilestones:
                Milestones.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationChronicle:
                Chronicle.Draw(x, contentY, width, contentHeight);
                break;
            case SidebarNavId.CivilizationLeaderboard:
                Leaderboard.Draw(x, contentY, width, contentHeight);
                break;
        }
    }

    #region Event Processors

    internal void ProcessTabEvents(IReadOnlyList<SubTabEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case SubTabEvent.DismissActionError:
                    State.LastActionError = null;
                    break;

                case SubTabEvent.DismissContextError dce:
                    switch (dce.Nav)
                    {
                        case SidebarNavId.CivilizationBrowse:
                            if (State.DetailState.ViewingCivilizationId != null)
                                State.DetailState.ErrorMsg = null;
                            else
                                State.BrowseState.ErrorMsg = null;
                            break;
                        case SidebarNavId.CivilizationInfo:
                            State.InfoState.ErrorMsg = null;
                            break;
                        case SidebarNavId.CivilizationInvites:
                            State.InviteState.ErrorMsg = null;
                            break;
                        case SidebarNavId.CivilizationDiplomacy:
                            State.DiplomacyState.ErrorMessage = null;
                            break;
                    }

                    break;

                case SubTabEvent.RetryRequested rr:
                    switch (rr.Nav)
                    {
                        case SidebarNavId.CivilizationBrowse:
                            if (State.DetailState.ViewingCivilizationId != null)
                                RequestCivilizationInfo(State.DetailState.ViewingCivilizationId);
                            else
                                RequestCivilizationList(State.BrowseState.DeityFilter);
                            break;
                        case SidebarNavId.CivilizationInfo:
                        case SidebarNavId.CivilizationInvites:
                            RequestCivilizationInfo();
                            break;
                        case SidebarNavId.CivilizationDiplomacy:
                            RequestDiplomacyInfo();
                            break;
                    }

                    break;
            }
    }

    internal void ProcessBrowseEvents(IReadOnlyList<BrowseEvent> events) => Browse.ProcessEvents(events);

    internal void ProcessDetailEvents(IReadOnlyList<DetailEvent> events) => Browse.ProcessDetailEvents(events);

    internal void ProcessInfoEvents(IReadOnlyList<InfoEvent> events) => Info.ProcessEvents(events);

    internal void ProcessInvitesEvents(IReadOnlyList<InvitesEvent> events) => Invites.ProcessEvents(events);

    internal void ProcessCreateEvents(IReadOnlyList<CreateEvent> events) => Create.ProcessEvents(events);

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
            _soundManager.PlaySuccess();

            // Nav redirect after join/leave/etc. is handled at the dialog level
            // (GuiDialogHandlers.OnCivilizationActionCompleted) so it can update
            // the sidebar nav. This method handles only data refreshes.

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

    /// <summary>
    ///     Configure a UI-only leaderboard data provider (fake in dev). When set,
    ///     the leaderboard draw path reads from it instead of the network. In Release
    ///     it stays null and the real server/network branch (slice 1) is used.
    /// </summary>
    internal void UseLeaderboardProvider(ILeaderboardProvider provider)
    {
        LeaderboardProvider = provider;
    }

    /// <summary>
    ///     Refresh the leaderboard from the configured provider (if any).
    /// </summary>
    internal void RefreshLeaderboardFromProvider()
    {
        LeaderboardProvider?.Refresh();
        RequestLeaderboard();
    }

    /// <summary>
    ///     Source the Standing of Realms boards for the leaderboard chapter. The
    ///     adapter seam: the dev fake provides seeded data; in Release the provider
    ///     is null and slice 1's network-backed source plugs into the else branch.
    /// </summary>
    internal IReadOnlyList<LeaderboardBoardVM> GetLeaderboards()
    {
        return LeaderboardProvider != null
            ? LeaderboardProvider.GetLeaderboards()
            : Array.Empty<LeaderboardBoardVM>();
    }

    #endregion
}