using System;
using System.Linq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI;

/// <summary>
///     Event handlers for BlessingDialog - extracted from main class for maintainability
/// </summary>
public partial class GuiDialog
{
    private const string PANTHEONWARS_SOUNDS_DEITIES = "pantheonwars:sounds/deities/";

    /// <summary>
    ///     Periodically check if player religion data is available
    /// </summary>
    private void OnCheckDataAvailability(float dt)
    {
        if (_state.IsReady) return;

        // Request blessing data from server
        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.RequestBlessingData();
            // Don't set _state.IsReady yet - wait for server response in OnBlessingDataReceived
            _capi!.Event.UnregisterGameTickListener(_checkDataId);
        }
    }

    /// <summary>
    ///     Handle blessing data received from server
    /// </summary>
    private void OnBlessingDataReceived(BlessingDataResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Processing blessing data: HasReligion={packet.HasReligion}");

        if (!packet.HasReligion)
        {
            _capi.Logger.Debug("[PantheonWars] Player has no religion - data ready for 'No Religion' state");
            _manager!.Reset();
            _state.IsReady = true; // Set ready so dialog can open to show "No Religion" state

            // Dialog will only open when player presses the keybind (Shift+G)

            return;
        }

        // Parse deity type from string
        if (!Enum.TryParse<DeityType>(packet.Deity, out var deityType))
        {
            _capi.Logger.Error($"[PantheonWars] Invalid deity type: {packet.Deity}");
            return;
        }

        // Initialize manager with real data
        _manager!.Initialize(packet.ReligionUID, deityType, packet.ReligionName, packet.FavorRank,
            packet.PrestigeRank);

        // Set current favor and prestige values for progress bars
        _manager.ReligionStateManager.CurrentFavor = packet.CurrentFavor;
        _manager.ReligionStateManager.CurrentPrestige = packet.CurrentPrestige;
        _manager.ReligionStateManager.TotalFavorEarned = packet.TotalFavorEarned;

        // Convert packet blessings to Blessing objects
        var playerBlessings = packet.PlayerBlessings.Select(p => new Blessing(p.BlessingId, p.Name, deityType)
        {
            Kind = BlessingKind.Player,
            Category = (BlessingCategory)p.Category,
            Description = p.Description,
            RequiredFavorRank = p.RequiredFavorRank,
            RequiredPrestigeRank = p.RequiredPrestigeRank,
            PrerequisiteBlessings = p.PrerequisiteBlessings,
            StatModifiers = p.StatModifiers
        }).ToList();

        var religionBlessings = packet.ReligionBlessings.Select(p => new Blessing(p.BlessingId, p.Name, deityType)
        {
            Kind = BlessingKind.Religion,
            Category = (BlessingCategory)p.Category,
            Description = p.Description,
            RequiredFavorRank = p.RequiredFavorRank,
            RequiredPrestigeRank = p.RequiredPrestigeRank,
            PrerequisiteBlessings = p.PrerequisiteBlessings,
            StatModifiers = p.StatModifiers
        }).ToList();

        // Load blessing states into manager
        _manager.ReligionStateManager.LoadBlessingStates(playerBlessings, religionBlessings);

        // Mark unlocked blessings
        foreach (var blessingId in packet.UnlockedPlayerBlessings) _manager.ReligionStateManager.SetBlessingUnlocked(blessingId, true);

        foreach (var blessingId in packet.UnlockedReligionBlessings) _manager.ReligionStateManager.SetBlessingUnlocked(blessingId, true);

        // Refresh states to update can-unlock status
        _manager.ReligionStateManager.RefreshAllBlessingStates();

        _state.IsReady = true;
        _capi.Logger.Notification(
            $"[PantheonWars] Loaded {playerBlessings.Count} player blessings and {religionBlessings.Count} religion blessings for {packet.Deity}");

        // Request player religion info to get founder status (needed for Manage Religion button)
        _pantheonWarsSystem?.RequestPlayerReligionInfo();

        // Request civilization info for player's religion (empty string = my civ)
        _pantheonWarsSystem?.RequestCivilizationInfo("");

    }

    /// <summary>
    ///     Handle religion state change (religion disbanded, kicked, etc.)
    /// </summary>
    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi!.Logger.Notification($"[PantheonWars] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi.ShowChatMessage(packet.Reason);

        // Close any open overlays

        // Reset blessing dialog state to "No Religion" mode
        _manager!.Reset();
        _state.IsReady = true; // Keep dialog ready so it doesn't close

        // Request fresh data from server (will show "No Religion" state)
        _pantheonWarsSystem?.RequestBlessingData();

        // If notification is about civilization, also refresh civilization data
        if (packet.Reason.Contains("civilization", StringComparison.OrdinalIgnoreCase))
        {
            _manager?.RequestCivilizationInfo(string.Empty);
        }
    }

    /// <summary>
    ///     Keybind handler - toggle dialog open/close
    /// </summary>
    private bool OnToggleDialog(KeyCombination keyCombination)
    {
        if (_state.IsOpen)
            Close();
        else
            Open();
        return true;
    }

    /// <summary>
    ///     Handle unlock button click
    /// </summary>
    private void OnUnlockButtonClicked()
    {
        var selectedState = _manager!.GetSelectedBlessingState();
        if (selectedState == null || !selectedState.CanUnlock || selectedState.IsUnlocked) return;

        // Client-side validation before sending the request
        if (string.IsNullOrEmpty(selectedState.Blessing.BlessingId))
        {
            _capi!.ShowChatMessage("Error: Invalid blessing ID");
            return;
        }

        // Send unlock request to server
        _capi!.Logger.Debug($"[PantheonWars] Sending unlock request for: {selectedState.Blessing.Name}");
        _pantheonWarsSystem?.RequestBlessingUnlock(selectedState.Blessing.BlessingId);
    }

    /// <summary>
    ///     Handle close button click
    /// </summary>
    private void OnCloseButtonClicked()
    {
        Close();
    }

    /// <summary>
    ///     Handle religion list received from server
    /// </summary>
    private void OnReligionListReceived(ReligionListResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received {packet.Religions.Count} religions from server");
        // Update manager religion tab state
        _manager!.ReligionStateManager.UpdateReligionList(packet.Religions);
    }

    /// <summary>
    ///     Handle religion action completed (join, leave, etc.)
    /// </summary>
    private void OnReligionActionCompleted(ReligionActionResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Religion action '{packet.Action}' completed: {packet.Message}");

        if (packet.Success)
        {
            _capi.ShowChatMessage(packet.Message);

            // Play success sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);


            // If leaving religion, reset blessing dialog state immediately
            if (packet.Action == "leave")
            {
                _capi.Logger.Debug("[PantheonWars] Resetting blessing dialog after leaving religion");
                _manager!.Reset();
            }

            // Refresh religion tab data
            _manager!.ReligionStateManager.State.BrowseState.IsBrowseLoading = true;
            _pantheonWarsSystem?.RequestReligionList(_manager.ReligionStateManager.State.BrowseState.DeityFilter);

            if (_manager.HasReligion() && packet.Action != "leave")
            {
                _manager.ReligionStateManager.State.InfoState.Loading = true;
                _pantheonWarsSystem?.RequestPlayerReligionInfo();
            }

            // Request fresh blessing data (religion may have changed)
            _pantheonWarsSystem?.RequestBlessingData();

            // Clear confirmations
            _manager.ReligionStateManager.State.InfoState.ShowDisbandConfirm = false;
            _manager.ReligionStateManager.State.InfoState.KickConfirmPlayerUID = null;
            _manager.ReligionStateManager.State.InfoState.BanConfirmPlayerUID = null;
        }
        else
        {
            _capi.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);

            // Store error in state
            _manager!.ReligionStateManager.State.ErrorState.LastActionError = packet.Message;
        }
    }

    /// <summary>
    ///     Handle player religion info received from server
    /// </summary>
    private void OnPlayerReligionInfoReceived(PlayerReligionInfoResponsePacket packet)
    {
        _capi!.Logger.Debug(
            $"[PantheonWars] Received player religion info: HasReligion={packet.HasReligion}, IsFounder={packet.IsFounder}");

        // Update manager religion tab state
        _manager!.ReligionStateManager.UpdatePlayerReligionInfo(packet);

        // Update manager with player's role (enables Manage Religion button for leaders)
        if (packet.HasReligion)
        {
            _manager.ReligionStateManager.PlayerRoleInReligion = packet.IsFounder ? "Leader" : "Member";
            _manager.ReligionStateManager.ReligionMemberCount = packet.Members.Count;
            _capi!.Logger.Debug(
                $"[PantheonWars] Set PlayerRoleInReligion to: {_manager.ReligionStateManager.PlayerRoleInReligion}, MemberCount: {_manager.ReligionStateManager.ReligionMemberCount}");
        }
        else
        {
            _manager.ReligionStateManager.PlayerRoleInReligion = null;
            _manager.ReligionStateManager.ReligionMemberCount = 0;
            _capi!.Logger.Debug("[PantheonWars] Cleared PlayerRoleInReligion (no religion)");
        }
    }

    /// <summary>
    ///     Handle player religion data updates (favor, rank, etc.)
    /// </summary>
    private void OnPlayerReligionDataUpdated(PlayerReligionDataPacket packet)
    {
        // Skip if manager is not initialized yet
        if (_manager == null) return;

        _capi!.Logger.Debug(
            $"[PantheonWars] Updating blessing dialog with new favor data: {packet.Favor}, Total: {packet.TotalFavorEarned}");

        // Always update manager with new values, even if dialog is closed
        // This ensures the UI shows correct values when opened
        _manager.ReligionStateManager.CurrentFavor = packet.Favor;
        _manager.ReligionStateManager.CurrentPrestige = packet.Prestige;
        _manager.ReligionStateManager.TotalFavorEarned = packet.TotalFavorEarned;

        // Update rank if it changed (this affects which blessings can be unlocked)
        // FavorRank comes as enum name (e.g., "Initiate", "Disciple"), parse to get numeric value
        if (Enum.TryParse<FavorRank>(packet.FavorRank, out var favorRankEnum))
            _manager.ReligionStateManager.CurrentFavorRank = (int)favorRankEnum;

        if (Enum.TryParse<PrestigeRank>(packet.PrestigeRank, out var prestigeRankEnum))
            _manager.ReligionStateManager.CurrentPrestigeRank = (int)prestigeRankEnum;

        // Refresh blessing states in case new blessings became available
        // Only do this if dialog is open to avoid unnecessary processing
        if (_state.IsOpen && _manager.HasReligion()) _manager.ReligionStateManager.RefreshAllBlessingStates();
    }

    /// <summary>
    ///     Handle blessing unlock response from server
    /// </summary>
    private void OnBlessingUnlockedFromServer(string blessingId, bool success)
    {
        if (!success)
        {
            _capi!.Logger.Debug($"[PantheonWars] Blessing unlock failed: {blessingId}");

            // Play error sound on failure
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);

            return;
        }

        _capi!.Logger.Debug($"[PantheonWars] Blessing unlocked from server: {blessingId}");

        // Play unlock success sound
        if (_manager != null)
        {
            switch (_manager.ReligionStateManager.CurrentDeity)
            {
                case DeityType.None:
                    _capi.World.PlaySoundAt(
                        new AssetLocation("pantheonwars:sounds/unlock"),
                        _capi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;
                case DeityType.Khoras:
                    _capi.World.PlaySoundAt(
                        new AssetLocation($"{PANTHEONWARS_SOUNDS_DEITIES}{nameof(DeityType.Khoras)}"),
                        _capi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;
                case DeityType.Lysa:
                    _capi.World.PlaySoundAt(new AssetLocation($"{PANTHEONWARS_SOUNDS_DEITIES}{nameof(DeityType.Lysa)}"),
                        _capi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;
                case DeityType.Aethra:
                    _capi.World.PlaySoundAt(
                        new AssetLocation($"{PANTHEONWARS_SOUNDS_DEITIES}{nameof(DeityType.Aethra)}"),
                        _capi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;
                case DeityType.Gaia:
                    _capi.World.PlaySoundAt(new AssetLocation($"{PANTHEONWARS_SOUNDS_DEITIES}{nameof(DeityType.Gaia)}"),
                        _capi.World.Player.Entity, null, false, 8f, 0.5f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update manager state
            _manager?.ReligionStateManager.SetBlessingUnlocked(blessingId, true);

            // Refresh all blessing states to update prerequisites and glow effects
            _manager?.ReligionStateManager.RefreshAllBlessingStates();
        }
    }

    /// <summary>
    ///     Handle civilization list received from server
    /// </summary>
    private void OnCivilizationListReceived(CivilizationListResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received civilization list: {packet.Civilizations.Count} items");

        // Update manager browse state
        _manager!.CivState.AllCivilizations = packet.Civilizations;
        _manager.CivState.IsBrowseLoading = false;
        _manager.CivState.BrowseError = null;
    }

    /// <summary>
    ///     Handle civilization info received from server
    /// </summary>
    private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received civilization info: HasCiv={packet.Details != null}");

        // Update manager with civilization state
        _manager!.UpdateCivilizationState(packet.Details);

        // Clear appropriate loading flags depending on whether we're viewing details or my civ
        if (!string.IsNullOrEmpty(_manager.CivState.ViewingCivilizationId) &&
            packet.Details != null &&
            packet.Details.CivId == _manager.CivState.ViewingCivilizationId)
        {
            _manager.CivState.IsDetailsLoading = false;
            _manager.CivState.DetailsError = null;
        }
        else
        {
            // Treat as my-civ refresh (also covers the null details case for "not in a civ")
            _manager.CivState.IsMyCivLoading = false;
            _manager.CivState.IsInvitesLoading = false;
            _manager.CivState.MyCivError = null;
            _manager.CivState.InvitesError = null;
            _manager.CivState.IsDetailsLoading = false; // ensure off if previously set
        }

        if (packet.Details != null)
            _capi.Logger.Notification(
                $"[PantheonWars] Loaded civilization '{packet.Details.Name}' with {packet.Details.MemberReligions?.Count} religions");
        else
            _capi.Logger.Debug("[PantheonWars] Player's religion is not in a civilization");
    }

    /// <summary>
    ///     Handle civilization action completed (create, invite, accept, leave, kick, disband)
    /// </summary>
    private void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
    {
        _capi!.Logger.Debug(
            $"[PantheonWars] Civilization action completed: Success={packet.Success}, Message={packet.Message}");

        // Show result message to user
        _capi.ShowChatMessage(packet.Message);

        if (packet.Success)
        {
            // Play success sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);

            // Request updated civilization data to refresh UI
            // Refresh both the browse list and the player's civilization info
            // Set loading flags since we're calling system directly (bypassing manager helpers)
            _manager!.CivState.IsBrowseLoading = true;
            _manager.CivState.BrowseError = null;
            _pantheonWarsSystem?.RequestCivilizationList(_manager.CivState.DeityFilter);

            if (_manager!.HasReligion())
            {
                // Request civilization info for player's religion (empty string = my civ)
                _manager.CivState.IsMyCivLoading = true;
                _manager.CivState.IsInvitesLoading = true;
                _manager.CivState.MyCivError = null;
                _manager.CivState.InvitesError = null;
                _pantheonWarsSystem?.RequestCivilizationInfo("");
            }
        }
        else
        {
            // Play error sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);

            // Surface the error to UI banner (Task 5.3 Phase C will render it)
            _manager!.CivState.LastActionError = packet.Message;
        }
    }
}