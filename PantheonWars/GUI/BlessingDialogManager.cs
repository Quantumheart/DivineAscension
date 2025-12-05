using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Models.Religion;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Adapters.ReligionMembers;
using PantheonWars.GUI.UI.Renderers.Religion;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages state for the Blessing Dialog UI
/// </summary>
public class BlessingDialogManager : IBlessingDialogManager
{
    private readonly ICoreClientAPI _capi;

    public BlessingDialogManager(ICoreClientAPI capi)
    {
        _capi = capi;
        // Initialize UI-only fake data provider in DEBUG builds. In Release it stays null.
#if DEBUG
        MembersProvider = new FakeReligionMemberProvider();
        MembersProvider.ConfigureDevSeed(500, 20251204);
#endif
    }

    // Composite UI state
    public CivilizationState CivState { get; } = new();
    public ReligionTabState ReligionState { get; } = new();

    // Civilization state
    public string? CurrentCivilizationId { get; set; }
    public string? CurrentCivilizationName { get; set; }
    public string? CivilizationFounderReligionUID { get; set; }
    public List<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions { get; set; } = new();

    public bool IsCivilizationFounder => !string.IsNullOrEmpty(CurrentReligionUID) &&
                                         !string.IsNullOrEmpty(CivilizationFounderReligionUID) &&
                                         CurrentReligionUID == CivilizationFounderReligionUID;

    // UI-only adapter for supplying religion members (fake or real). Null when not used.
    internal IReligionMemberProvider? MembersProvider { get; private set; }

    // Religion and deity state
    public string? CurrentReligionUID { get; set; }
    public DeityType CurrentDeity { get; set; } = DeityType.None;
    public string? CurrentReligionName { get; set; }
    public int ReligionMemberCount { get; set; }
    public string? PlayerRoleInReligion { get; set; } // "Leader", "Member", etc.

    // Player progression state
    public int CurrentFavorRank { get; set; }
    public int CurrentPrestigeRank { get; set; }
    public int CurrentFavor { get; set; } = 0;
    public int CurrentPrestige { get; set; } = 0;
    public int TotalFavorEarned { get; set; } = 0;

    // Blessing selection state
    public string? SelectedBlessingId { get; set; }
    public string? HoveringBlessingId { get; set; }

    // Scroll state
    public float PlayerTreeScrollX { get; set; }
    public float PlayerTreeScrollY { get; set; }
    public float ReligionTreeScrollX { get; set; }
    public float ReligionTreeScrollY { get; set; }

    // Data loaded flags
    public bool IsDataLoaded { get; set; }

