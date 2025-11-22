using PantheonWars.GUI.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages state for the Guild Management Dialog UI
/// </summary>
public class GuildDialogManager : IGuildDialogManager
{
    private readonly ICoreClientAPI _capi;

    public GuildDialogManager(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    // Religion state
    public string? CurrentReligionUID { get; set; }
    public string? CurrentReligionName { get; set; }
    public int ReligionMemberCount { get; set; } = 0;
    public string? PlayerRoleInReligion { get; set; } // "Leader", "Member", etc.

    // Data loaded flags
    public bool IsDataLoaded { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, string? religionName)
    {
        CurrentReligionUID = religionUID;
        CurrentReligionName = religionName;
        IsDataLoaded = true;
    }

    /// <summary>
    ///     Reset all state
    /// </summary>
    public void Reset()
    {
        CurrentReligionUID = null;
        CurrentReligionName = null;
        ReligionMemberCount = 0;
        PlayerRoleInReligion = null;
        IsDataLoaded = false;
    }

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(CurrentReligionUID);
    }
}
