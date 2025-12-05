using System.Collections.Generic;
using PantheonWars.GUI.State;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Network;

namespace PantheonWars.GUI.Interfaces;

public interface IReligionStateManager
{
    ReligionTabState State { get; }
    string? CurrentReligionUID { get; set; }
    DeityType CurrentDeity { get; set; }
    string? CurrentReligionName { get; set; }
    int ReligionMemberCount { get; set; }
    string? PlayerRoleInReligion { get; set; } // "Leader", "Member", etc.
    int CurrentFavorRank { get; set; }
    int CurrentPrestigeRank { get; set; }
    int CurrentFavor { get; set; }
    int CurrentPrestige { get; set; }
    int TotalFavorEarned { get; set; }
    Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; }
    Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    void Initialize(string? id, DeityType deity, string? religionName, int favorRank = 0,
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
    ///     Load blessing states for player and religion blessings
    ///     Called in Phase 6 when connected to BlessingRegistry
    /// </summary>
    void LoadBlessingStates(List<Blessing> playerBlessings, List<Blessing> religionBlessings);

    /// <summary>
    ///     Get blessing node state by ID
    /// </summary>
    BlessingNodeState? GetBlessingState(string blessingId);

    
    /// <summary>
    ///     Update unlock status for a blessing
    /// </summary>
    void SetBlessingUnlocked(string blessingId, bool unlocked);

    /// <summary>
    ///     Update all blessing states based on current unlock status and requirements
    ///     Called after data refresh in Phase 6
    /// </summary>
    void RefreshAllBlessingStates();

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