    // Blessing node states (Phase 2)
    public Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; } = new();
    public Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; } = new();

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        CurrentReligionUID = religionUID;
        CurrentDeity = deity;
        CurrentReligionName = religionName;
        CurrentFavorRank = favorRank;
        CurrentPrestigeRank = prestigeRank;
        IsDataLoaded = true;

        // Reset selection and scroll
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerTreeScrollX = 0f;
        PlayerTreeScrollY = 0f;
        ReligionTreeScrollX = 0f;
        ReligionTreeScrollY = 0f;
    }

    /// <summary>
    ///     Reset all state
    /// </summary>
    public void Reset()
    {
        CurrentReligionUID = null;
        CurrentDeity = DeityType.None;
        CurrentReligionName = null;
        ReligionMemberCount = 0;
        PlayerRoleInReligion = null;
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerTreeScrollX = 0f;
        PlayerTreeScrollY = 0f;
        ReligionTreeScrollX = 0f;
        ReligionTreeScrollY = 0f;
        IsDataLoaded = false;

        // Clear civilization state
        CurrentCivilizationId = null;
        CurrentCivilizationName = null;
        CivilizationFounderReligionUID = null;
        CivilizationMemberReligions.Clear();
        CivState.Reset();

        // Clear religion tab state
        ReligionState.Reset();

        // Clear blessing trees
        PlayerBlessingStates.Clear();
        ReligionBlessingStates.Clear();
    }

    /// <summary>
    ///     Select a blessing (for displaying details)
    /// </summary>
    public void SelectBlessing(string blessingId)
    {
        SelectedBlessingId = blessingId;
    }

    /// <summary>
    ///     Clear blessing selection
    /// </summary>
    public void ClearSelection()
    {
        SelectedBlessingId = null;
    }

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(CurrentReligionUID) && CurrentDeity != DeityType.None;
    }

    /// <summary>
    ///     Load blessing states for player and religion blessings
    ///     Called in Phase 6 when connected to BlessingRegistry
    /// </summary>
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

    /// <summary>
    ///     Get blessing node state by ID
    /// </summary>
    public BlessingNodeState? GetBlessingState(string blessingId)
    {
        if (PlayerBlessingStates.TryGetValue(blessingId, out var playerState)) return playerState;

        if (ReligionBlessingStates.TryGetValue(blessingId, out var religionState)) return religionState;

        return null;
    }

    /// <summary>
    ///     Get selected blessing's state (if any)
    /// </summary>
    public BlessingNodeState? GetSelectedBlessingState()
    {
        if (string.IsNullOrEmpty(SelectedBlessingId)) return null;

        return GetBlessingState(SelectedBlessingId);
    }

    /// <summary>
    ///     Update unlock status for a blessing
    /// </summary>
    public void SetBlessingUnlocked(string blessingId, bool unlocked)
    {
        var state = GetBlessingState(blessingId);
        if (state != null)
        {
            state.IsUnlocked = unlocked;
            state.UpdateVisualState();
        }
    }

    /// <summary>
    ///     Update all blessing states based on current unlock status and requirements
    ///     Called after data refresh in Phase 6
    /// </summary>
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

    /// <summary>
    ///     Get player favor progress data
    /// </summary>
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

    /// <summary>
    ///     Get religion prestige progress data
    /// </summary>
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

    /// <summary>
    ///     Check if a blessing can be unlocked based on prerequisites and rank requirements
    ///     This is a client-side validation - server will do final validation
    /// </summary>
    private bool CanUnlockBlessing(BlessingNodeState state)
    {
        // Already unlocked
        if (state.IsUnlocked) return false;

        // Check prerequisites
        if (state.Blessing.PrerequisiteBlessings != null && state.Blessing.PrerequisiteBlessings.Count > 0)
            foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
            {
                var prereqState = GetBlessingState(prereqId);
                if (prereqState == null || !prereqState.IsUnlocked) return false; // Prerequisite not unlocked
            }

        // Check rank requirements based on blessing kind
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
            CurrentCivilizationId = null;
            CurrentCivilizationName = null;
            CivilizationFounderReligionUID = null;
            CivilizationMemberReligions.Clear();
            CivState.MyCivilization = null;
            CivState.MyInvites.Clear();
            return;
        }

        // Check if this is for a civilization we're viewing (from "View Details")
        if (!string.IsNullOrEmpty(CivState.ViewingCivilizationId) && details.CivId == CivState.ViewingCivilizationId)
        {
            // Update viewing details
            CivState.ViewingCivilizationDetails = details;
        }
        else
        {
            // Update player's own civilization (or just invites if not in a civilization)
            if (string.IsNullOrEmpty(details.CivId))
            {
                // Player has no civilization; only update invites and keep civ info cleared
                CurrentCivilizationId = null;
                CurrentCivilizationName = null;
                CivilizationFounderReligionUID = null;
                CivilizationMemberReligions.Clear();
                CivState.MyCivilization = null;
                CivState.MyInvites = new List<CivilizationInfoResponsePacket.PendingInvite>(details.PendingInvites ??
                    []);
            }
            else
            {
                CurrentCivilizationId = details.CivId;
                CurrentCivilizationName = details.Name;
                CivilizationFounderReligionUID = details.FounderReligionUID;
                CivilizationMemberReligions =
                    new List<CivilizationInfoResponsePacket.MemberReligion>(details.MemberReligions ?? []);
                CivState.MyCivilization = details;
                CivState.MyInvites = new List<CivilizationInfoResponsePacket.PendingInvite>(details.PendingInvites ?? []);
            }
        }
    }

    /// <summary>
    ///     Request the list of civilizations from the server (filtered by deity when provided)
    /// </summary>
    public void RequestCivilizationList(string deityFilter = "")
    {
        // Set loading state for browse
        CivState.IsBrowseLoading = true;
        CivState.BrowseError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestCivilizationList(deityFilter);
    }

    /// <summary>
    ///     Request details for the current civilization (empty string means player religion's civ)
    /// </summary>
    public void RequestCivilizationInfo(string civIdOrEmpty = "")
    {
        // Toggle loading depending on details vs my civ
        if (string.IsNullOrEmpty(civIdOrEmpty))
        {
            CivState.IsMyCivLoading = true;
            CivState.IsInvitesLoading = true;
            CivState.MyCivError = null;
            CivState.InvitesError = null;
        }
        else
        {
            CivState.IsDetailsLoading = true;
            CivState.DetailsError = null;
        }

        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestCivilizationInfo(civIdOrEmpty);
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        // Clear transient action error; some actions will trigger refreshes
        CivState.LastActionError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestCivilizationAction(action, civId, targetId, name);
    }

    /// <summary>
    ///     Request the list of religions from the server (filtered by deity when provided)
    /// </summary>
    public void RequestReligionList(string deityFilter = "")
    {
        // Set loading state for browse
        ReligionState.IsBrowseLoading = true;
        ReligionState.BrowseError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestReligionList(deityFilter);
    }

    /// <summary>
    ///     Request player's current religion information from the server
    /// </summary>
    public void RequestPlayerReligionInfo()
    {
        ReligionState.IsMyReligionLoading = true;
        ReligionState.IsInvitesLoading = true; // also load invites list for players without a religion
        ReligionState.MyReligionError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestPlayerReligionInfo();
    }

    /// <summary>
    ///     Request a religion action (create, join, leave, invite, kick, ban, unban, edit_description, disband)
    /// </summary>
    public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
    {
        // Clear transient action error
        ReligionState.LastActionError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestReligionAction(action, religionUID, targetPlayerUID);
    }

    /// <summary>
    ///     Request to edit the current religion description
    /// </summary>
    public void RequestEditReligionDescription(string religionUID, string description)
    {
        // Clear transient action error
        ReligionState.LastActionError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.RequestEditDescription(religionUID, description);
    }

    /// <summary>
    ///     Update religion list from server response
    /// </summary>
    public void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions)
    {
        ReligionState.AllReligions = religions;
        ReligionState.IsBrowseLoading = false;
        ReligionState.BrowseError = null;
    }

    /// <summary>
    ///     Update player religion info from server response
    /// </summary>
    public void UpdatePlayerReligionInfo(PlayerReligionInfoResponsePacket? info)
    {
        ReligionState.MyReligionInfo = info;
        ReligionState.Description = info?.Description ?? string.Empty;
        ReligionState.IsMyReligionLoading = false;
        // Update invites (shown when player has no religion)
        ReligionState.MyInvites = info?.PendingInvites != null
            ? new List<PlayerReligionInfoResponsePacket.ReligionInviteInfo>(info.PendingInvites)
            : new List<PlayerReligionInfoResponsePacket.ReligionInviteInfo>();
        ReligionState.IsInvitesLoading = false;
        ReligionState.MyReligionError = null;
    }

    /// <summary>
    /// Draws the religion invites tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    public void DrawReligionInvites(float x, float y, float width, float height)
    {
        // Build view model from state
        var viewModel = new ReligionInvitesViewModel(
            invites: ConvertToInviteData(ReligionState.MyInvites),
            isLoading: ReligionState.IsInvitesLoading,
            scrollY: ReligionState.InvitesScrollY,
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionInvitesRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessInvitesEvents(result.Events);
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
                    ReligionState.InvitesScrollY = e.NewScrollY;
                    break;
            }
        }
    }

    /// <summary>
    /// Handle accept invite action
    /// All side effects (network, sound) happen here
    /// </summary>
    private void HandleAcceptInvite(string inviteId)
    {
        // Send network request
        RequestReligionAction("accept", string.Empty, inviteId);

        // Optional: Optimistic UI update
        ReligionState.IsInvitesLoading = true;
    }

    /// <summary>
    /// Handle decline invite action
    /// </summary>
    private void HandleDeclineInvite(string inviteId)
    {
        // Send network request
        RequestReligionAction("decline", string.Empty, inviteId);

        // Optional: Optimistic UI update
        ReligionState.IsInvitesLoading = true;
    }
}