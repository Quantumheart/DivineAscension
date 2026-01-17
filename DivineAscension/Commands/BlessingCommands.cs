using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Commands for managing blessings
/// </summary>
public class BlessingCommands(
    ICoreServerAPI? sapi,
    IBlessingRegistry? blessingRegistry,
    IPlayerProgressionDataManager? playerReligionDataManager,
    IReligionManager? religionManager,
    IBlessingEffectSystem? blessingEffectSystem,
    IServerNetworkChannel? serverChannel)
{
    private readonly IBlessingEffectSystem _blessingEffectSystem =
        blessingEffectSystem ?? throw new ArgumentNullException($"{nameof(blessingEffectSystem)}");

    private readonly IBlessingRegistry _blessingRegistry =
        blessingRegistry ?? throw new ArgumentNullException($"{nameof(blessingRegistry)}");

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException($"{nameof(playerReligionDataManager)}");

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException($"{nameof(religionManager)}");

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException($"{nameof(sapi)}");

    private readonly IServerNetworkChannel _serverChannel =
        serverChannel ?? throw new ArgumentNullException($"{nameof(serverChannel)}");

    /// <summary>
    ///     Registers all blessing commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create(BlessingCommandConstants.CommandName)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_DESC))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand(BlessingCommandConstants.SubCommandList)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_LIST_DESC))
            .HandleWith(OnList)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandPlayer)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_PLAYER_DESC))
            .HandleWith(OnPlayer)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandReligion)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_RELIGION_DESC))
            .HandleWith(OnReligion)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandInfo)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_INFO_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord(ParameterConstants.ParamBlessingId))
            .HandleWith(OnInfo)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandTree)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_TREE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word(ParameterConstants.ParamType))
            .HandleWith(OnTree)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandUnlock)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_UNLOCK_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word(ParameterConstants.ParamBlessingId))
            .HandleWith(OnUnlock)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandActive)
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_ACTIVE_DESC))
            .HandleWith(OnActive)
            .EndSubCommand()
            .BeginSubCommand("admin")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSINGS_ADMIN_DESC))
            .RequiresPrivilege(Privilege.root)
            .BeginSubCommand("unlock")
            .WithDescription("Force unlock a blessing for a player (bypasses requirements)")
            .WithArgs(_sapi.ChatCommands.Parsers.Word(ParameterConstants.ParamBlessingId),
                _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminUnlock)
            .EndSubCommand()
            .BeginSubCommand("lock")
            .WithDescription("Remove a specific unlocked blessing from a player")
            .WithArgs(_sapi.ChatCommands.Parsers.Word(ParameterConstants.ParamBlessingId),
                _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminLock)
            .EndSubCommand()
            .BeginSubCommand("reset")
            .WithDescription("Clear all unlocked blessings from a player")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminReset)
            .EndSubCommand()
            .BeginSubCommand("unlockall")
            .WithDescription("Unlock all available blessings for a player")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminUnlockAll)
            .EndSubCommand()
            .EndSubCommand();

        _sapi.Logger.Notification(LogMessageConstants.LogBlessingCommandsRegistered);
    }

    /// <summary>
    ///     /blessings list - Lists all available blessings for player's deity
    /// </summary>
    internal TextCommandResult OnList(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_PLAYER_NOT_FOUND));

        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        var playerDeity = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        if (playerDeity == DeityDomain.None)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_IN_RELIGION));

        var playerBlessings = _blessingRegistry.GetBlessingsForDeity(playerDeity, BlessingKind.Player);
        var religionBlessings = _blessingRegistry.GetBlessingsForDeity(playerDeity, BlessingKind.Religion);

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_FOR_DEITY,
            playerDeity.ToLocalizedString()));
        sb.AppendLine();

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_PLAYER));
        foreach (var blessing in playerBlessings)
        {
            var status = playerData.IsBlessingUnlocked(blessing.BlessingId)
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_UNLOCKED, blessing.Name)
                : LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_LOCKED, blessing.Name);
            var requiredRank = (FavorRank)blessing.RequiredFavorRank;
            sb.AppendLine(status);
            sb.AppendLine(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_ID, blessing.BlessingId));
            sb.AppendLine(
                $"  {LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_FAVOR_RANK)} {requiredRank.ToLocalizedString()}");
            sb.AppendLine($"  {blessing.Description}");
            sb.AppendLine();
        }

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_RELIGION));
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        foreach (var blessing in religionBlessings)
        {
            var unlocked = religion?.UnlockedBlessings.TryGetValue(blessing.BlessingId, out var u) == true && u;
            var status = unlocked
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_UNLOCKED, blessing.Name)
                : LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_LOCKED, blessing.Name);
            var requiredRank = (PrestigeRank)blessing.RequiredPrestigeRank;
            sb.AppendLine(status);
            sb.AppendLine(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_ID, blessing.BlessingId));
            sb.AppendLine(
                $"  {LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_PRESTIGE_RANK)} {requiredRank.ToLocalizedString()}");
            sb.AppendLine($"  {blessing.Description}");
            sb.AppendLine();
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     /blessings player - Shows unlocked player blessings
    /// </summary>
    internal TextCommandResult OnPlayer(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_PLAYER_NOT_FOUND));

        var (playerBlessings, _) = _blessingEffectSystem.GetActiveBlessings(player.PlayerUID);

        if (playerBlessings.Count == 0)
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_INFO_NO_PLAYER_UNLOCKED));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_UNLOCKED_PLAYER,
            playerBlessings.Count));
        sb.AppendLine();

        foreach (var blessing in playerBlessings)
        {
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_NAME, blessing.Name,
                blessing.Category));
            sb.AppendLine($"  {blessing.Description}");

            if (blessing.StatModifiers.Count > 0)
            {
                sb.AppendLine(
                    $"  {LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_STAT_MODIFIERS)}");
                foreach (var mod in blessing.StatModifiers)
                    sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_STAT_MODIFIER,
                        FormatStatName(mod.Key), mod.Value * 100));
            }

            sb.AppendLine();
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     /blessings religion - Shows religion's unlocked blessings
    /// </summary>
    internal TextCommandResult OnReligion(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_PLAYER_NOT_FOUND));

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_RELIGION));

        var (_, religionBlessings) = _blessingEffectSystem.GetActiveBlessings(player.PlayerUID);

        if (religionBlessings.Count == 0)
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_INFO_NO_RELIGION_UNLOCKED));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_RELIGION_WITH_NAME,
            religion?.ReligionName ?? "Unknown", religionBlessings.Count));
        sb.AppendLine();

        foreach (var blessing in religionBlessings)
        {
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_NAME, blessing.Name,
                blessing.Category));
            sb.AppendLine($"  {blessing.Description}");

            if (blessing.StatModifiers.Count > 0)
            {
                sb.AppendLine(
                    $"  {LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_STAT_MODIFIERS)}");
                foreach (var mod in blessing.StatModifiers)
                    sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_STAT_MODIFIER,
                        FormatStatName(mod.Key), mod.Value * 100));
            }

            sb.AppendLine();
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     /blessings info <blessingid /> - Shows detailed blessing information
    /// </summary>
    private TextCommandResult OnInfo(TextCommandCallingArgs args)
    {
        var blessingId = args[0] as string;
        return GetInfo(blessingId);
    }

    /// <summary>
    ///     Core logic for getting blessing info - extracted for testability
    /// </summary>
    internal TextCommandResult GetInfo(string? blessingId)
    {
        if (string.IsNullOrEmpty(blessingId))
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_USAGE_INFO));

        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_FOUND, blessingId));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_BLESSING_INFO,
            blessing.Name));
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_ID)} {blessing.BlessingId}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_DEITY)} {blessing.Domain.ToLocalizedString()}");
        sb.AppendLine($"{LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_TYPE)} {blessing.Kind}");
        sb.AppendLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_CATEGORY)} {blessing.Category}");
        sb.AppendLine();
        sb.AppendLine($"{blessing.Description}");
        sb.AppendLine();

        if (blessing.Kind == BlessingKind.Player)
        {
            var requiredRank = (FavorRank)blessing.RequiredFavorRank;
            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_FAVOR_RANK)} {requiredRank.ToLocalizedString()}");
        }
        else
        {
            var requiredRank = (PrestigeRank)blessing.RequiredPrestigeRank;
            sb.AppendLine(
                $"{LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_PRESTIGE_RANK)} {requiredRank.ToLocalizedString()}");
        }

        if (blessing.PrerequisiteBlessings is { Count: > 0 })
        {
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_PREREQUISITES));
            foreach (var prereqId in blessing.PrerequisiteBlessings)
            {
                var prereq = _blessingRegistry.GetBlessing(prereqId);
                var prereqName = prereq?.Name ?? prereqId;
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_PREREQUISITE,
                    prereqName));
            }
        }

        if (blessing.StatModifiers.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_STAT_MODIFIERS));
            foreach (var mod in blessing.StatModifiers)
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_STAT_MODIFIER,
                    FormatStatName(mod.Key), mod.Value * 100));
        }

        if (blessing.SpecialEffects is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_SPECIAL_EFFECTS));
            foreach (var effect in blessing.SpecialEffects)
                sb.AppendLine($"  - {effect}");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     /blessings tree [player/religion] - Displays blessing tree
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Arg parsing difficult")]
    private TextCommandResult OnTree(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var type = args[0] as string;
        return GetTree(player.PlayerUID, type);
    }

    /// <summary>
    ///     Core logic for getting blessing tree - extracted for testability
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Arg parsing difficult")]
    private TextCommandResult GetTree(string playerUid, string? type)
    {
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUid);
        var playerDeity = _religionManager.GetPlayerActiveDeityDomain(playerUid);
        if (playerDeity == DeityDomain.None)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_MUST_JOIN_FOR_TREE));

        type = type ?? "player";
        type = type.ToLower();

        var blessingKind = type == "religion"
            ? BlessingKind.Religion
            : BlessingKind.Player;

        var blessings = _blessingRegistry.GetBlessingsForDeity(playerDeity, blessingKind);

        var religion = _religionManager.GetPlayerReligion(playerUid);


        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_BLESSING_TREE,
            playerDeity.ToLocalizedString(), blessingKind));
        sb.AppendLine();

        // Group by rank
        if (blessingKind == BlessingKind.Player)
            foreach (FavorRank rank in Enum.GetValues(typeof(FavorRank)))
            {
                var rankBlessings = blessings
                    .Where(p => p.RequiredFavorRank == (int)rank)
                    .ToList();

                if (rankBlessings.Count == 0)
                    continue;

                sb.AppendLine($"=== {rank.ToLocalizedString()} ===");
                foreach (var blessing in rankBlessings)
                {
                    var unlocked = playerData.IsBlessingUnlocked(blessing.BlessingId);
                    var status = unlocked
                        ? "✓"
                        : "✗";
                    sb.AppendLine($"{status} {blessing.Name}");

                    if (blessing.PrerequisiteBlessings is { Count: > 0 })
                    {
                        sb.Append(
                            $"  {LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_PREREQUISITES)} ");
                        var prereqNames = blessing.PrerequisiteBlessings
                            .Select(id =>
                            {
                                var p = _blessingRegistry.GetBlessing(id);
                                return p?.Name ?? id;
                            });
                        sb.AppendLine(string.Join(", ", prereqNames));
                    }
                }

                sb.AppendLine();
            }
        else // Religion
            foreach (PrestigeRank rank in Enum.GetValues(typeof(PrestigeRank)))
            {
                var rankBlessings = blessings
                    .Where(p => p.RequiredPrestigeRank == (int)rank)
                    .ToList();

                if (rankBlessings.Count == 0)
                    continue;

                sb.AppendLine($"=== {rank.ToLocalizedString()} ===");
                foreach (var blessing in rankBlessings)
                {
                    var unlocked = religion?.UnlockedBlessings.TryGetValue(blessing.BlessingId, out var u) == true && u;
                    var status = unlocked
                        ? "✓"
                        : "✗";
                    sb.AppendLine($"{status} {blessing.Name}");

                    if (blessing.PrerequisiteBlessings is { Count: > 0 })
                    {
                        sb.Append(
                            $"  {LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_PREREQUISITES)} ");
                        var prereqNames = blessing.PrerequisiteBlessings
                            .Select(id =>
                            {
                                var p = _blessingRegistry.GetBlessing(id);
                                return p?.Name ?? id;
                            });
                        sb.AppendLine(string.Join(", ", prereqNames));
                    }
                }

                sb.AppendLine();
            }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     /blessings unlock <blessingid /> - Unlocks a blessing
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Arg parsing difficult")]
    private TextCommandResult OnUnlock(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));
        var blessingId = args[0] as string;
        return Unlock(player.PlayerUID, blessingId);
    }

    /// <summary>
    ///     Core logic for unlocking a blessing - extracted for testability
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Arg parsing difficult")]
    private TextCommandResult Unlock(string playerUid, string? blessingId)
    {
        if (string.IsNullOrEmpty(blessingId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_USAGE_UNLOCK));

        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_FOUND, blessingId));

        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUid);
        var religion = _religionManager.GetPlayerReligion(playerUid);
        var playerFavorRank = _playerProgressionDataManager.GetPlayerFavorRank(playerUid);

        var (canUnlock, reason) = _blessingRegistry.CanUnlockBlessing(playerUid, playerFavorRank, playerData, religion, blessing);
        if (!canUnlock)
            return TextCommandResult.Error(reason);

        // Unlock the blessing
        if (blessing.Kind == BlessingKind.Player)
        {
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_IN_RELIGION));

            var success = _playerProgressionDataManager.UnlockPlayerBlessing(playerUid, blessingId);
            if (!success)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_ALREADY_UNLOCKED));

            _blessingEffectSystem.RefreshPlayerBlessings(playerUid);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(playerUid);

            // Send blessing unlock notification to client for GUI update
            var player = _sapi.World.PlayerByUid(playerUid) as IServerPlayer;
            if (player != null)
            {
                var packet = new BlessingUnlockResponsePacket(true,
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_UNLOCKED, blessing.Name),
                    blessing.BlessingId);
                _serverChannel.SendPacket(packet, player);
            }

            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_UNLOCKED, blessing.Name));
        }

        // Religion blessing
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_IN_RELIGION));

        // Only founder can unlock religion blessings (optional restriction)
        if (!religion.IsFounder(playerUid))
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_FOUNDER));

        religion.UnlockBlessing(blessingId);
        _blessingEffectSystem.RefreshReligionBlessings(religion.ReligionUID);
        // Notify all members
        foreach (var memberUid in religion.MemberUIDs)
        {
            var member = _sapi.World.PlayerByUid(memberUid) as IServerPlayer;
            if (member != null)
            {
                // Send chat notification
                member.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_UNLOCKED, blessing.Name),
                    EnumChatType.Notification
                );

                // Notify player data changed (triggers HUD update)
                _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                // Send blessing unlock packet for GUI update
                var packet = new BlessingUnlockResponsePacket(true,
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_UNLOCKED, blessing.Name),
                    blessing.BlessingId);
                _serverChannel.SendPacket(packet, member);
            }
        }

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_UNLOCKED, blessing.Name));
    }

    /// <summary>
    ///     /blessings active - Shows all active blessings and combined modifiers
    /// </summary>
    internal TextCommandResult OnActive(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_PLAYER_NOT_FOUND));

        var (playerBlessings, religionBlessings) = _blessingEffectSystem.GetActiveBlessings(player.PlayerUID);
        var combinedModifiers = _blessingEffectSystem.GetCombinedStatModifiers(player.PlayerUID);

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_ACTIVE_BLESSINGS));
        sb.AppendLine();

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_TOTAL_PLAYER,
            playerBlessings.Count));
        if (playerBlessings.Count == 0)
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_NONE));
        else
            foreach (var blessing in playerBlessings)
                sb.AppendLine($"  - {blessing.Name}");

        sb.AppendLine();

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_TOTAL_RELIGION,
            religionBlessings.Count));
        if (religionBlessings.Count == 0)
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_FORMAT_NONE));
        else
            foreach (var blessing in religionBlessings)
                sb.AppendLine($"  - {blessing.Name}");

        sb.AppendLine();

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_HEADER_COMBINED_STATS));
        sb.AppendLine(combinedModifiers.Count == 0
            ? LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_LABEL_NO_ACTIVE)
            : _blessingEffectSystem.FormatStatModifiers(combinedModifiers));

        return TextCommandResult.Success(sb.ToString());
    }

    #region Helper Methods

    /// <summary>
    ///     Converts a stat key to its localized display name
    /// </summary>
    private static string FormatStatName(string statKey)
    {
        var localizationKey = statKey switch
        {
            VintageStoryStats.MeleeWeaponsDamage => LocalizationKeys.STAT_MELEE_DAMAGE,
            VintageStoryStats.RangedWeaponsDamage => LocalizationKeys.STAT_RANGED_DAMAGE,
            VintageStoryStats.WalkSpeed => LocalizationKeys.STAT_WALK_SPEED,
            VintageStoryStats.MaxHealthExtraPoints => LocalizationKeys.STAT_MAX_HEALTH,
            VintageStoryStats.MeleeWeaponArmor => LocalizationKeys.STAT_ARMOR,
            VintageStoryStats.ArmorEffectiveness => LocalizationKeys.STAT_ARMOR_EFFECTIVENESS,
            VintageStoryStats.MiningSpeed => LocalizationKeys.STAT_MINING_SPEED,
            VintageStoryStats.MeleeWeaponsSpeed => LocalizationKeys.STAT_ATTACK_SPEED,
            VintageStoryStats.HealingEffectiveness => LocalizationKeys.STAT_HEAL_EFFECTIVENESS,
            VintageStoryStats.HungerRate => LocalizationKeys.STAT_HUNGER_RATE,
            VintageStoryStats.ToolDurability => LocalizationKeys.STAT_TOOL_DURABILITY,
            VintageStoryStats.OreDropRate => LocalizationKeys.STAT_ORE_YIELD,
            VintageStoryStats.ColdResistance => LocalizationKeys.STAT_COLD_RESISTANCE,
            VintageStoryStats.RepairCostReduction => LocalizationKeys.STAT_REPAIR_COST_REDUCTION,
            VintageStoryStats.RepairEfficiency => LocalizationKeys.STAT_REPAIR_EFFICIENCY,
            VintageStoryStats.SmithingCostReduction => LocalizationKeys.STAT_SMITHING_COST_REDUCTION,
            VintageStoryStats.MetalArmorBonus => LocalizationKeys.STAT_METAL_ARMOR_BONUS,
            VintageStoryStats.ArmorDurabilityLoss => LocalizationKeys.STAT_ARMOR_DURABILITY_LOSS,
            VintageStoryStats.ArmorWalkSpeedAffectedness => LocalizationKeys.STAT_ARMOR_WALK_SPEED,
            VintageStoryStats.PotteryBatchCompletionChance => LocalizationKeys.STAT_POTTERY_BATCH_COMPLETION,
            VintageStoryStats.AnimalDrops => LocalizationKeys.STAT_ANIMAL_LOOT_DROPS,
            VintageStoryStats.ForageDropRate => LocalizationKeys.STAT_FORAGE_DROPS,
            VintageStoryStats.RangedWeaponsAccuracy => LocalizationKeys.STAT_RANGED_WEAPONS_SPEED,
            VintageStoryStats.CropYield => LocalizationKeys.STAT_CROP_GROWTH_SPEED,
            VintageStoryStats.StorageVesselCapacity => LocalizationKeys.STAT_WHOLE_VESSEL_CAPACITY,
            _ => null
        };

        return localizationKey != null
            ? LocalizationService.Instance.Get(localizationKey)
            : statKey; // Fallback to original key if no mapping exists
    }

    #endregion

    #region Admin Commands (Privilege.root)

    /// <summary>
    ///     /blessings admin unlock blessingid> [playername] - Force unlock a blessing for a player, bypassing all validation
    /// </summary>
    internal TextCommandResult OnAdminUnlock(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var blessingId = (string)args[0];
        var targetPlayerName = args.Parsers.Count > 1 ? (string?)args[1] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // After error check, targetPlayer and targetPlayerData are guaranteed non-null
        var resolvedPlayer = targetPlayer!;
        var resolvedPlayerData = targetPlayerData!;

        // Validate blessing exists
        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_FOUND, blessingId));

        // Get target's religion
        var targetReligion = _religionManager.GetPlayerReligion(resolvedPlayer.PlayerUID);
        if (targetReligion == null || string.IsNullOrEmpty(targetReligion.ReligionUID))
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_RELIGION));

        // Handle based on blessing kind
        if (blessing.Kind == BlessingKind.Player)
        {
            // Player blessing - unlock for target player
            if (resolvedPlayerData.IsBlessingUnlocked(blessing.BlessingId))
                return TextCommandResult.Success(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_ALREADY_UNLOCKED));

            resolvedPlayerData.UnlockBlessing(blessing.BlessingId);
            _blessingEffectSystem.RefreshPlayerBlessings(resolvedPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedPlayer.PlayerUID);

            // Send blessing unlock notification to target player for GUI update
            var packet =
                new BlessingUnlockResponsePacket(true,
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_UNLOCKED,
                        blessing.Name, resolvedPlayer.PlayerName), blessing.BlessingId);
            _serverChannel.SendPacket(packet, resolvedPlayer);

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} unlocked player blessing '{blessing.Name}' for {resolvedPlayer.PlayerName}");

            return TextCommandResult.Success(LocalizationService.Instance.Get(
                LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_UNLOCKED, blessing.Name, resolvedPlayer.PlayerName));
        }
        else
        {
            // Religion blessing - only founder can unlock (admin doesn't bypass this for game balance)
            if (targetReligion.FounderUID != resolvedPlayer.PlayerUID)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_FOUNDER));

            if (targetReligion.IsBlessingUnlocked(blessing.BlessingId))
                return TextCommandResult.Success(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_ALREADY_UNLOCKED));

            targetReligion.UnlockBlessing(blessing.BlessingId);
            _blessingEffectSystem.RefreshReligionBlessings(targetReligion.ReligionUID);
            _religionManager.Save(targetReligion);

            // Notify all members of the religion
            foreach (var memberUid in targetReligion.MemberUIDs)
            {
                var member = _sapi.World.PlayerByUid(memberUid) as IServerPlayer;
                if (member != null)
                {
                    // Notify player data changed (triggers HUD update)
                    _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                    // Send blessing unlock packet for GUI update
                    var memberPacket = new BlessingUnlockResponsePacket(true,
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_UNLOCKED,
                            blessing.Name, targetReligion.ReligionName), blessing.BlessingId);
                    _serverChannel.SendPacket(memberPacket, member);
                }
            }

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} unlocked religion blessing '{blessing.Name}' for {targetReligion.ReligionName}");

            return TextCommandResult.Success(LocalizationService.Instance.Get(
                LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_UNLOCKED, blessing.Name, targetReligion.ReligionName));
        }
    }

    /// <summary>
    ///     /blessings admin lock <blessingid> [playername] - Remove a specific unlocked blessing from a player
    /// </summary>
    internal TextCommandResult OnAdminLock(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var blessingId = (string)args[0];
        var targetPlayerName = args.Parsers.Count > 1 ? (string?)args[1] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // After error check, targetPlayer and targetPlayerData are guaranteed non-null
        var resolvedPlayer = targetPlayer!;
        var resolvedPlayerData = targetPlayerData!;

        // Validate blessing exists
        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_ERROR_NOT_FOUND, blessingId));

        // Get target's religion
        var targetReligion = _religionManager.GetPlayerReligion(resolvedPlayer.PlayerUID);
        if (targetReligion == null || string.IsNullOrEmpty(targetReligion.ReligionUID))
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_RELIGION));

        // Handle based on blessing kind
        if (blessing.Kind == BlessingKind.Player)
        {
            // Player blessing - lock for target player
            if (!resolvedPlayerData.IsBlessingUnlocked(blessing.BlessingId))
                return TextCommandResult.Success(
                    $"{resolvedPlayer.PlayerName} doesn't have blessing '{blessing.Name}' unlocked");

            resolvedPlayerData.LockBlessing(blessing.BlessingId);
            _blessingEffectSystem.RefreshPlayerBlessings(resolvedPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedPlayer.PlayerUID);

            // Send blessing lock notification to target player for GUI update
            var packet = new BlessingUnlockResponsePacket(false,
                LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_LOCKED, blessing.Name,
                    resolvedPlayer.PlayerName), blessing.BlessingId);
            _serverChannel.SendPacket(packet, resolvedPlayer);

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} locked player blessing '{blessing.Name}' for {resolvedPlayer.PlayerName}");

            return TextCommandResult.Success(LocalizationService.Instance.Get(
                LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_LOCKED, blessing.Name, resolvedPlayer.PlayerName));
        }
        else
        {
            // Religion blessing - only founder can lock
            if (targetReligion.FounderUID != resolvedPlayer.PlayerUID)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_FOUNDER));

            if (!targetReligion.IsBlessingUnlocked(blessing.BlessingId))
                return TextCommandResult.Success(
                    $"{targetReligion.ReligionName} doesn't have blessing '{blessing.Name}' unlocked");

            targetReligion.LockBlessing(blessing.BlessingId);
            _blessingEffectSystem.RefreshReligionBlessings(targetReligion.ReligionUID);
            _religionManager.Save(targetReligion);

            // Notify all members of the religion
            foreach (var memberUid in targetReligion.MemberUIDs)
            {
                var member = _sapi.World.PlayerByUid(memberUid) as IServerPlayer;
                if (member != null)
                {
                    // Notify player data changed (triggers HUD update)
                    _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                    // Send blessing lock packet for GUI update
                    var memberPacket = new BlessingUnlockResponsePacket(false,
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_LOCKED,
                            blessing.Name, targetReligion.ReligionName), blessing.BlessingId);
                    _serverChannel.SendPacket(memberPacket, member);
                }
            }

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} locked religion blessing '{blessing.Name}' for {targetReligion.ReligionName}");

            return TextCommandResult.Success(LocalizationService.Instance.Get(
                LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_LOCKED, blessing.Name, targetReligion.ReligionName));
        }
    }

    /// <summary>
    ///     /blessings admin reset [playername] - Clear all unlocked blessings from a player
    /// </summary>
    internal TextCommandResult OnAdminReset(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var targetPlayerName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // After error check, targetPlayer and targetPlayerData are guaranteed non-null
        var resolvedPlayer = targetPlayer!;
        var resolvedPlayerData = targetPlayerData!;

        var blessingCount = resolvedPlayerData.UnlockedBlessings.Count;
        if (blessingCount == 0)
            return TextCommandResult.Success($"{resolvedPlayer.PlayerName} has no blessings to reset");

        resolvedPlayerData.ClearUnlockedBlessings();
        _blessingEffectSystem.RefreshPlayerBlessings(resolvedPlayer.PlayerUID);

        // Notify player data changed (triggers HUD update)
        // Note: For reset, we don't send individual blessing packets since all were removed
        // The NotifyPlayerDataChanged will trigger a full state refresh on the client
        _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedPlayer.PlayerUID);

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} reset all blessings for {resolvedPlayer.PlayerName}");

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_RESET,
                resolvedPlayer.PlayerName));
    }

    /// <summary>
    ///     /blessings admin unlockall [playername] - Unlock all available blessings for a player
    /// </summary>
    internal TextCommandResult OnAdminUnlockAll(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var targetPlayerName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // After error check, targetPlayer and targetPlayerData are guaranteed non-null
        var resolvedPlayer = targetPlayer!;
        var resolvedPlayerData = targetPlayerData!;

        // Get target's deity
        var targetDeity = _religionManager.GetPlayerActiveDeityDomain(resolvedPlayer.PlayerUID);
        if (targetDeity == DeityDomain.None)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_RELIGION));

        // Get target's religion
        var targetReligion = _religionManager.GetPlayerReligion(resolvedPlayer.PlayerUID);
        if (targetReligion == null || string.IsNullOrEmpty(targetReligion.ReligionUID))
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_RELIGION));

        // Get all blessings for this deity
        var playerBlessings = _blessingRegistry.GetBlessingsForDeity(targetDeity, BlessingKind.Player);
        var religionBlessings = _blessingRegistry.GetBlessingsForDeity(targetDeity, BlessingKind.Religion);

        int playerUnlocked = 0;
        int religionUnlocked = 0;

        // Unlock all player blessings
        foreach (var blessing in playerBlessings)
        {
            if (!resolvedPlayerData.IsBlessingUnlocked(blessing.BlessingId))
            {
                resolvedPlayerData.UnlockBlessing(blessing.BlessingId);
                playerUnlocked++;
            }
        }

        // Unlock all religion blessings (only if founder)
        if (targetReligion.FounderUID == resolvedPlayer.PlayerUID)
        {
            foreach (var blessing in religionBlessings)
            {
                if (!targetReligion.IsBlessingUnlocked(blessing.BlessingId))
                {
                    targetReligion.UnlockBlessing(blessing.BlessingId);
                    religionUnlocked++;
                }
            }

            if (religionUnlocked > 0)
            {
                _blessingEffectSystem.RefreshReligionBlessings(targetReligion.ReligionUID);
                _religionManager.Save(targetReligion);

                // Notify all members of the religion
                foreach (var memberUid in targetReligion.MemberUIDs)
                {
                    var member = _sapi.World.PlayerByUid(memberUid) as IServerPlayer;
                    if (member != null)
                    {
                        // Notify player data changed (triggers HUD update)
                        _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                        // Send unlock notifications for all religion blessings
                        foreach (var blessing in religionBlessings)
                        {
                            if (targetReligion.UnlockedBlessings.ContainsKey(blessing.BlessingId))
                            {
                                var memberPacket = new BlessingUnlockResponsePacket(true,
                                    LocalizationService.Instance.Get(
                                        LocalizationKeys.NET_BLESSING_SUCCESS_UNLOCKED_FOR_RELIGION, blessing.Name),
                                    blessing.BlessingId);
                                _serverChannel.SendPacket(memberPacket, member);
                            }
                        }
                    }
                }
            }
        }

        if (playerUnlocked > 0)
        {
            _blessingEffectSystem.RefreshPlayerBlessings(resolvedPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedPlayer.PlayerUID);

            // Send unlock notifications for all player blessings
            foreach (var blessing in playerBlessings)
            {
                if (resolvedPlayerData.UnlockedBlessings.Contains(blessing.BlessingId))
                {
                    var packet = new BlessingUnlockResponsePacket(true,
                        LocalizationService.Instance.Get(LocalizationKeys.NET_BLESSING_SUCCESS_UNLOCKED, blessing.Name),
                        blessing.BlessingId);
                    _serverChannel.SendPacket(packet, resolvedPlayer);
                }
            }
        }

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} unlocked all blessings for {resolvedPlayer.PlayerName} ({playerUnlocked} player, {religionUnlocked} religion)");

        return TextCommandResult.Success(LocalizationService.Instance.Get(
            LocalizationKeys.CMD_BLESSING_SUCCESS_ADMIN_UNLOCKALL, playerUnlocked, resolvedPlayer.PlayerName));
    }

    #endregion
}