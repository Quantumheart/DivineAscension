using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles civilization-related network requests from clients.
///     Manages civilization list requests, info requests, and action requests
///     (create, invite, accept, leave, kick, disband).
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationNetworkHandler(
    ICoreServerAPI sapi,
    CivilizationManager civilizationManager,
    IReligionManager religionManager,
    IServerNetworkChannel serverChannel)
    : IServerNetworkHandler
{
    public void RegisterHandlers()
    {
        // Register handlers for civilization system packets
        serverChannel.SetMessageHandler<CivilizationListRequestPacket>(OnCivilizationListRequest);
        serverChannel.SetMessageHandler<CivilizationInfoRequestPacket>(OnCivilizationInfoRequest);
        serverChannel.SetMessageHandler<CivilizationActionRequestPacket>(OnCivilizationActionRequest);
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    /// <summary>
    ///     Handle civilization list request from client
    /// </summary>
    private void OnCivilizationListRequest(IServerPlayer fromPlayer, CivilizationListRequestPacket packet)
    {
        sapi.Logger.Debug(
            $"[DivineAscension] Civilization list requested by {fromPlayer.PlayerName}, filter: '{packet.FilterDeity}'");

        var civilizations = civilizationManager.GetAllCivilizations().ToList();
        var civInfoList = new List<CivilizationListResponsePacket.CivilizationInfo>();

        foreach (var civ in civilizations)
        {
            var religions = civilizationManager.GetCivReligions(civ.CivId);
            var deities = religions.Select(r => r.Domain.ToString()).Distinct().ToList();
            var religionNames = religions.Select(r => r.ReligionName).ToList();

            // Apply deity filter if specified
            if (!string.IsNullOrEmpty(packet.FilterDeity))
            {
                // Check if any religion in this civilization has the filtered deity
                var hasFilteredDeity = religions.Any(r =>
                    string.Equals(r.Domain.ToString(), packet.FilterDeity, StringComparison.OrdinalIgnoreCase));

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
                MemberReligionNames = religionNames,
                Icon = civ.Icon
            });
        }

        var response = new CivilizationListResponsePacket(civInfoList);
        serverChannel.SendPacket(response, fromPlayer);
        sapi.Logger.Debug(
            $"[DivineAscension] Sent {civInfoList.Count} civilizations (out of {civilizations.Count} total) with filter '{packet.FilterDeity}'");
    }

    /// <summary>
    ///     Handle civilization info request from client
    ///     If civId is empty, returns the civilization for the player's current religion
    /// </summary>
    private void OnCivilizationInfoRequest(IServerPlayer fromPlayer, CivilizationInfoRequestPacket packet)
    {
        sapi.Logger.Debug(
            $"[DivineAscension] Civilization info requested by {fromPlayer.PlayerName} for {packet.CivId}");

        var civId = packet.CivId;

        // If civId is empty, look up the player's religion's civilization
        if (string.IsNullOrEmpty(civId))
        {
            var playerReligion = religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
            if (playerReligion == null)
            {
                serverChannel.SendPacket(new CivilizationInfoResponsePacket(), fromPlayer);
                return;
            }

            var religionCiv = civilizationManager.GetCivilizationByReligion(playerReligion.ReligionUID);
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

                var invitesForReligion = civilizationManager.GetInvitesForReligion(playerReligion.ReligionUID);
                foreach (var invite in invitesForReligion)
                {
                    var invitingCiv = civilizationManager.GetCivilization(invite.CivId);
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

                serverChannel.SendPacket(new CivilizationInfoResponsePacket(detailsForInvitesOnly), fromPlayer);
                return;
            }

            civId = religionCiv.CivId;
        }

        var civ = civilizationManager.GetCivilization(civId);
        if (civ == null)
        {
            serverChannel.SendPacket(new CivilizationInfoResponsePacket(), fromPlayer);
            return;
        }

        // Get founding religion name and founder's cached name
        var founderReligion = religionManager.GetReligion(civ.FounderReligionUID);
        var founderReligionName = founderReligion?.ReligionName ??
                                  LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_UNKNOWN);
        var founderPlayerName = founderReligion?.GetMemberName(civ.FounderUID) ?? civ.FounderUID;

        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = civ.CivId,
            Name = civ.Name,
            FounderUID = civ.FounderUID,
            FounderName = founderPlayerName,
            FounderReligionUID = civ.FounderReligionUID,
            FounderReligionName = founderReligionName,
            CreatedDate = civ.CreatedDate,
            Icon = civ.Icon,
            MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>(),
            PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>()
        };

        // Get member religion details
        var religions = civilizationManager.GetCivReligions(civ.CivId);
        foreach (var religion in religions)
        {
            // Use cached religion founder name
            var religionFounderName = religion.FounderName;

            details.MemberReligions.Add(new CivilizationInfoResponsePacket.MemberReligion
            {
                ReligionId = religion.ReligionUID,
                ReligionName = religion.ReligionName,
                Domain = religion.Domain.ToString(),
                DeityName = religion.DeityName,
                FounderReligionUID = civ.FounderReligionUID,
                FounderUID = religion.FounderUID,
                FounderName = religionFounderName,
                MemberCount = religion.MemberUIDs.Count
            });
        }

        // Get pending invites (only show to founder)
        if (civ.FounderUID == fromPlayer.PlayerUID)
        {
            var invites = civilizationManager.GetInvitesForCiv(civ.CivId);
            foreach (var invite in invites)
            {
                var targetReligion = religionManager.GetReligion(invite.ReligionId);
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
        serverChannel.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle civilization action request from client
    /// </summary>
    private void OnCivilizationActionRequest(IServerPlayer fromPlayer, CivilizationActionRequestPacket packet)
    {
        sapi.Logger.Debug(
            $"[DivineAscension] Civilization action '{packet.Action}' requested by {fromPlayer.PlayerName}");

        var response = new CivilizationActionResponsePacket();
        response.Action = packet.Action;
        var religion = religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

        try
        {
            switch (packet.Action.ToLower())
            {
                case "create":
                    if (religion == null || string.IsNullOrEmpty(religion.ReligionUID))
                    {
                        response.Success = false;
                        response.Message =
                            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_MUST_BE_IN_RELIGION);
                        break;
                    }

                    if (ProfanityFilterService.Instance.ContainsProfanity(packet.Name))
                    {
                        response.Success = false;
                        response.Message =
                            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_NAME_PROFANITY);
                        break;
                    }

                    var iconToUse = string.IsNullOrWhiteSpace(packet.Icon) ? "default" : packet.Icon;
                    var newCiv = civilizationManager.CreateCivilization(packet.Name, fromPlayer.PlayerUID,
                        religion.ReligionUID, iconToUse);
                    if (newCiv != null)
                    {
                        response.Success = true;
                        response.Message =
                            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_CREATED, newCiv.Name);
                        response.CivId = newCiv.CivId;
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_CREATE_FAILED);
                    }

                    break;

                case "invite":
                    // Look up religion by name (packet.TargetId contains religion name from UI)
                    var targetReligion = religionManager.GetReligionByName(packet.TargetId);
                    if (targetReligion == null)
                    {
                        response.Success = false;
                        response.Message = LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_RELIGION_NOT_FOUND,
                            packet.TargetId);
                        break;
                    }

                    var success = civilizationManager.InviteReligion(packet.CivId, targetReligion.ReligionUID,
                        fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_INVITE_SENT,
                            targetReligion.ReligionName)
                        : LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_INVITE_FAILED);

                    // Notify all members of invited religion if invitation succeeded
                    if (success)
                    {
                        var civ = civilizationManager.GetCivilization(packet.CivId);
                        if (civ != null)
                        {
                            var notifiedCount = 0;
                            var offlineCount = 0;

                            foreach (var memberUID in targetReligion.MemberUIDs)
                            {
                                var memberPlayer = sapi.World.PlayerByUid(memberUID) as IServerPlayer;
                                if (memberPlayer != null)
                                {
                                    var statePacket = new ReligionStateChangedPacket
                                    {
                                        Reason = LocalizationService.Instance.Get(
                                            LocalizationKeys.NET_CIV_INVITED_NOTIFICATION, civ.Name),
                                        HasReligion = true
                                    };
                                    serverChannel.SendPacket(statePacket, memberPlayer);
                                    notifiedCount++;
                                }
                                else
                                {
                                    offlineCount++;
                                }
                            }

                            sapi.Logger.Notification(
                                $"[DivineAscension] Civilization invitation sent to {targetReligion.ReligionName}: " +
                                $"{notifiedCount} members notified, {offlineCount} offline");
                        }
                    }

                    response.CivId = packet.CivId;
                    break;

                case "accept":
                    success = civilizationManager.AcceptInvite(packet.TargetId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_JOINED)
                        : LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_JOIN_FAILED);
                    break;

                case "leave":
                    if (religion == null || string.IsNullOrEmpty(religion.ReligionUID))
                    {
                        response.Success = false;
                        response.Message = LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_NOT_IN_RELIGION);
                        break;
                    }

                    // Get religion to check if player is the founder
                    var playerReligion = religionManager.GetReligion(religion.ReligionUID);
                    if (playerReligion == null)
                    {
                        response.Success = false;
                        response.Message =
                            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_RELIGION_NOT_FOUND_PLAYER);
                        break;
                    }

                    // Check if player is the religion founder (only founders can leave)
                    if (playerReligion.FounderUID != fromPlayer.PlayerUID)
                    {
                        response.Success = false;
                        response.Message =
                            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_ONLY_FOUNDER_LEAVE);
                        break;
                    }

                    // Check if player is the civilization founder
                    var playerCiv = civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
                    if (playerCiv != null && playerCiv.FounderUID == fromPlayer.PlayerUID)
                    {
                        response.Success = false;
                        response.Message =
                            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_FOUNDER_MUST_DISBAND);
                        break;
                    }

                    success = civilizationManager.LeaveReligion(religion.ReligionUID, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_LEFT)
                        : LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_LEAVE_FAILED);
                    break;

                case "kick":
                    success = civilizationManager.KickReligion(packet.CivId, packet.TargetId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_KICKED)
                        : LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_KICK_FAILED);
                    response.CivId = packet.CivId;
                    break;

                case "disband":
                    success = civilizationManager.DisbandCivilization(packet.CivId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_DISBANDED)
                        : LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_DISBAND_FAILED);
                    response.CivId = packet.CivId;
                    break;

                case "updateicon":
                    success = civilizationManager.UpdateCivilizationIcon(packet.CivId, fromPlayer.PlayerUID,
                        packet.Icon);
                    response.Success = success;
                    response.Message = success
                        ? LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_ICON_UPDATED)
                        : LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_ICON_UPDATE_FAILED);
                    response.CivId = packet.CivId;
                    break;

                default:
                    response.Success = false;
                    response.Message =
                        LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_UNKNOWN_ACTION, packet.Action);
                    break;
            }
        }
        catch (Exception ex)
        {
            sapi.Logger.Error($"[DivineAscension] Error handling civilization action '{packet.Action}': {ex}");
            response.Success = false;
            response.Message = LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_ERROR);
        }

        serverChannel.SendPacket(response, fromPlayer);
    }
}