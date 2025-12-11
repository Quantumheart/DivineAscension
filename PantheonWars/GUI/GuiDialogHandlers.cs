using System;
using System.Linq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;

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
            _pantheonWarsSystem.NetworkClient?.RequestBlessingData();
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
        _manager.BlessingStateManager.LoadBlessingStates(playerBlessings, religionBlessings);

        // Mark unlocked blessings
        foreach (var blessingId in packet.UnlockedPlayerBlessings)
            _manager.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

        foreach (var blessingId in packet.UnlockedReligionBlessings)
            _manager.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

        // Refresh states to update can-unlock status
        _manager.BlessingStateManager.RefreshAllBlessingStates(_manager.ReligionStateManager.CurrentFavorRank,
            _manager.ReligionStateManager.CurrentPrestigeRank);

        _state.IsReady = true;
        _capi.Logger.Notification(
            $"[PantheonWars] Loaded {playerBlessings.Count} player blessings and {religionBlessings.Count} religion blessings for {packet.Deity}");

    }

    /// <summary>
    ///     Handle religion state change (religion disbanded, kicked, etc.)
    /// </summary>
    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi!.Logger.Notification($"[PantheonWars] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi.ShowChatMessage(packet.Reason);
        
        // Reset blessing dialog state to "No Religion" mode
        _manager!.Reset();
        _state.IsReady = true; // Keep dialog ready so it doesn't close

        // Request fresh data from server (will show "No Religion" state)
        _pantheonWarsSystem?.NetworkClient?.RequestBlessingData();

        // If notification is about civilization, also refresh civilization data
        if (packet.Reason.Contains("civilization", StringComparison.OrdinalIgnoreCase))
        {
            _manager?.CivilizationManager.RequestCivilizationInfo(string.Empty);
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
            _soundManager!.PlayClick();


            // If leaving religion, reset blessing dialog state immediately
            if (packet.Action == "leave")
            {
                _capi.Logger.Debug("[PantheonWars] Resetting blessing dialog after leaving religion");
                _manager!.Reset();
            }

            // Refresh religion tab data
            _manager!.ReligionStateManager.State.BrowseState.IsBrowseLoading = true;
            _pantheonWarsSystem?.NetworkClient?.RequestReligionList(_manager.ReligionStateManager.State.BrowseState
                .DeityFilter);

            if (_manager.HasReligion() && packet.Action != "leave")
            {
                _manager.ReligionStateManager.State.InfoState.Loading = true;
                _pantheonWarsSystem?.NetworkClient?.RequestPlayerReligionInfo();
            }

            // Request fresh blessing data (religion may have changed)
            _pantheonWarsSystem?.NetworkClient?.RequestBlessingData();

            // Clear confirmations
            _manager.ReligionStateManager.State.InfoState.ShowDisbandConfirm = false;
            _manager.ReligionStateManager.State.InfoState.KickConfirmPlayerUID = null;
            _manager.ReligionStateManager.State.InfoState.BanConfirmPlayerUID = null;
        }
        else
        {
            _capi.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _soundManager!.PlayError();

            // Store error in state
            _manager!.ReligionStateManager.State.ErrorState.LastActionError = packet.Message;
        }
    }

    private void OnReligionRolesReceived(ReligionRolesResponse packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received religion roles response: Success={packet.Success}");

        if (packet.Success)
        {
            _manager!.ReligionStateManager.State.RolesState.RolesData = packet;
            _manager!.ReligionStateManager.State.RolesState.Loading = false;
            _manager!.ReligionStateManager.State.ErrorState.RolesError = null;
        }
        else
        {
            _capi!.ShowChatMessage($"Error: {packet.ErrorMessage}");
            _manager!.ReligionStateManager.State.RolesState.Loading = false;
            _manager!.ReligionStateManager.State.ErrorState.RolesError = packet.ErrorMessage;
        }
    }

    private void OnRoleCreated(CreateRoleResponse packet)
    {
        if (packet.Success)
            // Refresh roles list
            RefreshRolesList();
    }

    private void OnRolePermissionsModified(ModifyRolePermissionsResponse packet)
    {
        if (packet.Success)
            // Refresh roles list
            RefreshRolesList();
    }

    private void OnRoleAssigned(AssignRoleResponse packet)
    {
        if (packet.Success)
            // Refresh roles list
            RefreshRolesList();
    }

    private void OnRoleDeleted(DeleteRoleResponse packet)
    {
        if (packet.Success)
            // Refresh roles list
            RefreshRolesList();
    }

    private void OnFounderTransferred(TransferFounderResponse packet)
    {
        if (packet.Success)
            // Refresh roles list
            RefreshRolesList();
    }

    private void RefreshRolesList()
    {
        var religionUID = _manager!.ReligionStateManager.CurrentReligionUID;
        if (!string.IsNullOrEmpty(religionUID)) _pantheonWarsSystem!.UiService.RequestReligionRoles(religionUID);
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

        // Update civilization manager's religion state
        _manager.UpdateCivilizationReligionState();
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
        if (_state.IsOpen && _manager.HasReligion())
            _manager.BlessingStateManager.RefreshAllBlessingStates(_manager.ReligionStateManager.CurrentFavorRank,
                _manager.ReligionStateManager.CurrentPrestigeRank);
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
            _soundManager!.PlayError();

            return;
        }

        _capi!.Logger.Debug($"[PantheonWars] Blessing unlocked from server: {blessingId}");

        // Play unlock success sound
        if (_manager != null)
        {
            switch (_manager.ReligionStateManager.CurrentDeity)
            {
                case DeityType.None:
                    break;
                case DeityType.Khoras:
                    _soundManager!.PlayDeityUnlock(DeityType.Khoras);
                    break;
                case DeityType.Lysa:
                    _soundManager!.PlayDeityUnlock(DeityType.Lysa);
                    break;
                case DeityType.Aethra:
                    _soundManager!.PlayDeityUnlock(DeityType.Aethra);
                    break;
                case DeityType.Gaia:
                    _soundManager!.PlayDeityUnlock(DeityType.Gaia);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update manager state
            _manager?.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

            // Refresh all blessing states to update prerequisites and glow effects
            _manager?.BlessingStateManager.RefreshAllBlessingStates(_manager.ReligionStateManager.CurrentFavorRank,
                _manager.ReligionStateManager.CurrentPrestigeRank);
        }
    }

    /// <summary>
    ///     Handle civilization list received from server
    /// </summary>
    private void OnCivilizationListReceived(CivilizationListResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received civilization list: {packet.Civilizations.Count} items");
        _manager!.CivilizationManager.OnCivilizationListReceived(packet);
    }

    /// <summary>
    ///     Handle civilization info received from server
    /// </summary>
    private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received civilization info: HasCiv={packet.Details != null}");
        _manager!.CivilizationManager.OnCivilizationInfoReceived(packet);

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

        // Delegate to StateManager for state updates and side effects
        _manager!.CivilizationManager.OnCivilizationActionCompleted(packet);
    }
}