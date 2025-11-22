using PantheonWars.GUI.Interfaces;
using PantheonWars.Models.Enum;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages state for the Guild Management Dialog UI
/// </summary>
public class BlessingDialogManager : IBlessingDialogManager
{
    private readonly ICoreClientAPI _capi;

    public BlessingDialogManager(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    // Religion state
    public string? CurrentReligionUID { get; set; }
    public DeityType CurrentDeity { get; set; } = DeityType.None;
    public string? CurrentReligionName { get; set; }
    public int ReligionMemberCount { get; set; } = 0;
    public string? PlayerRoleInReligion { get; set; } // "Leader", "Member", etc.

    // Data loaded flags
    public bool IsDataLoaded { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        CurrentReligionUID = religionUID;
        CurrentDeity = deity;
        CurrentReligionName = religionName;
        IsDataLoaded = true;
    }

    /// <summary>
    ///     Reset all state
    /// </summary>
    public void Reset()
    {
        CurrentReligionUID = null;
        CurrentDeity = DeityType.None;
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
