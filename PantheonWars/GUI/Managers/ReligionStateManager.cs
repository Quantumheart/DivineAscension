using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Models.Religion.Browse;
using PantheonWars.GUI.Models.Religion.Create;
using PantheonWars.GUI.Models.Religion.Invites;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Adapters.Religions;
using PantheonWars.GUI.UI.Adapters.ReligionMembers;
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

    public ReligionTabState State { get; } = new ReligionTabState();
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
    public Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; } = new();
    public Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; } = new();
    
    // UI-only adapters (fake or real). Null when not used.
    internal IReligionMemberProvider? MembersProvider { get; set; }
    internal IReligionProvider? ReligionsProvider { get; set; }



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

        // Clear blessing trees
        PlayerBlessingStates.Clear();
        ReligionBlessingStates.Clear();
    }


    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(CurrentReligionUID) && CurrentDeity != DeityType.None;
    }

    public void LoadBlessingStates(List<Blessing> playerBlessings, List<Blessing> religionBlessings)
    {
        PlayerBlessingStates.Clear();
        ReligionBlessingStates.Clear();

        foreach (var blessing in playerBlessings)
        {
            var state = new BlessingNodeState(blessing);
            PlayerBlessingStates[blessing.BlessingId] = state;
        }

        foreach (var blessing in religionBlessings)
        {
            var state = new BlessingNodeState(blessing);
            ReligionBlessingStates[blessing.BlessingId] = state;
        }
    }

    public BlessingNodeState? GetBlessingState(string blessingId)
    {
        return PlayerBlessingStates.TryGetValue(blessingId, out var playerState)
            ? playerState
            : ReligionBlessingStates.GetValueOrDefault(blessingId);
    }

    public void SetBlessingUnlocked(string blessingId, bool unlocked)
    {
        var state = GetBlessingState(blessingId);
        if (state != null)
        {
            state.IsUnlocked = unlocked;
            state.UpdateVisualState();
        }
    }

    public void RefreshAllBlessingStates()
    {
        // Update CanUnlock status for all player blessings
        foreach (var state in PlayerBlessingStates.Values)
        {
            state.CanUnlock = CanUnlockBlessing(state);
            state.UpdateVisualState();
        }

        // Update CanUnlock status for all religion blessings
        foreach (var state in ReligionBlessingStates.Values)
        {
            state.CanUnlock = CanUnlockBlessing(state);
            state.UpdateVisualState();
        }
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

    public void RequestReligionList(string deityFilter = "")
    {
        // Adapter short-circuit: if a UI-only provider is configured, use it instead of network
        if (ReligionsProvider != null)
        {
            State.IsBrowseLoading = true;
            State.BrowseError = null;

            var items = ReligionsProvider.GetReligions();
            var filter = deityFilter?.Trim();

            // Apply simple deity filter if provided (case-insensitive)
            if (!string.IsNullOrEmpty(filter))
            {
                items = new List<GUI.UI.Adapters.Religions.ReligionVM>(items)
                    .FindAll(r => string.Equals(r.deity, filter, System.StringComparison.OrdinalIgnoreCase));
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
        State.IsBrowseLoading = true;
        State.BrowseError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestReligionList(deityFilter);
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
        RequestReligionList(State.DeityFilter);
    }

    public void RequestPlayerReligionInfo()
    {
        State.IsMyReligionLoading = true;
        State.IsInvitesLoading = true; // also load the invites list for players without a religion
        State.MyReligionError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestPlayerReligionInfo();
    }

    public void RequestReligionAction(string action, string religionId = "", string targetPlayerId = "")
    {
        // Clear transient action error
        State.LastActionError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestReligionAction(action, religionId, targetPlayerId);
    }

    /// <summary>
    ///     Request to edit the current religion description
    /// </summary>
    public void RequestEditReligionDescription(string id, string description)
    {
        // Clear transient action error
        State.LastActionError = null;
        var system = _coreClientApi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestEditDescription(id, description);
    }

    /// <summary>
    ///     Update religion list from server response
    /// </summary>
    public void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions)
    {
        State.AllReligions = religions;
        State.IsBrowseLoading = false;
        State.BrowseError = null;
    }

    /// <summary>
    ///     Update player religion info from server response
    /// </summary>
    public void UpdatePlayerReligionInfo(PlayerReligionInfoResponsePacket? info)
    {
        State.MyReligionInfo = info;
        State.Description = info?.Description ?? string.Empty;
        State.IsMyReligionLoading = false;
        // Update invites (shown when the player has no religion)
        State.MyInvites = info?.PendingInvites != null
            ? [..info.PendingInvites]
            : new List<PlayerReligionInfoResponsePacket.ReligionInviteInfo>();
        State.IsInvitesLoading = false;
        State.MyReligionError = null;
    }

    /// <summary>
    /// Draws the religion browse tab using the refactored renderer (State → ViewModel → Renderer → Events → State)
    /// </summary>
    public void DrawReligionBrowse(float x, float y, float width, float height)
    {
        // Map State → ViewModel
        var deityFilters = new[] { "All", "Khoras", "Lysa", "Aethra", "Gaia" };
        var effectiveFilter = string.IsNullOrEmpty(State.DeityFilter) ? "All" : State.DeityFilter;

        var viewModel = new ReligionBrowseViewModel(
            deityFilters: deityFilters,
            currentDeityFilter: effectiveFilter,
            religions: State.AllReligions,
            isLoading: State.IsBrowseLoading,
            scrollY: State.BrowseScrollY,
            selectedReligionUID: State.SelectedReligionUID,
            userHasReligion: HasReligion(),
            x: x,
            y: y,
            width: width,
            height: height);

        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionBrowseRenderer.Draw(viewModel, drawList, _coreClientApi);

        // Process events (Events → State + side effects)
        ProcessBrowseEvents(result.Events);

        // Tooltip as side effect (not part of pure renderer output, but uses returned hover info)
        if (result.HoveredReligion != null)
        {
            var mousePos = ImGui.GetMousePos();
            GUI.UI.Renderers.Components.ReligionListRenderer.DrawTooltip(result.HoveredReligion, mousePos.X, mousePos.Y, width, height);
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
            invites: ConvertToInviteData(State.MyInvites),
            isLoading: State.IsInvitesLoading,
            scrollY: State.InvitesScrollY,
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
            religionName: State.ReligionCreateState.Name,
            deityName: State.ReligionCreateState.DeityName,
            isPublic: State.ReligionCreateState.IsPublic,
            availableDeities: new[] { "Khoras", "Lysa", "Aethra", "Gaia" },
            errorMessage: State.CreateError,
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionCreateRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessCreateEvents(result.Events);
    }

    /// <summary>
    ///     Check if a blessing can be unlocked based on prerequisites and rank requirements
    ///     This is a client-side validation - server will do final validation
    /// </summary>
    private bool CanUnlockBlessing(BlessingNodeState state)
    {
        // Already unlocked
        if (state.IsUnlocked) return false;

        // Check prerequisites
        if (state.Blessing.PrerequisiteBlessings is { Count: > 0 })
            foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
            {
                var prereqState = GetBlessingState(prereqId);
                if (prereqState == null || !prereqState.IsUnlocked) return false; // Prerequisite not unlocked
            }

        // Check rank requirements based on the blessing kind
        if (state.Blessing.Kind == BlessingKind.Player)
        {
            // Player blessings require favor rank
            if (state.Blessing.RequiredFavorRank > CurrentFavorRank) return false;
        }
        else if (state.Blessing.Kind == BlessingKind.Religion)
        {
            // Religion blessings require prestige rank
            if (state.Blessing.RequiredPrestigeRank > CurrentPrestigeRank) return false;
        }

        return true; // All requirements met
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
                    State.InvitesScrollY = e.NewScrollY;
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
                    State.ReligionCreateState.Name = e.NewName;
                    break;

                case ReligionCreateEvent.DeityChanged e:
                    State.ReligionCreateState.DeityName = e.NewDeity;
                    _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;

                case ReligionCreateEvent.IsPublicChanged e:
                    State.ReligionCreateState.IsPublic = e.IsPublic;
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
        if (string.IsNullOrWhiteSpace(State.ReligionCreateState.Name) ||
            State.ReligionCreateState.Name.Length < 3 ||
            State.ReligionCreateState.Name.Length > 32)
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
        RequestReligionCreate(State.ReligionCreateState.Name, State.ReligionCreateState.DeityName, State.ReligionCreateState.IsPublic);

        // Clear form
        State.ReligionCreateState.Name = string.Empty;
        State.ReligionCreateState.DeityName = "Khoras";
        State.ReligionCreateState.IsPublic = true;
        State.CreateError = null;

        // Switch to My Religion tab to see the new religion
        State.CurrentSubTab = GUI.State.ReligionSubTab.MyReligion;
    }

    private void RequestReligionCreate(string religionName, string deity, bool isPublic)
    {
        _system?.RequestCreateReligion(religionName, deity, isPublic);
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
                    State.DeityFilter = e.NewFilter == "All" ? string.Empty : e.NewFilter;
                    State.SelectedReligionUID = null;
                    State.BrowseScrollY = 0f;
                    // Request refresh with new filter
                    RequestReligionList(State.DeityFilter);
                    // Feedback sound
                    _coreClientApi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;

                case ReligionBrowseEvent.ReligionSelected e:
                    State.SelectedReligionUID = e.ReligionUID;
                    State.BrowseScrollY = e.NewScrollY;
                    break;

                case ReligionBrowseEvent.ScrollChanged e:
                    State.BrowseScrollY = e.NewScrollY;
                    break;

                case ReligionBrowseEvent.CreateReligionClicked:
                    // Switch to Create sub-tab
                    State.CurrentSubTab = GUI.State.ReligionSubTab.Create;
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
        State.IsInvitesLoading = true;
    }

    /// <summary>
    /// Handle decline invite action
    /// </summary>
    private void HandleDeclineInvite(string inviteId)
    {
        // Send network request
        RequestReligionAction("decline", string.Empty, inviteId);

        // Optional: Optimistic UI update
        State.IsInvitesLoading = true;
    }
}