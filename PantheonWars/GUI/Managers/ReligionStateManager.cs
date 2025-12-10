using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Models.Religion.Activity;
using PantheonWars.GUI.Models.Religion.Browse;
using PantheonWars.GUI.Models.Religion.Create;
using PantheonWars.GUI.Models.Religion.Info;
using PantheonWars.GUI.Models.Religion.Invites;
using PantheonWars.GUI.Models.Religion.Tab;
using PantheonWars.GUI.State;
using PantheonWars.GUI.State.Religion;
using PantheonWars.GUI.UI.Adapters.ReligionMembers;
using PantheonWars.GUI.UI.Adapters.Religions;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.Renderers.Religion;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.Managers;

public class ReligionStateManager : IReligionStateManager
{
    private readonly ICoreClientAPI _coreClientApi;

    public ReligionTabState State { get; } = new();
    private readonly PantheonWarsSystem _system;
    public string? CurrentReligionUID { get; set; }
    public DeityType CurrentDeity { get; set; }
    public string? CurrentReligionName { get; set; }
    public int ReligionMemberCount { get; set; }
    public string? PlayerRoleInReligion { get; set; }
    public int CurrentFavorRank { get; set; }
    public int CurrentPrestigeRank { get; set; }
    public int CurrentFavor { get; set; }
    public int CurrentPrestige { get; set; }
    public int TotalFavorEarned { get; set; }
    
    // UI-only adapters (fake or real). Null when not used.
    internal IReligionMemberProvider? MembersProvider { get; set; }
    internal IReligionProvider? ReligionsProvider { get; private set; }

    public ReligionStateManager(ICoreClientAPI coreClientApi)
    {
        _coreClientApi = coreClientApi;
        _system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
    }

    public void Initialize(string? id, DeityType deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        CurrentReligionUID = id;
        CurrentDeity = deity;
        CurrentReligionName = religionName;
        CurrentFavorRank = favorRank;
        CurrentPrestigeRank = prestigeRank;
    }

