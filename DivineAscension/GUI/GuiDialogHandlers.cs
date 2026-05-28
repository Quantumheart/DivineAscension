using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.State;
using DivineAscension.GUI.State.Religion;
using DivineAscension.GUI.UI.Renderers.Sidebar;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.GUI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using Vintagestory.API.Client;

namespace DivineAscension.GUI;

/// <summary>
///     Event handlers for BlessingDialog - extracted from main class for maintainability
/// </summary>
public partial class GuiDialog
{
    /// <summary>
    ///     Builds the per-deity favor-rank dict consumed by BlessingStateManager.
    ///     Sourced from the server packet via <see cref="ReligionStateManager.FavorRanksByDeity"/>.
    /// </summary>
    private Dictionary<DeityDomain, int> BuildFavorRanksByDeity()
    {
        if (_manager == null) return new Dictionary<DeityDomain, int>();
        return new Dictionary<DeityDomain, int>(_manager.ReligionStateManager.FavorRanksByDeity);
    }

    /// <summary>
    ///     Backoff schedule (ms) between blessing-data requests while we wait for the
    ///     server response. Previously the request fired exactly once and the tick was
    ///     unregistered immediately — a single dropped packet left the dialog stuck
    ///     closed until relog (#487). Now we keep retrying with growing gaps so we don't
    ///     spam the server, and stop only once data lands (see <see cref="MarkReady"/>).
    /// </summary>
    private static readonly long[] DataRequestBackoffMs = { 1000, 2000, 5000 };

    private int _dataRequestAttempts;
    private long _nextDataRequestAtMs;

    /// <summary>
    ///     Periodically check if player religion data is available, re-requesting with
    ///     backoff until the server responds.
    /// </summary>
    private void OnCheckDataAvailability(float dt)
    {
        if (_state.IsReady)
        {
            // Defensive: the response normally unregisters us via MarkReady(); ensure we
            // never keep ticking once ready.
            _capi?.Event.UnregisterGameTickListener(_checkDataId);
            return;
        }

        var now = Environment.TickCount64;
        if (now < _nextDataRequestAtMs) return;

        if (_divineAscensionModSystem != null)
        {
            _divineAscensionModSystem.NetworkClient?.RequestBlessingData();
            _divineAscensionModSystem.NetworkClient?.RequestAvailableDomains();
            // Don't set _state.IsReady yet - wait for server response in OnBlessingDataReceived.
            var idx = Math.Min(_dataRequestAttempts, DataRequestBackoffMs.Length - 1);
            _nextDataRequestAtMs = now + DataRequestBackoffMs[idx];
            _dataRequestAttempts++;
        }
    }

