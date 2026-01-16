using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Models.Religion.Activity;
using DivineAscension.GUI.Models.Religion.Browse;
using DivineAscension.GUI.Models.Religion.Create;
using DivineAscension.GUI.Models.Religion.Detail;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.GUI.Models.Religion.Roles;
using DivineAscension.GUI.Models.Religion.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.State.Religion;
using DivineAscension.GUI.UI.Adapters.ReligionMembers;
using DivineAscension.GUI.UI.Adapters.Religions;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Religion;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using ImGuiNET;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.Managers;

public class ReligionStateManager : IReligionStateManager
{
    private readonly ICoreClientAPI _coreClientApi;
    private readonly ISoundManager _soundManager;
    private readonly IUiService _uiService;

    public ReligionStateManager(ICoreClientAPI coreClientApi, IUiService uiService, ISoundManager soundManager)
    {
        _coreClientApi = coreClientApi ?? throw new ArgumentNullException(nameof(coreClientApi));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        _soundManager = soundManager ?? throw new ArgumentNullException(nameof(soundManager));
    }

    // UI-only adapters (fake or real). Null when not used.
    internal IReligionMemberProvider? MembersProvider { get; set; }
    internal IReligionProvider? ReligionsProvider { get; private set; }
    internal IReligionDetailProvider? ReligionDetailProvider { get; private set; }

    public ReligionTabState State { get; } = new();
    public string? CurrentReligionUID { get; set; }
    public DeityDomain CurrentReligionDomain { get; set; }
    public string? CurrentDeityName { get; set; }
    public string? CurrentReligionName { get; set; }
    public int ReligionMemberCount { get; set; }
    public string? PlayerRoleInReligion { get; set; }
    public int CurrentFavorRank { get; set; }
    public int CurrentPrestigeRank { get; set; }
    public int CurrentFavor { get; set; }
    public int CurrentPrestige { get; set; }
    public int TotalFavorEarned { get; set; }

    // Available domains (synced from server)
    private string[] _availableDomains = { "Craft", "Wild", "Conquest", "Harvest", "Stone" };

    // Config thresholds (synced from server)
    public int DiscipleThreshold { get; set; } = 500;
    public int ZealotThreshold { get; set; } = 2000;
    public int ChampionThreshold { get; set; } = 5000;
    public int AvatarThreshold { get; set; } = 10000;
    public int EstablishedThreshold { get; set; } = 2500;
    public int RenownedThreshold { get; set; } = 10000;
    public int LegendaryThreshold { get; set; } = 25000;
    public int MythicThreshold { get; set; } = 50000;

    public void Initialize(string? id, DeityDomain domain, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        CurrentReligionUID = id;
        CurrentReligionDomain = domain;
        CurrentReligionName = religionName;
        CurrentFavorRank = favorRank;
        CurrentPrestigeRank = prestigeRank;
    }

    /// <summary>
    ///     Set available domains from server
    /// </summary>
    public void SetAvailableDomains(List<string> domains)
    {
        if (domains.Count > 0)
        {
            _availableDomains = domains.ToArray();
            _coreClientApi.Logger.Debug($"[DivineAscension] Updated available domains: {string.Join(", ", _availableDomains)}");
        }
    }

