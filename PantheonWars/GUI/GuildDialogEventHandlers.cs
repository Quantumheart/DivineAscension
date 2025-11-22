using PantheonWars.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI;

/// <summary>
///     Event handlers for GuildManagementDialog - extracted from main class for maintainability
/// </summary>
public partial class GuildManagementDialog
{
    private const string PANTHEONWARS_SOUNDS_DEITIES = "pantheonwars:sounds/deities/";

    /// <summary>
    ///     Periodically check if player religion data is available
    /// </summary>
    private void OnCheckDataAvailability(float dt)
    {
        if (_state.IsReady) return;

        // Request religion info from server
        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.RequestPlayerReligionInfo();
            _state.IsReady = true;
            _capi!.Event.UnregisterGameTickListener(_checkDataId);
        }
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

        // Reset dialog state to "No Religion" mode
        _manager!.Reset();
        _state.IsReady = true; // Keep dialog ready so it doesn't close

        // Request fresh data from server
        _pantheonWarsSystem?.RequestPlayerReligionInfo();
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

            // If leaving religion, reset dialog state immediately
            if (packet.Action == "leave")
            {
                _capi.Logger.Debug("[PantheonWars] Resetting dialog after leaving religion");
                _manager!.Reset();
            }

            // Request fresh religion data (religion may have changed)
            _pantheonWarsSystem?.RequestPlayerReligionInfo();

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
    ///     Handle player religion data updates
    /// </summary>
    private void OnPlayerReligionDataUpdated(PlayerReligionDataPacket packet)
    {
        // Skip if manager is not initialized yet
        if (_manager == null) return;

        _capi!.Logger.Debug($"[PantheonWars] Updating dialog with religion data");

        // Request updated religion info
        _pantheonWarsSystem?.RequestPlayerReligionInfo();
    }
}
