using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PantheonWars.Commands;
using PantheonWars.GUI;
using PantheonWars.Network;
using PantheonWars.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars;

[ExcludeFromCodeCoverage]
public class PantheonWarsSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "pantheonwars";

    // Client-side systems
    private ICoreClientAPI? _capi;
    private IClientNetworkChannel? _clientChannel;
    private CreateReligionDialog? _createReligionDialog;
    private ReligionManagementDialog? _religionDialog;

    // Server-side systems
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _serverChannel;
    private PlayerReligionDataManager? _playerReligionDataManager;
    private ReligionCommands? _religionCommands;
    private ReligionManager? _religionManager;

    public string ModName => "pantheonwars";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification("[PantheonWars] Mod loaded!");

        // Register network channel and message types
        api.Network.RegisterChannel(NETWORK_CHANNEL)
            .RegisterMessageType<PlayerReligionDataPacket>()
            .RegisterMessageType<ReligionListRequestPacket>()
            .RegisterMessageType<ReligionListResponsePacket>()
            .RegisterMessageType<PlayerReligionInfoRequestPacket>()
            .RegisterMessageType<PlayerReligionInfoResponsePacket>()
            .RegisterMessageType<ReligionActionRequestPacket>()
            .RegisterMessageType<ReligionActionResponsePacket>()
            .RegisterMessageType<CreateReligionRequestPacket>()
            .RegisterMessageType<CreateReligionResponsePacket>()
            .RegisterMessageType<EditDescriptionRequestPacket>()
            .RegisterMessageType<EditDescriptionResponsePacket>()
            .RegisterMessageType<ReligionStateChangedPacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;
        api.Logger.Notification("[PantheonWars] Initializing server-side systems...");

        // Initialize religion systems
        _religionManager = new ReligionManager(api);
        _religionManager.Initialize();

        _playerReligionDataManager = new PlayerReligionDataManager(api, _religionManager);
        _playerReligionDataManager.Initialize();

        // Setup network channel and handlers
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _serverChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerMessageReceived);
        SetupServerNetworking(api);

        // Register commands
        _religionCommands = new ReligionCommands(api, _religionManager, _playerReligionDataManager, _serverChannel);
        _religionCommands.RegisterCommands();

        api.Logger.Notification("[PantheonWars] Server-side initialization complete");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _capi = api;
        api.Logger.Notification("[PantheonWars] Initializing client-side systems...");

        // Setup network handlers
        SetupClientNetworking(api);
        

        api.Logger.Notification("[PantheonWars] Client-side initialization complete");
    }

    public override void Dispose()
    {
        base.Dispose();

        // Cleanup
        _religionDialog?.Dispose();
        _createReligionDialog?.Dispose();
    }

    #region Server Networking

    private void SetupServerNetworking(ICoreServerAPI api)
    {
        // Register handlers for religion dialog packets
        _serverChannel!.SetMessageHandler<ReligionListRequestPacket>(OnReligionListRequest);
        _serverChannel.SetMessageHandler<PlayerReligionInfoRequestPacket>(OnPlayerReligionInfoRequest);
        _serverChannel.SetMessageHandler<ReligionActionRequestPacket>(OnReligionActionRequest);
        _serverChannel.SetMessageHandler<CreateReligionRequestPacket>(OnCreateReligionRequest);
        _serverChannel.SetMessageHandler<EditDescriptionRequestPacket>(OnEditDescriptionRequest);
    }

    private void OnServerMessageReceived(IServerPlayer fromPlayer, PlayerReligionDataPacket packet)
    {
        // Handle any client-to-server messages here
        // Currently not used, but necessary for channel setup
    }

    private void OnReligionListRequest(IServerPlayer fromPlayer, ReligionListRequestPacket packet)
    {
        var religions = _religionManager!.GetAllReligions();

        var religionInfoList = religions.Select(r => new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = r.ReligionUID,
            ReligionName = r.ReligionName,
            MemberCount = r.MemberUIDs.Count,
            IsPublic = r.IsPublic,
            FounderUID = r.FounderUID,
            Description = r.Description
        }).ToList();

        var response = new ReligionListResponsePacket(religionInfoList);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    private void OnPlayerReligionInfoRequest(IServerPlayer fromPlayer, PlayerReligionInfoRequestPacket packet)
    {
        var religion = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
        var response = new PlayerReligionInfoResponsePacket();

        if (religion != null)
        {
            response.HasReligion = true;
            response.ReligionUID = religion.ReligionUID;
            response.ReligionName = religion.ReligionName;
            response.FounderUID = religion.FounderUID;
            response.IsPublic = religion.IsPublic;
            response.Description = religion.Description;
            response.IsFounder = religion.FounderUID == fromPlayer.PlayerUID;

            // Build member list with player names
            foreach (var memberUID in religion.MemberUIDs)
            {
                var memberPlayer = _sapi!.World.PlayerByUid(memberUID);
                var memberName = memberPlayer?.PlayerName ?? memberUID;

                response.Members.Add(new PlayerReligionInfoResponsePacket.MemberInfo
                {
                    PlayerUID = memberUID,
                    PlayerName = memberName,
                    IsFounder = memberUID == religion.FounderUID
                });
            }

            // Build banned players list (only for founder)
            if (response.IsFounder)
            {
                var bannedPlayers = _religionManager!.GetBannedPlayers(religion.ReligionUID);
                foreach (var banEntry in bannedPlayers)
                {
                    var bannedPlayer = _sapi!.World.PlayerByUid(banEntry.PlayerUID);
                    var bannedName = bannedPlayer?.PlayerName ?? banEntry.PlayerUID;

                    response.BannedPlayers.Add(new PlayerReligionInfoResponsePacket.BanInfo
                    {
                        PlayerUID = banEntry.PlayerUID,
                        PlayerName = bannedName,
                        Reason = banEntry.Reason,
                        BannedAt = banEntry.BannedAt.ToString("yyyy-MM-dd HH:mm"),
                        ExpiresAt = banEntry.ExpiresAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never",
                        IsPermanent = banEntry.ExpiresAt == null
                    });
                }
            }
        }
        else
        {
            response.HasReligion = false;
        }

        _serverChannel!.SendPacket(response, fromPlayer);
    }

    private void OnReligionActionRequest(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        string message;
        var success = false;

        try
        {
            switch (packet.Action.ToLower())
            {
                case "join":
                    // Check if player is banned before attempting to join
                    if (_religionManager!.IsBanned(packet.ReligionUID, fromPlayer.PlayerUID))
                    {
                        var banDetails = _religionManager.GetBanDetails(packet.ReligionUID, fromPlayer.PlayerUID);
                        if (banDetails != null)
                        {
                            var expiryText = banDetails.ExpiresAt == null
                                ? "Permanent ban"
                                : $"Expires: {banDetails.ExpiresAt:yyyy-MM-dd HH:mm}";
                            message =
                                $"You are banned from this religion. Reason: {banDetails.Reason}. {expiryText}";
                        }
                        else
                        {
                            message = "You are banned from this religion.";
                        }
                    }
                    else if (_religionManager.CanJoinReligion(packet.ReligionUID, fromPlayer.PlayerUID))
                    {
                        _playerReligionDataManager!.JoinReligion(fromPlayer.PlayerUID, packet.ReligionUID);
                        var religion = _religionManager.GetReligion(packet.ReligionUID);
                        message = $"Successfully joined {religion?.ReligionName ?? "religion"}!";
                        success = true;
                        SendPlayerDataToClient(fromPlayer); // Refresh player's HUD
                    }
                    else
                    {
                        message =
                            "Cannot join this religion. Check if you already have a religion or if it's invite-only.";
                    }

                    break;

                case "leave":
                    var currentReligion = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
                    if (currentReligion != null)
                    {
                        var religionNameForLeave = currentReligion.ReligionName;
                        _playerReligionDataManager!.LeaveReligion(fromPlayer.PlayerUID);
                        message = $"Left {religionNameForLeave}.";
                        success = true;
                        SendPlayerDataToClient(fromPlayer); // Refresh player's HUD

                        // Send religion state changed packet
                        var statePacket = new ReligionStateChangedPacket
                        {
                            Reason = $"You left {religionNameForLeave}",
                            HasReligion = false
                        };
                        _serverChannel!.SendPacket(statePacket, fromPlayer);
                    }
                    else
                    {
                        message = "You are not in a religion.";
                    }

                    break;

                case "kick":
                    var religionForKick = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
                    if (religionForKick != null && religionForKick.FounderUID == fromPlayer.PlayerUID)
                    {
                        if (packet.TargetPlayerUID != fromPlayer.PlayerUID)
                        {
                            _playerReligionDataManager!.LeaveReligion(packet.TargetPlayerUID);
                            message = "Kicked player from religion.";
                            success = true;

                            // Notify kicked player if online
                            var kickedPlayer = _sapi!.World.PlayerByUid(packet.TargetPlayerUID) as IServerPlayer;
                            if (kickedPlayer != null)
                            {
                                kickedPlayer.SendMessage(0,
                                    $"You have been kicked from {religionForKick.ReligionName}.",
                                    EnumChatType.Notification);
                                SendPlayerDataToClient(kickedPlayer); // Refresh kicked player's HUD

                                // Send religion state changed packet
                                var statePacket = new ReligionStateChangedPacket
                                {
                                    Reason = $"You have been kicked from {religionForKick.ReligionName}",
                                    HasReligion = false
                                };
                                _serverChannel!.SendPacket(statePacket, kickedPlayer);
                            }
                        }
                        else
                        {
                            message = "You cannot kick yourself.";
                        }
                    }
                    else
                    {
                        message = "Only the founder can kick members.";
                    }

                    break;

                case "ban":
                    var religionForBan = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
                    if (religionForBan != null && religionForBan.IsFounder(fromPlayer.PlayerUID))
                    {
                        if (packet.TargetPlayerUID != fromPlayer.PlayerUID)
                        {
                            // Extract ban parameters from packet data
                            string reason = "No reason provided";
                            int? expiryDays = null;

                            if (packet.Data != null)
                            {
                                if (packet.Data.ContainsKey("Reason"))
                                    reason = packet.Data["Reason"]?.ToString() ?? "No reason provided";

                                if (packet.Data.ContainsKey("ExpiryDays"))
                                {
                                    var expiryValue = packet.Data["ExpiryDays"]?.ToString();
                                    if (!string.IsNullOrEmpty(expiryValue) && int.TryParse(expiryValue, out var days) &&
                                        days > 0) expiryDays = days;
                                }
                            }

                            // Kick the player if they're still a member
                            if (religionForBan.IsMember(packet.TargetPlayerUID))
                                _playerReligionDataManager!.LeaveReligion(packet.TargetPlayerUID);

                            // Ban the player
                            _religionManager.BanPlayer(
                                religionForBan.ReligionUID,
                                packet.TargetPlayerUID,
                                fromPlayer.PlayerUID,
                                reason,
                                expiryDays
                            );

                            var expiryText = expiryDays.HasValue ? $" for {expiryDays} days" : " permanently";
                            message = $"Player has been banned from the religion{expiryText}. Reason: {reason}";
                            success = true;

                            // Notify banned player if online
                            var bannedPlayer = _sapi!.World.PlayerByUid(packet.TargetPlayerUID) as IServerPlayer;
                            if (bannedPlayer != null)
                            {
                                bannedPlayer.SendMessage(0,
                                    $"You have been banned from {religionForBan.ReligionName}. Reason: {reason}",
                                    EnumChatType.Notification);
                                SendPlayerDataToClient(bannedPlayer);

                                // Send religion state changed packet
                                var statePacket = new ReligionStateChangedPacket
                                {
                                    Reason = $"You have been banned from {religionForBan.ReligionName}. Reason: {reason}",
                                    HasReligion = false
                                };
                                _serverChannel!.SendPacket(statePacket, bannedPlayer);
                            }
                        }
                        else
                        {
                            message = "You cannot ban yourself.";
                        }
                    }
                    else
                    {
                        message = "Only the founder can ban members.";
                    }

                    break;

                case "unban":
                    var religionForUnban = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
                    if (religionForUnban != null && religionForUnban.IsFounder(fromPlayer.PlayerUID))
                    {
                        if (_religionManager.UnbanPlayer(religionForUnban.ReligionUID, packet.TargetPlayerUID))
                        {
                            message = "Player has been unbanned from the religion.";
                            success = true;
                        }
                        else
                        {
                            message = "Failed to unban player. They may not be banned.";
                        }
                    }
                    else
                    {
                        message = "Only the founder can unban players.";
                    }

                    break;

                case "invite":
                    var religionForInvite = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
                    if (religionForInvite != null)
                    {
                        _religionManager.InvitePlayer(religionForInvite.ReligionUID, packet.TargetPlayerUID,
                            fromPlayer.PlayerUID);
                        message = "Invitation sent!";
                        success = true;
                    }
                    else
                    {
                        message = "You are not in a religion.";
                    }

                    break;
                case "disband":
                    var religionForDisband = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
                    if (religionForDisband != null && religionForDisband.FounderUID == fromPlayer.PlayerUID)
                    {
                        var religionName = religionForDisband.ReligionName;

                        // Remove all members
                        var members =
                            religionForDisband.MemberUIDs.ToList(); // Copy to avoid modification during iteration
                        foreach (var memberUID in members)
                        {
                            _playerReligionDataManager!.LeaveReligion(memberUID);

                            // Notify member if online
                            var memberPlayer = _sapi!.World.PlayerByUid(memberUID) as IServerPlayer;
                            if (memberPlayer != null)
                            {
                                // Send chat notification to other members
                                if (memberUID != fromPlayer.PlayerUID)
                                    memberPlayer.SendMessage(
                                        GlobalConstants.GeneralChatGroup,
                                        $"{religionName} has been disbanded by its founder",
                                        EnumChatType.Notification
                                    );

                                // Send religion state changed packet to all members (including founder)
                                var statePacket = new ReligionStateChangedPacket
                                {
                                    Reason = $"{religionName} has been disbanded",
                                    HasReligion = false
                                };
                                _serverChannel!.SendPacket(statePacket, memberPlayer);
                            }
                        }

                        // Delete the religion
                        _religionManager.DeleteReligion(religionForDisband.ReligionUID, fromPlayer.PlayerUID);

                        message = $"Successfully disbanded {religionForDisband.ReligionName ?? "religion"}!";
                    }
                    else
                    {
                        message = "Only the founder can kick members.";
                    }

                    break;

                default:
                    message = $"Unknown action: {packet.Action}";
                    break;
            }
        }
        catch (Exception ex)
        {
            message = $"Error: {ex.Message}";
            _sapi!.Logger.Error($"[PantheonWars] Religion action error: {ex}");
        }

        var response = new ReligionActionResponsePacket(success, message, packet.Action);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    private void OnCreateReligionRequest(IServerPlayer fromPlayer, CreateReligionRequestPacket packet)
    {
        string message;
        var success = false;
        var religionUID = "";

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(packet.ReligionName))
            {
                message = "Religion name cannot be empty.";
            }
            else if (packet.ReligionName.Length < 3)
            {
                message = "Religion name must be at least 3 characters.";
            }
            else if (packet.ReligionName.Length > 32)
            {
                message = "Religion name must be 32 characters or less.";
            }
            else if (_religionManager!.GetReligionByName(packet.ReligionName) != null)
            {
                message = "A religion with that name already exists.";
            }
            else if (_religionManager.HasReligion(fromPlayer.PlayerUID))
            {
                message = "You are already in a religion. Leave your current religion first.";
            }
            else
            {
                // Create the religion
                var newReligion = _religionManager.CreateReligion(
                    packet.ReligionName,
                    fromPlayer.PlayerUID,
                    packet.IsPublic
                );

                // Auto-join the founder
                _playerReligionDataManager!.JoinReligion(fromPlayer.PlayerUID, newReligion.ReligionUID);

                religionUID = newReligion.ReligionUID;
                message = $"Successfully created {packet.ReligionName}!";
                success = true;
            }
        }
        catch (Exception ex)
        {
            message = $"Error creating religion: {ex.Message}";
            _sapi!.Logger.Error($"[PantheonWars] Religion creation error: {ex}");
        }

        var response = new CreateReligionResponsePacket(success, message, religionUID);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    private void OnEditDescriptionRequest(IServerPlayer fromPlayer, EditDescriptionRequestPacket packet)
    {
        string message;
        var success = false;

        try
        {
            var religion = _religionManager!.GetReligion(packet.ReligionUID);

            if (religion == null)
            {
                message = "Religion not found.";
            }
            else if (religion.FounderUID != fromPlayer.PlayerUID)
            {
                message = "Only the founder can edit the description.";
            }
            else if (packet.Description.Length > 200)
            {
                message = "Description must be 200 characters or less.";
            }
            else
            {
                // Update description
                religion.Description = packet.Description;
                message = "Description updated successfully!";
                success = true;
            }
        }
        catch (Exception ex)
        {
            message = $"Error updating description: {ex.Message}";
            _sapi!.Logger.Error($"[PantheonWars] Description edit error: {ex}");
        }

        var response = new EditDescriptionResponsePacket(success, message);
        _serverChannel!.SendPacket(response, fromPlayer);
    }


    #endregion

    #region Client Networking

    private void SetupClientNetworking(ICoreClientAPI api)
    {
        _clientChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _clientChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerPlayerDataUpdate);
        _clientChannel.SetMessageHandler<ReligionListResponsePacket>(OnReligionListResponse);
        _clientChannel.SetMessageHandler<PlayerReligionInfoResponsePacket>(OnPlayerReligionInfoResponse);
        _clientChannel.SetMessageHandler<ReligionActionResponsePacket>(OnReligionActionResponse);
        _clientChannel.SetMessageHandler<CreateReligionResponsePacket>(OnCreateReligionResponse);
        _clientChannel.SetMessageHandler<EditDescriptionResponsePacket>(OnEditDescriptionResponse);
        _clientChannel.SetMessageHandler<ReligionStateChangedPacket>(OnReligionStateChanged);
        _clientChannel.RegisterMessageType(typeof(PlayerReligionDataPacket));
    }

    private void OnServerPlayerDataUpdate(PlayerReligionDataPacket packet)
    {
        // Trigger event for UI components
        PlayerReligionDataUpdated?.Invoke(packet);
    }

    private void OnReligionListResponse(ReligionListResponsePacket packet)
    {
        _religionDialog?.OnReligionListResponse(packet);
        ReligionListReceived?.Invoke(packet);
    }

    private void OnPlayerReligionInfoResponse(PlayerReligionInfoResponsePacket packet)
    {
        _religionDialog?.OnPlayerReligionInfoResponse(packet);
        PlayerReligionInfoReceived?.Invoke(packet);
    }

    private void OnReligionActionResponse(ReligionActionResponsePacket packet)
    {
        _religionDialog?.OnActionResponse(packet);
        ReligionActionCompleted?.Invoke(packet);
    }

    private void OnCreateReligionResponse(CreateReligionResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);

            // Refresh religion dialog data
            if (_religionDialog != null && _religionDialog.IsOpened())
            {
                _religionDialog.TryClose();
                _religionDialog.TryOpen(); // Reopen to refresh
            }
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _capi?.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.3f);
        }
    }

    private void OnEditDescriptionResponse(EditDescriptionResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);
            // Refresh religion dialog to show updated description
            if (_religionDialog != null && _religionDialog.IsOpened())
            {
                _religionDialog.TryClose();
                _religionDialog.TryOpen(); // Reopen to refresh
            }
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");
        }
    }


    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi?.Logger.Notification($"[PantheonWars] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi?.ShowChatMessage(packet.Reason);

        // Trigger event for UI components
        ReligionStateChanged?.Invoke(packet);
    }

    /// <summary>
    /// Request religion list from the server
    /// </summary>
    public void RequestReligionList()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion list: client channel not initialized");
            return;
        }

        var request = new ReligionListRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent religion list request");
    }

    /// <summary>
    /// Send a religion action request to the server (join, leave, kick, invite)
    /// </summary>
    public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot perform religion action: client channel not initialized");
            return;
        }

        var request = new ReligionActionRequestPacket(action, religionUID, targetPlayerUID);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent religion action request: {action}");
    }

    /// <summary>
    /// Request to create a new religion
    /// </summary>
    public void RequestCreateReligion(string religionName, bool isPublic)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot create religion: client channel not initialized");
            return;
        }

        var request = new CreateReligionRequestPacket(religionName, isPublic);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent create religion request: {religionName}");
    }

    /// <summary>
    /// Request player's religion info (for management overlay)
    /// </summary>
    public void RequestPlayerReligionInfo()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion info: client channel not initialized");
            return;
        }

        var request = new PlayerReligionInfoRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent player religion info request");
    }

    /// <summary>
    /// Request to edit religion description
    /// </summary>
    public void RequestEditDescription(string religionUID, string description)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot edit description: client channel not initialized");
            return;
        }

        var request = new EditDescriptionRequestPacket(religionUID, description);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent edit description request");
    }

    /// <summary>
    /// Event fired when player religion data is updated from the server
    /// </summary>
    public event Action<PlayerReligionDataPacket>? PlayerReligionDataUpdated;

    /// <summary>
    /// Event fired when the player's religion state changes (disbanded, kicked, etc.)
    /// </summary>
    public event Action<ReligionStateChangedPacket>? ReligionStateChanged;

    /// <summary>
    /// Event fired when religion list is received from server
    /// </summary>
    public event Action<ReligionListResponsePacket>? ReligionListReceived;

    /// <summary>
    /// Event fired when religion action is completed (join, leave, etc.)
    /// </summary>
    public event Action<ReligionActionResponsePacket>? ReligionActionCompleted;

    /// <summary>
    /// Event fired when player religion info is received from server
    /// </summary>
    public event Action<PlayerReligionInfoResponsePacket>? PlayerReligionInfoReceived;

    #endregion
}