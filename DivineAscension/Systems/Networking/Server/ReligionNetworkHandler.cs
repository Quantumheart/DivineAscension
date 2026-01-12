using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles all religion-related network requests from clients
/// </summary>
public class ReligionNetworkHandler : IServerNetworkHandler
{
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly IRoleManager _roleManager;
    private readonly ICoreServerAPI _sapi;
    private readonly IServerNetworkChannel _serverChannel;

    /// <summary>
    ///     Constructor for dependency injection
    /// </summary>
    public ReligionNetworkHandler(
        ICoreServerAPI sapi,
        IReligionManager religionManager,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IRoleManager roleManager,
        IServerNetworkChannel channel)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _playerProgressionDataManager = playerProgressionDataManager;
        _roleManager = roleManager;
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
        _serverChannel.SetMessageHandler<ReligionDetailRequestPacket>(OnReligionDetailRequest);
        _serverChannel.SetMessageHandler<SetDeityNameRequestPacket>(OnSetDeityNameRequest);

        // Register handlers for role management packets
        _serverChannel.SetMessageHandler<ReligionRolesRequest>(OnReligionRolesRequest);
        _serverChannel.SetMessageHandler<CreateRoleRequest>(OnCreateRoleRequest);
        _serverChannel.SetMessageHandler<ModifyRolePermissionsRequest>(OnModifyRolePermissionsRequest);
        _serverChannel.SetMessageHandler<AssignRoleRequest>(OnAssignRoleRequest);
        _serverChannel.SetMessageHandler<DeleteRoleRequest>(OnDeleteRoleRequest);
        _serverChannel.SetMessageHandler<TransferFounderRequest>(OnTransferFounderRequest);
    }

    public void Dispose()
    {
        // No cleanup needed for this handler
    }

    private void OnReligionListRequest(IServerPlayer fromPlayer, ReligionListRequestPacket packet)
    {
        var religions = string.IsNullOrEmpty(packet.FilterDeity)
            ? _religionManager!.GetAllReligions()
            : _religionManager!.GetReligionsByDomain(
                Enum.TryParse<DeityDomain>(packet.FilterDeity, out var deity) ? deity : DeityDomain.None);

        var religionInfoList = religions.Select(r => new ReligionListResponsePacket.ReligionInfo
        {
            ReligionUID = r.ReligionUID,
            ReligionName = r.ReligionName,
            Domain = r.Domain.ToString(),
            DeityName = r.DeityName,
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
            response.Domain = religion.Domain.ToString();
            response.DeityName = religion.DeityName;
            response.FounderUID = religion.FounderUID;
            response.FounderName = religion.FounderName;
            response.Prestige = religion.Prestige;
            response.PrestigeRank = religion.PrestigeRank.ToString();
            response.IsPublic = religion.IsPublic;
            response.Description = religion.Description;
            response.IsFounder = religion.FounderUID == fromPlayer.PlayerUID;

            // Build member list with player names and favor ranks
            foreach (var member in religion.Members)
            {
                var memberPlayerData = _playerProgressionDataManager!.GetOrCreatePlayerData(member.Key);

                // Use cached name from Members dictionary
                var memberName = religion.GetMemberName(member.Key);

                // Get member's role name
                var roleName =
                    LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DEFAULT_ROLE); // Default fallback
                var roleId = string.Empty;
                if (religion.MemberRoles.TryGetValue(member.Key, out var roleUID))
                {
                    var role = religion.GetRole(roleUID);
                    if (role != null)
                    {
                        roleName = role.RoleName;
                        roleId = role.RoleUID;
                    }
                }

                response.Members.Add(new PlayerReligionInfoResponsePacket.MemberInfo
                {
                    PlayerUID = member.Key,
                    PlayerName = memberName,
                    FavorRank = memberPlayerData.FavorRank.ToString(),
                    Favor = memberPlayerData.Favor,
                    IsFounder = roleId == RoleDefaults.FOUNDER_ROLE_ID,
                    RoleName = roleName,
                    RoleId = roleId
                });
            }

            // Build banned players list (only for founder)
            if (response.IsFounder)
            {
                var bannedPlayers = _religionManager!.GetBannedPlayers(religion.ReligionUID);
                foreach (var banEntry in bannedPlayers)
                {
                    // Use cached player name from BanEntry
                    var bannedName = !string.IsNullOrEmpty(banEntry.PlayerName)
                        ? banEntry.PlayerName
                        : banEntry.PlayerUID;

                    response.BannedPlayers.Add(new PlayerReligionInfoResponsePacket.BanInfo
                    {
                        PlayerUID = banEntry.PlayerUID,
                        PlayerName = bannedName,
                        Reason = banEntry.Reason,
                        BannedAt = banEntry.BannedAt.ToString("yyyy-MM-dd HH:mm"),
                        ExpiresAt = banEntry.ExpiresAt?.ToString("yyyy-MM-dd HH:mm") ??
                                    LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_NEVER),
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
                $"[DivineAscension] Player {fromPlayer.PlayerName} ({fromPlayer.PlayerUID}) has {invites.Count} pending invitations");

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
                    $"[DivineAscension] - Invitation to {rel?.ReligionName ?? inv.ReligionId}, expires {inv.ExpiresDate}");
            }
        }

        _serverChannel!.SendPacket(response, fromPlayer);
        _sapi!.Logger.Debug(
            $"[DivineAscension] Sent PlayerReligionInfoResponse to {fromPlayer.PlayerName}: HasReligion={response.HasReligion}, PendingInvites={response.PendingInvites.Count}");
    }

    private void OnReligionDetailRequest(IServerPlayer fromPlayer, ReligionDetailRequestPacket packet)
    {
        var religion = _religionManager!.GetReligion(packet.ReligionUID);
        var response = new ReligionDetailResponsePacket();

        if (religion == null)
        {
            // Return empty response if religion doesn't exist
            _serverChannel!.SendPacket(response, fromPlayer);
            _sapi!.Logger.Warning(
                $"[DivineAscension] Religion detail request for non-existent religion: {packet.ReligionUID}");
            return;
        }

        // Build response with religion details
        response.ReligionUID = religion.ReligionUID;
        response.ReligionName = religion.ReligionName;
        response.Domain = religion.Domain.ToString();
        response.DeityName = religion.DeityName;
        response.Description = religion.Description;
        response.Prestige = religion.Prestige;
        response.PrestigeRank = religion.PrestigeRank.ToString();
        response.IsPublic = religion.IsPublic;
        response.FounderUID = religion.FounderUID;
        response.FounderName = religion.FounderName;

        // Build member list with player names and favor ranks
        foreach (var member in religion.Members)
        {
            var memberPlayerData = _playerProgressionDataManager!.GetOrCreatePlayerData(member.Key);

            // Use cached name from Members dictionary
            var memberName = religion.GetMemberName(member.Key);

            response.Members.Add(new ReligionDetailResponsePacket.MemberInfo
            {
                PlayerUID = member.Key,
                PlayerName = memberName,
                FavorRank = memberPlayerData.FavorRank.ToString(),
                Favor = memberPlayerData.Favor
            });
        }

        _serverChannel!.SendPacket(response, fromPlayer);
        _sapi!.Logger.Debug(
            $"[DivineAscension] Sent ReligionDetailResponse to {fromPlayer.PlayerName} for {religion.ReligionName} with {response.Members.Count} members");
    }

    private void OnReligionActionRequest(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        ReligionActionResult result;

        try
        {
            result = packet.Action.ToLower() switch
            {
                "join" => HandleJoinAction(fromPlayer, packet),
                "accept" => HandleAcceptAction(fromPlayer, packet),
                "decline" => HandleDeclineAction(fromPlayer, packet),
                "leave" => HandleLeaveAction(fromPlayer, packet),
                "kick" => HandleKickAction(fromPlayer, packet),
                "ban" => HandleBanAction(fromPlayer, packet),
                "unban" => HandleUnbanAction(fromPlayer, packet),
                "invite" => HandleInviteAction(fromPlayer, packet),
                "disband" => HandleDisbandAction(fromPlayer, packet),
                _ => new ReligionActionResult
                {
                    Success = false,
                    Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_UNKNOWN_ACTION,
                        packet.Action)
                }
            };
        }
        catch (Exception ex)
        {
            result = new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _sapi.Logger.Error($"[DivineAscension] Religion action error: {ex}");
        }

        var response = new ReligionActionResponsePacket(result.Success, result.Message, packet.Action);
        _serverChannel.SendPacket(response, fromPlayer);
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
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NAME_EMPTY);
            }
            else if (packet.ReligionName.Length < 3)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NAME_TOO_SHORT);
            }
            else if (packet.ReligionName.Length > 32)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NAME_TOO_LONG);
            }
            else if (ProfanityFilterService.Instance.ContainsProfanity(packet.ReligionName))
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NAME_PROFANITY);
            }
            else if (_religionManager!.GetReligionByName(packet.ReligionName) != null)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NAME_EXISTS);
            }
            else if (_religionManager.HasReligion(fromPlayer.PlayerUID))
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ALREADY_IN_RELIGION);
            }
            else if (!Enum.TryParse<DeityDomain>(packet.Domain, out var domain) || domain == DeityDomain.None)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVALID_DEITY);
            }
            else if (string.IsNullOrWhiteSpace(packet.DeityName))
            {
                message = "Deity name is required";
            }
            else if (packet.DeityName.Length < 2 || packet.DeityName.Length > 48)
            {
                message = "Deity name must be between 2 and 48 characters";
            }
            else
            {
                // Create the religion
                var newReligion = _religionManager.CreateReligion(
                    packet.ReligionName,
                    domain,
                    packet.DeityName,
                    fromPlayer.PlayerUID,
                    packet.IsPublic
                );

                // Set up founder's player religion data (already added to Members via constructor)
                _playerProgressionDataManager!.SetPlayerReligionData(fromPlayer.PlayerUID, newReligion.ReligionUID);

                religionUID = newReligion.ReligionUID;
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_CREATED, packet.ReligionName);
                success = true;

                // Refresh player's HUD
                _playerProgressionDataManager!.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
            }
        }
        catch (Exception ex)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_CREATE_ERROR, ex.Message);
            _sapi!.Logger.Error($"[DivineAscension] Religion creation error: {ex}");
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
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NOT_FOUND);
            }
            else if (religion.FounderUID != fromPlayer.PlayerUID)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ONLY_FOUNDER_EDIT);
            }
            else if (packet.Description.Length > 200)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DESC_TOO_LONG);
            }
            else if (ProfanityFilterService.Instance.ContainsProfanity(packet.Description))
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DESC_PROFANITY);
            }
            else
            {
                // Update description
                religion.Description = packet.Description;
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DESC_UPDATED);
                success = true;
            }
        }
        catch (Exception ex)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DESC_ERROR, ex.Message);
            _sapi!.Logger.Error($"[DivineAscension] Description edit error: {ex}");
        }

        var response = new EditDescriptionResponsePacket(success, message);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    private void OnSetDeityNameRequest(IServerPlayer fromPlayer, SetDeityNameRequestPacket packet)
    {
        try
        {
            var religion = _religionManager!.GetReligion(packet.ReligionUID);

            if (religion == null)
            {
                var response = new SetDeityNameResponsePacket(false, "Religion not found");
                _serverChannel!.SendPacket(response, fromPlayer);
                return;
            }

            if (religion.FounderUID != fromPlayer.PlayerUID)
            {
                var response = new SetDeityNameResponsePacket(false, "Only the founder can change the deity name");
                _serverChannel!.SendPacket(response, fromPlayer);
                return;
            }

            if (_religionManager.SetDeityName(packet.ReligionUID, packet.NewDeityName, out var error))
            {
                var response = new SetDeityNameResponsePacket(true, null, packet.NewDeityName);
                _serverChannel!.SendPacket(response, fromPlayer);

                // Notify all online members about the change
                foreach (var memberUID in religion.MemberUIDs)
                {
                    var memberPlayer = _sapi!.World.PlayerByUid(memberUID) as IServerPlayer;
                    if (memberPlayer != null && memberPlayer.PlayerUID != fromPlayer.PlayerUID)
                    {
                        _serverChannel.SendPacket(new ReligionStateChangedPacket
                        {
                            Reason = $"Deity name changed to {packet.NewDeityName}",
                            HasReligion = true
                        }, memberPlayer);
                    }
                }
            }
            else
            {
                var response = new SetDeityNameResponsePacket(false, error);
                _serverChannel!.SendPacket(response, fromPlayer);
            }
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Set deity name error: {ex}");
            var response = new SetDeityNameResponsePacket(false, "An error occurred while updating the deity name");
            _serverChannel!.SendPacket(response, fromPlayer);
        }
    }

    #region Role Management Handlers

    private void OnReligionRolesRequest(IServerPlayer fromPlayer, ReligionRolesRequest packet)
    {
        try
        {
            var religion = _religionManager!.GetReligion(packet.ReligionUID);

            if (religion == null)
            {
                var errorResponse = new ReligionRolesResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NOT_FOUND)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            // Check if player is a member
            if (!religion.IsMember(fromPlayer.PlayerUID))
            {
                var errorResponse = new ReligionRolesResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NOT_MEMBER)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            var response = new ReligionRolesResponse
            {
                Success = true,
                Roles = _roleManager.GetReligionRoles(packet.ReligionUID),
                MemberRoles = religion.MemberRoles,
                MemberNames = new Dictionary<string, string>(),
                ErrorMessage = null
            };

            // Populate member names from cached data
            foreach (var uid in religion.MemberUIDs)
            {
                response.MemberNames[uid] = religion.GetMemberName(uid);

                // Opportunistic update if online
                var player = _sapi!.World.PlayerByUid(uid);
                if (player != null)
                {
                    religion.UpdateMemberName(uid, player.PlayerName);
                    response.MemberNames[uid] = player.PlayerName;
                }
            }

            _serverChannel!.SendPacket(response, fromPlayer);
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Error handling ReligionRolesRequest: {ex}");
            var errorResponse = new ReligionRolesResponse
            {
                Success = false,
                ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _serverChannel!.SendPacket(errorResponse, fromPlayer);
        }
    }

    private void OnCreateRoleRequest(IServerPlayer fromPlayer, CreateRoleRequest packet)
    {
        try
        {
            if (string.IsNullOrEmpty(packet.ReligionUID) || string.IsNullOrEmpty(packet.RoleName))
            {
                var errorResponse = new CreateRoleResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVALID_REQUEST)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            var (success, role, error) = _roleManager.CreateCustomRole(
                packet.ReligionUID,
                fromPlayer.PlayerUID,
                packet.RoleName
            );

            var response = new CreateRoleResponse
            {
                Success = success,
                CreatedRole = role,
                ErrorMessage = error
            };

            _serverChannel!.SendPacket(response, fromPlayer);
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Error handling CreateRoleRequest: {ex}");
            var errorResponse = new CreateRoleResponse
            {
                Success = false,
                ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _serverChannel!.SendPacket(errorResponse, fromPlayer);
        }
    }

    private void OnModifyRolePermissionsRequest(IServerPlayer fromPlayer, ModifyRolePermissionsRequest packet)
    {
        try
        {
            if (string.IsNullOrEmpty(packet.ReligionUID) || string.IsNullOrEmpty(packet.RoleUID) ||
                packet.Permissions == null)
            {
                var errorResponse = new ModifyRolePermissionsResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVALID_REQUEST)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            var (success, role, error) = _roleManager.ModifyRolePermissions(
                packet.ReligionUID,
                fromPlayer.PlayerUID,
                packet.RoleUID,
                packet.Permissions
            );

            var response = new ModifyRolePermissionsResponse
            {
                Success = success,
                UpdatedRole = role,
                ErrorMessage = error
            };

            _serverChannel!.SendPacket(response, fromPlayer);
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Error handling ModifyRolePermissionsRequest: {ex}");
            var errorResponse = new ModifyRolePermissionsResponse
            {
                Success = false,
                ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _serverChannel!.SendPacket(errorResponse, fromPlayer);
        }
    }

    private void OnAssignRoleRequest(IServerPlayer fromPlayer, AssignRoleRequest packet)
    {
        try
        {
            if (string.IsNullOrEmpty(packet.ReligionUID) || string.IsNullOrEmpty(packet.TargetPlayerUID) ||
                string.IsNullOrEmpty(packet.RoleUID))
            {
                var errorResponse = new AssignRoleResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVALID_REQUEST)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            var (success, error) = _roleManager.AssignRole(
                packet.ReligionUID,
                fromPlayer.PlayerUID,
                packet.TargetPlayerUID,
                packet.RoleUID
            );

            var response = new AssignRoleResponse
            {
                Success = success,
                ErrorMessage = error
            };

            _serverChannel!.SendPacket(response, fromPlayer);

            // Notify the target player if they're online and broadcast roles update
            if (success)
            {
                var religion = _religionManager.GetReligion(packet.ReligionUID);
                if (religion != null)
                {
                    var targetPlayer = _sapi!.World.PlayerByUid(packet.TargetPlayerUID) as IServerPlayer;
                    if (targetPlayer != null && targetPlayer.PlayerUID != fromPlayer.PlayerUID)
                    {
                        var role = religion.GetRole(packet.RoleUID);
                        if (role != null)
                            targetPlayer.SendMessage(0,
                                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ROLE_CHANGED,
                                    religion.ReligionName, role.RoleName),
                                EnumChatType.Notification);
                    }

                    // Broadcast roles update to all religion members so their UI updates
                    BroadcastRolesUpdateToReligion(religion);
                }
            }
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Error handling AssignRoleRequest: {ex}");
            var errorResponse = new AssignRoleResponse
            {
                Success = false,
                ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _serverChannel!.SendPacket(errorResponse, fromPlayer);
        }
    }

    private void OnDeleteRoleRequest(IServerPlayer fromPlayer, DeleteRoleRequest packet)
    {
        try
        {
            if (string.IsNullOrEmpty(packet.ReligionUID) || string.IsNullOrEmpty(packet.RoleUID))
            {
                var errorResponse = new DeleteRoleResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVALID_REQUEST)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            var (success, error) = _roleManager.DeleteRole(
                packet.ReligionUID,
                fromPlayer.PlayerUID,
                packet.RoleUID
            );

            var response = new DeleteRoleResponse
            {
                Success = success,
                ErrorMessage = error
            };

            _serverChannel!.SendPacket(response, fromPlayer);

            // Broadcast roles update if deletion succeeded (members were reassigned)
            if (success)
            {
                var religion = _religionManager.GetReligion(packet.ReligionUID);
                if (religion != null) BroadcastRolesUpdateToReligion(religion);
            }
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Error handling DeleteRoleRequest: {ex}");
            var errorResponse = new DeleteRoleResponse
            {
                Success = false,
                ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _serverChannel!.SendPacket(errorResponse, fromPlayer);
        }
    }

    private void OnTransferFounderRequest(IServerPlayer fromPlayer, TransferFounderRequest packet)
    {
        try
        {
            if (string.IsNullOrEmpty(packet.ReligionUID) || string.IsNullOrEmpty(packet.NewFounderUID))
            {
                var errorResponse = new TransferFounderResponse
                {
                    Success = false,
                    ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVALID_REQUEST)
                };
                _serverChannel!.SendPacket(errorResponse, fromPlayer);
                return;
            }

            var (success, error) = _roleManager.TransferFounder(
                packet.ReligionUID,
                fromPlayer.PlayerUID,
                packet.NewFounderUID
            );

            var response = new TransferFounderResponse
            {
                Success = success,
                ErrorMessage = error
            };

            _serverChannel!.SendPacket(response, fromPlayer);

            // Notify both players if successful
            if (success)
            {
                var religion = _religionManager!.GetReligion(packet.ReligionUID);
                if (religion != null)
                {
                    // Notify the new founder
                    var newFounder = _sapi!.World.PlayerByUid(packet.NewFounderUID) as IServerPlayer;
                    if (newFounder != null)
                        newFounder.SendMessage(0,
                            LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_FOUNDER_TRANSFERRED,
                                religion.ReligionName),
                            EnumChatType.Notification);

                    // Notify the old founder (fromPlayer)
                    fromPlayer.SendMessage(0,
                        LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_FOUNDER_TRANSFER_SUCCESS,
                            newFounder?.PlayerName ?? packet.NewFounderUID),
                        EnumChatType.Notification);

                    // Notify all other members
                    foreach (var memberUID in religion.MemberUIDs)
                        if (memberUID != fromPlayer.PlayerUID && memberUID != packet.NewFounderUID)
                        {
                            var member = _sapi!.World.PlayerByUid(memberUID) as IServerPlayer;
                            member?.SendMessage(0,
                                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_FOUNDER_TRANSFERRED,
                                    religion.ReligionName),
                                EnumChatType.Notification);
                        }

                    // Broadcast roles update to all religion members so their UI updates
                    BroadcastRolesUpdateToReligion(religion);
                }
            }
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[DivineAscension] Error handling TransferFounderRequest: {ex}");
            var errorResponse = new TransferFounderResponse
            {
                Success = false,
                ErrorMessage = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ERROR, ex.Message)
            };
            _serverChannel!.SendPacket(errorResponse, fromPlayer);
        }
    }

    #endregion

    #region Action Handlers

    private class ReligionActionResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
    }

    private void SendRolesUpdateToPlayer(IServerPlayer player, ReligionData religion)
    {
        var rolesResponse = new ReligionRolesResponse
        {
            Success = true,
            Roles = _roleManager.GetReligionRoles(religion.ReligionUID),
            MemberRoles = religion.MemberRoles,
            MemberNames = new Dictionary<string, string>()
        };

        foreach (var uid in religion.MemberUIDs)
            rolesResponse.MemberNames[uid] = religion.GetMemberName(uid);

        _serverChannel.SendPacket(rolesResponse, player);
    }

    private void BroadcastRolesUpdateToReligion(ReligionData religion)
    {
        var rolesResponse = new ReligionRolesResponse
        {
            Success = true,
            Roles = _roleManager.GetReligionRoles(religion.ReligionUID),
            MemberRoles = religion.MemberRoles,
            MemberNames = new Dictionary<string, string>()
        };

        foreach (var uid in religion.MemberUIDs)
            rolesResponse.MemberNames[uid] = religion.GetMemberName(uid);

        // Send to all online members
        foreach (var memberUID in religion.MemberUIDs)
        {
            var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
            if (memberPlayer != null) _serverChannel.SendPacket(rolesResponse, memberPlayer);
        }
    }

    private void NotifyPlayerReligionStateChanged(IServerPlayer player, string reason, bool hasReligion)
    {
        var statePacket = new ReligionStateChangedPacket
        {
            Reason = reason,
            HasReligion = hasReligion
        };
        _serverChannel.SendPacket(statePacket, player);
    }

    private ReligionActionResult HandleJoinAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var religion = _religionManager.GetReligion(packet.ReligionUID);

        if (_religionManager.IsBanned(packet.ReligionUID, fromPlayer.PlayerUID))
        {
            var banDetails = _religionManager.GetBanDetails(packet.ReligionUID, fromPlayer.PlayerUID);
            if (banDetails != null)
            {
                var expiryText = banDetails.ExpiresAt == null
                    ? LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BANNED_PERMANENT)
                    : LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BANNED_EXPIRES,
                        banDetails.ExpiresAt?.ToString("yyyy-MM-dd HH:mm") ?? "");
                return new ReligionActionResult
                {
                    Success = false,
                    Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BANNED_WITH_REASON,
                        banDetails.Reason, expiryText)
                };
            }

            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BANNED_GENERIC)
            };
        }

        if (_religionManager.CanJoinReligion(packet.ReligionUID, fromPlayer.PlayerUID))
        {
            _playerProgressionDataManager.JoinReligion(fromPlayer.PlayerUID, packet.ReligionUID);
            _roleManager.AssignRole(religion!.ReligionUID, "SYSTEM", fromPlayer.PlayerUID, RoleDefaults.MEMBER_ROLE_ID);
            _playerProgressionDataManager.NotifyPlayerDataChanged(fromPlayer.PlayerUID);

            // Broadcast roles update to all religion members so their UI updates
            BroadcastRolesUpdateToReligion(religion);

            return new ReligionActionResult
            {
                Success = true,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_JOINED,
                    religion?.ReligionName ?? "religion")
            };
        }

        return new ReligionActionResult
        {
            Success = false,
            Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_CANNOT_JOIN)
        };
    }

    private ReligionActionResult HandleAcceptAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var (success, religionId, message) =
            _religionManager.AcceptInvite(packet.TargetPlayerUID, fromPlayer.PlayerUID);

        if (success)
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion != null)
            {
                try
                {
                    // Note: AcceptInvite already called AddMember, but JoinReligion is idempotent
                    // It will skip re-adding if already a member
                    _playerProgressionDataManager.JoinReligion(fromPlayer.PlayerUID, religionId);
                }
                catch (Exception ex)
                {
                    _sapi.Logger.Error($"[DivineAscension] Failed to join religion after accepting invite: {ex}");
                    // Rollback: Remove from ReligionManager if JoinReligion failed
                    _religionManager.RemoveMember(religionId, fromPlayer.PlayerUID);
                    return new ReligionActionResult
                    {
                        Success = false,
                        Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_JOIN_ERROR)
                    };
                }

                _roleManager.AssignRole(religionId, "SYSTEM", fromPlayer.PlayerUID, RoleDefaults.MEMBER_ROLE_ID);
                _playerProgressionDataManager.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
                NotifyPlayerReligionStateChanged(fromPlayer,
                    LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_YOU_JOINED), true);

                // Broadcast roles update to all religion members so their UI updates
                BroadcastRolesUpdateToReligion(religion);
            }
        }

        return new ReligionActionResult
        {
            Success = success,
            Message = success
                ? LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_ACCEPTED)
                : LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_FAILED)
        };
    }

    private ReligionActionResult HandleDeclineAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var success = _religionManager.DeclineInvite(packet.TargetPlayerUID, fromPlayer.PlayerUID);

        // Notify client of state change so UI refreshes invite list
        if (success)
        {
            NotifyPlayerReligionStateChanged(fromPlayer,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_DECLINED), false);
        }

        return new ReligionActionResult
        {
            Success = success,
            Message = success
                ? LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_DECLINED)
                : LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_DECLINE_FAILED)
        };
    }

    private ReligionActionResult HandleLeaveAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var currentReligion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        if (currentReligion == null)
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NOT_IN_RELIGION)
            };

        if (currentReligion.GetPlayerRole(fromPlayer.PlayerUID) == RoleDefaults.FOUNDER_ROLE_ID)
        {
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_FOUNDER_CANNOT_LEAVE)
            };
        }

        var religionName = currentReligion.ReligionName;
        _playerProgressionDataManager.LeaveReligion(fromPlayer.PlayerUID);
        _playerProgressionDataManager.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
        NotifyPlayerReligionStateChanged(fromPlayer,
            LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_YOU_LEFT, religionName), false);

        // Broadcast roles update to remaining members (after player has left)
        BroadcastRolesUpdateToReligion(currentReligion);

        return new ReligionActionResult
        {
            Success = true,
            Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_LEFT, religionName)
        };
    }

    private ReligionActionResult HandleKickAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        if (religion == null || !religion.HasPermission(fromPlayer.PlayerUID, RolePermissions.KICK_MEMBERS))
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NO_KICK_PERMISSION)
            };

        if (packet.TargetPlayerUID == fromPlayer.PlayerUID)
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_CANNOT_KICK_SELF)
            };

        _playerProgressionDataManager.LeaveReligion(packet.TargetPlayerUID);

        // Notify kicked player if online
        var kickedPlayer = _sapi.World.PlayerByUid(packet.TargetPlayerUID) as IServerPlayer;
        if (kickedPlayer != null)
        {
            kickedPlayer.SendMessage(0,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_KICKED_NOTIFICATION,
                    religion.ReligionName),
                EnumChatType.Notification);
            _playerProgressionDataManager.NotifyPlayerDataChanged(kickedPlayer.PlayerUID);
            NotifyPlayerReligionStateChanged(kickedPlayer,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_KICKED_NOTIFICATION,
                    religion.ReligionName), false);
        }

        // Broadcast updated roles/members data to all religion members
        BroadcastRolesUpdateToReligion(religion);

        return new ReligionActionResult
        {
            Success = true,
            Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_KICKED)
        };
    }

    private ReligionActionResult HandleBanAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        if (religion == null || !religion.HasPermission(fromPlayer.PlayerUID, RolePermissions.BAN_PLAYERS))
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NO_BAN_PERMISSION)
            };

        if (packet.TargetPlayerUID == fromPlayer.PlayerUID)
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_CANNOT_BAN_SELF)
            };

        // Extract ban parameters
        var reason = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NO_REASON);
        int? expiryDays = null;

        if (packet.Data != null)
        {
            if (packet.Data.ContainsKey("Reason"))
                reason = packet.Data["Reason"]?.ToString() ??
                         LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NO_REASON);

            if (packet.Data.ContainsKey("ExpiryDays"))
            {
                var expiryValue = packet.Data["ExpiryDays"]?.ToString();
                if (!string.IsNullOrEmpty(expiryValue) && int.TryParse(expiryValue, out var days) && days > 0)
                    expiryDays = days;
            }
        }

        // Kick the player if they're still a member
        if (religion.IsMember(packet.TargetPlayerUID))
            _playerProgressionDataManager.LeaveReligion(packet.TargetPlayerUID);

        // Ban the player
        _religionManager.BanPlayer(
            religion.ReligionUID,
            packet.TargetPlayerUID,
            fromPlayer.PlayerUID,
            reason,
            expiryDays
        );

        // Notify banned player if online
        var bannedPlayer = _sapi.World.PlayerByUid(packet.TargetPlayerUID) as IServerPlayer;
        if (bannedPlayer != null)
        {
            bannedPlayer.SendMessage(0,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BANNED_NOTIFICATION,
                    religion.ReligionName, reason),
                EnumChatType.Notification);
            _playerProgressionDataManager.NotifyPlayerDataChanged(bannedPlayer.PlayerUID);
            NotifyPlayerReligionStateChanged(bannedPlayer,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BANNED_NOTIFICATION,
                    religion.ReligionName, reason), false);
        }

        // Broadcast updated roles/members data to all religion members
        BroadcastRolesUpdateToReligion(religion);

        return new ReligionActionResult
        {
            Success = true,
            Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_PLAYER_BANNED,
                expiryDays?.ToString() ?? "permanently", reason)
        };
    }

    private ReligionActionResult HandleUnbanAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        if (religion == null || !religion.IsFounder(fromPlayer.PlayerUID))
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ONLY_FOUNDER_UNBAN)
            };

        var success = _religionManager.UnbanPlayer(religion.ReligionUID, packet.TargetPlayerUID);

        return new ReligionActionResult
        {
            Success = success,
            Message = success
                ? LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_UNBANNED)
                : LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_UNBAN_FAILED)
        };
    }

    private ReligionActionResult HandleInviteAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        if (religion == null)
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_NOT_IN_RELIGION)
            };

        // Convert player name to UID (UI sends player name in TargetPlayerUID field)
        var targetPlayer = _sapi.World.AllOnlinePlayers
            .FirstOrDefault(p => p.PlayerName.Equals(packet.TargetPlayerUID,
                StringComparison.OrdinalIgnoreCase)) as IServerPlayer;

        if (targetPlayer == null)
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_PLAYER_NOT_FOUND,
                    packet.TargetPlayerUID)
            };

        var success = _religionManager.InvitePlayer(religion.ReligionUID,
            targetPlayer.PlayerUID,
            fromPlayer.PlayerUID);

        if (success)
        {
            NotifyPlayerReligionStateChanged(targetPlayer,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITED_NOTIFICATION,
                    religion.ReligionName), false);
            _sapi.Logger.Debug(
                $"[DivineAscension] Sent invitation notification to {targetPlayer.PlayerName} ({targetPlayer.PlayerUID})");
        }

        return new ReligionActionResult
        {
            Success = success,
            Message = success
                ? LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_SENT)
                : LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_INVITE_SEND_FAILED)
        };
    }

    private ReligionActionResult HandleDisbandAction(IServerPlayer fromPlayer, ReligionActionRequestPacket packet)
    {
        var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        if (religion == null || religion.FounderUID != fromPlayer.PlayerUID)
            return new ReligionActionResult
            {
                Success = false,
                Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ONLY_FOUNDER_KICK)
            };

        var religionName = religion.ReligionName;
        var members = religion.MemberUIDs.ToList();

        foreach (var memberUID in members)
        {
            _playerProgressionDataManager.LeaveReligion(memberUID);

            var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
            if (memberPlayer != null)
            {
                if (memberUID != fromPlayer.PlayerUID)
                    memberPlayer.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DISBANDED, religionName),
                        EnumChatType.Notification
                    );

                NotifyPlayerReligionStateChanged(memberPlayer,
                    LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DISBANDED, religionName), false);
            }
        }

        _religionManager.DeleteReligion(religion.ReligionUID, fromPlayer.PlayerUID);

        return new ReligionActionResult
        {
            Success = true,
            Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_DISBANDED,
                religionName ?? "religion")
        };
    }

    #endregion
}