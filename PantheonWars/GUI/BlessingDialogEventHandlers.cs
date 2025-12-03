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
public partial class BlessingDialog
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
        _manager.CurrentFavor = packet.CurrentFavor;
        _manager.CurrentPrestige = packet.CurrentPrestige;
        _manager.TotalFavorEarned = packet.TotalFavorEarned;

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
        _manager.LoadBlessingStates(playerBlessings, religionBlessings);

        // Mark unlocked blessings
        foreach (var blessingId in packet.UnlockedPlayerBlessings) _manager.SetBlessingUnlocked(blessingId, true);

        foreach (var blessingId in packet.UnlockedReligionBlessings) _manager.SetBlessingUnlocked(blessingId, true);

        // Refresh states to update can-unlock status
        _manager.RefreshAllBlessingStates();

        _state.IsReady = true;
        _capi.Logger.Notification(
            $"[PantheonWars] Loaded {playerBlessings.Count} player blessings and {religionBlessings.Count} religion blessings for {packet.Deity}");

        // Request player religion info to get founder status (needed for Manage Religion button)
        _pantheonWarsSystem?.RequestPlayerReligionInfo();

        // Request civilization info for player's religion (empty string = my civ)
        _pantheonWarsSystem?.RequestCivilizationInfo("");

        // Dialog will only open when player presses the keybind (Shift+G)
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
        _overlayCoordinator!.CloseAllOverlays();

        // Reset blessing dialog state to "No Religion" mode
        _manager!.Reset();
        _state.IsReady = true; // Keep dialog ready so it doesn't close

        // Request fresh data from server (will show "No Religion" state)
        _pantheonWarsSystem?.RequestBlessingData();
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

        // Client-side validation before sending request
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
    ///     Handle Change Religion button click
    /// </summary>
    private void OnChangeReligionClicked()
    {
        _capi!.Logger.Debug("[PantheonWars] Opening religion browser");
        _overlayCoordinator!.ShowReligionBrowser();

        // Initialize overlay and request religion list
        UI.Renderers.ReligionBrowserOverlay.Initialize();
        _pantheonWarsSystem?.RequestReligionList("");
    }

    /// <summary>
    ///     Handle Manage Religion button click (for leaders)
    /// </summary>
    private void OnManageReligionClicked()
    {
        _capi!.Logger.Debug("[PantheonWars] Manage Religion clicked");
        _overlayCoordinator!.ShowReligionManagement();
        UI.Renderers.ReligionManagementOverlay.Initialize();

        // Request religion info from server
        _pantheonWarsSystem?.RequestPlayerReligionInfo();
    }

    /// <summary>
    ///     Handle religion list received from server
    /// </summary>
    private void OnReligionListReceived(ReligionListResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received {packet.Religions.Count} religions from server");
        UI.Renderers.ReligionBrowserOverlay.UpdateReligionList(packet.Religions);
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

            // Close any open overlays
            _overlayCoordinator!.CloseReligionBrowser();
            _overlayCoordinator.CloseLeaveConfirmation();

            // If leaving religion, reset blessing dialog state immediately
            if (packet.Action == "leave")
            {
                _capi.Logger.Debug("[PantheonWars] Resetting blessing dialog after leaving religion");
                _manager!.Reset();
            }

            // Request fresh blessing data (religion may have changed)
            _pantheonWarsSystem?.RequestBlessingData();

            // Request updated religion info to refresh member count (for join/kick/ban/invite actions)
            if (packet.Action == "join" || packet.Action == "kick" || packet.Action == "ban" || packet.Action == "invite")
            {
                _pantheonWarsSystem?.RequestPlayerReligionInfo();
            }
        }
        else
        {
            _capi.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);
        }
    }

    /// <summary>
    ///     Handle join religion request
    /// </summary>
    private void OnJoinReligionClicked(string religionUID)
    {
        _capi!.Logger.Debug($"[PantheonWars] Requesting to join religion: {religionUID}");
        _pantheonWarsSystem?.RequestReligionAction("join", religionUID);
    }

    /// <summary>
    ///     Handle religion list refresh request
    /// </summary>
    private void OnRefreshReligionList(string deityFilter)
    {
        _capi!.Logger.Debug($"[PantheonWars] Refreshing religion list with filter: {deityFilter}");
        _pantheonWarsSystem?.RequestReligionList(deityFilter);
    }

    /// <summary>
    ///     Handle Leave Religion button click
    /// </summary>
    private void OnLeaveReligionClicked()
    {
        _capi!.Logger.Debug("[PantheonWars] Leave Religion clicked");

        if (_manager!.HasReligion())
        {
            // Show confirmation dialog
            _overlayCoordinator!.ShowLeaveConfirmation();
        }
        else
        {
            _capi.ShowChatMessage("You are not in a religion");
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.3f);
        }
    }

    /// <summary>
    ///     Handle leave religion confirmation
    /// </summary>
    private void OnLeaveReligionConfirmed()
    {
        _capi!.Logger.Debug("[PantheonWars] Leave religion confirmed");
        _pantheonWarsSystem?.RequestReligionAction("leave", _manager!.CurrentReligionUID ?? "");
        _overlayCoordinator!.CloseLeaveConfirmation();
    }

    /// <summary>
    ///     Handle leave religion cancelled
    /// </summary>
    private void OnLeaveReligionCancelled()
    {
        _capi!.Logger.Debug("[PantheonWars] Leave religion cancelled");
        _overlayCoordinator!.CloseLeaveConfirmation();
    }

    /// <summary>
    ///     Handle Create Religion button click
    /// </summary>
    private void OnCreateReligionClicked()
    {
        _capi!.Logger.Debug("[PantheonWars] Create Religion clicked");
        _overlayCoordinator!.ShowCreateReligion();
        UI.Renderers.CreateReligionOverlay.Initialize();
    }

    /// <summary>
    ///     Handle Create Religion form submission
    /// </summary>
    private void OnCreateReligionSubmit(string religionName, string deity, bool isPublic)
    {
        _capi!.Logger.Debug($"[PantheonWars] Creating religion: {religionName}, Deity: {deity}, Public: {isPublic}");
        _pantheonWarsSystem?.RequestCreateReligion(religionName, deity, isPublic);

        // Close create form and show browser to see the new religion
        _overlayCoordinator!.CloseCreateReligion();
        _overlayCoordinator.ShowReligionBrowser();

        // Request updated religion list to show the newly created religion
        _pantheonWarsSystem?.RequestReligionList("");
    }

    /// <summary>
    ///     Handle player religion info received from server
    /// </summary>
    private void OnPlayerReligionInfoReceived(PlayerReligionInfoResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received player religion info: HasReligion={packet.HasReligion}, IsFounder={packet.IsFounder}");

        // Update manager with player's role (enables Manage Religion button for leaders)
        if (packet.HasReligion)
        {
            _manager!.PlayerRoleInReligion = packet.IsFounder ? "Leader" : "Member";
            _manager.ReligionMemberCount = packet.Members.Count;
            _capi!.Logger.Debug($"[PantheonWars] Set PlayerRoleInReligion to: {_manager.PlayerRoleInReligion}, MemberCount: {_manager.ReligionMemberCount}");
        }
        else
        {
            _manager!.PlayerRoleInReligion = null;
            _manager.ReligionMemberCount = 0;
            _capi!.Logger.Debug("[PantheonWars] Cleared PlayerRoleInReligion (no religion)");
        }

        UI.Renderers.ReligionManagementOverlay.UpdateReligionInfo(packet);
    }

    /// <summary>
    ///     Handle request for religion info refresh
    /// </summary>
    private void OnRequestReligionInfo()
    {
        _capi!.Logger.Debug("[PantheonWars] Requesting religion info refresh");
        _pantheonWarsSystem?.RequestPlayerReligionInfo();
    }

    /// <summary>
    ///     Handle Kick Member action
    /// </summary>
    private void OnKickMemberClicked(string memberUID)
    {
        _capi!.Logger.Debug($"[PantheonWars] Kicking member: {memberUID}");
        _pantheonWarsSystem?.RequestReligionAction("kick", _manager!.CurrentReligionUID ?? "", memberUID);
    }

    /// <summary>
    ///     Handle Ban Member action
    /// </summary>
    private void OnBanMemberClicked(string memberUID)
    {
        _capi!.Logger.Debug($"[PantheonWars] Banning member: {memberUID}");
        // Note: The ban dialog will be shown by the ImGui renderer, which will handle the actual ban request
        // This is just a placeholder - the actual ban flow goes through BanPlayerDialog in the standard GUI
        // For ImGui, we need to implement a similar flow or trigger the BanPlayerDialog
        _pantheonWarsSystem?.RequestReligionAction("ban", _manager!.CurrentReligionUID ?? "", memberUID);
    }

    /// <summary>
    ///     Handle Unban Member action
    /// </summary>
    private void OnUnbanMemberClicked(string playerUID)
    {
        _capi!.Logger.Debug($"[PantheonWars] Unbanning player: {playerUID}");
        _pantheonWarsSystem?.RequestReligionAction("unban", _manager!.CurrentReligionUID ?? "", playerUID);
    }

    /// <summary>
    ///     Handle Invite Player action
    /// </summary>
    private void OnInvitePlayerClicked(string playerName)
    {
        _capi!.Logger.Debug($"[PantheonWars] Inviting player: {playerName}");
        // Note: The server expects playerUID, but we're sending playerName
        // The old system sends playerName in targetPlayerUID field
        _pantheonWarsSystem?.RequestReligionAction("invite", _manager!.CurrentReligionUID ?? "", playerName);
    }

    /// <summary>
    ///     Handle Edit Description action
    /// </summary>
    private void OnEditDescriptionClicked(string description)
    {
        _capi!.Logger.Debug("[PantheonWars] Editing religion description");
        _pantheonWarsSystem?.RequestEditDescription(_manager!.CurrentReligionUID ?? "", description);
    }

    /// <summary>
    ///     Handle Disband Religion action
    /// </summary>
    private void OnDisbandReligionClicked()
    {
        _capi!.Logger.Debug("[PantheonWars] Disbanding religion");
        _pantheonWarsSystem?.RequestReligionAction("disband", _manager!.CurrentReligionUID ?? "");

        // Close management overlay
        _overlayCoordinator!.CloseReligionManagement();
    }

    /// <summary>
    ///     Handle player religion data updates (favor, rank, etc.)
    /// </summary>
    private void OnPlayerReligionDataUpdated(PlayerReligionDataPacket packet)
    {
        // Skip if manager is not initialized yet
        if (_manager == null) return;

        _capi!.Logger.Debug($"[PantheonWars] Updating blessing dialog with new favor data: {packet.Favor}, Total: {packet.TotalFavorEarned}");

        // Always update manager with new values, even if dialog is closed
        // This ensures the UI shows correct values when opened
        _manager.CurrentFavor = packet.Favor;
        _manager.CurrentPrestige = packet.Prestige;
        _manager.TotalFavorEarned = packet.TotalFavorEarned;

        // Update rank if it changed (this affects which blessings can be unlocked)
        // FavorRank comes as enum name (e.g., "Initiate", "Disciple"), parse to get numeric value
        if (Enum.TryParse<FavorRank>(packet.FavorRank, out var favorRankEnum))
        {
            _manager.CurrentFavorRank = (int)favorRankEnum;
        }

        if (Enum.TryParse<PrestigeRank>(packet.PrestigeRank, out var prestigeRankEnum))
        {
            _manager.CurrentPrestigeRank = (int)prestigeRankEnum;
        }

        // Refresh blessing states in case new blessings became available
        // Only do this if dialog is open to avoid unnecessary processing
        if (_state.IsOpen && _manager.HasReligion())
        {
            _manager.RefreshAllBlessingStates();
        }
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
            switch (_manager.CurrentDeity)
            {
                case DeityType.None:
                    _capi.World.PlaySoundAt(
                        new AssetLocation($"pantheonwars:sounds/unlock"),
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
            _manager?.SetBlessingUnlocked(blessingId, true);

            // Refresh all blessing states to update prerequisites and glow effects
            _manager?.RefreshAllBlessingStates();
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
    }

    /// <summary>
    ///     Handle civilization info received from server
    /// </summary>
    private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Received civilization info: HasCiv={packet.Details != null}");

        // Update manager with civilization state
        _manager!.UpdateCivilizationState(packet.Details);

        if (packet.Details != null)
        {
            _capi.Logger.Notification($"[PantheonWars] Loaded civilization '{packet.Details.Name}' with {packet.Details.MemberReligions.Count} religions");
        }
        else
        {
            _capi.Logger.Debug("[PantheonWars] Player's religion is not in a civilization");
        }
    }

    /// <summary>
    ///     Handle civilization action completed (create, invite, accept, leave, kick, disband)
    /// </summary>
    private void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] Civilization action completed: Success={packet.Success}, Message={packet.Message}");

        // Show result message to user
        _capi.ShowChatMessage(packet.Message);

        if (packet.Success)
        {
            // Play success sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);

            // Request updated civilization data to refresh UI
            // Refresh both the browse list and the player's civilization info
            _pantheonWarsSystem?.RequestCivilizationList(_manager!.CivState.DeityFilter);

            if (_manager!.HasReligion())
            {
                // Request civilization info for player's religion (empty string = my civ)
                _pantheonWarsSystem?.RequestCivilizationInfo("");
            }
        }
        else
        {
            // Play error sound
            _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);
        }
    }

    /// <summary>
    ///     Handle Manage Civilization button click (for civ founders)
    /// </summary>
    private void OnManageCivilizationClicked()
    {
        _capi!.Logger.Debug("[PantheonWars] Manage Civilization clicked");

        // Open civilization dialog on "My Civilization" tab
        var civilizationDialog = _capi.ModLoader.GetModSystem<CivilizationDialog>();
        if (civilizationDialog != null)
        {
            civilizationDialog.Open(initialTab: 1); // Open to "My Civilization" tab
        }
        else
        {
            _capi.ShowChatMessage("Civilization dialog not available!");
        }
    }
}
