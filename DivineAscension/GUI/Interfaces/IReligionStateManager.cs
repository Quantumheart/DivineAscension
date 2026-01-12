using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;

namespace DivineAscension.GUI.Interfaces;

public interface IReligionStateManager
{
    ReligionTabState State { get; }
    string? CurrentReligionUID { get; set; }
    DeityDomain CurrentReligionDomain { get; set; }
    string? CurrentDeityName { get; set; }
    string? CurrentReligionName { get; set; }
    int ReligionMemberCount { get; set; }
    string? PlayerRoleInReligion { get; set; } // "Leader", "Member", etc.
    int CurrentFavorRank { get; set; }
    int CurrentPrestigeRank { get; set; }
    int CurrentFavor { get; set; }
    int CurrentPrestige { get; set; }
    int TotalFavorEarned { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    void Initialize(string? id, DeityDomain domain, string? religionName, int favorRank = 0,
        int prestigeRank = 0);

    /// <summary>
    ///     Reset all state
    /// </summary>
    void Reset();

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    bool HasReligion();

    /// <summary>
    ///     Get player favor progress data
    /// </summary>
    PlayerFavorProgress GetPlayerFavorProgress();

    /// <summary>
    ///     Get religion prestige progress data
    /// </summary>
    ReligionPrestigeProgress GetReligionPrestigeProgress();

    /// <summary>
    ///     Request the list of religions from the server (filtered by deity when provided)
    /// </summary>
    void RequestReligionList(string deityFilter = "");

    /// <summary>
    ///     Request player's current religion information from the server
    /// </summary>
    void RequestPlayerReligionInfo();

    /// <summary>
    ///     Request a religion action (create, join, leave, invite, kick, ban, unban, edit_description, disband)
    /// </summary>
    void RequestReligionAction(string action, string religionId = "", string targetPlayerId = "");

    /// <summary>
    ///     Request to edit the current religion description
    /// </summary>
    void RequestEditReligionDescription(string id, string description);

    /// <summary>
    ///     Update religion list from server response
    /// </summary>
    void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions);

    /// <summary>
    ///     Update player religion info from server response
    /// </summary>
    void UpdatePlayerReligionInfo(PlayerReligionInfoResponsePacket? info);

    /// <summary>
    /// Draws the religion invites tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    void DrawReligionInvites(float x, float y, float width, float height);

    /// <summary>
    /// Draws the religion create tab using the refactored renderer
    /// Builds ViewModel, calls pure renderer, processes events
    /// </summary>
    void DrawReligionCreate(float x, float y, float width, float height);
}