    public void Reset()
    {
        CurrentReligionUID = null;
        CurrentDeity = DeityType.None;
        CurrentReligionName = null;
        ReligionMemberCount = 0;
        PlayerRoleInReligion = null;

        // Clear religion tab state
        State.Reset();
    }


    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(CurrentReligionUID) && CurrentDeity != DeityType.None;
    }
    

    public PlayerFavorProgress GetPlayerFavorProgress()
    {
        return new PlayerFavorProgress
        {
            CurrentFavor = TotalFavorEarned,
            RequiredFavor = RankRequirements.GetRequiredFavorForNextRank(CurrentFavorRank),
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
            RequiredPrestige = RankRequirements.GetRequiredPrestigeForNextRank(CurrentPrestigeRank),
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
                    Deity = r.deity,
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
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        if (deityFilter != null) system?.NetworkClient?.RequestReligionList(deityFilter);
    }

    /// <summary>
    /// Configure a UI-only religion data provider (fake or real). When set, RequestReligionList()
    /// uses it instead of performing a network call.
    /// </summary>
    internal void UseReligionProvider(IReligionProvider provider)
    {
        ReligionsProvider = provider;
    }

    /// <summary>
    /// Refresh the current religion list from the configured provider (if any).
    /// </summary>
    public void RefreshReligionsFromProvider()
    {
        ReligionsProvider?.Refresh();
        RequestReligionList(State.BrowseState.DeityFilter);
    }

    public void RequestPlayerReligionInfo()
    {
        State.InfoState.Loading = true;
        State.InvitesState.Loading = true; // also load the invites list for players without a religion
        State.ErrorState.InfoError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.NetworkClient?.RequestPlayerReligionInfo();
    }

    public void RequestReligionAction(string action, string religionId = "", string targetPlayerId = "")
    {
        // Clear transient action error
        State.ErrorState.LastActionError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.NetworkClient?.RequestReligionAction(action, religionId, targetPlayerId);
    }

    /// <summary>
    ///     Request to edit the current religion description
    /// </summary>
    public void RequestEditReligionDescription(string id, string description)
    {
        // Clear transient action error
        State.ErrorState.LastActionError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.NetworkClient?.RequestEditDescription(id, description);
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
    /// Draws the religion browse tab using the refactored renderer (State → ViewModel → Renderer → Events → State)
    /// </summary>
    public void DrawReligionBrowse(float x, float y, float width, float height)
    {
        // Map State → ViewModel
        var deityFilters = new[] { "All", "Khoras", "Lysa", "Aethra", "Gaia" };
        var effectiveFilter = string.IsNullOrEmpty(State.BrowseState.DeityFilter) ? "All" : State.BrowseState.DeityFilter;

        var viewModel = new ReligionBrowseViewModel(
            deityFilters: deityFilters,
            currentDeityFilter: effectiveFilter,
            religions: State.BrowseState.AllReligions,
            isLoading: State.BrowseState.IsBrowseLoading,
            scrollY: State.BrowseState.BrowseScrollY,
            selectedReligionUID: State.BrowseState.SelectedReligionUID,
            userHasReligion: HasReligion(),
            x: x,
            y: y,
            width: width,
            height: height);

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
        // Build view model from state
        var viewModel = new ReligionCreateViewModel(
            religionName: State.CreateState.Name,
            deityName: State.CreateState.DeityName,
            isPublic: State.CreateState.IsPublic,
            availableDeities: new[] { "Khoras", "Lysa", "Aethra", "Gaia" },
            errorMessage: State.ErrorState.CreateError,
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionCreateRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessCreateEvents(result.Events);
    }

    /// <summary>
    /// Draws the religion info tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    public void DrawReligionInfo(float x, float y, float width, float height)
    {
        // Build view model from state
        var religion = State.InfoState.MyReligionInfo;
        var prestigeProgress = GetReligionPrestigeProgress();

        var viewModel = new ReligionInfoViewModel(
            isLoading: State.InfoState.Loading,
            hasReligion: religion != null && religion.HasReligion,
            religionUID: religion?.ReligionUID ?? string.Empty,
            religionName: religion?.ReligionName ?? string.Empty,
            deity: religion?.Deity ?? string.Empty,
            founderUID: religion?.FounderUID ?? string.Empty,
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

        var drawList = ImGui.GetWindowDrawList();
        var tabResult = ReligionTabRenderer.Draw(tabVm, drawList, _coreClientApi);

        // Handle emitted events
        foreach (var ev in tabResult.Events)
        {
            switch (ev)
            {
                case ReligionSubTabEvent.TabChanged(var sub):
                    State.CurrentSubTab = sub;
                    // Clear transient action error on tab change
                    State.ErrorState.LastActionError = null;
                    // Clear context-specific errors and possibly trigger loads
                    switch (sub)
                    {
                        case SubTab.Browse:
                            State.ErrorState.BrowseError = null;
                            break;
                        case SubTab.Info:
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
                            break;
                    }
                    break;
                case ReligionSubTabEvent.DismissActionError:
                    State.ErrorState.LastActionError = null;
                    break;
                case ReligionSubTabEvent.DismissContextError(var subTab):
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
                case ReligionSubTabEvent.RetryRequested(var subTab):
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
        }
    }

    /// <summary>
    ///     Temporary orchestration wrapper to draw the Blessings tab via renderer. This mirrors how Religion tab is handled
    ///     and will be expanded to full EDA-style event processing if needed.
    /// </summary>
    private void DrawReligionActivity(float x, float contentY, float width, float contentHeight)
    {
        ReligionActivityViewModel vm = new  ReligionActivityViewModel(x,  contentY, width, contentHeight);
        var result = ReligionActivityRenderer.Draw(vm);
        ProcessActivityEvents(result.Events);
    }

    private void ProcessActivityEvents(IReadOnlyList<ReligionActivityEvent>? resultEvents)
    {
        if (resultEvents == null || resultEvents.Count == 0) return;
        foreach (var ev in resultEvents)
        {
            switch (ev)
            {
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Process events emitted by the ReligionInfoRenderer (My Religion tab)
    /// Maps pure UI intents to state updates and side effects (network requests, sounds, etc.).
    /// </summary>
    private void ProcessInfoEvents(IReadOnlyList<ReligionInfoEvent>? events)
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
                case ReligionInfoEvent.ScrollChanged s:
                    State.InfoState.MyReligionScrollY = s.NewScrollY;
                    break;
                case ReligionInfoEvent.MemberScrollChanged ms:
                    State.InfoState.MemberScrollY = ms.NewScrollY;
                    break;
                case ReligionInfoEvent.BanListScrollChanged bs:
                    State.InfoState.BanListScrollY = bs.NewScrollY;
                    break;

                // Description edit/save
                case ReligionInfoEvent.DescriptionChanged dc:
                    State.InfoState.Description = dc.Text;
                    break;
                case ReligionInfoEvent.SaveDescriptionClicked sd:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestEditReligionDescription(religionId, sd.Text);
                        // Optimistically update local model so UI reflects change immediately
                        if (info != null)
                        {
                            info.Description = sd.Text;
                        }
                        State.InfoState.Description = sd.Text;
                    }
                    break;

                // Invite flow
                case ReligionInfoEvent.InviteNameChanged inc:
                    State.InfoState.InvitePlayerName = inc.Text;
                    break;
                case ReligionInfoEvent.InviteClicked ic:
                    if (!string.IsNullOrWhiteSpace(ic.PlayerName) && !string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestReligionAction("invite", religionId, ic.PlayerName);
                        State.InfoState.InvitePlayerName = string.Empty;
                    }
                    break;

                // Membership actions
                case ReligionInfoEvent.LeaveClicked:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestReligionAction("leave", religionId);
                    }
                    break;

                // Disband flow
                case ReligionInfoEvent.DisbandOpen:
                    State.InfoState.ShowDisbandConfirm = true;
                    break;
                case ReligionInfoEvent.DisbandCancel:
                    State.InfoState.ShowDisbandConfirm = false;
                    break;
                case ReligionInfoEvent.DisbandConfirm:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestReligionAction("disband", religionId);
                    }
                    State.InfoState.ShowDisbandConfirm = false;
                    break;

                // Kick flow
                case ReligionInfoEvent.KickOpen ko:
                    State.InfoState.KickConfirmPlayerUID = ko.PlayerUID;
                    State.InfoState.KickConfirmPlayerName = ko.PlayerName;
                    break;
                case ReligionInfoEvent.KickCancel:
                    State.InfoState.KickConfirmPlayerUID = null;
                    State.InfoState.KickConfirmPlayerName = null;
                    break;
                case ReligionInfoEvent.KickConfirm kc:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestReligionAction("kick", religionId, kc.PlayerUID);
                    }
                    State.InfoState.KickConfirmPlayerUID = null;
                    State.InfoState.KickConfirmPlayerName = null;
                    break;

                // Ban flow
                case ReligionInfoEvent.BanOpen bo:
                    State.InfoState.BanConfirmPlayerUID = bo.PlayerUID;
                    State.InfoState.BanConfirmPlayerName = bo.PlayerName;
                    break;
                case ReligionInfoEvent.BanCancel:
                    State.InfoState.BanConfirmPlayerUID = null;
                    State.InfoState.BanConfirmPlayerName = null;
                    break;
                case ReligionInfoEvent.BanConfirm bc:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestReligionAction("ban", religionId, bc.PlayerUID);
                    }
                    State.InfoState.BanConfirmPlayerUID = null;
                    State.InfoState.BanConfirmPlayerName = null;
                    break;

                // Unban
                case ReligionInfoEvent.UnbanClicked ub:
                    if (!string.IsNullOrWhiteSpace(religionId))
                    {
                        RequestReligionAction("unban", religionId, ub.PlayerUID);
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
    private void ProcessInvitesEvents(IReadOnlyList<ReligionInvitesEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case ReligionInvitesEvent.AcceptInviteClicked e:
                    HandleAcceptInvite(e.InviteId);
                    break;

                case ReligionInvitesEvent.DeclineInviteClicked e:
                    HandleDeclineInvite(e.InviteId);
                    break;

                case ReligionInvitesEvent.ScrollChanged e:
                    State.InvitesState.InvitesScrollY = e.NewScrollY;
                    break;
            }
        }
    }

    /// <summary>
    /// Process events from the create renderer
    /// </summary>
    private void ProcessCreateEvents(IReadOnlyList<ReligionCreateEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case ReligionCreateEvent.NameChanged e:
                    State.CreateState.Name = e.NewName;
                    break;

                case ReligionCreateEvent.DeityChanged e:
                    State.CreateState.DeityName = e.NewDeity;
                    _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;

                case ReligionCreateEvent.IsPublicChanged e:
                    State.CreateState.IsPublic = e.IsPublic;
                    _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.3f);
                    break;

                case ReligionCreateEvent.SubmitClicked:
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
            _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _coreClientApi.World.Player.Entity, null, false, 8f, 0.3f);
            return;
        }

        // Play success sound
        _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
            _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);

        // Request creation
        RequestReligionCreate(State.CreateState.Name, State.CreateState.DeityName, State.CreateState.IsPublic);

        // Clear form
        State.CreateState.Name = string.Empty;
        State.CreateState.DeityName = "Khoras";
        State.CreateState.IsPublic = true;
        State.ErrorState.CreateError = null;

        // Switch to My Religion tab to see the new religion
        State.CurrentSubTab = SubTab.Info;
    }

    private void RequestReligionCreate(string religionName, string deity, bool isPublic)
    {
        _system?.NetworkClient?.RequestCreateReligion(religionName, deity, isPublic);
    }

    /// <summary>
    /// Handle events from the browse renderer
    /// </summary>
    private void ProcessBrowseEvents(IReadOnlyList<ReligionBrowseEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case ReligionBrowseEvent.DeityFilterChanged e:
                    // Update filter (translate "All" → "")
                    State.BrowseState.DeityFilter = e.NewFilter == "All" ? string.Empty : e.NewFilter;
                    State.BrowseState.SelectedReligionUID = null;
                    State.BrowseState.BrowseScrollY = 0f;
                    // Request refresh with new filter
                    RequestReligionList(State.BrowseState.DeityFilter);
                    // Feedback sound
                    _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;

                case ReligionBrowseEvent.ReligionSelected e:
                    State.BrowseState.SelectedReligionUID = e.ReligionUID;
                    State.BrowseState.BrowseScrollY = e.NewScrollY;
                    break;

                case ReligionBrowseEvent.ScrollChanged e:
                    State.BrowseState.BrowseScrollY = e.NewScrollY;
                    break;

                case ReligionBrowseEvent.CreateReligionClicked:
                    // Switch to Create sub-tab
                    State.CurrentSubTab = SubTab.Create;
                    _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;

                case ReligionBrowseEvent.JoinReligionClicked e:
                    if (!string.IsNullOrEmpty(e.ReligionUID))
                    {
                        _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                            _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                        RequestReligionAction("join", e.ReligionUID);
                    }
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
}