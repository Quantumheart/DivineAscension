namespace PantheonWars.GUI.Interfaces;

/// <summary>
///     Interface for managing state for the Guild Management Dialog UI
/// </summary>
public interface IGuildDialogManager
{
    // Religion state
    string? CurrentReligionUID { get; set; }
    string? CurrentReligionName { get; set; }
    int ReligionMemberCount { get; set; }
    string? PlayerRoleInReligion { get; set; }

    // Data loaded flags
    bool IsDataLoaded { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    void Initialize(string? religionUID, string? religionName);

    /// <summary>
    ///     Reset all state
    /// </summary>
    void Reset();

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    bool HasReligion();
}
