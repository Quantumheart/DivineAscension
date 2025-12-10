using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
///     Handles blessing-related network requests from clients.
///     Manages blessing unlocks (both player and religion blessings) and blessing data requests.
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingNetworkHandler(
    ICoreServerAPI sapi,
    BlessingRegistry blessingRegistry,
    BlessingEffectSystem blessingEffectSystem,
    IPlayerReligionDataManager playerReligionDataManager,
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
                var playerData = playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                var religion = playerData.ReligionUID != null
                    ? religionManager!.GetReligion(playerData.ReligionUID)
                    : null;

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
                            success = playerReligionDataManager.UnlockPlayerBlessing(fromPlayer.PlayerUID,
                                packet.BlessingId);
                            if (success)
                            {
                                blessingEffectSystem!.RefreshPlayerBlessings(fromPlayer.PlayerUID);
                                message = $"Successfully unlocked {blessing.Name}!";

                                // Notify player data changed (triggers event that sends HUD update to client)
                                playerReligionDataManager.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
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
                                playerReligionDataManager.NotifyPlayerDataChanged(memberUid);

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
            sapi!.Logger.Error($"[PantheonWars] Blessing unlock error: {ex}");
        }

        var response = new BlessingUnlockResponsePacket(success, message, packet.BlessingId);
        serverChannel!.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle blessing data request from client
    /// </summary>
    private void OnBlessingDataRequest(IServerPlayer fromPlayer, BlessingDataRequestPacket packet)
    {
        sapi!.Logger.Debug($"[PantheonWars] Blessing data requested by {fromPlayer.PlayerName}");

        var response = new BlessingDataResponsePacket();

        try
        {
            var playerData = playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
            var religion = playerData.ReligionUID != null
                ? religionManager!.GetReligion(playerData.ReligionUID)
                : null;

            if (religion == null || playerData.ActiveDeity == DeityType.None)
            {
                response.HasReligion = false;
                serverChannel!.SendPacket(response, fromPlayer);
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
            var playerBlessings = blessingRegistry!.GetBlessingsForDeity(playerData.ActiveDeity, BlessingKind.Player);
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
                blessingRegistry.GetBlessingsForDeity(playerData.ActiveDeity, BlessingKind.Religion);
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

            sapi.Logger.Debug(
                $"[PantheonWars] Sending blessing data: {response.PlayerBlessings.Count} player, {response.ReligionBlessings.Count} religion");
        }
        catch (Exception ex)
        {
            sapi!.Logger.Error($"[PantheonWars] Error loading blessing data: {ex}");
            response.HasReligion = false;
        }

        serverChannel!.SendPacket(response, fromPlayer);
    }
}