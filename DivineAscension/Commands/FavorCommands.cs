using System;
using System.Linq;
using System.Text;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
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
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;
    private readonly IPlayerMessengerService _messenger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FavorCommands(
        ICoreServerAPI sapi,
        IPlayerProgressionDataManager playerReligionDataManager,
        IReligionManager religionManager,
        IPlayerMessengerService messengerService)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _playerProgressionDataManager = playerReligionDataManager ??
                                        throw new ArgumentNullException(nameof(playerReligionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _messenger = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
    }

    /// <summary>
    ///     Registers all favor-related commands
    /// </summary>
    public void RegisterCommands()
    {
        // Main /favor command with subcommands
        _sapi.ChatCommands.Create("favor")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_DESC))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnCheckFavor) // Default behavior: show current favor
            .BeginSubCommand("get")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_GET_DESC))
            .HandleWith(OnCheckFavor)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_INFO_DESC))
            .HandleWith(OnFavorInfo)
            .EndSubCommand()
            .BeginSubCommand("stats")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_STATS_DESC))
            .HandleWith(OnFavorStats)
            .EndSubCommand()
            .BeginSubCommand("ranks")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_RANKS_DESC))
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnListRanks)
            .EndSubCommand()
            .BeginSubCommand("set")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SET_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnSetFavor)
            .EndSubCommand()
            .BeginSubCommand("add")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ADD_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnAddFavor)
            .EndSubCommand()
            .BeginSubCommand("remove")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_REMOVE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnRemoveFavor)
            .EndSubCommand()
            .BeginSubCommand("reset")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_RESET_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnResetFavor)
            .EndSubCommand()
            .BeginSubCommand("max")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_MAX_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnMaxFavor)
            .EndSubCommand()
            .BeginSubCommand("settotal")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SETTOTAL_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Int("amount"), _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnSetTotalFavor)
            .EndSubCommand()
            .BeginSubCommand("resetcooldown")
            .WithDescription("Resets the prayer cooldown for a player")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnResetCooldown)
            .EndSubCommand();

        _sapi.Logger.Notification("[DivineAscension] Favor commands registered");
    }

    #region Helper Methods

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
    private string FormatTotalFavorResult(string playerUID, PlayerProgressionData playerData, int newAmount, int oldTotal,
        FavorRank oldRank)
    {
        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_TOTAL_SET,
            newAmount.ToString("N0"), oldTotal.ToString("N0")));

        var newRank = _playerProgressionDataManager.GetPlayerFavorRank(playerUID);
        if (oldRank != newRank)
            sb.Append(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_RANK_UPDATE,
                oldRank.ToLocalizedString(), newRank.ToLocalizedString()));
        else
            sb.Append(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_RANK_UNCHANGED,
                newRank.ToLocalizedString()));

        return sb.ToString();
    }

    #endregion

    #region Information Commands (Privilege.chat)

    /// <summary>
    ///     Shows current favor amount - default command and /favor get
    /// </summary>
    internal TextCommandResult OnCheckFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var (playerProgressionData, religionName, errorResult) =
            CommandHelpers.ValidatePlayerHasDeity(player, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        var deityName = deity.ToString();

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_CHECK, playerProgressionData!.Favor,
                deityName, _playerProgressionDataManager.GetPlayerFavorRank(player.PlayerUID).ToLocalizedString())
        );
    }

    /// <summary>
    ///     Shows detailed favor information and rank progression
    /// </summary>
    internal TextCommandResult OnFavorInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var (playerProgressionData, religionName, errorResult) =
            CommandHelpers.ValidatePlayerHasDeity(player, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deityDomain = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        var domainLocalized = deityDomain.ToLocalizedString();

        // Get current rank based on total favor
        var currentRank = GetCurrentFavorRank(playerProgressionData!.TotalFavorEarned);
        var currentRankName = RankRequirements.GetFavorRankName(currentRank);

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_HEADER_INFO));
        sb.AppendLine($"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_DOMAIN)} {domainLocalized}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_CURRENT)} {playerProgressionData.Favor:N0}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_TOTAL_EARNED)} {playerProgressionData.TotalFavorEarned:N0}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_CURRENT_RANK)} {currentRankName}");

        // Calculate next rank
        if (currentRank < 4) // Not at max rank
        {
            var nextRank = currentRank + 1;
            var nextRankName = RankRequirements.GetFavorRankName(nextRank);
            var nextThreshold = RankRequirements.GetRequiredFavorForNextRank(currentRank);

            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_NEXT_RANK)} {nextRankName} ({nextThreshold:N0} total favor required)");

            var remaining = nextThreshold - playerProgressionData.TotalFavorEarned;
            var progress = (float)playerProgressionData.TotalFavorEarned / nextThreshold * 100f;
            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_PROGRESS)} {progress:F1}% ({remaining:N0} favor needed)");
        }
        else
        {
            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_NEXT_RANK)} {LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_MAX_RANK)}");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Shows comprehensive favor statistics
    /// </summary>
    internal TextCommandResult OnFavorStats(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var (playerProgressionData, religionName, errorResult) =
            CommandHelpers.ValidatePlayerHasDeity(player, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error }) return errorResult;

        var deity = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        var deityName = deity.ToLocalizedString();

        // Get current rank based on total favor
        var currentRank = GetCurrentFavorRank(playerProgressionData!.TotalFavorEarned);
        var currentRankName = RankRequirements.GetFavorRankName(currentRank);

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_HEADER_STATS));
        sb.AppendLine($"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_DOMAIN)} {deityName}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_CURRENT)} {playerProgressionData.Favor:N0}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_TOTAL_EARNED)} {playerProgressionData.TotalFavorEarned:N0}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_DEVOTION_RANK)} {currentRankName}");

        // Calculate next rank
        if (currentRank < 4) // Not at max rank
        {
            var nextRank = currentRank + 1;
            var nextRankName = RankRequirements.GetFavorRankName(nextRank);
            var nextThreshold = RankRequirements.GetRequiredFavorForNextRank(currentRank);
            var remaining = nextThreshold - playerProgressionData.TotalFavorEarned;

            sb.AppendLine();
            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_NEXT_RANK)} {nextRankName}");
            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_FAVOR_NEEDED)} {remaining:N0}");
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
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_HEADER_RANKS));

        // List all ranks with their requirements
        for (var rank = 0; rank <= 4; rank++)
        {
            var rankName = RankRequirements.GetFavorRankName(rank);
            var totalRequired = rank == 0 ? 0 : RankRequirements.GetRequiredFavorForNextRank(rank - 1);
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_FORMAT_RANK_REQUIREMENT, rankName,
                totalRequired.ToString("N0")));
        }

        sb.AppendLine();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_LABEL_UNLOCK_MESSAGE));

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
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var amount = (int)args[0];
        var targetPlayerName = (string)args[1];

        // Validate amount
        if (amount < 0)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_NEGATIVE_AMOUNT));
        if (amount > 999999)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_EXCEEDS_MAX));

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = CommandHelpers.ResolveTargetPlayer(player, targetPlayerName,
            _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION));

        playerData.Favor = amount;

        var targetName = targetPlayerName != null ? $" for {targetPlayer?.PlayerName}" : "";
        return TextCommandResult.Success(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_SET,
            amount.ToString("N0"), targetName));
    }

    /// <summary>
    ///     Adds favor (Admin only)
    /// </summary>
    internal TextCommandResult OnAddFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var amount = (int)args[0];
        var targetPlayerName = (string)args[1];

        // Validate amount
        if (amount <= 0)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_AMOUNT_TOO_SMALL));
        if (amount > 999999)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_EXCEEDS_MAX));

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = CommandHelpers.ResolveTargetPlayer(player, targetPlayerName,
            _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null || targetPlayer is null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION));

        var oldFavor = playerData.Favor;
        _playerProgressionDataManager.AddFavor(targetPlayer.PlayerUID, amount);

        var targetName = targetPlayerName != null ? $" for {targetPlayer.PlayerName}" : "";
        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_ADD, amount.ToString("N0"), targetName,
                oldFavor.ToString("N0"), playerData.Favor.ToString("N0")));
    }

    /// <summary>
    ///     Removes favor (Admin only)
    /// </summary>
    internal TextCommandResult OnRemoveFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var amount = (int)args[0];
        var targetPlayerName = (string)args[1];

        // Validate amount
        if (amount <= 0)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_AMOUNT_TOO_SMALL));
        if (amount > 999999)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_EXCEEDS_MAX));

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = CommandHelpers.ResolveTargetPlayer(player, targetPlayerName,
            _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null || targetPlayer is null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION));

        var oldFavor = playerData.Favor;
        _playerProgressionDataManager.RemoveFavor(targetPlayer.PlayerUID, amount);
        var actualRemoved = oldFavor - playerData.Favor;

        var targetName = targetPlayerName != null ? $" for {targetPlayer.PlayerName}" : "";
        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_REMOVE, actualRemoved.ToString("N0"),
                targetName, oldFavor.ToString("N0"), playerData.Favor.ToString("N0")));
    }

    /// <summary>
    ///     Resets favor to 0 (Admin only)
    /// </summary>
    internal TextCommandResult OnResetFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var targetPlayerName = (string)args[0];

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = CommandHelpers.ResolveTargetPlayer(player, targetPlayerName,
            _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION));

        var oldFavor = playerData.Favor;
        playerData.Favor = 0;

        var targetName = targetPlayerName != null ? $" for {targetPlayer?.PlayerName}" : "";
        return TextCommandResult.Success(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_RESET,
            targetName, oldFavor.ToString("N0")));
    }

    /// <summary>
    ///     Sets favor to maximum (Admin only)
    /// </summary>
    internal TextCommandResult OnMaxFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var targetPlayerName = (string)args[0];

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = CommandHelpers.ResolveTargetPlayer(player, targetPlayerName,
            _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (playerData is null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION));

        var oldFavor = playerData.Favor;
        playerData.Favor = 99999;

        var targetName = targetPlayerName != null ? $" for {targetPlayer?.PlayerName}" : "";
        return TextCommandResult.Success(LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_SUCCESS_MAX,
            targetName, oldFavor.ToString("N0")));
    }

    /// <summary>
    ///     Resets the prayer cooldown for a player (Admin only)
    /// </summary>
    internal TextCommandResult OnResetCooldown(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var targetPlayerName = (string)args[0];

        // Resolve target player
        var (targetPlayer, playerData, errorResult) = CommandHelpers.ResolveTargetPlayer(player, targetPlayerName,
            _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // Get the actual player UID to reset
        var targetUID = targetPlayer?.PlayerUID ?? player.PlayerUID;
        var displayName = targetPlayerName ?? player.PlayerName;

        // Reset cooldown by setting expiry to 0
        _playerProgressionDataManager.SetPrayerCooldownExpiry(targetUID, 0);

        return TextCommandResult.Success($"Prayer cooldown reset for {displayName}. They can pray again immediately.");
    }

    /// <summary>
    ///     Sets total favor earned and updates devotion rank (Admin only)
    /// </summary>
    internal TextCommandResult OnSetTotalFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_BE_PLAYER));

        var amount = (int)args[0];

        // Validate amount
        if (amount < 0)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_TOTAL_NEGATIVE));

        if (amount > 999999)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_TOTAL_EXCEEDS_MAX));

        var targetPlayerArg = (string)args[1];

        // Handle targeting another player
        if (targetPlayerArg != null)
        {
            var targetPlayer = _sapi.World.AllPlayers
                .FirstOrDefault(p => string.Equals(p.PlayerName, targetPlayerArg, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer is null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_PLAYER_NOT_FOUND,
                        targetPlayerArg));

            var serverPlayer = targetPlayer as IServerPlayer;
            if (serverPlayer is null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_NOT_SERVER_PLAYER));

            var (targetProgressionData, _, targetErrorResult) =
                CommandHelpers.ValidatePlayerHasDeity(serverPlayer, _playerProgressionDataManager, _religionManager);
            if (targetErrorResult is { Status: EnumCommandStatus.Error })
                return targetErrorResult;

            if (targetProgressionData is null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_TARGET_NO_RELIGION));

            var oldTotal = targetProgressionData.TotalFavorEarned;
            var oldRank = _playerProgressionDataManager.GetPlayerFavorRank(serverPlayer.PlayerUID);

            targetProgressionData.TotalFavorEarned = amount;

            return TextCommandResult.Success(FormatTotalFavorResult(serverPlayer.PlayerUID, targetProgressionData, amount, oldTotal, oldRank));
        }

        // Handle setting own favor
        var (religionData, _, errorResult) =
            CommandHelpers.ValidatePlayerHasDeity(player, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        if (religionData is null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_FAVOR_ERROR_MUST_HAVE_RELIGION));

        var callerOldTotal = religionData.TotalFavorEarned;
        var callerOldRank = _playerProgressionDataManager.GetPlayerFavorRank(player.PlayerUID);

        religionData.TotalFavorEarned = amount;

        return TextCommandResult.Success(FormatTotalFavorResult(player.PlayerUID, religionData, amount, callerOldTotal, callerOldRank));
    }

    #endregion
}