using System;
using System.Linq;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Networking.Server;

/// <summary>
///     Handles all religion-related network requests from clients
/// </summary>
public class ReligionNetworkHandler : IServerNetworkHandler
{
    private readonly ICoreServerAPI _sapi;
    private readonly IReligionManager _religionManager;
    private readonly IPlayerReligionDataManager _playerReligionDataManager;
    private readonly IServerNetworkChannel _serverChannel;

    /// <summary>
    ///     Constructor for dependency injection
    /// </summary>
    public ReligionNetworkHandler(
        ICoreServerAPI sapi,
        IReligionManager religionManager,
        IPlayerReligionDataManager playerReligionDataManager,
        IServerNetworkChannel channel)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _playerReligionDataManager = playerReligionDataManager;
        _serverChannel = channel;
    }

    public void RegisterHandlers()
    {

        // Register handlers for religion dialog packets
        _serverChannel!.SetMessageHandler<ReligionListRequestPacket>(OnReligionListRequest);
        _serverChannel.SetMessageHandler<PlayerReligionInfoRequestPacket>(OnPlayerReligionInfoRequest);
        _serverChannel.SetMessageHandler<ReligionActionRequestPacket>(OnReligionActionRequest);
        _serverChannel.SetMessageHandler<CreateReligionRequestPacket>(OnCreateReligionRequest);
        _serverChannel.SetMessageHandler<EditDescriptionRequestPacket>(OnEditDescriptionRequest);
    }

    public void Dispose()
    {
        // No cleanup needed for this handler
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
                        _playerReligionDataManager
                            .NotifyPlayerDataChanged(fromPlayer.PlayerUID); // Refresh player's HUD
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
                        _playerReligionDataManager!.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
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
                        _playerReligionDataManager
                            .NotifyPlayerDataChanged(fromPlayer.PlayerUID); // Refresh player's HUD

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
                                _playerReligionDataManager!
                                    .NotifyPlayerDataChanged(kickedPlayer.PlayerUID); // Refresh kicked player's HUD

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
                                _playerReligionDataManager!.NotifyPlayerDataChanged(bannedPlayer.PlayerUID);

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
                            success = _religionManager.InvitePlayer(religionForInvite.ReligionUID,
                                targetPlayer.PlayerUID,
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
                _playerReligionDataManager!.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
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
}