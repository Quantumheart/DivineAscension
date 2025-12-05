using System.Collections.Generic;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.Interfaces;

/// <summary>
///     Interface for managing state for the Blessing Dialog UI
/// </summary>
public interface IBlessingDialogManager
{

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
    
    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0);

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
    ///     Get selected blessing's state (if any)
    /// </summary>
    BlessingNodeState? GetSelectedBlessingState();
}