    /// <summary>
    ///     Flip the dialog to ready, stop the data-availability retry tick, and replay a
    ///     queued open (#487). Called from every spot that marks blessing data as loaded.
    /// </summary>
    private void MarkReady()
    {
        _state.IsReady = true;
        _capi?.Event.UnregisterGameTickListener(_checkDataId);

        if (_state.PendingOpen)
        {
            _state.PendingOpen = false;
            Open();
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

            // Clear previous ranks when player has no religion
            _state.PreviousFavorRank = string.Empty;
            _state.PreviousPrestigeRank = string.Empty;
            _state.PreviousMaxBlessingSlots = -1;

            // Set ready so the dialog can open to the "No Religion" state, and replay a
            // queued open if the player clicked a lectern / pressed Shift+G during the race.
            MarkReady();

            return;
        }

        var patron = packet.PatronDomain;
        var patronFavorRank = packet.FavorRanksByDeity.GetValueOrDefault(patron);

        // Initialize manager with patron-deity values; per-deity dicts capture all five domains below.
        _manager!.Initialize(packet.ReligionUID, patron, packet.ReligionName, patronFavorRank,
            packet.PrestigeRank);

        _manager.ReligionStateManager.CurrentDeityName = packet.PatronName;
        _manager.ReligionStateManager.CurrentFavor = packet.FavorByDeity.GetValueOrDefault(patron);
        _manager.ReligionStateManager.CurrentPrestige = packet.CurrentPrestige;
        _manager.ReligionStateManager.TotalFavorEarned = packet.TotalFavorEarnedByDeity.GetValueOrDefault(patron);
        _manager.ReligionStateManager.FavorByDeity = new Dictionary<DeityDomain, int>(packet.FavorByDeity);
        _manager.ReligionStateManager.FavorRanksByDeity = new Dictionary<DeityDomain, int>(packet.FavorRanksByDeity);
        _manager.ReligionStateManager.TotalFavorEarnedByDeity =
            new Dictionary<DeityDomain, int>(packet.TotalFavorEarnedByDeity);

        Blessing ToModel(BlessingDataResponsePacket.BlessingInfo p, BlessingKind kind) =>
            new(p.BlessingId, p.Name, p.Domain)
            {
                Kind = kind,
                Category = (BlessingCategory)p.Category,
                Description = p.Description,
                RequiredFavorRank = p.RequiredFavorRank,
                RequiredPrestigeRank = p.RequiredPrestigeRank,
                PrerequisiteBlessings = p.PrerequisiteBlessings,
                StatModifiers = p.StatModifiers,
                IconName = p.IconName,
                Cost = p.Cost,
                Branch = p.Branch,
                ExclusiveBranches = p.ExclusiveBranches,
                RequiresPatron = p.RequiresPatron
            };

        var playerBlessings = packet.PlayerBlessings.Select(p => ToModel(p, BlessingKind.Player)).ToList();
        var religionBlessings = packet.ReligionBlessings.Select(p => ToModel(p, BlessingKind.Religion)).ToList();

        // Phase 5b: load all five deities; UI switches via deity selector.
        _manager.BlessingStateManager.LoadBlessingStates(playerBlessings, religionBlessings);
        _manager.BlessingStateManager.SetActiveDeity(
            patron != DeityDomain.None ? patron : DeityDomain.Craft);

        // Preload blessing textures for every deity to prevent stuttering when switching tabs.
        var allBlessings = playerBlessings.Concat(religionBlessings).ToList();
        foreach (var domain in new[] { DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest, DeityDomain.Harvest, DeityDomain.Stone })
            BlessingIconLoader.PreloadDeityTextures(
                allBlessings.Where(b => b.Domain == domain).ToList(), domain);
        _logger?.Debug(
            $"[DivineAscension] Preloaded blessing textures for all five deities ({allBlessings.Count} blessings total).");

        // Mark unlocked blessings
        foreach (var blessingId in packet.UnlockedPlayerBlessings)
            _manager.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

        foreach (var blessingId in packet.UnlockedReligionBlessings)
            _manager.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

        // Set branch state from server (committed and locked branches)
        _manager.BlessingStateManager.SetBranchState(packet.CommittedBranches, packet.LockedBranches);

        // Sync the unlearn refund percent so the cascade confirm dialog can preview the total (#460).
        _manager.BlessingStateManager.UnlearnRefundPercent = packet.UnlearnRefundPercent;

        // Free-respec window banner state (#462).
        _manager.BlessingStateManager.FreeRespecActive = packet.FreeRespecActive;

        // Religion blessing inscribe-slot cap for the "Sworn: X/Y" counter (#479). The used-count
        // is derived client-side from the per-domain sworn state so it matches the domain rows.
        _manager.BlessingStateManager.ReligionBlessingSlotCap = packet.ReligionBlessingSlotCap;

        _manager.BlessingStateManager.RefreshAllBlessingStates(
            BuildFavorRanksByDeity(),
            _manager.ReligionStateManager.CurrentPrestigeRank,
            _manager.ReligionStateManager.CurrentReligionDomain);

        // Initialize previous ranks to prevent false positives on first update
        var favorRankName = ((FavorRank)patronFavorRank).ToString();
        var prestigeRankName = ((PrestigeRank)packet.PrestigeRank).ToString();
        _state.PreviousFavorRank = favorRankName;
        _state.PreviousPrestigeRank = prestigeRankName;
        _logger?.Debug(
            $"[DivineAscension] Initialized previous ranks: Favor={favorRankName}, Prestige={prestigeRankName}");

        MarkReady();
        _logger?.Notification(
            $"[DivineAscension] Loaded {playerBlessings.Count} player + {religionBlessings.Count} religion blessings across all five deities; active={patron}");
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
            _state.PreviousMaxBlessingSlots = -1;
            // No PlayerReligionDataPacket is sent to non-religion players, so the slot cap would
            // otherwise stay stale (e.g. an ex-member of a Mythic order keeping its bonus slots).
            // Clear it so the slot header hides and client-side gating falls back to server auth (#446).
            _manager.BlessingStateManager.MaxPlayerBlessingSlots = 0;
            _manager.BlessingStateManager.ReligionBlessingSlotCap = 0;
            _logger?.Debug(
                "[DivineAscension] Cleared notification queue and previous ranks due to religion state change");
        }

        // Reset blessing dialog state to "No Religion" mode
        _manager!.Reset();
        _state.IsReady = true; // Keep dialog ready so it doesn't close

