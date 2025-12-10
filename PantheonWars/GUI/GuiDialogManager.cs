using System.Collections.Generic;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Managers;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Adapters.ReligionMembers;
using PantheonWars.GUI.UI.Adapters.Religions;
using PantheonWars.Models.Enum;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages state for the Gui Dialog
/// </summary>
public class GuiDialogManager : IBlessingDialogManager
{
    private readonly ICoreClientAPI _capi;

    public GuiDialogManager(ICoreClientAPI capi)
    {
        _capi = capi;
        ReligionStateManager = new ReligionStateManager(capi);
        BlessingStateManager = new BlessingStateManager(capi);
        // Initialize UI-only fake data provider in DEBUG builds. In Release it stays null.
#if DEBUG
        ReligionStateManager.MembersProvider = new FakeReligionMemberProvider();
        ReligionStateManager.MembersProvider.ConfigureDevSeed(500, 20251204);
        ReligionStateManager.UseReligionProvider(new FakeReligionProvider());
        ReligionStateManager.ReligionsProvider!.ConfigureDevSeed(500, 20251204);
        ReligionStateManager.RefreshReligionsFromProvider();
#endif
    }

    // Composite UI state
    public CivilizationState CivState { get; } = new();

    public ReligionStateManager ReligionStateManager { get; }
    public BlessingStateManager BlessingStateManager { get; }

    // Civilization state
    public string? CurrentCivilizationId { get; set; }
    public string? CurrentCivilizationName { get; set; }
    public string? CivilizationFounderReligionUID { get; set; }
    public List<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions { get; set; } = new();

    public bool IsCivilizationFounder => !string.IsNullOrEmpty(ReligionStateManager.CurrentReligionUID) &&
                                         !string.IsNullOrEmpty(CivilizationFounderReligionUID) &&
                                         ReligionStateManager.CurrentReligionUID == CivilizationFounderReligionUID;
    

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
        BlessingStateManager.State.Reset();
    }

    /// <summary>
    ///     Reset all state
    /// </summary>
    public void Reset()
    {
        ReligionStateManager.Reset();
        BlessingStateManager.State.Reset();

        // Keep blessing UI state reset here (for backward compatibility)
        IsDataLoaded = false;

        // Keep civilization state (separate concern)
        CurrentCivilizationId = null;
        CurrentCivilizationName = null;
        CivilizationFounderReligionUID = null;
        CivilizationMemberReligions.Clear();
        CivState.Reset();
    }

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    public bool HasReligion() => ReligionStateManager.HasReligion();

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
        system?.NetworkClient?.RequestCivilizationList(deityFilter);
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
        system?.NetworkClient?.RequestCivilizationInfo(civIdOrEmpty);
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        // Clear transient action error; some actions will trigger refreshes
        CivState.LastActionError = null;
        var system = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        system?.NetworkClient?.RequestCivilizationAction(action, civId, targetId, name);
    }
}