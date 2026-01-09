using System;
using System.Linq;
using System.Text;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Chat commands for favor management and testing
/// </summary>
public class FavorCommands
{
    private readonly IDeityRegistry _deityRegistry;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FavorCommands(
        ICoreServerAPI sapi,
        IDeityRegistry deityRegistry,
        IPlayerProgressionDataManager playerReligionDataManager,
        IReligionManager religionManager)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _deityRegistry = deityRegistry ?? throw new ArgumentNullException(nameof(deityRegistry));
        _playerProgressionDataManager = playerReligionDataManager ??
                                        throw new ArgumentNullException(nameof(playerReligionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
    }

    /// <summary>
    ///     Registers all favor-related commands
    /// </summary>
    public void RegisterCommands()
    {
        // Main /favor command with subcommands
        _sapi.ChatCommands.Create("favor")
            .WithDescription("Manage and check your divine favor")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnCheckFavor) // Default behavior: show current favor
            .BeginSubCommand("get")
            .WithDescription("Check your current divine favor")
            .HandleWith(OnCheckFavor)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription("View detailed favor information and rank progression")
            .HandleWith(OnFavorInfo)
            .EndSubCommand()
            .BeginSubCommand("stats")
            .WithDescription("View comprehensive favor statistics")
            .HandleWith(OnFavorStats)
            .EndSubCommand()
            .BeginSubCommand("ranks")
            .WithDescription("List all devotion ranks and their requirements")
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnListRanks)
            .EndSubCommand()
            .BeginSubCommand("set")
            .WithDescription("Set favor to a specific amount (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnSetFavor)
            .EndSubCommand()
            .BeginSubCommand("add")
            .WithDescription("Add favor (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnAddFavor)
            .EndSubCommand()
            .BeginSubCommand("remove")
            .WithDescription("Remove favor (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnRemoveFavor)
            .EndSubCommand()
            .BeginSubCommand("reset")
            .WithDescription("Reset favor to 0 (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnResetFavor)
            .EndSubCommand()
            .BeginSubCommand("max")
            .WithDescription("Set favor to maximum (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnMaxFavor)
            .EndSubCommand()
            .BeginSubCommand("settotal")
            .WithDescription("Set total favor earned and update rank (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnSetTotalFavor)
            .EndSubCommand();

        _sapi.Logger.Notification("[DivineAscension] Favor commands registered");
    }

    #region Helper Methods

    /// <summary>
    ///     Get player's religion data and validate they have a deity
    /// </summary>
    internal (PlayerProgressionData? playerProgressionData, string? religionName, TextCommandResult? errorResult)
        ValidatePlayerHasDeity(IServerPlayer player)
    {
        var playerProgressionData = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (_religionManager.GetPlayerActiveDeity(player.PlayerUID) == DeityType.None)
            return (null, null, TextCommandResult.Error("You are not in a religion or do not have an active deity."));

        // Get religion name if in a religion
        string? religionName = null;
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (!string.IsNullOrEmpty(religion.ReligionUID))
            religionName = religion.ReligionName; // This will be improved when we have access to ReligionManager

        return (playerProgressionData, religionName, null);
    }

    /// <summary>
    ///     Gets the current favor rank as integer (0-4)
    /// </summary>
    private int GetCurrentFavorRank(int totalFavorEarned)
    {
        if (totalFavorEarned >= 10000) return 4; // Avatar
        if (totalFavorEarned >= 5000) return 3; // Champion
        if (totalFavorEarned >= 2000) return 2; // Zealot
        if (totalFavorEarned >= 500) return 1; // Disciple
        return 0; // Initiate
    }

    /// <summary>
    ///     Formats the result message for total favor changes
    /// </summary>
    private static string FormatTotalFavorResult(PlayerProgressionData playerData, int newAmount, int oldTotal,
        FavorRank oldRank)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Total favor earned set to {newAmount:N0} (was {oldTotal:N0})");

        var newRank = playerData.FavorRank;
        if (oldRank != newRank)
            sb.Append($"Rank updated: {oldRank} → {newRank}");
        else
            sb.Append($"Rank unchanged: {newRank}");

        return sb.ToString();
    }

    /// <summary>
    ///     Resolves the target player for admin commands. If targetPlayerName is provided, finds and validates that player.
    ///     Otherwise, uses the caller as the target.
    /// </summary>
    internal (IServerPlayer? targetPlayer, PlayerProgressionData? playerData, TextCommandResult? errorResult)
        ResolveTargetPlayer(IServerPlayer caller, string? targetPlayerName)
    {
        if (targetPlayerName != null)
        {
            // Find the target player
            var targetPlayer = _sapi.World.AllPlayers
                .FirstOrDefault(p => string.Equals(p.PlayerName, targetPlayerName, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer is null)
                return (null, null, TextCommandResult.Error($"Cannot find player with name '{targetPlayerName}'"));

            var serverPlayer = targetPlayer as IServerPlayer;
            if (serverPlayer is null)
                return (null, null, TextCommandResult.Error("Target player is not a server player"));

            var (targetPlayerData, _, targetErrorResult) = ValidatePlayerHasDeity(serverPlayer);
            if (targetErrorResult is { Status: EnumCommandStatus.Error })
                return (null, null, targetErrorResult);

            if (targetPlayerData is null)
                return (null, null, TextCommandResult.Error("Target must have a religion"));

            return (serverPlayer, targetPlayerData, null);
        }

        // Use caller as target
        var (callerData, _, callerErrorResult) = ValidatePlayerHasDeity(caller);
        if (callerErrorResult is { Status: EnumCommandStatus.Error })
            return (null, null, callerErrorResult);

        if (callerData is null)
            return (null, null, TextCommandResult.Error("Player must have a religion"));

        return (caller, callerData, null);
    }

    #endregion

    #region Information Commands (Privilege.chat)

    /// <summary>
    ///     Shows current favor amount - default command and /favor get
    /// </summary>
    internal TextCommandResult OnCheckFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (playerProgressionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _deityRegistry.GetDeity(_religionManager.GetPlayerActiveDeity(player.PlayerUID));
        var deityName = deity?.Name;

        return TextCommandResult.Success(
            $"You have {playerProgressionData.Favor} favor with {deityName} (Rank: {playerProgressionData.FavorRank})"
        );
    }

    /// <summary>
    ///     Shows detailed favor information and rank progression
    /// </summary>
    internal TextCommandResult OnFavorInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (playerProgressionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _deityRegistry.GetDeity(_religionManager.GetPlayerActiveDeity(player.PlayerUID));
        var deityName = deity?.Name;

        // Get current rank based on total favor
        var currentRank = GetCurrentFavorRank(playerProgressionData.TotalFavorEarned);
        var currentRankName = RankRequirements.GetFavorRankName(currentRank);

        var sb = new StringBuilder();
        sb.AppendLine("=== Divine Favor ===");
        sb.AppendLine($"Deity: {deityName}");
        sb.AppendLine($"Current Favor: {playerProgressionData.Favor:N0}");
        sb.AppendLine($"Total Favor Earned: {playerProgressionData.TotalFavorEarned:N0}");
        sb.AppendLine($"Current Rank: {currentRankName}");

        // Calculate next rank
        if (currentRank < 4) // Not at max rank
        {
            var nextRank = currentRank + 1;
            var nextRankName = RankRequirements.GetFavorRankName(nextRank);
            var nextThreshold = RankRequirements.GetRequiredFavorForNextRank(currentRank);

            sb.AppendLine($"Next Rank: {nextRankName} ({nextThreshold:N0} total favor required)");

            var remaining = nextThreshold - playerProgressionData.TotalFavorEarned;
            var progress = (float)playerProgressionData.TotalFavorEarned / nextThreshold * 100f;
            sb.AppendLine($"Progress: {progress:F1}% ({remaining:N0} favor needed)");
        }
        else
        {
            sb.AppendLine("Next Rank: None (Maximum rank achieved!)");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Shows comprehensive favor statistics
    /// </summary>
    internal TextCommandResult OnFavorStats(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (playerProgressionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _deityRegistry.GetDeity(_religionManager.GetPlayerActiveDeity(player.PlayerUID));
        var deityName = deity?.Name;

        // Get current rank based on total favor
        var currentRank = GetCurrentFavorRank(playerProgressionData.TotalFavorEarned);
        var currentRankName = RankRequirements.GetFavorRankName(currentRank);

        var sb = new StringBuilder();
        sb.AppendLine("=== Divine Statistics ===");
        sb.AppendLine($"Deity: {deityName}");
        sb.AppendLine($"Current Favor: {playerProgressionData.Favor:N0}");
        sb.AppendLine($"Total Favor Earned: {playerProgressionData.TotalFavorEarned:N0}");
        sb.AppendLine($"Devotion Rank: {currentRankName}");

        // Calculate next rank
        if (currentRank < 4) // Not at max rank
        {
            var nextRank = currentRank + 1;
            var nextRankName = RankRequirements.GetFavorRankName(nextRank);
            var nextThreshold = RankRequirements.GetRequiredFavorForNextRank(currentRank);
            var remaining = nextThreshold - playerProgressionData.TotalFavorEarned;

            sb.AppendLine();
            sb.AppendLine($"Next Rank: {nextRankName}");
            sb.AppendLine($"Favor Needed: {remaining:N0}");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Lists all devotion ranks and their requirements
    ///     Does NOT require deity pledge - informational only
    /// </summary>
    internal TextCommandResult OnListRanks(TextCommandCallingArgs args)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Favor Ranks ===");

        // List all ranks with their requirements
        for (var rank = 0; rank <= 4; rank++)
        {
            var rankName = RankRequirements.GetFavorRankName(rank);
            var totalRequired = rank == 0 ? 0 : RankRequirements.GetRequiredFavorForNextRank(rank - 1);
            sb.AppendLine($"{rankName}: {totalRequired:N0} total favor");
        }

        sb.AppendLine();
        sb.AppendLine("Higher ranks unlock more powerful blessings.");

        return TextCommandResult.Success(sb.ToString());
    }

    #endregion

    #region Admin Mutation Commands (Privilege.root)

    /// <summary>
    ///     Sets favor to a specific amount (Admin only)
    /// </summary>
    internal TextCommandResult OnSetFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var amount = (int)args[0];
        var targetPlayerName = (string)args[1];

        // Validate amount
        if (amount < 0) return TextCommandResult.Error("Favor amount cannot be negative.");
        if (amount > 999999) return TextCommandResult.Error("Favor amount cannot exceed 999,999.");

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = ResolveTargetPlayer(player, targetPlayerName);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null)
            return TextCommandResult.Error("Player must have a religion");

        playerData.Favor = amount;

        var targetName = targetPlayerName != null ? $" for {targetPlayer?.PlayerName}" : "";
        return TextCommandResult.Success($"Favor set to {amount:N0}{targetName}");
    }

    /// <summary>
    ///     Adds favor (Admin only)
    /// </summary>
    internal TextCommandResult OnAddFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var amount = (int)args[0];
        var targetPlayerName = (string)args[1];

        // Validate amount
        if (amount <= 0) return TextCommandResult.Error("Amount must be greater than 0.");
        if (amount > 999999) return TextCommandResult.Error("Amount cannot exceed 999,999.");

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = ResolveTargetPlayer(player, targetPlayerName);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null || targetPlayer is null)
            return TextCommandResult.Error("Player must have a religion");

        var oldFavor = playerData.Favor;
        _playerProgressionDataManager.AddFavor(targetPlayer.PlayerUID, amount);

        var targetName = targetPlayerName != null ? $" for {targetPlayer.PlayerName}" : "";
        return TextCommandResult.Success(
            $"Added {amount:N0} favor{targetName} ({oldFavor:N0} → {playerData.Favor:N0})");
    }

    /// <summary>
    ///     Removes favor (Admin only)
    /// </summary>
    internal TextCommandResult OnRemoveFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var amount = (int)args[0];
        var targetPlayerName = (string)args[1];

        // Validate amount
        if (amount <= 0) return TextCommandResult.Error("Amount must be greater than 0.");
        if (amount > 999999) return TextCommandResult.Error("Amount cannot exceed 999,999.");

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = ResolveTargetPlayer(player, targetPlayerName);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null || targetPlayer is null)
            return TextCommandResult.Error("Player must have a religion");

        var oldFavor = playerData.Favor;
        _playerProgressionDataManager.RemoveFavor(targetPlayer.PlayerUID, amount);
        var actualRemoved = oldFavor - playerData.Favor;

        var targetName = targetPlayerName != null ? $" for {targetPlayer.PlayerName}" : "";
        return TextCommandResult.Success(
            $"Removed {actualRemoved:N0} favor{targetName} ({oldFavor:N0} → {playerData.Favor:N0})");
    }

    /// <summary>
    ///     Resets favor to 0 (Admin only)
    /// </summary>
    internal TextCommandResult OnResetFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var targetPlayerName = (string)args[0];

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = ResolveTargetPlayer(player, targetPlayerName);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null)
            return TextCommandResult.Error("Player must have a religion");

        var oldFavor = playerData.Favor;
        playerData.Favor = 0;

        var targetName = targetPlayerName != null ? $" for {targetPlayer?.PlayerName}" : "";
        return TextCommandResult.Success($"Favor reset to 0{targetName} (was {oldFavor:N0})");
    }

    /// <summary>
    ///     Sets favor to maximum (Admin only)
    /// </summary>
    internal TextCommandResult OnMaxFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var targetPlayerName = (string)args[0];

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = ResolveTargetPlayer(player, targetPlayerName);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null)
            return TextCommandResult.Error("Player must have a religion");

