using System;
using System.Collections.Generic;
using PantheonWars.GUI.State;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.Managers;

public class CivilizationStateManager(ICoreClientAPI coreClientApi, IUiService uiService)
{
    private readonly ICoreClientAPI _coreClientApi =
        coreClientApi ?? throw new ArgumentNullException(nameof(coreClientApi));

    private readonly IUiService _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

    private CivilizationTabState State { get; } = new();

    public string CurrentCivilizationId { get; set; } = string.Empty;

    public List<CivilizationInfoResponsePacket.MemberReligion>? CivilizationMemberReligions { get; set; } = new();

    public string CivilizationFounderReligionUID { get; set; } = string.Empty;

    public string CurrentCivilizationName { get; set; } = string.Empty;

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
            CivilizationFounderReligionUID = string.Empty;
            CivilizationMemberReligions?.Clear();
            State.InfoState.MyCivilization = null;
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
                CivilizationFounderReligionUID = string.Empty;
                CivilizationMemberReligions?.Clear();
                State.InfoState.MyCivilization = null;
                State.InviteState.MyInvites = new List<CivilizationInfoResponsePacket.PendingInvite>(
                    details.PendingInvites ??
                    []);
            }
            else
            {
                CurrentCivilizationId = details.CivId;
                CurrentCivilizationName = details.Name;
                CivilizationFounderReligionUID = details.FounderReligionUID;
                CivilizationMemberReligions =
                    new List<CivilizationInfoResponsePacket.MemberReligion>(details.MemberReligions ?? []);
                State.InfoState.MyCivilization = details;
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
        // Set loading state for browse
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
    ///     Request a civilization action (create, invite, accept, leave, kick, disband)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        // Clear transient action error; some actions will trigger refreshes
        State.LastActionError = null;
        _uiService.RequestCivilizationAction(action, civId, targetId, name);
    }
}