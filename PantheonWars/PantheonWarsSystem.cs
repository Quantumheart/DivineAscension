using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems;
using PantheonWars.Systems.Patches;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars;

[ExcludeFromCodeCoverage]
public class PantheonWarsSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "pantheonwars";
    private BlessingEffectSystem? _blessingEffectSystem;
    private BlessingRegistry? _blessingRegistry;

    // Client-side systems
    private ICoreClientAPI? _capi;

    // Use interfaces for better testability and dependency injection
    private CivilizationManager? _civilizationManager;
    private IClientNetworkChannel? _clientChannel;
    private DeityRegistry? _deityRegistry;
    private FavorSystem? _favorSystem;

    private Harmony? _harmony;
    private PlayerReligionDataManager? _playerReligionDataManager;
    private ReligionManager? _religionManager;

    // Server-side systems
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _serverChannel;

    public string ModName => "pantheonwars";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification("[PantheonWars] Mod loaded!");

        // Register Harmony Patches
        if (_harmony == null)
        {
            _harmony = new Harmony("com.pantheonwars.patches");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            api.Logger.Notification("[PantheonWars] Harmony patches registered.");
        }

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
            .RegisterMessageType<BlessingUnlockRequestPacket>()
            .RegisterMessageType<BlessingUnlockResponsePacket>()
            .RegisterMessageType<BlessingDataRequestPacket>()
            .RegisterMessageType<BlessingDataResponsePacket>()
            .RegisterMessageType<ReligionStateChangedPacket>()
            .RegisterMessageType<CivilizationListRequestPacket>()
            .RegisterMessageType<CivilizationListResponsePacket>()
            .RegisterMessageType<CivilizationInfoRequestPacket>()
            .RegisterMessageType<CivilizationInfoResponsePacket>()
            .RegisterMessageType<CivilizationActionRequestPacket>()
            .RegisterMessageType<CivilizationActionResponsePacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;

        // Setup network channel and handlers
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _serverChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerMessageReceived);
        SetupServerNetworking(api);

        // Initialize all server systems using the initializer
        var result = PantheonWarsSystemInitializer.InitializeServerSystems(api, _serverChannel);

        // Store references to managers for disposal and event subscriptions
        _deityRegistry = result.DeityRegistry;
        _religionManager = result.ReligionManager;
        _civilizationManager = result.CivilizationManager;
        _playerReligionDataManager = result.PlayerReligionDataManager;
        _favorSystem = result.FavorSystem;
        _blessingRegistry = result.BlessingRegistry;
        _blessingEffectSystem = result.BlessingEffectSystem;

        // Subscribe to player data changed event
        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;

        // Hook player join to send initial data
        api.Event.PlayerJoin += OnPlayerJoin;

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

        // Unpatch Harmony
        _harmony?.UnpatchAll("com.pantheonwars.patches");

        // Cleanup systems
        _favorSystem?.Dispose();
        _playerReligionDataManager?.Dispose();
        _religionManager?.Dispose();

        // Clear static events
        PitKilnPatches.ClearSubscribers();
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

        // Register handlers for blessing system packets
        _serverChannel.SetMessageHandler<BlessingUnlockRequestPacket>(OnBlessingUnlockRequest);
        _serverChannel.SetMessageHandler<BlessingDataRequestPacket>(OnBlessingDataRequest);

        // Register handlers for civilization system packets
        _serverChannel.SetMessageHandler<CivilizationListRequestPacket>(OnCivilizationListRequest);
        _serverChannel.SetMessageHandler<CivilizationInfoRequestPacket>(OnCivilizationInfoRequest);
        _serverChannel.SetMessageHandler<CivilizationActionRequestPacket>(OnCivilizationActionRequest);
    }

    private void OnServerMessageReceived(IServerPlayer fromPlayer, PlayerReligionDataPacket packet)
    {
        // Handle any client-to-server messages here
        // Currently not used, but necessary for channel setup
        // Future implementation: Handle deity selection from client dialog
    }

    private void OnReligionListRequest(IServerPlayer fromPlayer, ReligionListRequestPacket packet)
    {
        var religions = string.IsNullOrEmpty(packet.FilterDeity)
            ? _religionManager!.GetAllReligions()
            : _religionManager!.GetReligionsByDeity(
                Enum.TryParse<DeityType>(packet.FilterDeity, out var deity) ? deity : DeityType.None);

        var religionInfoList = religions.Select(r => new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = r.ReligionUID,
            ReligionName = r.ReligionName,
            Deity = r.Deity.ToString(),
            MemberCount = r.MemberUIDs.Count,
            Prestige = r.Prestige,
            PrestigeRank = r.PrestigeRank.ToString(),
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
            response.Deity = religion.Deity.ToString();
            response.FounderUID = religion.FounderUID;
            response.Prestige = religion.Prestige;
            response.PrestigeRank = religion.PrestigeRank.ToString();
            response.IsPublic = religion.IsPublic;
            response.Description = religion.Description;
            response.IsFounder = religion.FounderUID == fromPlayer.PlayerUID;

            // Build member list with player names and favor ranks
            foreach (var memberUID in religion.MemberUIDs)
            {
                var memberPlayerData = _playerReligionDataManager!.GetOrCreatePlayerData(memberUID);
                var memberPlayer = _sapi!.World.PlayerByUid(memberUID);
                var memberName = memberPlayer?.PlayerName ?? memberUID;

                response.Members.Add(new PlayerReligionInfoResponsePacket.MemberInfo
                {
                    PlayerUID = memberUID,
                    PlayerName = memberName,
                    FavorRank = memberPlayerData.FavorRank.ToString(),
                    Favor = memberPlayerData.Favor,
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

            // Include pending religion invites for the player
            var invites = _religionManager!.GetPlayerInvitations(fromPlayer.PlayerUID);
            _sapi!.Logger.Debug(
                $"[PantheonWars] Player {fromPlayer.PlayerName} ({fromPlayer.PlayerUID}) has {invites.Count} pending invitations");

            foreach (var inv in invites)
            {
                var rel = _religionManager.GetReligion(inv.ReligionId);
                response.PendingInvites.Add(new PlayerReligionInfoResponsePacket.ReligionInviteInfo
                {
                    InviteId = inv.InviteId,
                    ReligionId = inv.ReligionId,
                    ReligionName = rel?.ReligionName ?? inv.ReligionId,
                    ExpiresAt = inv.ExpiresDate
                });
                _sapi!.Logger.Debug(
                    $"[PantheonWars] - Invitation to {rel?.ReligionName ?? inv.ReligionId}, expires {inv.ExpiresDate}");
            }
        }

        _serverChannel!.SendPacket(response, fromPlayer);
        _sapi!.Logger.Debug(
            $"[PantheonWars] Sent PlayerReligionInfoResponse to {fromPlayer.PlayerName}: HasReligion={response.HasReligion}, PendingInvites={response.PendingInvites.Count}");
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

                case "accept":
                    // Accept religion invite; TargetPlayerUID carries InviteId in this action
                    success = _religionManager!.AcceptInvite(packet.TargetPlayerUID, fromPlayer.PlayerUID);
                    message = success
                        ? "You have joined the religion!"
                        : "Failed to accept invitation. It may have expired or you already have a religion.";
                    if (success)
                    {
                        SendPlayerDataToClient(fromPlayer);
                        var statePacket = new ReligionStateChangedPacket
                        {
                            Reason = "You joined a religion",
                            HasReligion = true
                        };
                        _serverChannel!.SendPacket(statePacket, fromPlayer);
                    }

                    break;

                case "decline":
                    // Decline religion invite; TargetPlayerUID carries InviteId in this action
                    success = _religionManager!.DeclineInvite(packet.TargetPlayerUID, fromPlayer.PlayerUID);
                    message = success ? "Invitation declined." : "Failed to decline invitation.";
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
                            var reason = "No reason provided";
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
                                    Reason =
                                        $"You have been banned from {religionForBan.ReligionName}. Reason: {reason}",
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
                        // Convert player name to UID (UI sends player name in TargetPlayerUID field)
                        var targetPlayer = _sapi!.World.AllOnlinePlayers
                            .FirstOrDefault(p => p.PlayerName.Equals(packet.TargetPlayerUID,
                                StringComparison.OrdinalIgnoreCase)) as IServerPlayer;

                        if (targetPlayer == null)
                        {
                            message = $"Player '{packet.TargetPlayerUID}' not found online.";
                            success = false;
                        }
                        else
                        {
                            success = _religionManager.InvitePlayer(religionForInvite.ReligionUID, targetPlayer.PlayerUID,
                                fromPlayer.PlayerUID);

                            if (success)
                            {
                                // Notify invited player
                                var statePacket = new ReligionStateChangedPacket
                                {
                                    Reason = $"You have been invited to join {religionForInvite.ReligionName}",
                                    HasReligion = false
                                };
                                _serverChannel!.SendPacket(statePacket, targetPlayer);
                                _sapi.Logger.Debug(
                                    $"[PantheonWars] Sent invitation notification to {targetPlayer.PlayerName} ({targetPlayer.PlayerUID})");

                                message = "Invitation sent!";
                            }
                            else
                            {
                                message = "Failed to send invitation. They may already have a pending invite.";
                            }
                        }
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
            else if (!Enum.TryParse<DeityType>(packet.Deity, out var deity) || deity == DeityType.None)
            {
                message = "Invalid deity selected.";
            }
            else
            {
                // Create the religion
                var newReligion = _religionManager.CreateReligion(
                    packet.ReligionName,
                    deity,
                    fromPlayer.PlayerUID,
                    packet.IsPublic
                );

                // Auto-join the founder
                _playerReligionDataManager!.JoinReligion(fromPlayer.PlayerUID, newReligion.ReligionUID);

                religionUID = newReligion.ReligionUID;
                message = $"Successfully created {packet.ReligionName}!";
                success = true;

                // Refresh player's HUD
                SendPlayerDataToClient(fromPlayer);
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

    private void OnBlessingUnlockRequest(IServerPlayer fromPlayer, BlessingUnlockRequestPacket packet)
    {
        string message;
        var success = false;

        try
        {
            var blessing = _blessingRegistry!.GetBlessing(packet.BlessingId);
            if (blessing == null)
            {
                message = $"Blessing '{packet.BlessingId}' not found.";
            }
            else
            {
                var playerData = _playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                var religion = playerData.ReligionUID != null
                    ? _religionManager!.GetReligion(playerData.ReligionUID)
                    : null;

                var (canUnlock, reason) = _blessingRegistry.CanUnlockBlessing(playerData, religion, blessing);
                if (!canUnlock)
                {
                    message = reason;
                }
                else
                {
                    // Unlock the blessing
                    if (blessing.Kind == BlessingKind.Player)
                    {
                        if (religion == null)
                        {
                            message = "You must be in a religion to unlock player blessings.";
                        }
                        else
                        {
                            success = _playerReligionDataManager.UnlockPlayerBlessing(fromPlayer.PlayerUID,
                                packet.BlessingId);
                            if (success)
                            {
                                _blessingEffectSystem!.RefreshPlayerBlessings(fromPlayer.PlayerUID);
                                message = $"Successfully unlocked {blessing.Name}!";

                                // Send updated player data to client
                                SendPlayerDataToClient(fromPlayer);
                            }
                            else
                            {
                                message = "Failed to unlock blessing. Please try again.";
                            }
                        }
                    }
                    else // Religion blessing
                    {
                        if (religion == null)
                        {
                            message = "You must be in a religion to unlock religion blessings.";
                        }
                        else if (!religion.IsFounder(fromPlayer.PlayerUID))
                        {
                            message = "Only the religion founder can unlock religion blessings.";
                        }
                        else
                        {
                            religion.UnlockedBlessings[packet.BlessingId] = true;
                            _blessingEffectSystem!.RefreshReligionBlessings(religion.ReligionUID);
                            message = $"Successfully unlocked {blessing.Name} for all religion members!";
                            success = true;

                            // Notify all members
                            foreach (var memberUid in religion.MemberUIDs)
                            {
                                var member = _sapi!.World.PlayerByUid(memberUid) as IServerPlayer;
                                if (member != null)
                                {
                                    // Send updated data to each member
                                    SendPlayerDataToClient(member);

                                    member.SendMessage(
                                        GlobalConstants.GeneralChatGroup,
                                        $"{blessing.Name} has been unlocked!",
                                        EnumChatType.Notification
                                    );
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            message = $"Error unlocking blessing: {ex.Message}";
            _sapi!.Logger.Error($"[PantheonWars] Blessing unlock error: {ex}");
        }

        var response = new BlessingUnlockResponsePacket(success, message, packet.BlessingId);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle blessing data request from client
    /// </summary>
    private void OnBlessingDataRequest(IServerPlayer fromPlayer, BlessingDataRequestPacket packet)
    {
        _sapi!.Logger.Debug($"[PantheonWars] Blessing data requested by {fromPlayer.PlayerName}");

        var response = new BlessingDataResponsePacket();

        try
        {
            var playerData = _playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
            var religion = playerData.ReligionUID != null
                ? _religionManager!.GetReligion(playerData.ReligionUID)
                : null;

            if (religion == null || playerData.ActiveDeity == DeityType.None)
            {
                response.HasReligion = false;
                _serverChannel!.SendPacket(response, fromPlayer);
                return;
            }

            response.HasReligion = true;
            response.ReligionUID = religion.ReligionUID;
            response.ReligionName = religion.ReligionName;
            response.Deity = playerData.ActiveDeity.ToString();
            response.FavorRank = (int)playerData.FavorRank;
            response.PrestigeRank = (int)religion.PrestigeRank;
            response.CurrentFavor = playerData.Favor;
            response.CurrentPrestige = religion.Prestige;
            response.TotalFavorEarned = playerData.TotalFavorEarned;

            // Get player blessings for this deity
            var playerBlessings = _blessingRegistry!.GetBlessingsForDeity(playerData.ActiveDeity, BlessingKind.Player);
            response.PlayerBlessings = playerBlessings.Select(p => new BlessingDataResponsePacket.BlessingInfo
            {
                BlessingId = p.BlessingId,
                Name = p.Name,
                Description = p.Description,
                RequiredFavorRank = p.RequiredFavorRank,
                RequiredPrestigeRank = p.RequiredPrestigeRank,
                PrerequisiteBlessings = p.PrerequisiteBlessings ?? new List<string>(),
                Category = (int)p.Category,
                StatModifiers = p.StatModifiers ?? new Dictionary<string, float>()
            }).ToList();

            // Get religion blessings for this deity
            var religionBlessings =
                _blessingRegistry.GetBlessingsForDeity(playerData.ActiveDeity, BlessingKind.Religion);
            response.ReligionBlessings = religionBlessings.Select(p => new BlessingDataResponsePacket.BlessingInfo
            {
                BlessingId = p.BlessingId,
                Name = p.Name,
                Description = p.Description,
                RequiredFavorRank = p.RequiredPrestigeRank,
                RequiredPrestigeRank = p.RequiredPrestigeRank,
                PrerequisiteBlessings = p.PrerequisiteBlessings ?? new List<string>(),
                Category = (int)p.Category,
                StatModifiers = p.StatModifiers ?? new Dictionary<string, float>()
            }).ToList();

            // Get unlocked player blessings
            response.UnlockedPlayerBlessings = playerData.UnlockedBlessings
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            // Get unlocked religion blessings
            response.UnlockedReligionBlessings = religion.UnlockedBlessings
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            _sapi.Logger.Debug(
                $"[PantheonWars] Sending blessing data: {response.PlayerBlessings.Count} player, {response.ReligionBlessings.Count} religion");
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[PantheonWars] Error loading blessing data: {ex}");
            response.HasReligion = false;
        }

        _serverChannel!.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle civilization list request from client
    /// </summary>
    private void OnCivilizationListRequest(IServerPlayer fromPlayer, CivilizationListRequestPacket packet)
    {
        _sapi!.Logger.Debug(
            $"[PantheonWars] Civilization list requested by {fromPlayer.PlayerName}, filter: '{packet.FilterDeity}'");

        var civilizations = _civilizationManager!.GetAllCivilizations().ToList();
        var civInfoList = new List<CivilizationListResponsePacket.CivilizationInfo>();

        foreach (var civ in civilizations)
        {
            var religions = _civilizationManager.GetCivReligions(civ.CivId);
            var deities = religions.Select(r => r.Deity.ToString()).Distinct().ToList();
            var religionNames = religions.Select(r => r.ReligionName).ToList();

            // Apply deity filter if specified
            if (!string.IsNullOrEmpty(packet.FilterDeity))
            {
                // Check if any religion in this civilization has the filtered deity
                var hasFilteredDeity = religions.Any(r =>
                    string.Equals(r.Deity.ToString(), packet.FilterDeity, StringComparison.OrdinalIgnoreCase));

                if (!hasFilteredDeity) continue; // Skip this civilization if it doesn't have the filtered deity
            }

            civInfoList.Add(new CivilizationListResponsePacket.CivilizationInfo
            {
                CivId = civ.CivId,
                Name = civ.Name,
                FounderUID = civ.FounderUID,
                FounderReligionUID = civ.FounderReligionUID,
                MemberCount = civ.MemberReligionIds.Count,
                MemberDeities = deities,
                MemberReligionNames = religionNames
            });
        }

        var response = new CivilizationListResponsePacket(civInfoList);
        _serverChannel!.SendPacket(response, fromPlayer);
        _sapi!.Logger.Debug(
            $"[PantheonWars] Sent {civInfoList.Count} civilizations (out of {civilizations.Count} total) with filter '{packet.FilterDeity}'");
    }

    /// <summary>
    ///     Handle civilization info request from client
    ///     If civId is empty, returns the civilization for the player's current religion
    /// </summary>
    private void OnCivilizationInfoRequest(IServerPlayer fromPlayer, CivilizationInfoRequestPacket packet)
    {
        _sapi!.Logger.Debug(
            $"[PantheonWars] Civilization info requested by {fromPlayer.PlayerName} for {packet.CivId}");

        var civId = packet.CivId;

        // If civId is empty, look up the player's religion's civilization
        if (string.IsNullOrEmpty(civId))
        {
            var playerReligion = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
            if (playerReligion == null)
            {
                _serverChannel!.SendPacket(new CivilizationInfoResponsePacket(), fromPlayer);
                return;
            }

            var religionCiv = _civilizationManager!.GetCivilizationByReligion(playerReligion.ReligionUID);
            if (religionCiv == null)
            {
                // Player's religion is not in any civilization.
                // In this case, still return an invites list so the client can render the "Invites" tab.
                var detailsForInvitesOnly = new CivilizationInfoResponsePacket.CivilizationDetails
                {
                    CivId = string.Empty, // signals "no civilization" to the client
                    Name = string.Empty,
                    FounderUID = string.Empty,
                    FounderReligionUID = string.Empty,
                    CreatedDate = default,
                    MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>(),
                    PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>()
                };

                var invitesForReligion = _civilizationManager.GetInvitesForReligion(playerReligion.ReligionUID);
                foreach (var invite in invitesForReligion)
                {
                    var invitingCiv = _civilizationManager.GetCivilization(invite.CivId);
                    var civName = invitingCiv?.Name ?? invite.CivId;
                    detailsForInvitesOnly.PendingInvites.Add(new CivilizationInfoResponsePacket.PendingInvite
                    {
                        InviteId = invite.InviteId,
                        ReligionId = invite.ReligionId,
                        // For invites targeted at this religion, expose the inviting civilization name
                        ReligionName = civName,
                        ExpiresAt = invite.ExpiresDate
                    });
                }

                _serverChannel!.SendPacket(new CivilizationInfoResponsePacket(detailsForInvitesOnly), fromPlayer);
                return;
            }

            civId = religionCiv.CivId;
        }

        var civ = _civilizationManager!.GetCivilization(civId);
        if (civ == null)
        {
            _serverChannel!.SendPacket(new CivilizationInfoResponsePacket(), fromPlayer);
            return;
        }

        // Get civilization founder's player name
        var founderPlayer = _sapi.World.PlayerByUid(civ.FounderUID);
        var founderPlayerName = founderPlayer?.PlayerName ?? civ.FounderUID;

        // Get founding religion name
        var founderReligion = _religionManager!.GetReligion(civ.FounderReligionUID);
        var founderReligionName = founderReligion?.ReligionName ?? "Unknown";

        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = civ.CivId,
            Name = civ.Name,
            FounderUID = civ.FounderUID,
            FounderName = founderPlayerName,
            FounderReligionUID = civ.FounderReligionUID,
            FounderReligionName = founderReligionName,
            CreatedDate = civ.CreatedDate,
            MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>(),
            PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>()
        };

        // Get member religion details
        var religions = _civilizationManager.GetCivReligions(civ.CivId);
        foreach (var religion in religions)
        {
            // Get religion founder's player name
            var religionFounderPlayer = _sapi.World.PlayerByUid(religion.FounderUID);
            var religionFounderName = religionFounderPlayer?.PlayerName ?? religion.FounderUID;

            details.MemberReligions.Add(new CivilizationInfoResponsePacket.MemberReligion
            {
                ReligionId = religion.ReligionUID,
                ReligionName = religion.ReligionName,
                Deity = religion.Deity.ToString(),
                FounderReligionUID = civ.FounderReligionUID,
                FounderUID = religion.FounderUID,
                FounderName = religionFounderName,
                MemberCount = religion.MemberUIDs.Count
            });
        }

        // Get pending invites (only show to founder)
        if (civ.FounderUID == fromPlayer.PlayerUID)
        {
            var invites = _civilizationManager.GetInvitesForCiv(civ.CivId);
            foreach (var invite in invites)
            {
                var targetReligion = _religionManager!.GetReligion(invite.ReligionId);
                if (targetReligion != null)
                    details.PendingInvites.Add(new CivilizationInfoResponsePacket.PendingInvite
                    {
                        InviteId = invite.InviteId,
                        ReligionId = invite.ReligionId,
                        ReligionName = targetReligion.ReligionName,
                        ExpiresAt = invite.ExpiresDate
                    });
            }
        }

        var response = new CivilizationInfoResponsePacket(details);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle civilization action request from client
    /// </summary>
    private void OnCivilizationActionRequest(IServerPlayer fromPlayer, CivilizationActionRequestPacket packet)
    {
        _sapi!.Logger.Debug(
            $"[PantheonWars] Civilization action '{packet.Action}' requested by {fromPlayer.PlayerName}");

        var response = new CivilizationActionResponsePacket();
        response.Action = packet.Action;

        try
        {
            switch (packet.Action.ToLower())
            {
                case "create":
                    var playerData = _playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                    if (string.IsNullOrEmpty(playerData.ReligionUID))
                    {
                        response.Success = false;
                        response.Message = "You must be in a religion to create a civilization.";
                        break;
                    }

                    var newCiv = _civilizationManager!.CreateCivilization(packet.Name, fromPlayer.PlayerUID,
                        playerData.ReligionUID);
                    if (newCiv != null)
                    {
                        response.Success = true;
                        response.Message = $"Civilization '{newCiv.Name}' created successfully!";
                        response.CivId = newCiv.CivId;
                    }
                    else
                    {
                        response.Success = false;
                        response.Message =
                            "Failed to create civilization. Check name requirements and cooldown status.";
                    }

                    break;

                case "invite":
                    // Look up religion by name (packet.TargetId contains religion name from UI)
                    var targetReligion = _religionManager!.GetReligionByName(packet.TargetId);
                    if (targetReligion == null)
                    {
                        response.Success = false;
                        response.Message = $"Religion '{packet.TargetId}' not found.";
                        break;
                    }

                    var success = _civilizationManager!.InviteReligion(packet.CivId, targetReligion.ReligionUID,
                        fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? $"Invitation sent to '{targetReligion.ReligionName}' successfully!"
                        : "Failed to send invitation. Check permissions and civilization requirements.";

                    // Notify all members of invited religion if invitation succeeded
                    if (success)
                    {
                        var civ = _civilizationManager!.GetCivilization(packet.CivId);
                        if (civ != null)
                        {
                            int notifiedCount = 0;
                            int offlineCount = 0;

                            foreach (var memberUID in targetReligion.MemberUIDs)
                            {
                                var memberPlayer = _sapi!.World.PlayerByUid(memberUID) as IServerPlayer;
                                if (memberPlayer != null)
                                {
                                    var statePacket = new ReligionStateChangedPacket
                                    {
                                        Reason = $"Your religion has been invited to join the civilization '{civ.Name}'",
                                        HasReligion = true
                                    };
                                    _serverChannel!.SendPacket(statePacket, memberPlayer);
                                    notifiedCount++;
                                }
                                else
                                {
                                    offlineCount++;
                                }
                            }

                            _sapi!.Logger.Notification(
                                $"[PantheonWars] Civilization invitation sent to {targetReligion.ReligionName}: " +
                                $"{notifiedCount} members notified, {offlineCount} offline");
                        }
                    }

                    response.CivId = packet.CivId;
                    break;

                case "accept":
                    success = _civilizationManager!.AcceptInvite(packet.TargetId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "You have joined the civilization!"
                        : "Failed to accept invitation. It may have expired or the civilization is full.";
                    break;

                case "leave":
                    playerData = _playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                    if (string.IsNullOrEmpty(playerData.ReligionUID))
                    {
                        response.Success = false;
                        response.Message = "You are not in a religion.";
                        break;
                    }

                    // Get religion to check if player is the founder
                    var playerReligion = _religionManager!.GetReligion(playerData.ReligionUID);
                    if (playerReligion == null)
                    {
                        response.Success = false;
                        response.Message = "Your religion was not found.";
                        break;
                    }

                    // Check if player is the religion founder (only founders can leave)
                    if (playerReligion.FounderUID != fromPlayer.PlayerUID)
                    {
                        response.Success = false;
                        response.Message = "Only religion founders can leave a civilization.";
                        break;
                    }

                    // Check if player is the civilization founder
                    var playerCiv = _civilizationManager!.GetCivilizationByReligion(playerData.ReligionUID);
                    if (playerCiv != null && playerCiv.FounderUID == fromPlayer.PlayerUID)
                    {
                        response.Success = false;
                        response.Message = "Civilization founders must disband instead of leaving.";
                        break;
                    }

                    success = _civilizationManager!.LeaveReligion(playerData.ReligionUID, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "You have left the civilization. A 7-day cooldown has been applied."
                        : "Failed to leave civilization.";
                    break;

                case "kick":
                    success = _civilizationManager!.KickReligion(packet.CivId, packet.TargetId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "Religion kicked from civilization. A 7-day cooldown has been applied."
                        : "Failed to kick religion. Only the civilization founder can kick members.";
                    response.CivId = packet.CivId;
                    break;

                case "disband":
                    success = _civilizationManager!.DisbandCivilization(packet.CivId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "Civilization disbanded successfully."
                        : "Failed to disband civilization. Only the founder can disband.";
                    response.CivId = packet.CivId;
                    break;

                default:
                    response.Success = false;
                    response.Message = $"Unknown action: {packet.Action}";
                    break;
            }
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[PantheonWars] Error handling civilization action '{packet.Action}': {ex}");
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
        }

        _serverChannel!.SendPacket(response, fromPlayer);
    }

    private void OnPlayerJoin(IServerPlayer player)
    {
        // Send initial player data to client
        SendPlayerDataToClient(player);
    }

    /// <summary>
    ///     Handle player data changes (favor, rank, etc.) and notify client
    /// </summary>
    private void OnPlayerDataChanged(string playerUID)
    {
        var player = _sapi!.World.PlayerByUid(playerUID) as IServerPlayer;
        if (player != null) SendPlayerDataToClient(player);
    }

    private void SendPlayerDataToClient(IServerPlayer player)
    {
        if (_playerReligionDataManager == null || _religionManager == null || _deityRegistry == null ||
            _serverChannel == null) return;

        var playerReligionData = _playerReligionDataManager!.GetOrCreatePlayerData(player.PlayerUID);
        var religionData = _religionManager!.GetPlayerReligion(player.PlayerUID);
        var deity = _deityRegistry.GetDeity(playerReligionData.ActiveDeity);
        var deityName = deity?.Name ?? "None";

        if (religionData != null)
        {
            var packet = new PlayerReligionDataPacket(
                religionData.ReligionName,
                deityName,
                playerReligionData.Favor,
                playerReligionData.FavorRank.ToString(),
                religionData.Prestige,
                religionData.PrestigeRank.ToString(),
                playerReligionData.TotalFavorEarned
            );

            _serverChannel.SendPacket(packet, player);
        }
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
        _clientChannel.SetMessageHandler<BlessingUnlockResponsePacket>(OnBlessingUnlockResponse);
        _clientChannel.SetMessageHandler<BlessingDataResponsePacket>(OnBlessingDataResponse);
        _clientChannel.SetMessageHandler<ReligionStateChangedPacket>(OnReligionStateChanged);
        _clientChannel.SetMessageHandler<CivilizationListResponsePacket>(OnCivilizationListResponse);
        _clientChannel.SetMessageHandler<CivilizationInfoResponsePacket>(OnCivilizationInfoResponse);
        _clientChannel.SetMessageHandler<CivilizationActionResponsePacket>(OnCivilizationActionResponse);
        _clientChannel.RegisterMessageType(typeof(PlayerReligionDataPacket));
    }

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

            // Play error sound
            _capi?.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.3f);
        }
    }

    private void OnEditDescriptionResponse(EditDescriptionResponsePacket packet)
    {
        if (packet.Success)
            _capi?.ShowChatMessage(packet.Message);
        else
            _capi?.ShowChatMessage($"Error: {packet.Message}");
    }

    private void OnBlessingUnlockResponse(BlessingUnlockResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);
            _capi?.Logger.Notification($"[PantheonWars] Blessing unlocked: {packet.BlessingId}");

            // Trigger blessing unlock event for UI refresh
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");
            _capi?.Logger.Warning($"[PantheonWars] Failed to unlock blessing: {packet.Message}");

            // Trigger event even on failure so UI can update
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
    }

    private void OnBlessingDataResponse(BlessingDataResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Received blessing data: HasReligion={packet.HasReligion}");

        // Trigger event for BlessingDialog to consume
        BlessingDataReceived?.Invoke(packet);
    }

    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi?.Logger.Notification($"[PantheonWars] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi?.ShowChatMessage(packet.Reason);

        // Trigger event for BlessingDialog to refresh its data
        ReligionStateChanged?.Invoke(packet);
    }

    private void OnCivilizationListResponse(CivilizationListResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Received civilization list: {packet.Civilizations.Count} civilizations");
        CivilizationListReceived?.Invoke(packet);
    }

    private void OnCivilizationInfoResponse(CivilizationInfoResponsePacket packet)
    {
        _capi?.Logger.Debug("[PantheonWars] Received civilization info");
        CivilizationInfoReceived?.Invoke(packet);
    }

    private void OnCivilizationActionResponse(CivilizationActionResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Civilization action '{packet.Action}' response: {packet.Success}");

        // Show message to user
        if (!string.IsNullOrEmpty(packet.Message)) _capi?.ShowChatMessage(packet.Message);

        CivilizationActionCompleted?.Invoke(packet);
    }

    /// <summary>
    ///     Request blessing data from the server
    /// </summary>
    public void RequestBlessingData()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request blessing data: client channel not initialized");
            return;
        }

        var request = new BlessingDataRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent blessing data request to server");
    }

    /// <summary>
    ///     Send a blessing unlock request to the server
    /// </summary>
    public void RequestBlessingUnlock(string blessingId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot unlock blessing: client channel not initialized");
            return;
        }

        var request = new BlessingUnlockRequestPacket(blessingId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent unlock request for blessing: {blessingId}");
    }

    /// <summary>
    ///     Request religion list from the server
    /// </summary>
    public void RequestReligionList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion list: client channel not initialized");
            return;
        }

        var request = new ReligionListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent religion list request with filter: {deityFilter}");
    }

    /// <summary>
    ///     Send a religion action request to the server (join, leave, kick, invite)
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
    ///     Request to create a new religion
    /// </summary>
    public void RequestCreateReligion(string religionName, string deity, bool isPublic)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot create religion: client channel not initialized");
            return;
        }

        var request = new CreateReligionRequestPacket(religionName, deity, isPublic);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent create religion request: {religionName}, {deity}");
    }

    /// <summary>
    ///     Request player's religion info (for management overlay)
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
    ///     Request to edit religion description
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
    ///     Request list of all civilizations
    /// </summary>
    public void RequestCivilizationList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization list: client channel not initialized");
            return;
        }

        var request = new CivilizationListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization list request with filter: '{deityFilter}'");
    }

    /// <summary>
    ///     Request detailed information about a specific civilization
    /// </summary>
    public void RequestCivilizationInfo(string civId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization info: client channel not initialized");
            return;
        }

        var request = new CivilizationInfoRequestPacket(civId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization info request for {civId}");
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization action: client channel not initialized");
            return;
        }

        var request = new CivilizationActionRequestPacket(action, civId, targetId, name);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization action request: {action}");
    }

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

    #endregion
}