        var oldFavor = playerData.Favor;
        playerData.Favor = 99999;

        var targetName = targetPlayerName != null ? $" for {targetPlayer?.PlayerName}" : "";
        return TextCommandResult.Success($"Favor set to maximum: 99,999{targetName} (was {oldFavor:N0})");
    }

    /// <summary>
    ///     Sets total favor earned and updates devotion rank (Admin only)
    /// </summary>
    internal TextCommandResult OnSetTotalFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var amount = (int)args[0];

        // Validate amount
        if (amount < 0) return TextCommandResult.Error("Total favor earned cannot be negative.");

        if (amount > 999999) return TextCommandResult.Error("Total favor earned cannot exceed 999,999.");

        var targetPlayerArg = (string)args[1];

        // Handle targeting another player
        if (targetPlayerArg != null)
        {
            var targetPlayer = _sapi.World.AllPlayers
                .FirstOrDefault(p => string.Equals(p.PlayerName, targetPlayerArg, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer is null)
                return TextCommandResult.Error($"Cannot find player with name '{targetPlayerArg}'");

            var serverPlayer = targetPlayer as IServerPlayer;
            if (serverPlayer is null)
                return TextCommandResult.Error("Target player is not a server player");

            var (targetProgressionData, _, targetErrorResult) = ValidatePlayerHasDeity(serverPlayer);
            if (targetErrorResult is { Status: EnumCommandStatus.Error })
                return targetErrorResult;

            if (targetProgressionData is null)
                return TextCommandResult.Error("Target must have a religion");

            var oldTotal = targetProgressionData.TotalFavorEarned;
            var oldRank = targetProgressionData.FavorRank;

            targetProgressionData.TotalFavorEarned = amount;

            return TextCommandResult.Success(FormatTotalFavorResult(targetProgressionData, amount, oldTotal, oldRank));
        }

        // Handle setting own favor
        var (religionData, _, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (religionData is null)
            return TextCommandResult.Error("Player must have a religion");

        var callerOldTotal = religionData.TotalFavorEarned;
        var callerOldRank = religionData.FavorRank;

        religionData.TotalFavorEarned = amount;

        return TextCommandResult.Success(FormatTotalFavorResult(religionData, amount, callerOldTotal, callerOldRank));
    }

    #endregion
}