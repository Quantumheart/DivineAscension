using System;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Shared helper methods for command handlers
/// </summary>
public static class CommandHelpers
{
    /// <summary>
    ///     Get player's religion data and validate they have a deity
    /// </summary>
    public static (PlayerProgressionData? playerProgressionData, string? religionName, TextCommandResult? errorResult)
        ValidatePlayerHasDeity(IServerPlayer player, IPlayerProgressionDataManager playerProgressionDataManager,
            IReligionManager religionManager)
    {
        var playerProgressionData = playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionManager.GetPlayerActiveDeityDomain(player.PlayerUID) == DeityDomain.None)
            return (null, null,
                TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_DEITY)));

        // Get religion name if in a religion
        string? religionName = null;
        var religion = religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion != null && !string.IsNullOrEmpty(religion.ReligionUID))
            religionName = religion.ReligionName;

        return (playerProgressionData, religionName, null);
    }

    /// <summary>
    ///     Resolves the target player for admin commands. If targetPlayerName is provided, finds and validates that player.
    ///     Otherwise, uses the caller as the target.
    /// </summary>
    public static (IServerPlayer? targetPlayer, PlayerProgressionData? playerData, TextCommandResult? errorResult)
        ResolveTargetPlayer(IServerPlayer caller, string? targetPlayerName, ICoreServerAPI sapi,
            IPlayerProgressionDataManager playerProgressionDataManager, IReligionManager religionManager)
    {
        if (targetPlayerName != null)
        {
            // Find the target player
            var targetPlayer = sapi.World.AllPlayers
                .FirstOrDefault(p => string.Equals(p.PlayerName, targetPlayerName, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer is null)
                return (null, null,
                    TextCommandResult.Error(
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_PLAYER_NOT_FOUND,
                            targetPlayerName)));

            var serverPlayer = targetPlayer as IServerPlayer;
            if (serverPlayer is null)
                return (null, null,
                    TextCommandResult.Error(
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_NOT_SERVER_PLAYER)));

            var (targetPlayerData, _, targetErrorResult) =
                ValidatePlayerHasDeity(serverPlayer, playerProgressionDataManager, religionManager);
            if (targetErrorResult is { Status: EnumCommandStatus.Error })
                return (null, null, targetErrorResult);

            if (targetPlayerData is null)
                return (null, null,
                    TextCommandResult.Error(
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_TARGET_NO_RELIGION)));

            return (serverPlayer, targetPlayerData, null);
        }

        // Use caller as target
        var (callerData, _, callerErrorResult) =
            ValidatePlayerHasDeity(caller, playerProgressionDataManager, religionManager);
        if (callerErrorResult is { Status: EnumCommandStatus.Error })
            return (null, null, callerErrorResult);

        if (callerData is null)
            return (null, null,
                TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION)));

        return (caller, callerData, null);
    }
}