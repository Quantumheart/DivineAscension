using System;
using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.GUI.Utilities;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Client;
using GuiDialog = DivineAscension.GUI.GuiDialog;

namespace DivineAscension.Systems.Networking.Client;

/// <summary>
///     Handles all client-side network communication for the DivineAscension mod.
///     Manages requests to the server and processes responses for religion, blessing, and civilization systems.
/// </summary>
public class DivineAscensionNetworkClient : IClientNetworkHandler
{
    private ICoreClientAPI? _capi;
    private IClientNetworkChannel? _clientChannel;

    private bool IsNetworkAvailable()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot edit description: client channel not initialized");
            return false;
        }

        return true;
    }

    #region IClientNetworkHandler Implementation

    public void Initialize(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    public void RegisterHandlers(IClientNetworkChannel channel)
    {
        _clientChannel = channel;

        // Register all message handlers
        _clientChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerPlayerDataUpdate);
        _clientChannel.SetMessageHandler<ReligionListResponsePacket>(OnReligionListResponse);
        _clientChannel.SetMessageHandler<PlayerReligionInfoResponsePacket>(OnPlayerReligionInfoResponse);
        _clientChannel.SetMessageHandler<ReligionDetailResponsePacket>(OnReligionDetailResponse);
        _clientChannel.SetMessageHandler<ReligionActionResponsePacket>(OnReligionActionResponse);
        _clientChannel.SetMessageHandler<CreateReligionResponsePacket>(OnCreateReligionResponse);
        _clientChannel.SetMessageHandler<EditDescriptionResponsePacket>(OnEditDescriptionResponse);
        _clientChannel.SetMessageHandler<BlessingUnlockResponsePacket>(OnBlessingUnlockResponse);
        _clientChannel.SetMessageHandler<BlessingDataResponsePacket>(OnBlessingDataResponse);
        _clientChannel.SetMessageHandler<ReligionStateChangedPacket>(OnReligionStateChanged);
        _clientChannel.SetMessageHandler<CivilizationListResponsePacket>(OnCivilizationListResponse);
        _clientChannel.SetMessageHandler<CivilizationInfoResponsePacket>(OnCivilizationInfoResponse);
        _clientChannel.SetMessageHandler<CivilizationActionResponsePacket>(OnCivilizationActionResponse);

        // Register handlers for role management responses
        _clientChannel.SetMessageHandler<ReligionRolesResponse>(OnReligionRolesResponse);
        _clientChannel.SetMessageHandler<CreateRoleResponse>(OnCreateRoleResponse);
        _clientChannel.SetMessageHandler<ModifyRolePermissionsResponse>(OnModifyRolePermissionsResponse);
        _clientChannel.SetMessageHandler<AssignRoleResponse>(OnAssignRoleResponse);
        _clientChannel.SetMessageHandler<DeleteRoleResponse>(OnDeleteRoleResponse);
        _clientChannel.SetMessageHandler<TransferFounderResponse>(OnTransferFounderResponse);

        // Register handlers for diplomacy responses
        _clientChannel.SetMessageHandler<DiplomacyInfoResponsePacket>(OnDiplomacyInfoResponse);
        _clientChannel.SetMessageHandler<DiplomacyActionResponsePacket>(OnDiplomacyActionResponse);
        _clientChannel.SetMessageHandler<WarDeclarationPacket>(OnWarDeclarationBroadcast);

        // Register handler for deity name change response
        _clientChannel.SetMessageHandler<SetDeityNameResponsePacket>(OnSetDeityNameResponse);

        // Register handler for activity log response
        _clientChannel.SetMessageHandler<ActivityLogResponsePacket>(OnActivityLogResponse);

        _clientChannel.RegisterMessageType(typeof(PlayerReligionDataPacket));
    }

    public void Dispose()
    {
        // Clear event subscriptions
        PlayerReligionDataUpdated = null;
        BlessingDataReceived = null;
        BlessingUnlocked = null;
        ReligionStateChanged = null;
        ReligionListReceived = null;
        ReligionActionCompleted = null;
        PlayerReligionInfoReceived = null;
        CivilizationListReceived = null;
        CivilizationInfoReceived = null;
        CivilizationActionCompleted = null;
        ReligionRolesReceived = null;
        RoleCreated = null;
        RolePermissionsModified = null;
        RoleAssigned = null;
        RoleDeleted = null;
        FounderTransferred = null;
        DiplomacyInfoReceived = null;
        DiplomacyActionCompleted = null;
        WarDeclared = null;
        ActivityLogReceived = null;
    }

    #endregion

    #region Response Handlers

    private void OnServerPlayerDataUpdate(PlayerReligionDataPacket packet)
    {
        // Trigger event for BlessingDialog and other UI components
        PlayerReligionDataUpdated?.Invoke(packet);
    }

    private void OnReligionListResponse(ReligionListResponsePacket packet)
    {
        ReligionListReceived?.Invoke(packet);
    }

    private void OnPlayerReligionInfoResponse(PlayerReligionInfoResponsePacket packet)
    {
        PlayerReligionInfoReceived?.Invoke(packet);
    }

    private void OnReligionDetailResponse(ReligionDetailResponsePacket packet)
    {
        ReligionDetailReceived?.Invoke(packet);
    }

    private void OnReligionActionResponse(ReligionActionResponsePacket packet)
    {
        ReligionActionCompleted?.Invoke(packet);
    }

    private void OnCreateReligionResponse(CreateReligionResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);

            // Request fresh blessing data (now in a religion)
            // Use a small delay to ensure server has processed the religion creation
            _capi?.Event.RegisterCallback(dt =>
            {
                var request = new BlessingDataRequestPacket();
                _clientChannel?.SendPacket(request);
            }, 100);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");
        }
    }

    private void OnEditDescriptionResponse(EditDescriptionResponsePacket packet)
    {
        if (packet.Success)
            _capi?.ShowChatMessage(packet.Message);
        else
            _capi?.ShowChatMessage($"Error: {packet.Message}");
    }

    private void OnSetDeityNameResponse(SetDeityNameResponsePacket packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Received set deity name response: Success={packet.Success}");
        DeityNameChanged?.Invoke(packet);
    }

    private void OnReligionRolesResponse(ReligionRolesResponse packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Received religion roles response: Success={packet.Success}");

        if (!packet.Success)
        {
            _capi?.Logger.Warning($"[DivineAscension] Religion roles request failed: {packet.ErrorMessage}");
            _capi?.ShowChatMessage($"Error: {packet.ErrorMessage}");
        }

        ReligionRolesReceived?.Invoke(packet);
    }

    private void OnCreateRoleResponse(CreateRoleResponse packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Create role response: Success={packet.Success}");

        if (packet.Success)
            _capi?.ShowChatMessage($"Role '{packet.CreatedRole?.RoleName}' created successfully!");
        else
            _capi?.ShowChatMessage($"Error: {packet.ErrorMessage}");

        RoleCreated?.Invoke(packet);
    }

    private void OnModifyRolePermissionsResponse(ModifyRolePermissionsResponse packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Modify role permissions response: Success={packet.Success}");

        if (packet.Success)
            _capi?.ShowChatMessage("Role permissions updated successfully!");
        else
            _capi?.ShowChatMessage($"Error: {packet.ErrorMessage}");

        RolePermissionsModified?.Invoke(packet);
    }

    private void OnAssignRoleResponse(AssignRoleResponse packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Assign role response: Success={packet.Success}");

        if (packet.Success)
            _capi?.ShowChatMessage("Role assigned successfully!");
        else
            _capi?.ShowChatMessage($"Error: {packet.ErrorMessage}");

        RoleAssigned?.Invoke(packet);
    }

    private void OnDeleteRoleResponse(DeleteRoleResponse packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Delete role response: Success={packet.Success}");

        if (packet.Success)
            _capi?.ShowChatMessage("Role deleted successfully!");
        else
            _capi?.ShowChatMessage($"Error: {packet.ErrorMessage}");

        RoleDeleted?.Invoke(packet);
    }

    private void OnTransferFounderResponse(TransferFounderResponse packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Transfer founder response: Success={packet.Success}");

        if (packet.Success)
            _capi?.ShowChatMessage("Founder status transferred successfully!");
        else
            _capi?.ShowChatMessage($"Error: {packet.ErrorMessage}");

        FounderTransferred?.Invoke(packet);
    }

    private void OnBlessingUnlockResponse(BlessingUnlockResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);
            _capi?.Logger.Notification($"[DivineAscension] Blessing unlocked: {packet.BlessingId}");

            // Trigger blessing unlock event for UI refresh
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");
            _capi?.Logger.Warning($"[DivineAscension] Failed to unlock blessing: {packet.Message}");

            // Trigger event even on failure so UI can update
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
    }

    private void OnBlessingDataResponse(BlessingDataResponsePacket packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Received blessing data: HasReligion={packet.HasReligion}");

        // Trigger event for BlessingDialog to consume
        BlessingDataReceived?.Invoke(packet);
    }

    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi?.Logger.Notification($"[DivineAscension] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi?.ShowChatMessage(packet.Reason);

        // Trigger event for BlessingDialog to refresh its data
        ReligionStateChanged?.Invoke(packet);
    }

    private void OnCivilizationListResponse(CivilizationListResponsePacket packet)
    {
        _capi?.Logger.Debug(
            $"[DivineAscension] Received civilization list: {packet.Civilizations.Count} civilizations");
        CivilizationListReceived?.Invoke(packet);
    }

    private void OnCivilizationInfoResponse(CivilizationInfoResponsePacket packet)
    {
        _capi?.Logger.Debug("[DivineAscension] Received civilization info");
        CivilizationInfoReceived?.Invoke(packet);
    }

    private void OnCivilizationActionResponse(CivilizationActionResponsePacket packet)
    {
        _capi?.Logger.Debug($"[DivineAscension] Civilization action '{packet.Action}' response: {packet.Success}");

        // Show message to user
        if (!string.IsNullOrEmpty(packet.Message)) _capi?.ShowChatMessage(packet.Message);

        CivilizationActionCompleted?.Invoke(packet);
    }

    private void OnDiplomacyInfoResponse(DiplomacyInfoResponsePacket packet)
    {
        try
        {
            _capi?.Logger.Debug(
                $"[DivineAscension:Diplomacy] Received diplomacy info for civ {packet.CivId}: {packet.Relationships.Count} relationships, {packet.IncomingProposals.Count} incoming, {packet.OutgoingProposals.Count} outgoing");

            // Update civilization state manager via GuiDialogManager
            var guiDialogManager = _capi?.ModLoader.GetModSystem<GuiDialog>()?.DialogManager;
            if (guiDialogManager != null)
            {
                var civManager = guiDialogManager.CivilizationManager;
                var diplomacyState = civManager.DiplomacyState;

                // Update state with received data
                diplomacyState.ActiveRelationships = packet.Relationships;
                diplomacyState.IncomingProposals = packet.IncomingProposals;
                diplomacyState.OutgoingProposals = packet.OutgoingProposals;
                diplomacyState.IsLoading = false;
                diplomacyState.LastRefresh = DateTime.UtcNow;
                diplomacyState.ErrorMessage = null;

                _capi?.Logger.Debug("[DivineAscension:Diplomacy] Updated diplomacy state successfully");
            }
            else
            {
                _capi?.Logger.Warning("[DivineAscension:Diplomacy] Could not access GuiDialogManager to update state");
            }

            // Fire event for additional subscribers
            DiplomacyInfoReceived?.Invoke(packet.CivId);
        }
        catch (Exception ex)
        {
            _capi?.Logger.Error($"[DivineAscension:Diplomacy] Error processing diplomacy info response: {ex.Message}");
            _capi?.Logger.Error($"Stack trace: {ex.StackTrace}");

            // Set error in state if accessible
            var guiDialogManager = _capi?.ModLoader.GetModSystem<GuiDialog>()?.DialogManager;
            if (guiDialogManager != null)
            {
                var civManager = guiDialogManager.CivilizationManager;
                civManager.DiplomacyState.ErrorMessage = "Failed to load diplomacy data";
                civManager.DiplomacyState.IsLoading = false;
            }
        }
    }

    private void OnDiplomacyActionResponse(DiplomacyActionResponsePacket packet)
    {
        try
        {
            _capi?.Logger.Debug(
                $"[DivineAscension:Diplomacy] Diplomacy action '{packet.Action}' response: Success={packet.Success}");

            // Show message to user
            if (!string.IsNullOrEmpty(packet.Message))
            {
                if (packet.Success)
                    _capi?.ShowChatMessage(packet.Message);
                else
                    _capi?.ShowChatMessage($"Error: {packet.Message}");
            }

            // Update state
            var guiDialogManager = _capi?.ModLoader.GetModSystem<GuiDialog>()?.DialogManager;
            if (guiDialogManager != null)
            {
                var civManager = guiDialogManager.CivilizationManager;
                var diplomacyState = civManager.DiplomacyState;

                if (packet.Success)
                {
                    // Clear confirmation flags on success
                    diplomacyState.ConfirmBreakRelationshipId = null;
                    diplomacyState.ConfirmWarCivId = null;
                    diplomacyState.ErrorMessage = null;

                    // Trigger refresh of diplomacy data
                    civManager.RequestDiplomacyInfo();
                }
                else
                {
                    // Set error message
                    diplomacyState.ErrorMessage = packet.Message;
                }
            }

            // Fire event for additional subscribers
            DiplomacyActionCompleted?.Invoke(packet.Action ?? string.Empty, packet.Success);
        }
        catch (Exception ex)
        {
            _capi?.Logger.Error(
                $"[DivineAscension:Diplomacy] Error processing diplomacy action response: {ex.Message}");
            _capi?.Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnWarDeclarationBroadcast(WarDeclarationPacket packet)
    {
        try
        {
            _capi?.Logger.Debug(
                $"[DivineAscension:Diplomacy] War declared: {packet.DeclarerCivName} vs {packet.TargetCivName}");

            // Check if player is in either civilization
            var guiDialogManager = _capi?.ModLoader.GetModSystem<GuiDialog>()?.DialogManager;
            if (guiDialogManager != null)
            {
                var civManager = guiDialogManager.CivilizationManager;
                var playerCivId = civManager.CurrentCivilizationId;

                var isInvolved = playerCivId == packet.DeclarerCivId || playerCivId == packet.TargetCivId;

                // Show notification using helper
                if (_capi != null)
                {
                    DiplomacyNotificationHelper.NotifyWarDeclared(_capi, packet.DeclarerCivName, packet.TargetCivName,
                        isInvolved);
                }

                // Refresh diplomacy data if tab is open and player is involved
                if (isInvolved && civManager.CurrentSubTab == CivilizationSubTab.Diplomacy)
                {
                    civManager.RequestDiplomacyInfo();
                }
            }

            // Fire event for additional subscribers
            WarDeclared?.Invoke(packet.DeclarerCivId, packet.TargetCivId);
        }
        catch (Exception ex)
        {
            _capi?.Logger.Error(
                $"[DivineAscension:Diplomacy] Error processing war declaration broadcast: {ex.Message}");
            _capi?.Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnActivityLogResponse(ActivityLogResponsePacket packet)
    {
        _capi?.Logger.Debug(
            $"[DivineAscension] Received activity log response with {packet.Entries.Count} entries");

        // Fire event for subscribers (e.g., ReligionStateManager)
        ActivityLogReceived?.Invoke(packet);
    }

    #endregion

    #region Request Methods

    /// <summary>
    ///     Request blessing data from the server
    /// </summary>
    public void RequestBlessingData()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request blessing data: client channel not initialized");
            return;
        }

        var request = new BlessingDataRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent blessing data request to server");
    }

    /// <summary>
    ///     Send a blessing unlock request to the server
    /// </summary>
    public void RequestBlessingUnlock(string blessingId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot unlock blessing: client channel not initialized");
            return;
        }

        var request = new BlessingUnlockRequestPacket(blessingId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent unlock request for blessing: {blessingId}");
    }

    /// <summary>
    ///     Request religion list from the server
    /// </summary>
    public void RequestReligionList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request religion list: client channel not initialized");
            return;
        }

        var request = new ReligionListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent religion list request with filter: {deityFilter}");
    }

    /// <summary>
    ///     Send a religion action request to the server (join, leave, kick, invite)
    /// </summary>
    public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot perform religion action: client channel not initialized");
            return;
        }

        var request = new ReligionActionRequestPacket(action, religionUID, targetPlayerUID);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent religion action request: {action}");
    }

    /// <summary>
    ///     Request to create a new religion
    /// </summary>
    public void RequestCreateReligion(string religionName, string domain, string deityName, bool isPublic)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot create religion: client channel not initialized");
            return;
        }

        var request = new CreateReligionRequestPacket(religionName, domain, deityName, isPublic);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug(
            $"[DivineAscension] Sent create religion request: {religionName}, domain={domain}, deityName={deityName}");
    }

    /// <summary>
    ///     Request player's religion info (for management overlay)
    /// </summary>
    public void RequestPlayerReligionInfo()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request religion info: client channel not initialized");
            return;
        }

        var request = new PlayerReligionInfoRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent player religion info request");
    }

    /// <summary>
    ///     Request to edit religion description
    /// </summary>
    public void RequestEditDescription(string religionUID, string description)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot edit description: client channel not initialized");
            return;
        }

        var request = new EditDescriptionRequestPacket(religionUID, description);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent edit description request");
    }

    /// <summary>
    ///     Request to change the deity name for a religion
    /// </summary>
    public void SendSetDeityNameRequest(string religionUID, string newDeityName)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot set deity name: client channel not initialized");
            return;
        }

        var request = new SetDeityNameRequestPacket(religionUID, newDeityName);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent set deity name request for {religionUID}");
    }

    /// <summary>
    ///     Request detailed information about a specific religion
    /// </summary>
    public void RequestReligionDetail(string religionUID)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request religion detail: client channel not initialized");
            return;
        }

        var request = new ReligionDetailRequestPacket { ReligionUID = religionUID };
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent religion detail request for {religionUID}");
    }

    /// <summary>
    ///     Request roles data for a religion
    /// </summary>
    public void RequestReligionRoles(string religionUID)
    {
        if (!IsNetworkAvailable()) return;

        var request = new ReligionRolesRequest(religionUID);
        _clientChannel!.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent request religion roles");
    }

    /// <summary>
    ///     Request to create a custom role
    /// </summary>
    public void RequestCreateRole(string id, string roleName)
    {
        if (!IsNetworkAvailable()) return;

        var request = new CreateRoleRequest(id, roleName);
        _clientChannel!.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent create role request");
    }

    /// <summary>
    ///     Request to modify role permissions
    /// </summary>
    public void RequestModifyRolePermissions(string id, string roleId, HashSet<string> permissions)
    {
        if (!IsNetworkAvailable()) return;

        var request = new ModifyRolePermissionsRequest(id, roleId, permissions);
        _clientChannel!.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent modify role permissions request");
    }

    /// <summary>
    ///     Request to delete a role
    /// </summary>
    public void RequestDeleteRole(string id, string roleId)
    {
        if (!IsNetworkAvailable()) return;

        var request = new DeleteRoleRequest(id, roleId);
        _clientChannel!.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent delete role request");
    }

    /// <summary>
    ///     Request to assign a role to a player
    /// </summary>
    public void RequestAssignRole(string id, string targetPlayerId, string roleId)
    {
        if (!IsNetworkAvailable()) return;

        var request = new AssignRoleRequest(id, targetPlayerId, roleId);
        _clientChannel!.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent assign role request");
    }

    /// <summary>
    ///     Request to transfer founder status
    /// </summary>
    public void RequestTransferFounder(string id, string founderId)
    {
        if (!IsNetworkAvailable()) return;

        var request = new TransferFounderRequest(id, founderId);
        _clientChannel!.SendPacket(request);
        _capi?.Logger.Debug("[DivineAscension] Sent transfer founder request");
    }

    /// <summary>
    ///     Request list of all civilizations
    /// </summary>
    public void RequestCivilizationList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request civilization list: client channel not initialized");
            return;
        }

        var request = new CivilizationListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent civilization list request with filter: '{deityFilter}'");
    }

    /// <summary>
    ///     Request detailed information about a specific civilization
    /// </summary>
    public void RequestCivilizationInfo(string civId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request civilization info: client channel not initialized");
            return;
        }

        var request = new CivilizationInfoRequestPacket(civId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent civilization info request for {civId}");
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband, setdescription)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "",
        string icon = "", string description = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request civilization action: client channel not initialized");
            return;
        }

        var request = new CivilizationActionRequestPacket(action, civId, targetId, name, icon, description);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent civilization action request: {action}");
    }

    /// <summary>
    ///     Request diplomacy information for a civilization
    /// </summary>
    public void RequestDiplomacyInfo(string civId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error(
                "[DivineAscension:Diplomacy] Cannot request diplomacy info: client channel not initialized");
            return;
        }

        var request = new DiplomacyInfoRequestPacket(civId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension:Diplomacy] Sent diplomacy info request for civilization: {civId}");
    }

    /// <summary>
    ///     Request a diplomacy action (propose, accept, decline, schedulebreak, cancelbreak, declarewar, declarepeace)
    /// </summary>
    public void RequestDiplomacyAction(string action, string targetCivId = "", string proposalOrRelationshipId = "",
        string proposedStatus = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error(
                "[DivineAscension:Diplomacy] Cannot request diplomacy action: client channel not initialized");
            return;
        }

        var request = new DiplomacyActionRequestPacket(
            action: action,
            targetCivId: targetCivId,
            proposalId: proposalOrRelationshipId,
            proposedStatus: proposedStatus);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug(
            $"[DivineAscension:Diplomacy] Sent diplomacy action request: {action}, target: {targetCivId}, id: {proposalOrRelationshipId}, status: {proposedStatus}");
    }

    /// <summary>
    ///     Request activity log for a religion
    /// </summary>
    public void RequestActivityLog(string religionUID, int limit = 50)
    {
        if (!IsNetworkAvailable())
        {
            _capi?.Logger.Error("[DivineAscension] Cannot request activity log: client channel not initialized");
            return;
        }

        var request = new ActivityLogRequestPacket
        {
            ReligionUID = religionUID,
            Limit = limit
        };

        _clientChannel?.SendPacket(request);
        _capi?.Logger.Debug($"[DivineAscension] Sent activity log request for religion {religionUID}, limit: {limit}");
    }

    #endregion

    #region Events

    /// <summary>
    ///     Event fired when player religion data is updated from the server
    /// </summary>
    public event Action<PlayerReligionDataPacket>? PlayerReligionDataUpdated;

    /// <summary>
    ///     Event fired when blessing data is received from the server
    /// </summary>
    public event Action<BlessingDataResponsePacket>? BlessingDataReceived;

    /// <summary>
    ///     Event fired when a blessing unlock response is received from the server
    ///     Parameters: (blessingId, success)
    /// </summary>
    public event Action<string, bool>? BlessingUnlocked;

    /// <summary>
    ///     Event fired when the player's religion state changes (disbanded, kicked, etc.)
    /// </summary>
    public event Action<ReligionStateChangedPacket>? ReligionStateChanged;

    /// <summary>
    ///     Event fired when religion list is received from server
    /// </summary>
    public event Action<ReligionListResponsePacket>? ReligionListReceived;

    /// <summary>
    ///     Event fired when religion action is completed (join, leave, etc.)
    /// </summary>
    public event Action<ReligionActionResponsePacket>? ReligionActionCompleted;

    /// <summary>
    ///     Event fired when player religion info is received from server
    /// </summary>
    public event Action<PlayerReligionInfoResponsePacket>? PlayerReligionInfoReceived;

    /// <summary>
    ///     Event fired when religion detail info is received from server
    /// </summary>
    public event Action<ReligionDetailResponsePacket>? ReligionDetailReceived;

    /// <summary>
    ///     Event fired when civilization list is received from server
    /// </summary>
    public event Action<CivilizationListResponsePacket>? CivilizationListReceived;

    /// <summary>
    ///     Event fired when civilization info is received from server
    /// </summary>
    public event Action<CivilizationInfoResponsePacket>? CivilizationInfoReceived;

    /// <summary>
    ///     Event fired when civilization action is completed (create, invite, accept, leave, kick, disband)
    /// </summary>
    public event Action<CivilizationActionResponsePacket>? CivilizationActionCompleted;

    /// <summary>
    ///     Event fired when religion roles data is received from server
    /// </summary>
    public event Action<ReligionRolesResponse>? ReligionRolesReceived;

    /// <summary>
    ///     Event fired when a role is created
    /// </summary>
    public event Action<CreateRoleResponse>? RoleCreated;

    /// <summary>
    ///     Event fired when role permissions are modified
    /// </summary>
    public event Action<ModifyRolePermissionsResponse>? RolePermissionsModified;

    /// <summary>
    ///     Event fired when a role is assigned to a player
    /// </summary>
    public event Action<AssignRoleResponse>? RoleAssigned;

    /// <summary>
    ///     Event fired when a role is deleted
    /// </summary>
    public event Action<DeleteRoleResponse>? RoleDeleted;

    /// <summary>
    ///     Event fired when founder status is transferred
    /// </summary>
    public event Action<TransferFounderResponse>? FounderTransferred;

    /// <summary>
    ///     Event fired when diplomacy info is received for a civilization
    /// </summary>
    public event Action<string>? DiplomacyInfoReceived;

    /// <summary>
    ///     Event fired when a diplomacy action completes (success or failure)
    /// </summary>
    public event Action<string, bool>? DiplomacyActionCompleted;

    /// <summary>
    ///     Event fired when a war declaration broadcast is received
    /// </summary>
    public event Action<string, string>? WarDeclared;

    /// <summary>
    ///     Event fired when a deity name change response is received
    /// </summary>
    public event Action<SetDeityNameResponsePacket>? DeityNameChanged;

    /// <summary>
    ///     Event fired when activity log data is received from the server
    /// </summary>
    public event Action<ActivityLogResponsePacket>? ActivityLogReceived;

    #endregion
}