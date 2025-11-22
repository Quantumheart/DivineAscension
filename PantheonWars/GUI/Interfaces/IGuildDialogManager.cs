using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.Interfaces;

/// <summary>
///     Interface for managing state for the Guild Management Dialog UI
/// </summary>
public interface IGuildDialogManager
{
    // Religion state
    string? CurrentReligionUID { get; set; }
    DeityType CurrentDeity { get; set; }
    string? CurrentReligionName { get; set; }
    int ReligionMemberCount { get; set; }
    string? PlayerRoleInReligion { get; set; }

    // Data loaded flags
    bool IsDataLoaded { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0, int prestigeRank = 0);

    /// <summary>
    ///     Reset all state
    /// </summary>
    void Reset();

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    bool HasReligion();
}
