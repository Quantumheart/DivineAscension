using System;
using System.Collections.Generic;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Models.Blessing.Tab;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Renderers.Blessing;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.Managers;

/// <summary>
///     Manages blessing tab state and event processing
/// </summary>
public class BlessingStateManager(ICoreClientAPI api, IUiService uiService)
{
    private readonly ICoreClientAPI _coreClientApi = api ?? throw new ArgumentNullException(nameof(api));
    private readonly IUiService _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

    public BlessingTabState State { get; } = new();

    /// <summary>
    ///     Draws the blessings tab and processes all events
    /// </summary>
    public void DrawBlessingsTab(BlessingTabViewModel vm)
    {
        var result = BlessingTabRenderer.DrawBlessingsTab(vm);

        ProcessBlessingTabEvents(result);
    }

    /// <summary>
    ///     Processes all blessing tab events (side effects: state updates, sounds, network requests)
    /// </summary>
    private void ProcessBlessingTabEvents(BlessingTabRenderResult result)
    {
        // Update hovering state from result
        State.TreeState.HoveringBlessingId = result.HoveringBlessingId;

        // Process tree events
        foreach (var ev in result.TreeEvents)
            switch (ev)
            {
                case BlessingTreeEvent.BlessingSelected e:
                    // Update state
                    State.TreeState.SelectedBlessingId = e.BlessingId;
                    // Play sound
                    _coreClientApi.World.PlaySoundAt(
                        new AssetLocation("pantheonwars:sounds/click"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;

                case BlessingTreeEvent.BlessingHovered e:
                    // State already updated from result.HoveringBlessingId
                    break;

                case BlessingTreeEvent.PlayerTreeScrollChanged e:
                    // Update state
                    State.TreeState.PlayerScrollState.X = e.ScrollX;
                    State.TreeState.PlayerScrollState.Y = e.ScrollY;
                    break;

                case BlessingTreeEvent.ReligionTreeScrollChanged e:
                    // Update state
                    State.TreeState.ReligionScrollState.X = e.ScrollX;
                    State.TreeState.ReligionScrollState.Y = e.ScrollY;
                    break;
            }

        // Process action events
        foreach (var ev in result.ActionsEvents)
            switch (ev)
            {
                case BlessingActionsEvent.UnlockClicked:
                    HandleUnlockClicked();
                    break;

                case BlessingActionsEvent.UnlockBlockedClicked:
                    // Play error sound
                    _coreClientApi.World.PlaySoundAt(
                        new AssetLocation("pantheonwars:sounds/error"),
                        _coreClientApi.World.Player.Entity, null, false, 8f, 0.3f);
                    break;
            }
    }


    /// <summary>
    ///     Handles blessing unlock request
    /// </summary>
    private void HandleUnlockClicked()
    {
        if (State.TreeState.SelectedBlessingId == null) return;
        var selectedState = GetBlessingState(State.TreeState.SelectedBlessingId);
        if (selectedState == null || !selectedState.CanUnlock || selectedState.IsUnlocked)
            return;

        // Client-side validation
        if (string.IsNullOrEmpty(selectedState.Blessing.BlessingId))
        {
            _coreClientApi.ShowChatMessage("Error: Invalid blessing ID");
            return;
        }

        // Play click sound
        _coreClientApi.World.PlaySoundAt(
            new AssetLocation("pantheonwars:sounds/click"),
            _coreClientApi.World.Player.Entity, null, false, 8f, 0.5f);

        _uiService.RequestBlessingUnlock(selectedState.Blessing.BlessingId);
    }

    private BlessingNodeState? GetBlessingState(string? blessingId)
    {
        return State.PlayerBlessingStates.TryGetValue(blessingId, out var playerState)
            ? playerState
            : State.ReligionBlessingStates.GetValueOrDefault(blessingId);
    }

    /// <summary>
    ///     Get a selected blessing's state (if any)
    /// </summary>
    public BlessingNodeState? GetSelectedBlessingState()
    {
        if (string.IsNullOrEmpty(State.TreeState.SelectedBlessingId)) return null;

        return GetBlessingState(State.TreeState.SelectedBlessingId);
    }

    public void LoadBlessingStates(List<Blessing> playerBlessings, List<Blessing> religionBlessings)
    {
        State.PlayerBlessingStates.Clear();
        State.ReligionBlessingStates.Clear();

        foreach (var blessing in playerBlessings)
        {
            var state = new BlessingNodeState(blessing);
            State.PlayerBlessingStates[blessing.BlessingId] = state;
        }

        foreach (var blessing in religionBlessings)
        {
            var state = new BlessingNodeState(blessing);
            State.ReligionBlessingStates[blessing.BlessingId] = state;
        }
    }

    public void SetBlessingUnlocked(string blessingId, bool unlocked)
    {
        var state = GetBlessingState(blessingId);
        if (state != null)
        {
            state.IsUnlocked = unlocked;
            state.UpdateVisualState();
        }
    }

    public void RefreshAllBlessingStates(int currentFavorRank, int currentPrestigeRank)
    {
        // Update CanUnlock status for all player blessings
        foreach (var state in State.PlayerBlessingStates.Values)
        {
            state.CanUnlock = CanUnlockBlessing(state, currentFavorRank, currentPrestigeRank);
            state.UpdateVisualState();
        }

        // Update CanUnlock status for all religion blessings
        foreach (var state in State.ReligionBlessingStates.Values)
        {
            state.CanUnlock = CanUnlockBlessing(state, currentFavorRank, currentPrestigeRank);
            state.UpdateVisualState();
        }
    }

    /// <summary>
    ///     Check if a blessing can be unlocked based on prerequisites and rank requirements
    ///     This is a client-side validation - server will do final validation
    /// </summary>
    private bool CanUnlockBlessing(BlessingNodeState state, int currentFavorRank, int currentPrestigeRank)
    {
        // Already unlocked
        if (state.IsUnlocked) return false;

        // Check prerequisites
        if (state.Blessing.PrerequisiteBlessings is { Count: > 0 })
            foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
            {
                var prereqState = GetBlessingState(prereqId);
                if (prereqState == null || !prereqState.IsUnlocked) return false; // Prerequisite not unlocked
            }

        // Check rank requirements based on the blessing kind
        if (state.Blessing.Kind == BlessingKind.Player)
        {
            // Player blessings require favor rank
            if (state.Blessing.RequiredFavorRank > currentFavorRank) return false;
        }
        else if (state.Blessing.Kind == BlessingKind.Religion)
        {
            // Religion blessings require prestige rank
            if (state.Blessing.RequiredPrestigeRank > currentPrestigeRank) return false;
        }

        return true; // All requirements met
    }

    /// <summary>
    ///     Clear blessing selection
    /// </summary>
    public void ClearSelection()
    {
        State.TreeState.SelectedBlessingId = null;
    }


    /// <summary>
    ///     Select a blessing (for displaying details)
    /// </summary>
    public void SelectBlessing(string blessingId)
    {
        State.TreeState.SelectedBlessingId = blessingId;
    }
}