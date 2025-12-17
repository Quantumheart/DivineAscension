using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Interfaces;

/// <summary>
///     Interface for managing state for the Blessing Dialog UI
/// </summary>
public interface IBlessingDialogManager
{
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
    ///     Check if player has a religion
    /// </summary>
    bool HasReligion();
}