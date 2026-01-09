using System;
using System.Linq;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
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

        if (religionManager.GetPlayerActiveDeity(player.PlayerUID) == DeityType.None)
            return (null, null, TextCommandResult.Error("You are not in a religion or do not have an active deity."));

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
                return (null, null, TextCommandResult.Error($"Cannot find player with name '{targetPlayerName}'"));

            var serverPlayer = targetPlayer as IServerPlayer;
            if (serverPlayer is null)
                return (null, null, TextCommandResult.Error("Target player is not a server player"));

            var (targetPlayerData, _, targetErrorResult) =
                ValidatePlayerHasDeity(serverPlayer, playerProgressionDataManager, religionManager);
            if (targetErrorResult is { Status: EnumCommandStatus.Error })
                return (null, null, targetErrorResult);

            if (targetPlayerData is null)
                return (null, null, TextCommandResult.Error("Target must have a religion"));

            return (serverPlayer, targetPlayerData, null);
        }

        // Use caller as target
        var (callerData, _, callerErrorResult) =
            ValidatePlayerHasDeity(caller, playerProgressionDataManager, religionManager);
        if (callerErrorResult is { Status: EnumCommandStatus.Error })
            return (null, null, callerErrorResult);

        if (callerData is null)
            return (null, null, TextCommandResult.Error("Player must have a religion"));

        return (caller, callerData, null);
    }
}