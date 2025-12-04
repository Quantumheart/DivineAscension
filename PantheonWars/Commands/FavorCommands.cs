using System;
using System.Text;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Commands;

/// <summary>
///     Chat commands for favor management and testing
/// </summary>
public class FavorCommands
{
    private readonly IDeityRegistry _deityRegistry;
    private readonly IPlayerReligionDataManager _playerReligionDataManager;
    private readonly ICoreServerAPI _sapi;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FavorCommands(
        ICoreServerAPI sapi,
        IDeityRegistry deityRegistry,
        IPlayerReligionDataManager playerReligionDataManager)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _deityRegistry = deityRegistry ?? throw new ArgumentNullException(nameof(deityRegistry));
        _playerReligionDataManager = playerReligionDataManager ??
                                     throw new ArgumentNullException(nameof(playerReligionDataManager));
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
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnSetFavor)
            .EndSubCommand()
            .BeginSubCommand("add")
            .WithDescription("Add favor (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnAddFavor)
            .EndSubCommand()
            .BeginSubCommand("remove")
            .WithDescription("Remove favor (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnRemoveFavor)
            .EndSubCommand()
            .BeginSubCommand("reset")
            .WithDescription("Reset favor to 0 (Admin only)")
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnResetFavor)
            .EndSubCommand()
            .BeginSubCommand("max")
            .WithDescription("Set favor to maximum (Admin only)")
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnMaxFavor)
            .EndSubCommand()
            .BeginSubCommand("settotal")
            .WithDescription("Set total favor earned and update rank (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnSetTotalFavor)
            .EndSubCommand();

        _sapi.Logger.Notification("[PantheonWars] Favor commands registered");
    }

    #region Helper Methods

    /// <summary>
    ///     Get player's religion data and validate they have a deity
    /// </summary>
    internal (PlayerReligionData? religionData, string? religionName, TextCommandResult? errorResult)
        ValidatePlayerHasDeity(IServerPlayer player)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None)
            return (null, null, TextCommandResult.Error("You are not in a religion or do not have an active deity."));

        // Get religion name if in a religion
        string? religionName = null;
        if (!string.IsNullOrEmpty(religionData.ReligionUID))
            religionName = religionData.ReligionUID; // This will be improved when we have access to ReligionManager

        return (religionData, religionName, null);
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

    #endregion

    #region Information Commands (Privilege.chat)

    /// <summary>
    ///     Shows current favor amount - default command and /favor get
    /// </summary>
    internal TextCommandResult OnCheckFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _deityRegistry.GetDeity(religionData!.ActiveDeity);
        var deityName = deity?.Name ?? religionData.ActiveDeity.ToString();

        return TextCommandResult.Success(
            $"You have {religionData.Favor} favor with {deityName} (Rank: {religionData.FavorRank})"
        );
    }

    /// <summary>
    ///     Shows detailed favor information and rank progression
    /// </summary>
    internal TextCommandResult OnFavorInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _deityRegistry.GetDeity(religionData!.ActiveDeity);
        var deityName = deity?.Name ?? religionData.ActiveDeity.ToString();

        // Get current rank based on total favor
        var currentRank = GetCurrentFavorRank(religionData.TotalFavorEarned);
        var currentRankName = RankRequirements.GetFavorRankName(currentRank);

        var sb = new StringBuilder();
        sb.AppendLine("=== Divine Favor ===");
        sb.AppendLine($"Deity: {deityName}");
        sb.AppendLine($"Current Favor: {religionData.Favor:N0}");
        sb.AppendLine($"Total Favor Earned: {religionData.TotalFavorEarned:N0}");
        sb.AppendLine($"Current Rank: {currentRankName}");

        // Calculate next rank
        if (currentRank < 4) // Not at max rank
        {
            var nextRank = currentRank + 1;
            var nextRankName = RankRequirements.GetFavorRankName(nextRank);
            var nextThreshold = RankRequirements.GetRequiredFavorForNextRank(currentRank);

            sb.AppendLine($"Next Rank: {nextRankName} ({nextThreshold:N0} total favor required)");

            var remaining = nextThreshold - religionData.TotalFavorEarned;
            var progress = (float)religionData.TotalFavorEarned / nextThreshold * 100f;
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

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _deityRegistry.GetDeity(religionData!.ActiveDeity);
        var deityName = deity?.Name ?? religionData.ActiveDeity.ToString();

        // Get current rank based on total favor
        var currentRank = GetCurrentFavorRank(religionData.TotalFavorEarned);
        var currentRankName = RankRequirements.GetFavorRankName(currentRank);

        var sb = new StringBuilder();
        sb.AppendLine("=== Divine Statistics ===");
        sb.AppendLine($"Deity: {deityName}");
        sb.AppendLine($"Current Favor: {religionData.Favor:N0}");
        sb.AppendLine($"Total Favor Earned: {religionData.TotalFavorEarned:N0}");
        sb.AppendLine($"Devotion Rank: {currentRankName}");
        sb.AppendLine($"Kill Count: {religionData.KillCount}");

        if (religionData.LastReligionSwitch.HasValue)
        {
            var daysServed = (DateTime.UtcNow - religionData.LastReligionSwitch.Value).Days;
            sb.AppendLine($"Days Served: {daysServed}");
            sb.AppendLine($"Join Date: {religionData.LastReligionSwitch.Value:yyyy-MM-dd}");
        }

        // Calculate next rank
        if (currentRank < 4) // Not at max rank
        {
            var nextRank = currentRank + 1;
            var nextRankName = RankRequirements.GetFavorRankName(nextRank);
            var nextThreshold = RankRequirements.GetRequiredFavorForNextRank(currentRank);
            var remaining = nextThreshold - religionData.TotalFavorEarned;

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

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var amount = (int)args[0];

        // Validate amount
        if (amount < 0) return TextCommandResult.Error("Favor amount cannot be negative.");

        if (amount > 999999) return TextCommandResult.Error("Favor amount cannot exceed 999,999.");

        religionData.Favor = amount;

        return TextCommandResult.Success($"Favor set to {amount:N0}");
    }

    /// <summary>
    ///     Adds favor (Admin only)
    /// </summary>
    internal TextCommandResult OnAddFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var amount = (int)args[0];

        // Validate amount
        if (amount <= 0) return TextCommandResult.Error("Amount must be greater than 0.");

        if (amount > 999999) return TextCommandResult.Error("Amount cannot exceed 999,999.");

        var oldFavor = religionData.Favor;
        _playerReligionDataManager.AddFavor(player.PlayerUID, amount);

        return TextCommandResult.Success($"Added {amount:N0} favor ({oldFavor:N0} → {religionData.Favor:N0})");
    }

    /// <summary>
    ///     Removes favor (Admin only)
    /// </summary>
    internal TextCommandResult OnRemoveFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var amount = (int)args[0];

        // Validate amount
        if (amount <= 0) return TextCommandResult.Error("Amount must be greater than 0.");

        if (amount > 999999) return TextCommandResult.Error("Amount cannot exceed 999,999.");

        var oldFavor = religionData.Favor;
        _playerReligionDataManager.RemoveFavor(player.PlayerUID, amount);
        var actualRemoved = oldFavor - religionData.Favor;

        return TextCommandResult.Success(
            $"Removed {actualRemoved:N0} favor ({oldFavor:N0} → {religionData.Favor:N0})");
    }

    /// <summary>
    ///     Resets favor to 0 (Admin only)
    /// </summary>
    internal TextCommandResult OnResetFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var oldFavor = religionData.Favor;
        religionData.Favor = 0;

        return TextCommandResult.Success($"Favor reset to 0 (was {oldFavor:N0})");
    }

    /// <summary>
    ///     Sets favor to maximum (Admin only)
    /// </summary>
    internal TextCommandResult OnMaxFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var oldFavor = religionData.Favor;
        religionData.Favor = 99999;

        return TextCommandResult.Success($"Favor set to maximum: 99,999 (was {oldFavor:N0})");
    }

    /// <summary>
    ///     Sets total favor earned and updates devotion rank (Admin only)
    /// </summary>
    internal TextCommandResult OnSetTotalFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var (religionData, religionName, errorResult) = ValidatePlayerHasDeity(player);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var amount = (int)args[0];

        // Validate amount
        if (amount < 0) return TextCommandResult.Error("Total favor earned cannot be negative.");

        if (amount > 999999) return TextCommandResult.Error("Total favor earned cannot exceed 999,999.");

        var oldTotal = religionData.TotalFavorEarned;
        var oldRank = religionData.FavorRank;

        religionData.TotalFavorEarned = amount;
        religionData.UpdateFavorRank();

        var newRank = religionData.FavorRank;

        var sb = new StringBuilder();
        sb.AppendLine($"Total favor earned set to {amount:N0} (was {oldTotal:N0})");

        if (oldRank != newRank)
            sb.AppendLine($"Rank updated: {oldRank} → {newRank}");
        else
            sb.AppendLine($"Rank unchanged: {newRank}");

        return TextCommandResult.Success(sb.ToString());
    }

    #endregion
}