    public void Reset()
    {
        CurrentReligionUID = null;
        CurrentReligionDomain = DeityDomain.None;
        CurrentDeityName = null;
        CurrentReligionName = null;
        ReligionMemberCount = 0;
        PlayerRoleInReligion = null;

        // Clear religion tab state
        State.Reset();
    }


    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(CurrentReligionUID) && CurrentReligionDomain != DeityDomain.None;
    }


    public PlayerFavorProgress GetPlayerFavorProgress()
    {
        return new PlayerFavorProgress
        {
            CurrentFavor = TotalFavorEarned,
            RequiredFavor = RankRequirements.GetRequiredFavorForNextRank(
                CurrentFavorRank,
                DiscipleThreshold,
                ZealotThreshold,
                ChampionThreshold,
                AvatarThreshold),
            CurrentRank = CurrentFavorRank,
            NextRank = CurrentFavorRank + 1,
            IsMaxRank = CurrentFavorRank >= 4
        };
    }

    public ReligionPrestigeProgress GetReligionPrestigeProgress()
    {
        return new ReligionPrestigeProgress
        {
            CurrentPrestige = CurrentPrestige,
            RequiredPrestige = RankRequirements.GetRequiredPrestigeForNextRank(
                CurrentPrestigeRank,
                EstablishedThreshold,
                RenownedThreshold,
                LegendaryThreshold,
                MythicThreshold),
            CurrentRank = CurrentPrestigeRank,
            NextRank = CurrentPrestigeRank + 1,
            IsMaxRank = CurrentPrestigeRank >= 4
        };
    }

    public void RequestReligionList(string? deityFilter = "")
    {
        // Adapter short-circuit: if a UI-only provider is configured, use it instead of network
        if (ReligionsProvider != null)
        {
            State.BrowseState.IsBrowseLoading = true;
            State.ErrorState.BrowseError = null;

            var items = ReligionsProvider.GetReligions();
            var filter = deityFilter?.Trim();

            // Apply simple deity filter if provided (case-insensitive)
            if (!string.IsNullOrEmpty(filter))
            {
                items = new List<ReligionVM>(items)
                    .FindAll(r => string.Equals(r.deity, filter, StringComparison.OrdinalIgnoreCase));
            }

            // Map adapter VM → Network DTO used by UI state
            var mapped = new List<ReligionListResponsePacket.ReligionInfo>(items.Count);
            foreach (var r in items)
            {
                mapped.Add(new ReligionListResponsePacket.ReligionInfo
                {
                    ReligionUID = r.religionUID,
                    ReligionName = r.religionName,
                    Domain = r.deity,
                    DeityName = r.deityName,
                    MemberCount = r.memberCount,
                    Prestige = r.prestige,
                    PrestigeRank = r.prestigeRank,
                    IsPublic = r.isPublic,
                    FounderUID = r.founderUID,
                    Description = r.description
                });
            }

            UpdateReligionList(mapped);
            return;
        }

        // Default: request from server
        State.BrowseState.IsBrowseLoading = true;
        State.ErrorState.BrowseError = null;
        if (deityFilter != null) _uiService.RequestReligionList(deityFilter);
    }

    public void RequestPlayerReligionInfo()
    {
        State.InfoState.Loading = true;
        State.InvitesState.Loading = true; // also load the invites list for players without a religion
        State.ErrorState.InfoError = null;
        _uiService.RequestPlayerReligionInfo();
    }

    public void RequestReligionAction(string action, string religionId = "", string targetPlayerId = "")
    {
        // Clear transient action error
        State.ErrorState.LastActionError = null;
        _uiService.RequestReligionAction(action, religionId, targetPlayerId);
    }

    /// <summary>
    ///     Request to edit the current religion description
    /// </summary>
    public void RequestEditReligionDescription(string id, string description)
    {
        // Clear transient action error
        State.ErrorState.LastActionError = null;
        _uiService.RequestEditDescription(id, description);
    }

    /// <summary>
    ///     Update religion list from server response
    /// </summary>
    public void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions)
    {
        State.BrowseState.AllReligions = religions;
        State.BrowseState.IsBrowseLoading = false;
        State.ErrorState.BrowseError = null;
    }

    /// <summary>
    ///     Update player religion info from server response
    /// </summary>
    public void UpdatePlayerReligionInfo(PlayerReligionInfoResponsePacket? info)
    {
        State.InfoState.MyReligionInfo = info;
        State.InfoState.Description = info?.Description ?? string.Empty;
        State.InfoState.Loading = false;
        // Update invites (shown when the player has no religion)
        State.InvitesState.MyInvites = info?.PendingInvites != null
            ? [..info.PendingInvites]
            : new List<PlayerReligionInfoResponsePacket.ReligionInviteInfo>();
        State.InvitesState.Loading = false;
        State.ErrorState.InfoError = null;
    }

    /// <summary>
    /// Draws the religion invites tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    public void DrawReligionInvites(float x, float y, float width, float height)
    {
        // Build view model from state
        var viewModel = new ReligionInvitesViewModel(
            invites: ConvertToInviteData(State.InvitesState.MyInvites),
            isLoading: State.InvitesState.Loading,
            scrollY: State.InvitesState.InvitesScrollY,
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionInvitesRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessInvitesEvents(result.Events);
    }

    /// <summary>
    /// Draws the religion create tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    public void DrawReligionCreate(float x, float y, float width, float height)
    {
        // Check for profanity in religion name
        string? religionNameProfanityWord = null;
        if (!string.IsNullOrWhiteSpace(State.CreateState.Name))
        {
            ProfanityFilterService.Instance.ContainsProfanity(State.CreateState.Name, out religionNameProfanityWord);
        }

        // Check for profanity in deity name
        string? deityNameProfanityWord = null;
        if (!string.IsNullOrWhiteSpace(State.CreateState.DeityName))
        {
            ProfanityFilterService.Instance.ContainsProfanity(State.CreateState.DeityName, out deityNameProfanityWord);
        }

        // Build view model from state
        var viewModel = new ReligionCreateViewModel(
            religionName: State.CreateState.Name,
            domain: State.CreateState.Domain,
            deityName: State.CreateState.DeityName,
            isPublic: State.CreateState.IsPublic,
            availableDomains: _availableDomains,
            errorMessage: State.ErrorState.CreateError,
            religionNameProfanityWord: religionNameProfanityWord,
            deityNameProfanityWord: deityNameProfanityWord,
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionCreateRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessCreateEvents(result.Events);
    }

    internal void RequestReligionRoles()
    {
        State.ErrorState.RolesError = null;
        State.RolesState.Loading = true;
        _uiService.RequestReligionRoles(CurrentReligionUID ?? string.Empty);
    }

    /// <summary>
    ///     Configure a UI-only religion data provider (fake or real). When set, RequestReligionList()
    ///     uses it instead of performing a network call.
    /// </summary>
    internal void UseReligionProvider(IReligionProvider provider)
    {
        ReligionsProvider = provider;
    }

    /// <summary>
    ///     Configure a UI-only religion detail provider (fake or real). When set, RequestReligionDetail()
    ///     uses it instead of performing a network call.
    /// </summary>
    internal void UseReligionDetailProvider(IReligionDetailProvider provider)
    {
        ReligionDetailProvider = provider;
    }

    /// <summary>
    ///     Refresh the current religion list from the configured provider (if any).
    /// </summary>
    public void RefreshReligionsFromProvider()
    {
        ReligionsProvider?.Refresh();
        RequestReligionList(State.BrowseState.DeityFilter);
    }

    /// <summary>
    ///     Draws the religion browse tab using the refactored renderer (State → ViewModel → Renderer → Events → State)
    /// </summary>
    public void DrawReligionBrowse(float x, float y, float width, float height)
    {
        // Check if viewing detail (conditional rendering pattern)
        if (!string.IsNullOrEmpty(State.BrowseState.DetailState.ViewingReligionUID))
        {
            DrawReligionDetail(x, y, width, height);
            return;
        }

        // Map State → ViewModel
        var deityFilters = new[] { "All" }.Concat(_availableDomains).ToArray();
        var effectiveFilter =
            string.IsNullOrEmpty(State.BrowseState.DeityFilter) ? "All" : State.BrowseState.DeityFilter;

        var viewModel = new ReligionBrowseViewModel(
            deityFilters,
            effectiveFilter,
            State.BrowseState.AllReligions,
            State.BrowseState.IsBrowseLoading,
            State.BrowseState.BrowseScrollY,
            State.BrowseState.SelectedReligionUID,
            HasReligion(),
            State.BrowseState.IsDeityDropdownOpen,
            x,
            y,
            width,
            height);

        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionBrowseRenderer.Draw(viewModel, drawList);

        // Process events (Events → State + side effects)
        ProcessBrowseEvents(result.Events);

        // Tooltip as side effect (not part of pure renderer output, but uses returned hover info)
        if (result.HoveredReligion != null)
        {
            var mousePos = ImGui.GetMousePos();
            ReligionListRenderer.DrawTooltip(result.HoveredReligion, mousePos.X, mousePos.Y, width, height);
        }
    }

    /// <summary>
    ///     Draws the religion detail view
    /// </summary>
    private void DrawReligionDetail(float x, float y, float width, float height)
    {
        var details = State.BrowseState.DetailState.ViewingReligionDetails;
        var canJoin = !HasReligion();

        var vm = new ReligionDetailViewModel(
            State.BrowseState.DetailState.IsLoading,
            State.BrowseState.DetailState.ViewingReligionUID ?? string.Empty,
            details?.ReligionName ?? string.Empty,
            details?.Domain ?? string.Empty,
            details?.DeityName ?? string.Empty,
            details?.PrestigeRank ?? string.Empty,
            details?.Prestige ?? 0,
            details?.IsPublic ?? true,
            details?.Description ?? string.Empty,
            details?.Members ?? new List<ReligionDetailResponsePacket.MemberInfo>(),
            State.BrowseState.DetailState.MemberScrollY,
            canJoin,
            x, y, width, height
        );

        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionDetailRenderer.Draw(vm, drawList);
        ProcessDetailEvents(result.Events);
    }

    /// <summary>
    ///     Process events from detail renderer
    /// </summary>
    private void ProcessDetailEvents(IReadOnlyList<DetailEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case DetailEvent.BackToBrowseClicked:
                    State.BrowseState.DetailState.Reset();
                    _soundManager.PlayClick();
                    break;

                case DetailEvent.MemberScrollChanged msc:
                    State.BrowseState.DetailState.MemberScrollY = msc.NewScrollY;
                    break;

                case DetailEvent.JoinClicked jc:
                    if (!string.IsNullOrEmpty(jc.ReligionUID))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("join", jc.ReligionUID);
                    }

                    break;
            }
        }
    }

    /// <summary>
    ///     Request detailed information about a religion
    /// </summary>
    private void RequestReligionDetail(string religionUID)
    {
        State.BrowseState.DetailState.IsLoading = true;
        State.BrowseState.DetailState.ViewingReligionUID = religionUID;

        // Adapter short-circuit: if a UI-only provider is configured, use it
        if (ReligionDetailProvider != null)
        {
            var detail = ReligionDetailProvider.GetReligionDetail(religionUID);
            if (detail != null)
            {
                // Map ReligionDetailVM → ReligionDetailResponsePacket
                var packet = new ReligionDetailResponsePacket
                {
                    ReligionUID = detail.ReligionUID,
                    ReligionName = detail.ReligionName,
                    Domain = detail.Deity,
                    DeityName = detail.DeityName,
                    Description = detail.Description,
                    Prestige = detail.Prestige,
                    PrestigeRank = detail.PrestigeRank,
                    IsPublic = detail.IsPublic,
                    FounderUID = detail.FounderUID,
                    FounderName = detail.FounderName,
                    Members = detail.Members.Select(m => new ReligionDetailResponsePacket.MemberInfo
                    {
                        PlayerUID = m.PlayerUID,
                        PlayerName = m.PlayerName,
                        FavorRank = m.FavorRank,
                        Favor = m.Favor
                    }).ToList()
                };
                UpdateReligionDetail(packet);
                return;
            }
        }

        // Default: request from server
        _uiService.RequestReligionDetail(religionUID);
    }

    /// <summary>
    ///     Update religion detail state from network response
    /// </summary>
    public void UpdateReligionDetail(ReligionDetailResponsePacket packet)
    {
        State.BrowseState.DetailState.ViewingReligionDetails = packet;
        State.BrowseState.DetailState.IsLoading = false;
    }

    /// <summary>
    /// Draws the religion info tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    internal void DrawReligionInfo(float x, float y, float width, float height)
    {
        // Build view model from state
        var religion = State.InfoState.MyReligionInfo;
        var prestigeProgress = GetReligionPrestigeProgress();

        var viewModel = new ReligionInfoViewModel(
            isLoading: State.InfoState.Loading,
            hasReligion: religion != null && religion.HasReligion,
            religionUID: religion?.ReligionUID ?? string.Empty,
            religionName: religion?.ReligionName ?? string.Empty,
            deity: religion?.Domain ?? string.Empty,
            deityName: religion?.DeityName ?? string.Empty,
            founderUID: religion?.FounderUID ?? string.Empty,
            founderName: religion?.FounderName ?? string.Empty,
            // todo: just send the player id
            currentPlayerUID: _coreClientApi.World.Player?.PlayerUID ?? string.Empty,
            isFounder: religion?.IsFounder ?? false,
            description: religion?.Description,
            members: religion?.Members ?? new List<PlayerReligionInfoResponsePacket.MemberInfo>(),
            bannedPlayers: religion?.BannedPlayers,
            prestige: prestigeProgress.CurrentPrestige,
            prestigeRank: prestigeProgress.CurrentRank.ToString(),
            isPublic: religion?.IsPublic ?? true,
            descriptionText: State.InfoState.Description ?? religion?.Description ?? string.Empty,
            invitePlayerName: State.InfoState.InvitePlayerName ?? string.Empty,
            showDisbandConfirm: State.InfoState.ShowDisbandConfirm,
            kickConfirmPlayerUID: State.InfoState.KickConfirmPlayerUID,
            kickConfirmPlayerName: State.InfoState.KickConfirmPlayerName,
            banConfirmPlayerUID: State.InfoState.BanConfirmPlayerUID,
            banConfirmPlayerName: State.InfoState.BanConfirmPlayerName,
            isEditingDeityName: State.InfoState.IsEditingDeityName,
            editDeityNameValue: State.InfoState.EditDeityNameValue,
            isSavingDeityName: State.InfoState.IsSavingDeityName,
            deityNameError: State.InfoState.DeityNameError,
            x: x, y: y, width: width, height: height,
            scrollY: State.InfoState.MyReligionScrollY,
            memberScrollY: State.InfoState.MemberScrollY,
            banListScrollY: State.InfoState.BanListScrollY
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionInfoRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessInfoEvents(result.Events);
    }

    /// <summary>
    /// Draws the Religion tab header + error banner via pure renderer and routes to active sub-tab.
    /// This is the EDA orchestration point: builds the tab ViewModel, calls renderer, handles events, then draws sub-tab.
    /// </summary>
    public void DrawReligionTab(float x, float y, float width, float height)
    {
        // Build view model from state
        var tabVm = new ReligionTabViewModel(
            currentSubTab: State.CurrentSubTab,
            errorState: State.ErrorState,
            hasReligion: HasReligion(),
            x: x,
            y: y,
            width: width,
            height: height);

        // AUTO-CORRECTION: Ensure active tab is valid for current religion state
        var isCurrentTabValid = State.CurrentSubTab switch
        {
            SubTab.Browse => true, // Always visible
            SubTab.Info => tabVm.ShowInfoTab,
            SubTab.Activity => tabVm.ShowActivityTab,
            SubTab.Roles => tabVm.ShowRolesTab,
            SubTab.Invites => tabVm.ShowInvitesTab,
            SubTab.Create => tabVm.ShowCreateTab,
            _ => false
        };

        if (!isCurrentTabValid)
        {
            _coreClientApi.Logger.Debug(
                $"[DivineAscension] Auto-switching from {State.CurrentSubTab} to Browse (tab now hidden for HasReligion={HasReligion()})");
            State.CurrentSubTab = SubTab.Browse;

            // Rebuild ViewModel with corrected tab
            tabVm = new ReligionTabViewModel(
                currentSubTab: State.CurrentSubTab,
                errorState: State.ErrorState,
                hasReligion: HasReligion(),
                x: x, y: y, width: width, height: height);
        }

        var drawList = ImGui.GetWindowDrawList();
        var tabResult = ReligionTabRenderer.Draw(tabVm, drawList, _coreClientApi);

        // Handle emitted events
        foreach (var ev in tabResult.Events)
        {
            switch (ev)
            {
                case SubTabEvent.TabChanged(var sub):
                    // Validate that the requested tab is visible for current religion state
                    var isTabVisible = sub switch
                    {
                        SubTab.Browse => true,
                        SubTab.Info => HasReligion(),
                        SubTab.Activity => HasReligion(),
                        SubTab.Roles => HasReligion(),
                        SubTab.Invites => !HasReligion(),
                        SubTab.Create => !HasReligion(),
                        _ => false
                    };

                    if (!isTabVisible)
                    {
                        _coreClientApi.Logger.Warning(
                            $"[DivineAscension] Attempted to switch to hidden tab {sub} (HasReligion={HasReligion()}). Ignoring.");
                        break; // Don't process the tab change
                    }

                    State.CurrentSubTab = sub;
                    // Clear transient action error on tab change
                    State.ErrorState.LastActionError = null;
                    // Clear context-specific errors and possibly trigger loads
                    switch (sub)
                    {
                        case SubTab.Browse:
                            RequestReligionList(State.BrowseState.DeityFilter);
                            State.ErrorState.BrowseError = null;
                            break;
                        case SubTab.Info:
                            RequestPlayerReligionInfo();
                            State.ErrorState.InfoError = null;
                            break;
                        case SubTab.Activity:
                            State.ErrorState.ActivityError = null;
                            break;
                        case SubTab.Invites:
                            State.InvitesState.InvitesError = null;
                            State.InvitesState.Loading = true;
                            RequestPlayerReligionInfo();
                            break;
                        case SubTab.Create:
                            State.ErrorState.CreateError = null;
                            RequestPlayerReligionInfo();
                            break;
                        case SubTab.Roles:
                            RequestReligionRoles();
                            break;
                    }

                    break;
                case SubTabEvent.DismissActionError:
                    State.ErrorState.LastActionError = null;
                    break;
                case SubTabEvent.DismissContextError(var subTab):
                    switch (subTab)
                    {
                        case SubTab.Browse:
                            State.ErrorState.BrowseError = null;
                            break;
                        case SubTab.Info:
                            State.ErrorState.InfoError = null;
                            break;
                        case SubTab.Create:
                            State.ErrorState.CreateError = null;
                            break;
                    }

                    break;
                case SubTabEvent.RetryRequested(var subTab):
                    switch (subTab)
                    {
                        case SubTab.Browse:
                            RequestReligionList(State.BrowseState.DeityFilter);
                            break;
                        case SubTab.Info:
                            RequestPlayerReligionInfo();
                            break;
                    }

                    break;
            }
        }

        // Route to sub-renderers
        var contentY = y + tabResult.RenderedHeight;
        var contentHeight = height - tabResult.RenderedHeight;

        switch (State.CurrentSubTab)
        {
            case SubTab.Browse:
                DrawReligionBrowse(x, contentY, width, contentHeight);
                break;
            case SubTab.Info:
                DrawReligionInfo(x, contentY, width, contentHeight);
                break;
            case SubTab.Activity:
                DrawReligionActivity(x, contentY, width, contentHeight);
                break;
            case SubTab.Invites:
                DrawReligionInvites(x, contentY, width, contentHeight);
                break;
            case SubTab.Create:
                DrawReligionCreate(x, contentY, width, contentHeight);
                break;
            case SubTab.Roles:
                DrawReligionRoles(x, contentY, width, contentHeight);
                break;
        }
    }

    /// <summary>
    ///     Temporary orchestration wrapper to draw the Blessings tab via renderer. This mirrors how Religion tab is handled
    ///     and will be expanded to full EDA-style event processing if needed.
    /// </summary>
    private void DrawReligionActivity(float x, float contentY, float width, float contentHeight)
    {
        // Request activity log on first load
        if (State.ActivityState.LastRefresh == DateTime.MinValue && !State.ActivityState.IsLoading)
        {
            _coreClientApi.Logger.Debug(
                $"[ReligionStateManager] First load detected for Activity tab, requesting activity log");
            RequestActivityLog();
        }

        var vm = new ReligionActivityViewModel(
            x,
            contentY,
            width,
            contentHeight,
            State.ActivityState.ActivityEntries,
            State.ActivityState.ActivityScrollY,
            State.ActivityState.IsLoading,
            State.ActivityState.ErrorMessage
        );

        var result = ReligionActivityRenderer.Draw(vm);
        ProcessActivityEvents(result.Events);
    }

    private void ProcessActivityEvents(IReadOnlyList<ActivityEvent>? resultEvents)
    {
        if (resultEvents == null || resultEvents.Count == 0) return;

        foreach (var ev in resultEvents)
        {
            switch (ev)
            {
                case ActivityEvent.ScrollChanged e:
                    State.ActivityState.ActivityScrollY = e.NewScrollY;
                    break;

                case ActivityEvent.RefreshRequested:
                    RequestActivityLog();
                    break;

                default:
                    break;
            }
        }
    }

    private void RequestActivityLog()
    {
        _coreClientApi.Logger.Debug(
            $"[ReligionStateManager] RequestActivityLog called, CurrentReligionUID: {CurrentReligionUID ?? "null"}");

        if (string.IsNullOrEmpty(CurrentReligionUID))
        {
            _coreClientApi.Logger.Warning(
                "[ReligionStateManager] Cannot request activity log: CurrentReligionUID is null or empty");
            return;
        }

        _coreClientApi.Logger.Debug(
            $"[ReligionStateManager] Requesting activity log for religion: {CurrentReligionUID}");
        State.ActivityState.IsLoading = true;
        _uiService.RequestActivityLog(CurrentReligionUID, 50);
    }

    /// <summary>
    /// Process events emitted by the ReligionInfoRenderer (My Religion tab)
    /// Maps pure UI intents to state updates and side effects (network requests, sounds, etc.).
    /// </summary>
    private void ProcessInfoEvents(IReadOnlyList<InfoEvent>? events)
    {
        if (events == null || events.Count == 0) return;

        // Cache commonly used references
        var info = State.InfoState.MyReligionInfo;
        var religionId = info?.ReligionUID ?? string.Empty;

        foreach (var ev in events)
        {
            switch (ev)
            {
                // Scrolling
                case InfoEvent.ScrollChanged s:
                    State.InfoState.MyReligionScrollY = s.NewScrollY;
                    break;
                case InfoEvent.MemberScrollChanged ms:
                    State.InfoState.MemberScrollY = ms.NewScrollY;
                    break;
                case InfoEvent.BanListScrollChanged bs:
                    State.InfoState.BanListScrollY = bs.NewScrollY;
                    break;

                // Description edit/save
                case InfoEvent.DescriptionChanged dc:
                    State.InfoState.Description = dc.Text;
                    break;
                case InfoEvent.SaveDescriptionClicked sd:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestEditReligionDescription(religionId, sd.Text);
                        _soundManager.PlayClick();
                        // Optimistically update local model so UI reflects change immediately
                        if (info != null)
                        {
                            info.Description = sd.Text;
                        }

                        State.InfoState.Description = sd.Text;
                    }

                    break;

                // Invite flow
                case InfoEvent.InviteNameChanged inc:
                    State.InfoState.InvitePlayerName = inc.Text;
                    break;
                case InfoEvent.InviteClicked ic:
                    if (!string.IsNullOrWhiteSpace(ic.PlayerName) && !string.IsNullOrWhiteSpace(religionId))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("invite", religionId, ic.PlayerName);
                        State.InfoState.InvitePlayerName = string.Empty;
                    }

                    break;

                // Membership actions
                case InfoEvent.LeaveClicked:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("leave", religionId);
                    }

                    break;

                // Disband flow
                case InfoEvent.DisbandOpen:
                    State.InfoState.ShowDisbandConfirm = true;
                    break;
                case InfoEvent.DisbandCancel:
                    State.InfoState.ShowDisbandConfirm = false;
                    break;
                case InfoEvent.DisbandConfirm:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("disband", religionId);
                    }

                    State.InfoState.ShowDisbandConfirm = false;
                    break;

                // Kick flow
                case InfoEvent.KickOpen ko:
                    State.InfoState.KickConfirmPlayerUID = ko.PlayerUID;
                    State.InfoState.KickConfirmPlayerName = ko.PlayerName;
                    break;
                case InfoEvent.KickCancel:
                    State.InfoState.KickConfirmPlayerUID = null;
                    State.InfoState.KickConfirmPlayerName = null;
                    break;
                case InfoEvent.KickConfirm kc:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("kick", religionId, kc.PlayerUID);
                    }

                    State.InfoState.KickConfirmPlayerUID = null;
                    State.InfoState.KickConfirmPlayerName = null;
                    break;

                // Ban flow
                case InfoEvent.BanOpen bo:
                    State.InfoState.BanConfirmPlayerUID = bo.PlayerUID;
                    State.InfoState.BanConfirmPlayerName = bo.PlayerName;
                    break;
                case InfoEvent.BanCancel:
                    State.InfoState.BanConfirmPlayerUID = null;
                    State.InfoState.BanConfirmPlayerName = null;
                    break;
                case InfoEvent.BanConfirm bc:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("ban", religionId, bc.PlayerUID);
                    }

                    State.InfoState.BanConfirmPlayerUID = null;
                    State.InfoState.BanConfirmPlayerName = null;
                    break;

                // Unban
                case InfoEvent.UnbanClicked ub:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("unban", religionId, ub.PlayerUID);
                    }

                    break;

                // Deity name editing
                case InfoEvent.EditDeityNameOpen:
                    State.InfoState.IsEditingDeityName = true;
                    State.InfoState.EditDeityNameValue = State.InfoState.MyReligionInfo?.DeityName ?? string.Empty;
                    State.InfoState.DeityNameError = null;
                    break;

                case InfoEvent.EditDeityNameChanged edc:
                    State.InfoState.EditDeityNameValue = edc.Text;
                    State.InfoState.DeityNameError = null;
                    break;

                case InfoEvent.EditDeityNameCancel:
                    State.InfoState.IsEditingDeityName = false;
                    State.InfoState.EditDeityNameValue = string.Empty;
                    State.InfoState.DeityNameError = null;
                    break;

                case InfoEvent.EditDeityNameSave eds:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        State.InfoState.IsSavingDeityName = true;
                        State.InfoState.DeityNameError = null;
                        _uiService.RequestSetDeityName(religionId, eds.NewDeityName);
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Convert network packet data to view model data
    /// </summary>
    private IReadOnlyList<InviteData> ConvertToInviteData(
        List<PlayerReligionInfoResponsePacket.ReligionInviteInfo> packetInvites)
    {
        return packetInvites
            .Select(i => new InviteData(
                inviteId: i.InviteId,
                religionName: i.ReligionName,
                expiresAt: i.ExpiresAt))
            .ToList();
    }

    /// <summary>
    /// Process events from the invites renderer
    /// </summary>
    private void ProcessInvitesEvents(IReadOnlyList<InvitesEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case InvitesEvent.AcceptInviteClicked e:
                    HandleAcceptInvite(e.InviteId);
                    break;

                case InvitesEvent.DeclineInviteClicked e:
                    HandleDeclineInvite(e.InviteId);
                    break;

                case InvitesEvent.ScrollChanged e:
                    State.InvitesState.InvitesScrollY = e.NewScrollY;
                    break;
            }
        }
    }

    /// <summary>
    /// Process events from the create renderer
    /// </summary>
    private void ProcessCreateEvents(IReadOnlyList<CreateEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case CreateEvent.NameChanged e:
                    State.CreateState.Name = e.NewName;
                    break;

                case CreateEvent.DeityChanged e:
                    State.CreateState.Domain = e.NewDeity;
                    break;

                case CreateEvent.DeityNameChanged e:
                    State.CreateState.DeityName = e.NewDeityName;
                    break;

                case CreateEvent.IsPublicChanged e:
                    State.CreateState.IsPublic = e.IsPublic;
                    _soundManager.PlayClick();
                    break;

                case CreateEvent.SubmitClicked:
                    HandleCreateReligionSubmit();
                    break;
            }
        }
    }

    /// <summary>
    /// Handle the religion creation submission
    /// </summary>
    private void HandleCreateReligionSubmit()
    {
        // Validate before submission
        if (string.IsNullOrWhiteSpace(State.CreateState.Name) ||
            State.CreateState.Name.Length < 3 ||
            State.CreateState.Name.Length > 32)
        {
            // Play error sound
            _soundManager.PlayError();
            return;
        }

        // Play success sound
        _soundManager.PlayClick();

        // Request creation
        RequestReligionCreate(State.CreateState.Name, State.CreateState.Domain, State.CreateState.DeityName,
            State.CreateState.IsPublic);

        // Clear form
        State.CreateState.Name = string.Empty;
        State.CreateState.Domain = nameof(DeityDomain.Craft);
        State.CreateState.DeityName = string.Empty;
        State.CreateState.IsPublic = true;
        State.ErrorState.CreateError = null;

        // Switch to My Religion tab to see the new religion
        State.CurrentSubTab = SubTab.Info;
        RequestPlayerReligionInfo();
    }

    private void RequestReligionCreate(string religionName, string domain, string deityName, bool isPublic)
    {
        _uiService.RequestCreateReligion(religionName, domain, deityName, isPublic);
    }

    /// <summary>
    /// Handle events from the browse renderer
    /// </summary>
    private void ProcessBrowseEvents(IReadOnlyList<BrowseEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case BrowseEvent.DeityFilterChanged e:
                    // Update filter (translate "All" → "")
                    State.BrowseState.DeityFilter = e.NewFilter == "All" ? string.Empty : e.NewFilter;
                    State.BrowseState.SelectedReligionUID = null;
                    State.BrowseState.BrowseScrollY = 0f;
                    // Request refresh with new filter
                    RequestReligionList(State.BrowseState.DeityFilter);
                    // Feedback sound
                    _soundManager.PlayClick();
                    break;

                case BrowseEvent.Selected e:
                    // Navigate to detail view instead of just selecting
                    if (e.ReligionUID != null)
                    {
                        RequestReligionDetail(e.ReligionUID);
                    }

                    State.BrowseState.BrowseScrollY = e.NewScrollY;
                    break;

                case BrowseEvent.ScrollChanged e:
                    State.BrowseState.BrowseScrollY = e.NewScrollY;
                    break;

                case BrowseEvent.CreateClicked:
                    // Switch to Create sub-tab
                    State.CurrentSubTab = SubTab.Create;
                    _soundManager.PlayClick();
                    break;

                case BrowseEvent.JoinClicked e:
                    if (!string.IsNullOrEmpty(e.ReligionUID))
                    {
                        _soundManager.PlayClick();
                        RequestReligionAction("join", e.ReligionUID);
                    }

                    break;

                case BrowseEvent.DeityDropDownToggled e:
                    State.BrowseState.IsDeityDropdownOpen = e.IsOpen;
                    break;

                case BrowseEvent.RefreshClicked:
                    RequestReligionList(State.BrowseState.DeityFilter);
                    _soundManager.PlayClick();
                    break;
            }
        }
    }

    /// <summary>
    /// Handle accept the invite action
    /// All side effects (network, sound) happen here
    /// </summary>
    private void HandleAcceptInvite(string inviteId)
    {
        // Send network request
        RequestReligionAction("accept", string.Empty, inviteId);

        // Optional: Optimistic UI update
        State.InvitesState.Loading = true;
    }

    /// <summary>
    /// Handle decline invite action
    /// </summary>
    private void HandleDeclineInvite(string inviteId)
    {
        // Send network request
        RequestReligionAction("decline", string.Empty, inviteId);

        // Optional: Optimistic UI update
        State.InvitesState.Loading = true;
    }

    /// <summary>
    ///     Draws the religion roles tab using conditional rendering pattern
    ///     Routes to browse or detail view based on state
    /// </summary>
    public void DrawReligionRoles(float x, float y, float width, float height)
    {
        // Check if viewing role details (conditional rendering pattern)
        if (!string.IsNullOrEmpty(State.RolesState.DetailState.ViewingRoleUID))
        {
            DrawRoleDetail(x, y, width, height);
            return;
        }

        // Otherwise, draw browse view
        DrawRolesBrowse(x, y, width, height);
    }

    /// <summary>
    ///     Draws the roles browse view (role cards list)
    /// </summary>
    private void DrawRolesBrowse(float x, float y, float width, float height)
    {
        // Build browse view model
        var viewModel = new ReligionRolesBrowseViewModel(
            State.RolesState.Loading,
            HasReligion(),
            CurrentReligionUID ?? string.Empty,
            _coreClientApi.World.Player?.PlayerUID ?? string.Empty,
            State.RolesState.RolesData,
            State.RolesState.BrowseState.ShowRoleEditor,
            State.RolesState.BrowseState.EditingRoleUID,
            State.RolesState.BrowseState.EditingRoleName,
            State.RolesState.BrowseState.EditingPermissions,
            State.RolesState.BrowseState.ShowCreateRoleDialog,
            State.RolesState.BrowseState.NewRoleName,
            State.RolesState.BrowseState.ShowDeleteConfirm,
            State.RolesState.BrowseState.DeleteRoleUID,
            State.RolesState.BrowseState.DeleteRoleName,
            x, y, width, height,
            State.RolesState.BrowseState.ScrollY
        );

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionRolesBrowseRenderer.Draw(viewModel, drawList);

        // Process events
        ProcessRolesBrowseEvents(result.Events);
    }

    /// <summary>
    ///     Draws the role detail view (viewing members with a specific role)
    /// </summary>
    private void DrawRoleDetail(float x, float y, float width, float height)
    {
        // Build detail view model
        var viewModel = new ReligionRoleDetailViewModel(
            State.RolesState.DetailState.ViewingRoleUID ?? string.Empty,
            State.RolesState.DetailState.ViewingRoleName ?? string.Empty,
            State.RolesState.Loading,
            _coreClientApi.World.Player?.PlayerUID ?? string.Empty,
            State.RolesState.RolesData,
            State.RolesState.DetailState.OpenAssignRoleDropdownMemberUID,
            State.RolesState.DetailState.ShowAssignRoleConfirm,
            State.RolesState.DetailState.AssignRoleConfirmMemberUID,
            State.RolesState.DetailState.AssignRoleConfirmMemberName,
            State.RolesState.DetailState.AssignRoleConfirmCurrentRoleUID,
            State.RolesState.DetailState.AssignRoleConfirmNewRoleUID,
            State.RolesState.DetailState.AssignRoleConfirmNewRoleName,
            x, y, width, height,
            State.RolesState.DetailState.MemberScrollY
        );

        // Render
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionRoleDetailRenderer.Draw(viewModel, drawList);

        // Process events
        ProcessRoleDetailEvents(result.Events);
    }

    /// <summary>
    ///     Process events emitted by the ReligionRolesBrowseRenderer
    ///     Maps pure UI intents to state updates and side effects
    /// </summary>
    private void ProcessRolesBrowseEvents(IReadOnlyList<RolesBrowseEvent>? events)
    {
        if (events == null || events.Count == 0) return;

        foreach (var ev in events)
            switch (ev)
            {
                case RolesBrowseEvent.ViewRoleDetailsClicked e:
                    // Navigate to detail view
                    State.RolesState.DetailState.ViewingRoleUID = e.RoleUID;
                    State.RolesState.DetailState.ViewingRoleName = e.RoleName;
                    State.RolesState.DetailState.MemberScrollY = 0f;
                    break;

                case RolesBrowseEvent.ScrollChanged e:
                    State.RolesState.BrowseState.ScrollY = e.NewScrollY;
                    break;

                case RolesBrowseEvent.CreateRoleOpen:
                    State.RolesState.BrowseState.ShowCreateRoleDialog = true;
                    State.RolesState.BrowseState.NewRoleName = string.Empty;
                    break;

                case RolesBrowseEvent.CreateRoleCancel:
                    State.RolesState.BrowseState.ShowCreateRoleDialog = false;
                    State.RolesState.BrowseState.NewRoleName = string.Empty;
                    break;

                case RolesBrowseEvent.CreateRoleNameChanged e:
                    State.RolesState.BrowseState.NewRoleName = e.RoleName;
                    break;

                case RolesBrowseEvent.CreateRoleConfirm e:
                    State.RolesState.BrowseState.ShowCreateRoleDialog = false;
                    _uiService.RequestCreateRole(CurrentReligionUID ?? string.Empty, e.RoleName ?? string.Empty);
                    _soundManager.PlayClick();
                    break;

                case RolesBrowseEvent.EditRoleOpen e:
                    var role = State.RolesState.RolesData?.Roles?.FirstOrDefault(r => r.RoleUID == e.RoleUID);
                    if (role != null)
                    {
                        State.RolesState.BrowseState.ShowRoleEditor = true;
                        State.RolesState.BrowseState.EditingRoleUID = e.RoleUID;
                        State.RolesState.BrowseState.EditingRoleName = role.RoleName;
                        State.RolesState.BrowseState.EditingPermissions = new HashSet<string>(role.Permissions);
                    }

                    break;

                case RolesBrowseEvent.EditRoleCancel:
                    State.RolesState.BrowseState.ShowRoleEditor = false;
                    State.RolesState.BrowseState.EditingRoleUID = null;
                    State.RolesState.BrowseState.EditingRoleName = string.Empty;
                    State.RolesState.BrowseState.EditingPermissions.Clear();
                    break;

                case RolesBrowseEvent.EditRoleNameChanged e:
                    State.RolesState.BrowseState.EditingRoleName = e.RoleName;
                    break;

                case RolesBrowseEvent.EditRolePermissionToggled e:
                    if (e.Enabled)
                        State.RolesState.BrowseState.EditingPermissions.Add(e.Permission);
                    else
                        State.RolesState.BrowseState.EditingPermissions.Remove(e.Permission);
                    break;

                case RolesBrowseEvent.EditRoleSave e:
                    State.RolesState.BrowseState.ShowRoleEditor = false;
                    _uiService.RequestModifyRolePermissions(CurrentReligionUID ?? string.Empty, e.RoleUID,
                        e.Permissions);
                    _soundManager.PlayClick();
                    State.RolesState.BrowseState.EditingRoleUID = null;
                    State.RolesState.BrowseState.EditingRoleName = string.Empty;
                    State.RolesState.BrowseState.EditingPermissions.Clear();
                    break;

                case RolesBrowseEvent.DeleteRoleOpen e:
                    State.RolesState.BrowseState.ShowDeleteConfirm = true;
                    State.RolesState.BrowseState.DeleteRoleUID = e.RoleUID;
                    State.RolesState.BrowseState.DeleteRoleName = e.RoleName;
                    break;

                case RolesBrowseEvent.DeleteRoleConfirm e:
                    State.RolesState.BrowseState.ShowDeleteConfirm = false;
                    _uiService.RequestDeleteRole(CurrentReligionUID ?? string.Empty, e.RoleUID);
                    _soundManager.PlayClick();
                    State.RolesState.BrowseState.DeleteRoleUID = null;
                    State.RolesState.BrowseState.DeleteRoleName = null;
                    break;

                case RolesBrowseEvent.DeleteRoleCancel:
                    State.RolesState.BrowseState.ShowDeleteConfirm = false;
                    State.RolesState.BrowseState.DeleteRoleUID = null;
                    State.RolesState.BrowseState.DeleteRoleName = null;
                    break;

                case RolesBrowseEvent.RefreshRequested:
                    State.RolesState.Loading = true;
                    _uiService.RequestReligionRoles(CurrentReligionUID ?? string.Empty);
                    break;
            }
    }

    /// <summary>
    ///     Process events emitted by the ReligionRoleDetailRenderer
    ///     Maps pure UI intents to state updates and side effects
    /// </summary>
    private void ProcessRoleDetailEvents(IReadOnlyList<RoleDetailEvent>? events)
    {
        if (events == null || events.Count == 0) return;

        foreach (var ev in events)
            switch (ev)
            {
                case RoleDetailEvent.BackToRolesClicked:
                    // Navigate back to browse view
                    State.RolesState.DetailState.ViewingRoleUID = null;
                    State.RolesState.DetailState.ViewingRoleName = null;
                    State.RolesState.DetailState.Reset();
                    break;

                case RoleDetailEvent.MemberScrollChanged e:
                    State.RolesState.DetailState.MemberScrollY = e.NewScrollY;
                    break;

                case RoleDetailEvent.AssignRoleDropdownToggled e:
                    // Only one dropdown open at a time
                    State.RolesState.DetailState.OpenAssignRoleDropdownMemberUID = e.IsOpen ? e.MemberUID : null;
                    break;

                case RoleDetailEvent.AssignRoleConfirmOpen e:
                    State.RolesState.DetailState.ShowAssignRoleConfirm = true;
                    State.RolesState.DetailState.AssignRoleConfirmMemberUID = e.MemberUID;
                    State.RolesState.DetailState.AssignRoleConfirmMemberName = e.MemberName;
                    State.RolesState.DetailState.AssignRoleConfirmCurrentRoleUID = e.CurrentRoleUID;
                    State.RolesState.DetailState.AssignRoleConfirmNewRoleUID = e.NewRoleUID;
                    State.RolesState.DetailState.AssignRoleConfirmNewRoleName = e.NewRoleName;
                    State.RolesState.DetailState.OpenAssignRoleDropdownMemberUID = null; // Close dropdown
                    break;

                case RoleDetailEvent.AssignRoleConfirm e:
                    State.RolesState.DetailState.ShowAssignRoleConfirm = false;
                    _uiService.RequestAssignRole(
                        CurrentReligionUID ?? string.Empty,
                        e.MemberUID,
                        e.NewRoleUID);
                    _soundManager.PlayClick();
                    // Clear confirmation state
                    State.RolesState.DetailState.AssignRoleConfirmMemberUID = null;
                    State.RolesState.DetailState.AssignRoleConfirmMemberName = null;
                    State.RolesState.DetailState.AssignRoleConfirmCurrentRoleUID = null;
                    State.RolesState.DetailState.AssignRoleConfirmNewRoleUID = null;
                    State.RolesState.DetailState.AssignRoleConfirmNewRoleName = null;
                    break;

                case RoleDetailEvent.AssignRoleCancel:
                    State.RolesState.DetailState.ShowAssignRoleConfirm = false;
                    // Clear confirmation state
                    State.RolesState.DetailState.AssignRoleConfirmMemberUID = null;
                    State.RolesState.DetailState.AssignRoleConfirmMemberName = null;
                    State.RolesState.DetailState.AssignRoleConfirmCurrentRoleUID = null;
                    State.RolesState.DetailState.AssignRoleConfirmNewRoleUID = null;
                    State.RolesState.DetailState.AssignRoleConfirmNewRoleName = null;
                    break;
            }
    }
}