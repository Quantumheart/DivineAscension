using System.Collections.Generic;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.Interfaces;

/// <summary>
///     Interface for managing state for the Blessing Dialog UI
/// </summary>
public interface IBlessingDialogManager
{
    // Religion and deity state
    string? CurrentReligionUID { get; set; }
    DeityType CurrentDeity { get; set; }
    string? CurrentReligionName { get; set; }
    int ReligionMemberCount { get; set; }
    string? PlayerRoleInReligion { get; set; }

    // Player progression state (religion-only, no personal favor)
    int CurrentFavorRank { get; set; } // Kept for compatibility
    int CurrentPrestigeRank { get; set; }
    int CurrentFavor { get; set; } // Kept for compatibility
    int CurrentPrestige { get; set; }
    int TotalFavorEarned { get; set; } // Kept for compatibility

    // Blessing selection state
    string? SelectedBlessingId { get; set; }
    string? HoveringBlessingId { get; set; }

    // Scroll state
    float PlayerTreeScrollX { get; set; }
    float PlayerTreeScrollY { get; set; }
    float ReligionTreeScrollX { get; set; }
    float ReligionTreeScrollY { get; set; }

    // Data loaded flags
    bool IsDataLoaded { get; set; }

    // Blessing node states
    Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; }
    Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0, int prestigeRank = 0);

    /// <summary>
    ///     Reset all state
    /// </summary>
    void Reset();

    /// <summary>
    ///     Select a blessing (for displaying details)
    /// </summary>
    void SelectBlessing(string blessingId);

    /// <summary>
    ///     Clear blessing selection
    /// </summary>
    void ClearSelection();

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    bool HasReligion();

    /// <summary>
    ///     Load blessing states for player and religion blessings
    /// </summary>
    void LoadBlessingStates(List<Blessing> playerBlessings, List<Blessing> religionBlessings);

    /// <summary>
    ///     Get blessing node state by ID
    /// </summary>
    BlessingNodeState? GetBlessingState(string blessingId);

    /// <summary>
    ///     Get selected blessing's state (if any)
    /// </summary>
    BlessingNodeState? GetSelectedBlessingState();

    /// <summary>
    ///     Update unlock status for a blessing
    /// </summary>
    void SetBlessingUnlocked(string blessingId, bool unlocked);

    /// <summary>
    ///     Update all blessing states based on current unlock status and requirements
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
}