        // AUTO-CORRECT NAV: If the current sidebar destination becomes invalid
        // for the new religion state, snap back to Browse.
        var hasReligion = packet.HasReligion;
        var currentNav = _state.Sidebar.CurrentNav;
        var shouldSwitchTab = currentNav switch
        {
            SidebarNavId.ReligionInfo or SidebarNavId.ReligionRoster or SidebarNavId.ReligionActivity or SidebarNavId.ReligionChronicle or SidebarNavId.ReligionSacredCalendar or SidebarNavId.ReligionRoles => !hasReligion,
            SidebarNavId.ReligionInvites or SidebarNavId.ReligionCreate => hasReligion,
            _ => false
        };

        if (shouldSwitchTab)
        {
            _logger?.Debug(
                $"[DivineAscension] Switching nav from {currentNav} to ReligionBrowse due to religion state change");
            _state.Sidebar.CurrentNav = SidebarNavId.ReligionBrowse;
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

#if DEBUG
    /// <summary>
    ///     Debug-only Shift+G handler. Opens to Blessings tab; bypasses the
    ///     server-authoritative lectern check used in release builds.
    /// </summary>
    private bool OnToggleDialog(KeyCombination keyCombination)
    {
        if (_state.IsOpen)
        {
            Close();
        }
        else
        {
            _state.Sidebar.CurrentNav = ResolveRestoreNav();
            Open();
        }
        return true;
    }
#endif

    /// <summary>
    ///     Resolve the page the codex should land on: the player's last-visited
    ///     page (<see cref="UiPrefs.LastNavId" />), falling back to
    ///     <see cref="SidebarNavId.PlayerInfo" /> when that page is disabled for the
    ///     current player (#474).
    /// </summary>
    private SidebarNavId ResolveRestoreNav()
    {
        var desired = _divineAscensionModSystem?.Config.UiPrefs.LastNavId ?? SidebarNavId.PlayerInfo;
        if (_manager == null) return desired;

        var ctx = SidebarNavMapper.ContextFromManager(_manager, _state.Sidebar);
        return SidebarNavMapper.ResolveRestoreNav(desired, ctx);
    }

    /// <summary>
    ///     Server-driven open: fires when the player right-clicks a lectern and the
    ///     server validates the interaction (see LecternInteractionHandler).
    /// </summary>
    private void OnOpenMenuRequested(OpenMenuPacket packet)
    {
        if (_state.IsOpen) return;
        _state.Sidebar.CurrentNav = ResolveRestoreNav();
        Open();
    }

    /// <summary>
    ///     Server-driven close: fires when the player walks away from the lectern
    ///     they opened the menu at.
    /// </summary>
    private void OnCloseMenuRequested(CloseMenuPacket packet)
    {
        if (!_state.IsOpen) return;
        _state.RequestClose = true;
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

            // Play writing sound on save (religion action completed by server)
            _soundManager!.PlaySuccess();


            // If leaving religion, reset blessing dialog state immediately
            if (packet.Action == "leave")
            {
                _logger?.Debug("[DivineAscension] Resetting blessing dialog after leaving religion");
                _manager!.Reset();

                // AUTO-CORRECT NAV: snap away from member-only destinations.
                var currentNav = _state.Sidebar.CurrentNav;
                if (currentNav is SidebarNavId.ReligionInfo or SidebarNavId.ReligionActivity
                    or SidebarNavId.ReligionChronicle or SidebarNavId.ReligionSacredCalendar or SidebarNavId.ReligionRoles)
                {
                    _logger?.Debug(
                        $"[DivineAscension] Switching nav from {currentNav} to ReligionBrowse after leaving religion");
                    _state.Sidebar.CurrentNav = SidebarNavId.ReligionBrowse;
                }
            }

            // If joining/creating religion, switch to Info to show the new religion.
            if (packet.Action is "join" or "create" or "accept")
            {
                var currentNav = _state.Sidebar.CurrentNav;
                if (currentNav is SidebarNavId.ReligionInvites or SidebarNavId.ReligionCreate)
                {
                    _logger?.Debug(
                        $"[DivineAscension] Switching nav from {currentNav} to ReligionInfo after joining religion");
                    _state.Sidebar.CurrentNav = SidebarNavId.ReligionInfo;
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

        var patron = packet.PatronDomain;
        var patronFavor = packet.FavorByDeity.GetValueOrDefault(patron);
        var patronTotal = packet.TotalFavorEarnedByDeity.GetValueOrDefault(patron);
        var patronRankName = packet.FavorRanksByDeity.GetValueOrDefault(patron) ?? FavorRank.Initiate.ToString();

        _logger?.Debug(
            $"[DivineAscension] Updating blessing dialog with new favor data: {patronFavor}, Total: {patronTotal}");

        _manager.ReligionStateManager.CurrentFavor = patronFavor;
        _manager.ReligionStateManager.CurrentPrestige = packet.Prestige;
        _manager.ReligionStateManager.TotalFavorEarned = patronTotal;

        // Sync the unlock-slot cap so the blessing tree can show usage and gate at the cap (#446).
        _manager.BlessingStateManager.MaxPlayerBlessingSlots = packet.MaxBlessingSlots;

        // Update per-deity dicts.
        _manager.ReligionStateManager.FavorByDeity = new Dictionary<DeityDomain, int>(packet.FavorByDeity);
        _manager.ReligionStateManager.TotalFavorEarnedByDeity =
            new Dictionary<DeityDomain, int>(packet.TotalFavorEarnedByDeity);
        var ranksByDeity = new Dictionary<DeityDomain, int>();
        foreach (var kvp in packet.FavorRanksByDeity)
        {
            if (Enum.TryParse<FavorRank>(kvp.Value, out var r))
                ranksByDeity[kvp.Key] = (int)r;
        }
        _manager.ReligionStateManager.FavorRanksByDeity = ranksByDeity;

        // Update config thresholds (synced from server so UI displays correct progression caps)
        _manager.ReligionStateManager.DiscipleThreshold = packet.DiscipleThreshold;
        _manager.ReligionStateManager.ZealotThreshold = packet.ZealotThreshold;
        _manager.ReligionStateManager.ChampionThreshold = packet.ChampionThreshold;
        _manager.ReligionStateManager.AvatarThreshold = packet.AvatarThreshold;
        _manager.ReligionStateManager.EstablishedThreshold = packet.EstablishedThreshold;
        _manager.ReligionStateManager.RenownedThreshold = packet.RenownedThreshold;
        _manager.ReligionStateManager.LegendaryThreshold = packet.LegendaryThreshold;
        _manager.ReligionStateManager.MythicThreshold = packet.MythicThreshold;

        if (Enum.TryParse<FavorRank>(patronRankName, out var favorRankEnum))
            _manager.ReligionStateManager.CurrentFavorRank = (int)favorRankEnum;
        else
            favorRankEnum = FavorRank.Initiate;

        if (Enum.TryParse<PrestigeRank>(packet.PrestigeRank, out var prestigeRankEnum))
            _manager.ReligionStateManager.CurrentPrestigeRank = (int)prestigeRankEnum;

        // Check for favor rank-up (on patron deity)
        if (!string.IsNullOrEmpty(_state.PreviousFavorRank) &&
            Enum.TryParse<FavorRank>(_state.PreviousFavorRank, out var previousFavorRank) &&
            favorRankEnum > previousFavorRank)
        {
            _logger?.Notification(
                $"[DivineAscension] Favor rank increased: {previousFavorRank} → {favorRankEnum}");
            var description = FavorRankDescriptions.GetDescription(favorRankEnum);
            _manager.NotificationManager.QueueRankUpNotification(
                NotificationType.FavorRankUp,
                patronRankName,
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

        // Check for blessing-slot increase (#445). Fires whenever the cap grows —
        // typically alongside a favor or prestige rank-up that crossed a slot threshold,
        // but also any future config-driven bump. Sentinel -1 suppresses the toast on
        // the first packet for a session.
        if (_state.PreviousMaxBlessingSlots >= 0 &&
            packet.MaxBlessingSlots > _state.PreviousMaxBlessingSlots)
        {
            var gained = packet.MaxBlessingSlots - _state.PreviousMaxBlessingSlots;
            _logger?.Notification(
                $"[DivineAscension] Blessing slots increased: {_state.PreviousMaxBlessingSlots} → {packet.MaxBlessingSlots}");
            var slotTitle = LocalizationService.Instance.Get(
                LocalizationKeys.UI_RANKUP_SLOTS_TITLE, packet.MaxBlessingSlots);
            var slotDescription = LocalizationService.Instance.Get(
                LocalizationKeys.UI_RANKUP_SLOTS_DESCRIPTION, packet.MaxBlessingSlots, gained);
            _manager.NotificationManager.QueueRankUpNotification(
                NotificationType.BlessingSlotsIncreased,
                slotTitle,
                slotDescription,
                _manager.ReligionStateManager.CurrentReligionDomain);
        }

        _state.PreviousFavorRank = patronRankName;
        if (packet.PrestigeRank != null) _state.PreviousPrestigeRank = packet.PrestigeRank;
        _state.PreviousMaxBlessingSlots = packet.MaxBlessingSlots;

        // Refresh blessing states in case new blessings became available
        // Only do this if dialog is open to avoid unnecessary processing
        if (_state.IsOpen && _manager.HasReligion())
            _manager.BlessingStateManager.RefreshAllBlessingStates(
                BuildFavorRanksByDeity(),
                _manager.ReligionStateManager.CurrentPrestigeRank,
                _manager.ReligionStateManager.CurrentReligionDomain);
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

        // Play unlock success sound (writing). Fires regardless of domain — the
        // sound is now domain-agnostic since #381.
        _soundManager!.PlaySuccess();

        if (_manager != null)
        {
            // Update manager state
            _manager?.BlessingStateManager.SetBlessingUnlocked(blessingId, true);

            // Refresh all blessing states to update prerequisites and glow effects
            _manager?.BlessingStateManager.RefreshAllBlessingStates(
                BuildFavorRanksByDeity(),
                _manager.ReligionStateManager.CurrentPrestigeRank,
                _manager.ReligionStateManager.CurrentReligionDomain);
        }
    }

    /// <summary>
    ///     Handle a blessing unlearn response from the server (epic #425, slice 1 — #459).
    ///     On success, flips the node back to locked and recomputes prerequisites/glow; the
    ///     refunded favor reaches the UI via the player-data-changed push the server sends.
    /// </summary>
    private void OnBlessingUnlearnedFromServer(UnlearnBlessingResponsePacket packet)
    {
        if (!packet.Success)
        {
            _logger?.Debug($"[DivineAscension] Blessing unlearn failed: {packet.BlessingId}");
            _soundManager!.PlayError();
            return;
        }

        _logger?.Debug(
            $"[DivineAscension] Blessing unlearned from server: {packet.BlessingId} (+{packet.StruckBlessingIds.Count - 1} cascaded)");
        _soundManager!.PlaySuccess();

        if (_manager != null)
        {
            // Re-lock every blessing the server struck — the target and all cascaded dependents (#460).
            var struck = packet.StruckBlessingIds.Count > 0
                ? (IEnumerable<string>)packet.StruckBlessingIds
                : new[] { packet.BlessingId };
            foreach (var id in struck)
                _manager.BlessingStateManager.SetBlessingUnlocked(id, false);

            _manager.BlessingStateManager.RefreshAllBlessingStates(
                BuildFavorRanksByDeity(),
                _manager.ReligionStateManager.CurrentPrestigeRank,
                _manager.ReligionStateManager.CurrentReligionDomain);
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

        _capi?.ShowChatMessage(packet.Message);

        _manager!.CivilizationManager.OnCivilizationActionCompleted(packet);

        // AUTO-CORRECT NAV: snap the sidebar to a sensible destination after
        // join/leave/disband etc. The manager focuses on data; nav is the
        // dialog's concern.
        if (packet.Success)
        {
            var currentNav = _state.Sidebar.CurrentNav;
            switch (packet.Action?.ToLowerInvariant())
            {
                case "leave" or "disband":
                    if (currentNav is SidebarNavId.CivilizationInfo
                        or SidebarNavId.CivilizationDiplomacy
                        or SidebarNavId.CivilizationProposeAccord
                        or SidebarNavId.CivilizationHolySites
                        or SidebarNavId.CivilizationMilestones)
                    {
                        _logger?.Debug(
                            $"[DivineAscension] Switching nav from {currentNav} to CivilizationBrowse after leaving civilization");
                        _state.Sidebar.CurrentNav = SidebarNavId.CivilizationBrowse;
                    }
                    break;
                case "join" or "create" or "accept":
                    if (currentNav is SidebarNavId.CivilizationInvites or SidebarNavId.CivilizationCreate)
                    {
                        _logger?.Debug(
                            $"[DivineAscension] Switching nav from {currentNav} to CivilizationInfo after joining civilization");
                        _state.Sidebar.CurrentNav = SidebarNavId.CivilizationInfo;
                    }
                    break;
            }
        }
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

    /// <summary>
    ///     Handle Standing of Realms leaderboard received from server
    /// </summary>
    private void OnLeaderboardReceived(DivineAscension.Network.Civilization.LeaderboardResponsePacket packet)
    {
        _logger?.Debug($"[DivineAscension] Received leaderboard with {packet.Boards.Count} boards, {packet.TotalRealms} realms");

        _manager!.CivilizationManager.UpdateLeaderboard(packet);
    }
}