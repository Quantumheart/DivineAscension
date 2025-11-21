using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PantheonWars.Commands;
using PantheonWars.GUI;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Systems;
using PantheonWars.Systems.BuffSystem;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars;

[ExcludeFromCodeCoverage]
public class PantheonWarsSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "pantheonwars";
    private AbilityCommands? _abilityCommands;
    private AbilityRegistry? _abilityRegistry;
    private AbilitySystem? _abilitySystem;

    // Use interfaces for better testability and dependency injection
    private IBuffManager? _buffManager;

    // Client-side systems
    private ICoreClientAPI? _capi;
    private IClientNetworkChannel? _clientChannel;
    private DeityRegistry? _clientDeityRegistry;
    private AbilityCooldownManager? _cooldownManager;
    private CreateReligionDialog? _createReligionDialog;
    private DeityRegistry? _deityRegistry;
    private BlessingCommands? _blessingCommands;
    private BlessingEffectSystem? _blessingEffectSystem;
    private BlessingRegistry? _blessingRegistry;
    private PlayerDataManager? _playerDataManager;
    private PlayerReligionDataManager? _playerReligionDataManager;
    private ReligionCommands? _religionCommands;
    private ReligionManagementDialog? _religionDialog;
    private ReligionManager? _religionManager;
    private ReligionPrestigeManager? _religionPrestigeManager;

    // Server-side systems
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _serverChannel;

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
            .RegisterMessageType<BlessingUnlockRequestPacket>()
            .RegisterMessageType<BlessingUnlockResponsePacket>()
            .RegisterMessageType<BlessingDataRequestPacket>()
            .RegisterMessageType<BlessingDataResponsePacket>()
            .RegisterMessageType<ReligionStateChangedPacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;
        api.Logger.Notification("[PantheonWars] Initializing server-side systems...");

        // Register entity behaviors
        api.RegisterEntityBehaviorClass("PantheonWarsBuffTracker", typeof(EntityBehaviorBuffTracker));

        // Initialize deity registry (concrete implementation, stored as interface)
        _deityRegistry = new DeityRegistry(api);
        _deityRegistry.Initialize();

        // Initialize ability registry
        _abilityRegistry = new AbilityRegistry(api);
        _abilityRegistry.Initialize();

        // Initialize player data manager
        _playerDataManager = new PlayerDataManager(api);
        _playerDataManager.Initialize();

        // Initialize ability cooldown manager
        _cooldownManager = new AbilityCooldownManager(api);
        _cooldownManager.Initialize();

        // Initialize buff manager (concrete implementation, stored as interface)
        _buffManager = new BuffManager(api);

        // Initialize religion systems
        _religionManager = new ReligionManager(api);
        _religionManager.Initialize();

        _playerReligionDataManager = new PlayerReligionDataManager(api, _religionManager);
        _playerReligionDataManager.Initialize();
        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;

        // Initialize ability system (pass buff manager interface)
        _abilitySystem = new AbilitySystem(api, _abilityRegistry, _playerDataManager, _cooldownManager, _buffManager);
        _abilitySystem.Initialize();

        // Initialize religion prestige manager
        _religionPrestigeManager = new ReligionPrestigeManager(api, _religionManager);
        _religionPrestigeManager.Initialize();

        // Initialize blessing systems (Phase 3.3)
        _blessingRegistry = new BlessingRegistry(api);
        _blessingRegistry.Initialize();

        _blessingEffectSystem = new BlessingEffectSystem(api, _blessingRegistry, _playerReligionDataManager, _religionManager);
        _blessingEffectSystem.Initialize();

        // Connect blessing systems to religion prestige manager
        _religionPrestigeManager.SetBlessingSystems(_blessingRegistry, _blessingEffectSystem);

        // Register commands
        _blessingCommands = new BlessingCommands(api, _blessingRegistry, _playerReligionDataManager, _religionManager,
            _blessingEffectSystem);
        _blessingCommands.RegisterCommands();

        // Setup network channel and handlers
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _serverChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerMessageReceived);
        SetupServerNetworking(api);
        _religionCommands = new ReligionCommands(api, _religionManager, _playerReligionDataManager, _serverChannel);
        _religionCommands.RegisterCommands();
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

        // Register handlers for blessing system packets
        _serverChannel.SetMessageHandler<BlessingUnlockRequestPacket>(OnBlessingUnlockRequest);
        _serverChannel.SetMessageHandler<BlessingDataRequestPacket>(OnBlessingDataRequest);
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

            // Build member list with player names
            foreach (var memberUID in religion.MemberUIDs)
            {
                var memberPlayer = _sapi!.World.PlayerByUid(memberUID);
                var memberName = memberPlayer?.PlayerName ?? memberUID;

                response.Members.Add(new PlayerReligionInfoResponsePacket.MemberInfo
                {
                    PlayerUID = memberUID,
                    PlayerName = memberName,
                    FavorRank = "N/A",
                    Favor = 0,
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
                    // Religion blessing only in new system
                    if (religion == null)
                    {
                        message = "You must be in a religion to unlock blessings.";
                    }
                    else if (!religion.IsFounder(fromPlayer.PlayerUID))
                    {
                        message = "Only the religion founder can unlock blessings.";
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
            response.FavorRank = 0; // No longer used in religion-only system
            response.PrestigeRank = (int)religion.PrestigeRank;
            response.CurrentFavor = 0; // No longer used in religion-only system
            response.CurrentPrestige = religion.Prestige;
            response.TotalFavorEarned = 0; // No longer used in religion-only system

            // In religion-only system, there are no player blessings
            response.PlayerBlessings = new List<BlessingDataResponsePacket.BlessingInfo>();

            // Get religion blessings for this deity
            var religionBlessings = _blessingRegistry!.GetBlessingsForDeity(playerData.ActiveDeity);
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

            // In religion-only system, there are no player blessings
            response.UnlockedPlayerBlessings = new List<string>();

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
        if (player != null)
        {
            SendPlayerDataToClient(player);
        }
    }

    private void SendPlayerDataToClient(IServerPlayer player)
    {
        if (_playerDataManager == null || _deityRegistry == null || _serverChannel == null) return;

        var playerReligionData = _playerReligionDataManager!.GetOrCreatePlayerData(player.PlayerUID);
        var religionData = _religionManager!.GetPlayerReligion(player.PlayerUID);
        var deity = _deityRegistry.GetDeity(playerReligionData.ActiveDeity);
        var deityName = deity?.Name ?? "None";

        if (religionData != null)
        {
            var packet = new PlayerReligionDataPacket(
                religionData.ReligionName,
                deityName,
                0, // Favor no longer used in religion-only system
                "N/A", // FavorRank no longer used
                religionData.Prestige,
                religionData.PrestigeRank.ToString(),
                0 // TotalFavorEarned no longer used
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
        _clientChannel.RegisterMessageType(typeof(PlayerReligionDataPacket));
    }

    private void OnServerPlayerDataUpdate(PlayerReligionDataPacket packet)
    {
        // Trigger event for BlessingDialog and other UI components
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

            // Request fresh blessing data (now in a religion)
            // Use a small delay to ensure server has processed the religion creation
            _capi?.Event.RegisterCallback((dt) =>
            {
                var request = new BlessingDataRequestPacket();
                _clientChannel?.SendPacket(request);
            }, 100);
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

    /// <summary>
    /// Request blessing data from the server
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
    /// Send a blessing unlock request to the server
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
    /// Request religion list from the server
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
    /// Event fired when blessing data is received from the server
    /// </summary>
    public event Action<BlessingDataResponsePacket>? BlessingDataReceived;

    /// <summary>
    /// Event fired when a blessing unlock response is received from the server
    /// Parameters: (blessingId, success)
    /// </summary>
    public event Action<string, bool>? BlessingUnlocked;

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