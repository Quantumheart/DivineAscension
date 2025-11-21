using System.Collections.Generic;
using PantheonWars.GUI.Interfaces;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages state for the Blessing Dialog UI
/// </summary>
public class BlessingDialogManager : IBlessingDialogManager
{
    private readonly ICoreClientAPI _capi;

    public BlessingDialogManager(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    // Religion and deity state
    public string? CurrentReligionUID { get; set; }
    public DeityType CurrentDeity { get; set; } = DeityType.None;
    public string? CurrentReligionName { get; set; }
    public int ReligionMemberCount { get; set; } = 0;
    public string? PlayerRoleInReligion { get; set; } // "Leader", "Member", etc.

    // Player progression state (religion-only, no personal favor)
    public int CurrentFavorRank { get; set; } = 0; // Kept for interface compatibility
    public int CurrentPrestigeRank { get; set; } = 0;
    public int CurrentFavor { get; set; } = 0; // Kept for interface compatibility
    public int CurrentPrestige { get; set; } = 0;
    public int TotalFavorEarned { get; set; } = 0; // Kept for interface compatibility

    // Blessing selection state
    public string? SelectedBlessingId { get; set; }
    public string? HoveringBlessingId { get; set; }

    // Scroll state
    public float PlayerTreeScrollX { get; set; }
    public float PlayerTreeScrollY { get; set; }
    public float ReligionTreeScrollX { get; set; }
    public float ReligionTreeScrollY { get; set; }

    // Data loaded flags
    public bool IsDataLoaded { get; set; }

    // Blessing node states (Phase 2)
    public Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; } = new();
    public Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; } = new();

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        CurrentReligionUID = religionUID;
        CurrentDeity = deity;
        CurrentReligionName = religionName;
        CurrentFavorRank = favorRank;
        CurrentPrestigeRank = prestigeRank;
        IsDataLoaded = true;

        // Reset selection and scroll
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerTreeScrollX = 0f;
        PlayerTreeScrollY = 0f;
        ReligionTreeScrollX = 0f;
        ReligionTreeScrollY = 0f;
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
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerTreeScrollX = 0f;
        PlayerTreeScrollY = 0f;
        ReligionTreeScrollX = 0f;
        ReligionTreeScrollY = 0f;
        IsDataLoaded = false;

        // Clear blessing trees
        PlayerBlessingStates.Clear();
        ReligionBlessingStates.Clear();
    }

    /// <summary>
    ///     Select a blessing (for displaying details)
    /// </summary>
    public void SelectBlessing(string blessingId)
    {
        SelectedBlessingId = blessingId;
    }

    /// <summary>
    ///     Clear blessing selection
    /// </summary>
    public void ClearSelection()
    {
        SelectedBlessingId = null;
    }

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(CurrentReligionUID) && CurrentDeity != DeityType.None;
    }

    /// <summary>
    ///     Load blessing states for player and religion blessings
    ///     Called in Phase 6 when connected to BlessingRegistry
    /// </summary>
    public void LoadBlessingStates(List<Blessing> playerBlessings, List<Blessing> religionBlessings)
    {
        PlayerBlessingStates.Clear();
        ReligionBlessingStates.Clear();

        foreach (var blessing in playerBlessings)
        {
            var state = new BlessingNodeState(blessing);
            PlayerBlessingStates[blessing.BlessingId] = state;
        }

        foreach (var blessing in religionBlessings)
        {
            var state = new BlessingNodeState(blessing);
            ReligionBlessingStates[blessing.BlessingId] = state;
        }
    }

    /// <summary>
    ///     Get blessing node state by ID
    /// </summary>
    public BlessingNodeState? GetBlessingState(string blessingId)
    {
        if (PlayerBlessingStates.TryGetValue(blessingId, out var playerState)) return playerState;

        if (ReligionBlessingStates.TryGetValue(blessingId, out var religionState)) return religionState;

        return null;
    }

    /// <summary>
    ///     Get selected blessing's state (if any)
    /// </summary>
    public BlessingNodeState? GetSelectedBlessingState()
    {
        if (string.IsNullOrEmpty(SelectedBlessingId)) return null;

        return GetBlessingState(SelectedBlessingId);
    }

    /// <summary>
    ///     Update unlock status for a blessing
    /// </summary>
    public void SetBlessingUnlocked(string blessingId, bool unlocked)
    {
        var state = GetBlessingState(blessingId);
        if (state != null)
        {
            state.IsUnlocked = unlocked;
            state.UpdateVisualState();
        }
    }

    /// <summary>
    ///     Update all blessing states based on current unlock status and requirements
    ///     Called after data refresh in Phase 6
    /// </summary>
    public void RefreshAllBlessingStates()
    {
        // Update CanUnlock status for all player blessings
        foreach (var state in PlayerBlessingStates.Values)
        {
            state.CanUnlock = CanUnlockBlessing(state);
            state.UpdateVisualState();
        }

        // Update CanUnlock status for all religion blessings
        foreach (var state in ReligionBlessingStates.Values)
        {
            state.CanUnlock = CanUnlockBlessing(state);
            state.UpdateVisualState();
        }
    }

    /// <summary>
    ///     Check if a blessing can be unlocked based on prerequisites and rank requirements
    ///     This is a client-side validation - server will do final validation
    /// </summary>
    private bool CanUnlockBlessing(BlessingNodeState state)
    {
        // Already unlocked
        if (state.IsUnlocked) return false;

        // Check prerequisites
        if (state.Blessing.PrerequisiteBlessings != null && state.Blessing.PrerequisiteBlessings.Count > 0)
        {
            foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
            {
                var prereqState = GetBlessingState(prereqId);
                if (prereqState == null || !prereqState.IsUnlocked) return false; // Prerequisite not unlocked
            }
        }

        // Check rank requirements based on blessing kind
        if (state.Blessing.Kind == BlessingKind.Player)
        {
            // Player blessings require favor rank
            if (state.Blessing.RequiredFavorRank > CurrentFavorRank) return false;
        }
        else if (state.Blessing.Kind == BlessingKind.Religion)
        {
            // Religion blessings require prestige rank
            if (state.Blessing.RequiredPrestigeRank > CurrentPrestigeRank) return false;
        }

        return true; // All requirements met
    }

    /// <summary>
    ///     Get player favor progress data (stub - favor system removed)
    /// </summary>
    public PlayerFavorProgress GetPlayerFavorProgress()
    {
        // Favor system removed - return empty progress
        return new PlayerFavorProgress
        {
            CurrentFavor = 0,
            RequiredFavor = 0,
            CurrentRank = 0,
            NextRank = 0,
            IsMaxRank = true
        };
    }

    /// <summary>
    ///     Get religion prestige progress data
    /// </summary>
    public ReligionPrestigeProgress GetReligionPrestigeProgress()
    {
        return new ReligionPrestigeProgress
        {
            CurrentPrestige = CurrentPrestige,
            RequiredPrestige = RankRequirements.GetRequiredPrestigeForNextRank(CurrentPrestigeRank),
            CurrentRank = CurrentPrestigeRank,
            NextRank = CurrentPrestigeRank + 1,
            IsMaxRank = CurrentPrestigeRank >= 4
        };
    }
}