using System.Collections.Generic;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Managers;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Adapters.ReligionMembers;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Network.Civilization;
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
        ReligionStateManager = new ReligionStateManager(capi);
        // Initialize UI-only fake data provider in DEBUG builds. In Release it stays null.
#if DEBUG
        ReligionStateManager.MembersProvider = new FakeReligionMemberProvider();
        ReligionStateManager.MembersProvider.ConfigureDevSeed(500, 20251204);
#endif
    }

    // Composite UI state
    public CivilizationState CivState { get; } = new();
    
    public ReligionStateManager ReligionStateManager { get; }

    // Civilization state
    public string? CurrentCivilizationId { get; set; }
    public string? CurrentCivilizationName { get; set; }
    public string? CivilizationFounderReligionUID { get; set; }
    public List<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions { get; set; } = new();

    public bool IsCivilizationFounder => !string.IsNullOrEmpty(ReligionStateManager.CurrentReligionUID) &&
                                         !string.IsNullOrEmpty(CivilizationFounderReligionUID) &&
                                         ReligionStateManager.CurrentReligionUID == CivilizationFounderReligionUID;

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
    
    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        ReligionStateManager.Initialize(religionUID, deity, religionName, favorRank, prestigeRank);
        IsDataLoaded = true;
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
        ReligionStateManager.Reset();
        // Keep blessing UI state reset here
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerTreeScrollX = 0f;
        PlayerTreeScrollY = 0f;
        ReligionTreeScrollX = 0f;
        ReligionTreeScrollY = 0f;
        IsDataLoaded = false;

        // Keep civilization state (separate concern)
        CurrentCivilizationId = null;
        CurrentCivilizationName = null;
        CivilizationFounderReligionUID = null;
        CivilizationMemberReligions.Clear();
        CivState.Reset();
        
        ReligionStateManager.Reset();
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
    public bool HasReligion() => ReligionStateManager.HasReligion();

    /// <summary>
    ///     Get selected blessing's state (if any)
    /// </summary>
    public BlessingNodeState? GetSelectedBlessingState()
    {
        if (string.IsNullOrEmpty(SelectedBlessingId)) return null;

        return ReligionStateManager.GetBlessingState(SelectedBlessingId);
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
}