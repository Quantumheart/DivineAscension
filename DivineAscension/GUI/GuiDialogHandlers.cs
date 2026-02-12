using System;
using System.Linq;
using DivineAscension.GUI.State;
using DivineAscension.GUI.State.Religion;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.GUI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Network.HolySite;
using Vintagestory.API.Client;

namespace DivineAscension.GUI;

/// <summary>
///     Event handlers for BlessingDialog - extracted from main class for maintainability
/// </summary>
public partial class GuiDialog
{
    /// <summary>
    ///     Periodically check if player religion data is available
    /// </summary>
    private void OnCheckDataAvailability(float dt)
    {
        if (_state.IsReady) return;

        // Request blessing data from server
        if (_divineAscensionModSystem != null)
        {
            _divineAscensionModSystem.NetworkClient?.RequestBlessingData();
            _divineAscensionModSystem.NetworkClient?.RequestAvailableDomains();
            // Don't set _state.IsReady yet - wait for server response in OnBlessingDataReceived
            _capi!.Event.UnregisterGameTickListener(_checkDataId);
        }
    }

    /// <summary>
    ///     Handle blessing data received from server
    /// </summary>
    private void OnBlessingDataReceived(BlessingDataResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Processing blessing data: HasReligion={packet.HasReligion}");

        if (!packet.HasReligion)
        {
            _logger?.Debug("[DivineAscension] Player has no religion - data ready for 'No Religion' state");
            _manager!.Reset();
            _state.IsReady = true; // Set ready so dialog can open to show "No Religion" state

            // Clear previous ranks when player has no religion
            _state.PreviousFavorRank = string.Empty;
            _state.PreviousPrestigeRank = string.Empty;

            // Dialog will only open when player presses the keybind (Shift+G)

            return;
        }

        // Parse deity type from string
        if (!Enum.TryParse<DeityDomain>(packet.Domain, out var deityType))
        {
            _logger?.Error($"[DivineAscension] Invalid deity type: {packet.Domain}");
            return;
        }

        // Initialize manager with real data
        _manager!.Initialize(packet.ReligionUID, deityType, packet.ReligionName, packet.FavorRank,
            packet.PrestigeRank);

        // Set deity name and current favor/prestige values for progress bars
        _manager.ReligionStateManager.CurrentDeityName = packet.DeityName;
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
            StatModifiers = p.StatModifiers,
            IconName = p.IconName,
            Cost = p.Cost,
            Branch = p.Branch,
            ExclusiveBranches = p.ExclusiveBranches
        }).ToList();

        var religionBlessings = packet.ReligionBlessings.Select(p => new Blessing(p.BlessingId, p.Name, deityType)
        {
            Kind = BlessingKind.Religion,
            Category = (BlessingCategory)p.Category,
            Description = p.Description,
            RequiredFavorRank = p.RequiredFavorRank,
            RequiredPrestigeRank = p.RequiredPrestigeRank,
            PrerequisiteBlessings = p.PrerequisiteBlessings,
            StatModifiers = p.StatModifiers,
            IconName = p.IconName,
            Cost = p.Cost,
            Branch = p.Branch,
            ExclusiveBranches = p.ExclusiveBranches
        }).ToList();

        // Load blessing states into manager
        _manager.BlessingStateManager.LoadBlessingStates(playerBlessings, religionBlessings);

        // Preload blessing textures for this deity domain to prevent stuttering on first render
        var allBlessings = playerBlessings.Concat(religionBlessings).ToList();
        _logger?.Debug(
            $"[DivineAscension] Preloading {allBlessings.Count} blessing textures for {deityType}...");
        BlessingIconLoader.PreloadDeityTextures(allBlessings, deityType);

        // Mark unlocked blessings
        foreach (var blessingId in packet.UnlockedPlayerBlessings)
            _manager.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

        foreach (var blessingId in packet.UnlockedReligionBlessings)
            _manager.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

        // Set branch state from server (committed and locked branches)
        _manager.BlessingStateManager.SetBranchState(packet.CommittedBranches, packet.LockedBranches);

        // Refresh states to update can-unlock status
        _manager.BlessingStateManager.RefreshAllBlessingStates(_manager.ReligionStateManager.CurrentFavorRank,
            _manager.ReligionStateManager.CurrentPrestigeRank);

        // Initialize previous ranks to prevent false positives on first update
        var favorRankName = ((FavorRank)packet.FavorRank).ToString();
        var prestigeRankName = ((PrestigeRank)packet.PrestigeRank).ToString();
        _state.PreviousFavorRank = favorRankName;
        _state.PreviousPrestigeRank = prestigeRankName;
        _logger?.Debug(
            $"[DivineAscension] Initialized previous ranks: Favor={favorRankName}, Prestige={prestigeRankName}");

        _state.IsReady = true;
        _logger?.Notification(
            $"[DivineAscension] Loaded {playerBlessings.Count} player blessings and {religionBlessings.Count} religion blessings for {packet.Domain}");

        // Ensure HUD is visible now that we have religion data
        EnsureHudVisible();
    }

    /// <summary>
    ///     Handle religion state change (religion disbanded, kicked, etc.)
    /// </summary>
    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _logger?.Notification($"[DivineAscension] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi?.ShowChatMessage(packet.Reason);

        // Clear notification queue and previous ranks when player leaves religion
        if (!packet.HasReligion)
        {
            _manager!.NotificationManager.State.IsVisible = false;
            _manager.NotificationManager.State.PendingNotifications.Clear();
            _state.PreviousFavorRank = string.Empty;
            _state.PreviousPrestigeRank = string.Empty;
            _logger?.Debug(
                "[DivineAscension] Cleared notification queue and previous ranks due to religion state change");
        }

        // Reset blessing dialog state to "No Religion" mode
        _manager!.Reset();
        _state.IsReady = true; // Keep dialog ready so it doesn't close

        // AUTO-CORRECT TAB: If current tab becomes invalid, switch to Browse
        var hasReligion = packet.HasReligion;
        var currentTab = _manager.ReligionStateManager.State.CurrentSubTab;
        var shouldSwitchTab = currentTab switch
        {
            SubTab.Info or SubTab.Activity or SubTab.Roles => !hasReligion, // These require religion
            SubTab.Invites or SubTab.Create => hasReligion, // These require NO religion
            _ => false
        };

        if (shouldSwitchTab)
        {
            _logger?.Debug(
                $"[DivineAscension] Switching tab from {currentTab} to Browse due to religion state change");
            _manager.ReligionStateManager.State.CurrentSubTab = SubTab.Browse;
        }

        // Request fresh data from server (will show "No Religion" state)
        _divineAscensionModSystem?.NetworkClient?.RequestBlessingData();

        // Request player religion info if player now has a religion
        // This handles accept invite, join, and any other state transitions
        if (packet.HasReligion)
        {
            _manager.ReligionStateManager.State.InfoState.Loading = true;
            _divineAscensionModSystem?.NetworkClient?.RequestPlayerReligionInfo();
        }

        // If notification is about civilization, also refresh civilization data
        if (packet.Reason.Contains("civilization", StringComparison.OrdinalIgnoreCase))
        {
            _manager?.CivilizationManager.RequestCivilizationInfo(string.Empty);
        }
    }

    /// <summary>
    ///     Keybind handler - toggle dialog open/close
    ///     Opens to Blessings tab by default
    /// </summary>
    private bool OnToggleDialog(KeyCombination keyCombination)
    {
        if (_state.IsOpen)
            Close();
        else
        {
            // Default to Blessings tab when opening via hotkey
            _state.CurrentMainTab = MainDialogTab.Blessings;
            Open();
        }

        return true;
    }

    /// <summary>
    ///     Handle religion list received from server
    /// </summary>
    private void OnReligionListReceived(ReligionListResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Received {packet.Religions.Count} religions from server");
        // Update manager religion tab state
        _manager!.ReligionStateManager.UpdateReligionList(packet.Religions);
    }

    /// <summary>
    ///     Handle religion action completed (join, leave, etc.)
    /// </summary>
    private void OnReligionActionCompleted(ReligionActionResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Religion action '{packet.Action}' completed: {packet.Message}");

        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);

            // Play success sound
            _soundManager!.PlayClick();


            // If leaving religion, reset blessing dialog state immediately
            if (packet.Action == "leave")
            {
                _logger?.Debug("[DivineAscension] Resetting blessing dialog after leaving religion");
                _manager!.Reset();

                // AUTO-CORRECT TAB: Switch away from member-only tabs
                var currentTab = _manager.ReligionStateManager.State.CurrentSubTab;
                if (currentTab is SubTab.Info or SubTab.Activity or SubTab.Roles)
                {
                    _logger?.Debug(
                        $"[DivineAscension] Switching tab from {currentTab} to Browse after leaving religion");
                    _manager.ReligionStateManager.State.CurrentSubTab = SubTab.Browse;
                }
            }

            // If joining/creating religion, switch to Info to show new religion
            if (packet.Action is "join" or "create" or "accept")
            {
                var currentTab = _manager!.ReligionStateManager.State.CurrentSubTab;
                if (currentTab is SubTab.Invites or SubTab.Create)
                {
                    _logger?.Debug(
                        $"[DivineAscension] Switching tab from {currentTab} to Info after joining religion");
                    _manager.ReligionStateManager.State.CurrentSubTab = SubTab.Info;
                }
            }

            // Refresh religion tab data
            _manager!.ReligionStateManager.State.BrowseState.IsBrowseLoading = true;
            _divineAscensionModSystem?.NetworkClient?.RequestReligionList(_manager.ReligionStateManager.State
                .BrowseState
                .DeityFilter);

            if (_manager.HasReligion() && packet.Action != "leave")
            {
                _manager.ReligionStateManager.State.InfoState.Loading = true;
                _divineAscensionModSystem?.NetworkClient?.RequestPlayerReligionInfo();
            }

            // Request fresh blessing data (religion may have changed)
            _divineAscensionModSystem?.NetworkClient?.RequestBlessingData();

            // Clear confirmations
            _manager.ReligionStateManager.State.InfoState.ShowDisbandConfirm = false;
            _manager.ReligionStateManager.State.InfoState.KickConfirmPlayerUID = null;
            _manager.ReligionStateManager.State.InfoState.BanConfirmPlayerUID = null;
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _soundManager!.PlayError();

            // Store error in state
            _manager!.ReligionStateManager.State.ErrorState.LastActionError = packet.Message;
        }
    }

    private void OnReligionRolesReceived(ReligionRolesResponse packet)
    {
        _logger?.Debug($"[DivineAscension] Received religion roles response: Success={packet.Success}");

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
        if (!string.IsNullOrEmpty(religionUID)) _divineAscensionModSystem!.UiService.RequestReligionRoles(religionUID);
    }

    /// <summary>
    ///     Handle player religion info received from server
    /// </summary>
    private void OnPlayerReligionInfoReceived(PlayerReligionInfoResponsePacket packet)
    {
        _logger?.Debug(
            $"[DivineAscension] Received player religion info: HasReligion={packet.HasReligion}, IsFounder={packet.IsFounder}");

        // Update manager religion tab state
        _manager!.ReligionStateManager.UpdatePlayerReligionInfo(packet);

        // Update manager with player's role (enables Manage Religion button for leaders)
        if (packet.HasReligion)
        {
            _manager.ReligionStateManager.PlayerRoleInReligion = packet.IsFounder ? "Leader" : "Member";
            _manager.ReligionStateManager.ReligionMemberCount = packet.Members.Count;
            _manager.ReligionStateManager.CurrentReligionUID = packet.ReligionUID;
            _logger?.Debug(
                $"[DivineAscension] Set PlayerRoleInReligion to: {_manager.ReligionStateManager.PlayerRoleInReligion}, MemberCount: {_manager.ReligionStateManager.ReligionMemberCount}");

            // CRITICAL: Reset ActivityState so it will request activity log on next draw
            // This handles the case where Activity tab was opened before religion info arrived
            _manager.ReligionStateManager.State.ActivityState.LastRefresh = DateTime.MinValue;
            _manager.ReligionStateManager.State.ActivityState.IsLoading = false;
            _logger?.Debug("[DivineAscension] Reset ActivityState to trigger activity log request");
        }
        else
        {
            _manager.ReligionStateManager.PlayerRoleInReligion = null;
            _manager.ReligionStateManager.ReligionMemberCount = 0;
            _logger?.Debug("[DivineAscension] Cleared PlayerRoleInReligion (no religion)");
        }

        // Update civilization manager's religion state
        _manager.UpdateCivilizationReligionState();
    }

    /// <summary>
    ///     Handle deity name change response
    /// </summary>
    private void OnDeityNameChanged(SetDeityNameResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Deity name change response: Success={packet.Success}");

        // Update state - stop saving indicator
        _manager!.ReligionStateManager.State.InfoState.IsSavingDeityName = false;

        if (packet.Success)
        {
            // Exit edit mode
            _manager.ReligionStateManager.State.InfoState.IsEditingDeityName = false;
            _manager.ReligionStateManager.State.InfoState.EditDeityNameValue = string.Empty;
            _manager.ReligionStateManager.State.InfoState.DeityNameError = null;

            // Update the cached religion info with the new deity name
            var myReligionInfo = _manager.ReligionStateManager.State.InfoState.MyReligionInfo;
            if (myReligionInfo != null && packet.NewDeityName != null)
            {
                myReligionInfo.DeityName = packet.NewDeityName;
                // Also update the header-level deity name
                _manager.ReligionStateManager.CurrentDeityName = packet.NewDeityName;
            }

            // Refresh the religion info to ensure everything is in sync
            _manager.ReligionStateManager.RequestPlayerReligionInfo();
        }
        else
        {
            // Show error
            _manager.ReligionStateManager.State.InfoState.DeityNameError =
                packet.ErrorMessage ?? "Failed to update deity name";
        }
    }

    /// <summary>
    ///     Handle religion detail response
    /// </summary>
    private void OnReligionDetailReceived(ReligionDetailResponsePacket packet)
    {
        _logger?.Debug(
            $"[DivineAscension] Received religion detail: {packet.ReligionName}, Members={packet.Members.Count}");

        // Update religion state manager
        _manager!.ReligionStateManager.UpdateReligionDetail(packet);
    }

    /// <summary>
    ///     Handle player religion data updates (favor, rank, etc.)
    /// </summary>
    private void OnPlayerReligionDataUpdated(PlayerReligionDataPacket packet)
    {
        // Skip if manager is not initialized yet
        if (_manager == null) return;

        _logger?.Debug(
            $"[DivineAscension] Updating blessing dialog with new favor data: {packet.Favor}, Total: {packet.TotalFavorEarned}");

        // Always update manager with new values, even if dialog is closed
        // This ensures the UI shows correct values when opened
        _manager.ReligionStateManager.CurrentFavor = packet.Favor;
        _manager.ReligionStateManager.CurrentPrestige = packet.Prestige;
        _manager.ReligionStateManager.TotalFavorEarned = packet.TotalFavorEarned;

        // Update config thresholds (synced from server so UI displays correct progression caps)
        _manager.ReligionStateManager.DiscipleThreshold = packet.DiscipleThreshold;
        _manager.ReligionStateManager.ZealotThreshold = packet.ZealotThreshold;
        _manager.ReligionStateManager.ChampionThreshold = packet.ChampionThreshold;
        _manager.ReligionStateManager.AvatarThreshold = packet.AvatarThreshold;
        _manager.ReligionStateManager.EstablishedThreshold = packet.EstablishedThreshold;
        _manager.ReligionStateManager.RenownedThreshold = packet.RenownedThreshold;
        _manager.ReligionStateManager.LegendaryThreshold = packet.LegendaryThreshold;
        _manager.ReligionStateManager.MythicThreshold = packet.MythicThreshold;

        // Update rank if it changed (this affects which blessings can be unlocked)
        // FavorRank comes as enum name (e.g., "Initiate", "Disciple"), parse to get numeric value
        if (Enum.TryParse<FavorRank>(packet.FavorRank, out var favorRankEnum))
            _manager.ReligionStateManager.CurrentFavorRank = (int)favorRankEnum;

        if (Enum.TryParse<PrestigeRank>(packet.PrestigeRank, out var prestigeRankEnum))
            _manager.ReligionStateManager.CurrentPrestigeRank = (int)prestigeRankEnum;

        // Check for favor rank-up
        if (!string.IsNullOrEmpty(_state.PreviousFavorRank) &&
            Enum.TryParse<FavorRank>(_state.PreviousFavorRank, out var previousFavorRank) &&
            favorRankEnum > previousFavorRank)
        {
            _logger?.Notification(
                $"[DivineAscension] Favor rank increased: {previousFavorRank} → {favorRankEnum}");
            var description = FavorRankDescriptions.GetDescription(favorRankEnum);
            if (packet.FavorRank != null)
                _manager.NotificationManager.QueueRankUpNotification(
                    NotificationType.FavorRankUp,
                    packet.FavorRank,
                    description,
                    _manager.ReligionStateManager.CurrentReligionDomain);
        }

        // Check for prestige rank-up
        if (!string.IsNullOrEmpty(_state.PreviousPrestigeRank) &&
            Enum.TryParse<PrestigeRank>(_state.PreviousPrestigeRank, out var previousPrestigeRank) &&
            prestigeRankEnum > previousPrestigeRank)
        {
            _logger?.Notification(
                $"[DivineAscension] Prestige rank increased: {previousPrestigeRank} → {prestigeRankEnum}");
            var description = PrestigeRankDescriptions.GetDescription(prestigeRankEnum);
            if (packet.PrestigeRank != null)
                _manager.NotificationManager.QueueRankUpNotification(
                    NotificationType.PrestigeRankUp,
                    packet.PrestigeRank,
                    description,
                    _manager.ReligionStateManager.CurrentReligionDomain);
        }

        // Update previous ranks for next comparison
        if (packet.FavorRank != null) _state.PreviousFavorRank = packet.FavorRank;
        if (packet.PrestigeRank != null) _state.PreviousPrestigeRank = packet.PrestigeRank;

        // Refresh blessing states in case new blessings became available
        // Only do this if dialog is open to avoid unnecessary processing
        if (_state.IsOpen && _manager.HasReligion())
            _manager.BlessingStateManager.RefreshAllBlessingStates(_manager.ReligionStateManager.CurrentFavorRank,
                _manager.ReligionStateManager.CurrentPrestigeRank);

        // Ensure HUD is visible when player has religion data
        EnsureHudVisible();
    }

    /// <summary>
    ///     Handle blessing unlock response from server
    /// </summary>
    private void OnBlessingUnlockedFromServer(string blessingId, bool success)
    {
        if (!success)
        {
            _logger?.Debug($"[DivineAscension] Blessing unlock failed: {blessingId}");

            // Play error sound on failure
            _soundManager!.PlayError();

            return;
        }

        _logger?.Debug($"[DivineAscension] Blessing unlocked from server: {blessingId}");

        // Play unlock success sound
        if (_manager != null)
        {
            switch (_manager.ReligionStateManager.CurrentReligionDomain)
            {
                case DeityDomain.None:
                    break;
                case DeityDomain.Craft:
                    _soundManager!.PlayDeityUnlock(DeityDomain.Craft);
                    break;
                case DeityDomain.Wild:
                    _soundManager!.PlayDeityUnlock(DeityDomain.Wild);
                    break;
                case DeityDomain.Harvest:
                    _soundManager!.PlayDeityUnlock(DeityDomain.Harvest);
                    break;
                case DeityDomain.Stone:
                    _soundManager!.PlayDeityUnlock(DeityDomain.Stone);
                    break;
                case DeityDomain.Conquest:
                    _soundManager!.PlayDeityUnlock(DeityDomain.Conquest);
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
        _logger?.Debug($"[DivineAscension] Received civilization list: {packet.Civilizations.Count} items");
        _manager!.CivilizationManager.OnCivilizationListReceived(packet);
    }

    /// <summary>
    ///     Handle civilization info received from server
    /// </summary>
    private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Received civilization info: HasCiv={packet.Details != null}");
        _manager!.CivilizationManager.OnCivilizationInfoReceived(packet);

        if (packet.Details != null)
            _logger?.Notification(
                $"[DivineAscension] Loaded civilization '{packet.Details.Name}' with {packet.Details.MemberReligions?.Count} religions");
        else
            _logger?.Debug("[DivineAscension] Player's religion is not in a civilization");
    }

    /// <summary>
    ///     Handle civilization action completed (create, invite, accept, leave, kick, disband)
    /// </summary>
    private void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
    {
        _logger?.Debug(
            $"[DivineAscension] Civilization action completed: Success={packet.Success}, Message={packet.Message}");

        // Show result message to user
        _capi?.ShowChatMessage(packet.Message);

        // Delegate to StateManager for state updates and side effects
        _manager!.CivilizationManager.OnCivilizationActionCompleted(packet);
    }

    /// <summary>
    ///     Handle activity log received from server
    /// </summary>
    private void OnActivityLogReceived(ActivityLogResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Received activity log with {packet.Entries.Count} entries");

        // Update activity state
        _manager!.ReligionStateManager.State.ActivityState.UpdateEntries(packet.Entries);
    }

    /// <summary>
    ///     Handle holy site data received from server
    /// </summary>
    private void OnHolySiteDataReceived(HolySiteResponsePacket packet)
    {
        // Handle detail info
        if (packet.DetailInfo != null)
        {
            _logger?.Debug(
                $"[DivineAscension] Received holy site detail info for site {packet.DetailInfo.SiteUID}");
            _manager!.CivilizationManager.UpdateHolySiteDetail(packet.DetailInfo);
            return;
        }

        _logger?.Debug($"[DivineAscension] Received holy site list: {packet.Sites.Count} sites");

        // Update civilization holy sites state
        _manager!.CivilizationManager.UpdateHolySiteList(packet.Sites);
    }

    /// <summary>
    ///     Handle holy site update response (after rename or description change)
    /// </summary>
    private void OnHolySiteUpdated(HolySiteUpdateResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Holy site updated: Success={packet.Success}");

        if (packet.Success && !string.IsNullOrEmpty(packet.SiteUID))
        {
            _manager!.CivilizationManager.OnHolySiteUpdateSuccess(packet.SiteUID);
        }
    }

    /// <summary>
    ///     Handle available domains received from server
    /// </summary>
    private void OnAvailableDomainsReceived(AvailableDomainsResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Received {packet.Domains.Count} available domains from server");

        // Update religion state manager with available domains
        _manager!.ReligionStateManager.SetAvailableDomains(packet.Domains);
    }

    /// <summary>
    ///     Handle milestone progress received from server
    /// </summary>
    private void OnMilestoneProgressReceived(MilestoneProgressResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Received milestone progress for civ {packet.CivId}: Rank={packet.Rank}, Completed={packet.CompletedMilestones?.Count ?? 0}");

        _manager!.CivilizationManager.UpdateMilestoneProgress(packet);
    }
}