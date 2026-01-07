using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles blessing-related network requests from clients.
///     Manages blessing unlocks (both player and religion blessings) and blessing data requests.
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingNetworkHandler(
    ICoreServerAPI sapi,
    BlessingRegistry blessingRegistry,
    BlessingEffectSystem blessingEffectSystem,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IReligionManager religionManager,
    IServerNetworkChannel serverChannel)
    : IServerNetworkHandler
{
    public void RegisterHandlers()
    {
        // Register handlers for blessing system packets
        serverChannel.SetMessageHandler<BlessingUnlockRequestPacket>(OnBlessingUnlockRequest);
        serverChannel.SetMessageHandler<BlessingDataRequestPacket>(OnBlessingDataRequest);
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    private void OnBlessingUnlockRequest(IServerPlayer fromPlayer, BlessingUnlockRequestPacket packet)
    {
        string message;
        var success = false;

        try
        {
            var blessing = blessingRegistry!.GetBlessing(packet.BlessingId);
            if (blessing == null)
            {
                message = $"Blessing '{packet.BlessingId}' not found.";
            }
            else
            {
                var playerData = playerProgressionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                var religion = religionManager.GetPlayerReligion(fromPlayer.PlayerUID);


                var (canUnlock, reason) = blessingRegistry.CanUnlockBlessing(playerData, religion, blessing);
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
                            success = playerProgressionDataManager.UnlockPlayerBlessing(fromPlayer.PlayerUID,
                                packet.BlessingId);
                            if (success)
                            {
                                blessingEffectSystem!.RefreshPlayerBlessings(fromPlayer.PlayerUID);
                                message = $"Successfully unlocked {blessing.Name}!";

                                // Notify player data changed (triggers event that sends HUD update to client)
                                playerProgressionDataManager.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
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
                            blessingEffectSystem!.RefreshReligionBlessings(religion.ReligionUID);
                            message = $"Successfully unlocked {blessing.Name} for all religion members!";
                            success = true;

                            // Notify all members
                            foreach (var memberUid in religion.MemberUIDs)
                            {
                                // Notify player data changed (triggers event that sends HUD update to client)
                                playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                                var member = sapi!.World.PlayerByUid(memberUid) as IServerPlayer;
                                if (member != null)
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
            sapi!.Logger.Error($"[DivineAscension] Blessing unlock error: {ex}");
        }

        var response = new BlessingUnlockResponsePacket(success, message, packet.BlessingId);
        serverChannel!.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle blessing data request from client
    /// </summary>
    private void OnBlessingDataRequest(IServerPlayer fromPlayer, BlessingDataRequestPacket packet)
    {
        sapi!.Logger.Debug($"[DivineAscension] Blessing data requested by {fromPlayer.PlayerName}");

        var response = new BlessingDataResponsePacket();

        try
        {
            var playerData = playerProgressionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);

            var religion = religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
            var deity = playerProgressionDataManager.GetPlayerDeityType(fromPlayer.PlayerUID);
            if (religion == null || deity == DeityType.None)
            {
                response.HasReligion = false;
                serverChannel!.SendPacket(response, fromPlayer);
                return;
            }

            response.HasReligion = true;
            response.ReligionUID = religion.ReligionUID;
            response.ReligionName = religion.ReligionName;
            response.Deity = deity.ToString();
            response.FavorRank = (int)playerData.FavorRank;
            response.PrestigeRank = (int)religion.PrestigeRank;
            response.CurrentFavor = playerData.Favor;
            response.CurrentPrestige = religion.Prestige;
            response.TotalFavorEarned = playerData.TotalFavorEarned;

            // Get player blessings for this deity
            var playerBlessings = blessingRegistry!.GetBlessingsForDeity(deity, BlessingKind.Player);
            response.PlayerBlessings = playerBlessings.Select(p => new BlessingDataResponsePacket.BlessingInfo
            {
                BlessingId = p.BlessingId,
                Name = p.Name,
                Description = p.Description,
                RequiredFavorRank = p.RequiredFavorRank,
                RequiredPrestigeRank = p.RequiredPrestigeRank,
                PrerequisiteBlessings = p.PrerequisiteBlessings ?? new List<string>(),
                Category = (int)p.Category,
                StatModifiers = p.StatModifiers ?? new Dictionary<string, float>(),
                IconName = p.IconName
            }).ToList();

            // Get religion blessings for this deity
            var religionBlessings =
                blessingRegistry.GetBlessingsForDeity(deity, BlessingKind.Religion);
            response.ReligionBlessings = religionBlessings.Select(p => new BlessingDataResponsePacket.BlessingInfo
            {
                BlessingId = p.BlessingId,
                Name = p.Name,
                Description = p.Description,
                RequiredFavorRank = p.RequiredPrestigeRank,
                RequiredPrestigeRank = p.RequiredPrestigeRank,
                PrerequisiteBlessings = p.PrerequisiteBlessings ?? new List<string>(),
                Category = (int)p.Category,
                StatModifiers = p.StatModifiers ?? new Dictionary<string, float>(),
                IconName = p.IconName
            }).ToList();

            // Get unlocked player blessings
            response.UnlockedPlayerBlessings = playerData.UnlockedBlessings
                .ToList();

            // Get unlocked religion blessings
            response.UnlockedReligionBlessings = religion.UnlockedBlessings
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            sapi.Logger.Debug(
                $"[DivineAscension] Sending blessing data: {response.PlayerBlessings.Count} player, {response.ReligionBlessings.Count} religion");
        }
        catch (Exception ex)
        {
            sapi!.Logger.Error($"[DivineAscension] Error loading blessing data: {ex}");
            response.HasReligion = false;
        }

        serverChannel!.SendPacket(response, fromPlayer);
    }
}