using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
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
            .WithDescription(BlessingDescriptionConstants.CommandDescription)
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand(BlessingCommandConstants.SubCommandList)
            .WithDescription(BlessingDescriptionConstants.DescriptionList)
            .HandleWith(OnList)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandPlayer)
            .WithDescription(BlessingDescriptionConstants.DescriptionPlayer)
            .HandleWith(OnPlayer)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandReligion)
            .WithDescription(BlessingDescriptionConstants.DescriptionReligion)
            .HandleWith(OnReligion)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandInfo)
            .WithDescription(BlessingDescriptionConstants.DescriptionInfo)
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord(ParameterConstants.ParamBlessingId))
            .HandleWith(OnInfo)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandTree)
            .WithDescription(BlessingDescriptionConstants.DescriptionTree)
            .WithArgs(_sapi.ChatCommands.Parsers.Word(ParameterConstants.ParamType))
            .HandleWith(OnTree)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandUnlock)
            .WithDescription(BlessingDescriptionConstants.DescriptionUnlock)
            .WithArgs(_sapi.ChatCommands.Parsers.Word(ParameterConstants.ParamBlessingId))
            .HandleWith(OnUnlock)
            .EndSubCommand()
            .BeginSubCommand(BlessingCommandConstants.SubCommandActive)
            .WithDescription(BlessingDescriptionConstants.DescriptionActive)
            .HandleWith(OnActive)
            .EndSubCommand()
            .BeginSubCommand("admin")
            .WithDescription("Admin commands for blessing management")
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
        if (player == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorPlayerNotFound);

        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        var playerDeity = _religionManager.GetPlayerActiveDeity(player.PlayerUID);
        if (playerDeity == DeityType.None)
            return TextCommandResult.Error(ErrorMessageConstants.ErrorMustJoinReligion);

        var playerBlessings = _blessingRegistry.GetBlessingsForDeity(playerDeity, BlessingKind.Player);
        var religionBlessings = _blessingRegistry.GetBlessingsForDeity(playerDeity, BlessingKind.Religion);

        var sb = new StringBuilder();
        sb.AppendLine(string.Format(FormatStringConstants.HeaderBlessingsForDeity, playerDeity));
        sb.AppendLine();

        sb.AppendLine(FormatStringConstants.HeaderPlayerBlessings);
        foreach (var blessing in playerBlessings)
        {
            var status = playerData.IsBlessingUnlocked(blessing.BlessingId) ? FormatStringConstants.LabelUnlocked : "";
            var requiredRank = (FavorRank)blessing.RequiredFavorRank;
            sb.AppendLine($"{blessing.Name} {status}");
            sb.AppendLine(string.Format(FormatStringConstants.FormatBlessingId, blessing.BlessingId));
            sb.AppendLine(string.Format(FormatStringConstants.FormatRequiredRank, requiredRank));
            sb.AppendLine(string.Format(FormatStringConstants.FormatDescription, blessing.Description));
            sb.AppendLine();
        }

        sb.AppendLine(FormatStringConstants.HeaderReligionBlessings);
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        foreach (var blessing in religionBlessings)
        {
            var unlocked = religion?.UnlockedBlessings.TryGetValue(blessing.BlessingId, out var u) == true && u;
            var status = unlocked ? FormatStringConstants.LabelUnlocked : "";
            var requiredRank = (PrestigeRank)blessing.RequiredPrestigeRank;
            sb.AppendLine($"{blessing.Name} {status}");
            sb.AppendLine(string.Format(FormatStringConstants.FormatBlessingId, blessing.BlessingId));
            sb.AppendLine(string.Format(FormatStringConstants.FormatRequiredRank, requiredRank));
            sb.AppendLine(string.Format(FormatStringConstants.FormatDescription, blessing.Description));
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
        if (player == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorPlayerNotFound);

        var (playerBlessings, _) = _blessingEffectSystem.GetActiveBlessings(player.PlayerUID);

        if (playerBlessings.Count == 0) return TextCommandResult.Success(InfoMessageConstants.InfoNoPlayerBlessings);

        var sb = new StringBuilder();
        sb.AppendLine(string.Format(FormatStringConstants.HeaderUnlockedPlayerBlessings, playerBlessings.Count));
        sb.AppendLine();

        foreach (var blessing in playerBlessings)
        {
            sb.AppendLine(string.Format(FormatStringConstants.FormatBlessingNameCategory, blessing.Name,
                blessing.Category));
            sb.AppendLine(string.Format(FormatStringConstants.FormatDescription, blessing.Description));

            if (blessing.StatModifiers.Count > 0)
            {
                sb.AppendLine(FormatStringConstants.LabelEffects);
                foreach (var mod in blessing.StatModifiers)
                    sb.AppendLine(string.Format(FormatStringConstants.FormatStatModifier, mod.Key,
                        mod.Value * 100));
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
        if (player == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorPlayerNotFound);

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(ErrorMessageConstants.ErrorNoReligion);

        var (_, religionBlessings) = _blessingEffectSystem.GetActiveBlessings(player.PlayerUID);

        if (religionBlessings.Count == 0)
            return TextCommandResult.Success(InfoMessageConstants.InfoNoReligionBlessings);

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        var sb = new StringBuilder();
        sb.AppendLine(string.Format(FormatStringConstants.HeaderReligionBlessingsWithName, religion?.ReligionName,
            religionBlessings.Count));
        sb.AppendLine();

        foreach (var blessing in religionBlessings)
        {
            sb.AppendLine(string.Format(FormatStringConstants.FormatBlessingNameCategory, blessing.Name,
                blessing.Category));
            sb.AppendLine(string.Format(FormatStringConstants.FormatDescription, blessing.Description));

            if (blessing.StatModifiers.Count > 0)
            {
                sb.AppendLine(FormatStringConstants.LabelEffectsForAllMembers);
                foreach (var mod in blessing.StatModifiers)
                    sb.AppendLine(string.Format(FormatStringConstants.FormatStatModifier, mod.Key,
                        mod.Value * 100));
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
        if (string.IsNullOrEmpty(blessingId)) return TextCommandResult.Error(UsageMessageConstants.UsageBlessingsInfo);

        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error(string.Format(ErrorMessageConstants.ErrorBlessingNotFound,
                blessingId));

        var sb = new StringBuilder();
        sb.AppendLine(string.Format(FormatStringConstants.HeaderBlessingInfo, blessing.Name));
        sb.AppendLine(string.Format(FormatStringConstants.LabelId, blessing.BlessingId));
        sb.AppendLine(string.Format(FormatStringConstants.LabelDeity, blessing.Deity));
        sb.AppendLine(string.Format(FormatStringConstants.LabelType, blessing.Kind));
        sb.AppendLine(string.Format(FormatStringConstants.LabelCategory, blessing.Category));
        sb.AppendLine();
        sb.AppendLine(string.Format(FormatStringConstants.LabelDescriptionStandalone,
            blessing.Description));
        sb.AppendLine();

        if (blessing.Kind == BlessingKind.Player)
        {
            var requiredRank = (FavorRank)blessing.RequiredFavorRank;
            sb.AppendLine(
                string.Format(FormatStringConstants.LabelRequiredFavorRank, requiredRank));
        }
        else
        {
            var requiredRank = (PrestigeRank)blessing.RequiredPrestigeRank;
            sb.AppendLine(string.Format(FormatStringConstants.LabelRequiredPrestigeRank,
                requiredRank));
        }

        if (blessing.PrerequisiteBlessings is { Count: > 0 })
        {
            sb.AppendLine(FormatStringConstants.LabelPrerequisites);
            foreach (var prereqId in blessing.PrerequisiteBlessings)
            {
                var prereq = _blessingRegistry.GetBlessing(prereqId);
                var prereqName = prereq?.Name ?? prereqId;
                sb.AppendLine(string.Format(FormatStringConstants.LabelPrerequisiteItem,
                    prereqName));
            }
        }

        if (blessing.StatModifiers.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(FormatStringConstants.LabelStatModifiers);
            foreach (var mod in blessing.StatModifiers)
                sb.AppendLine(string.Format(FormatStringConstants.FormatStatModifierPercent,
                    mod.Key, mod.Value * 100));
        }

        if (blessing.SpecialEffects is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine(FormatStringConstants.LabelSpecialEffects);
            foreach (var effect in blessing.SpecialEffects)
                sb.AppendLine(string.Format(FormatStringConstants.LabelSpecialEffectItem, effect));
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
        if (player == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorPlayerNotFound);

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
        var playerDeity = _religionManager.GetPlayerActiveDeity(playerUid);
        if (playerDeity == DeityType.None)
            return TextCommandResult.Error(ErrorMessageConstants.ErrorMustJoinReligionForTree);

        type = type ?? FormatStringConstants.TypePlayer;
        type = type.ToLower();

        var blessingKind = type == FormatStringConstants.TypeReligion
            ? BlessingKind.Religion
            : BlessingKind.Player;

        var blessings = _blessingRegistry.GetBlessingsForDeity(playerDeity, blessingKind);

        var religion = _religionManager.GetPlayerReligion(playerUid);


        var sb = new StringBuilder();
        sb.AppendLine(string.Format(FormatStringConstants.HeaderBlessingTree, playerDeity, blessingKind));
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

                sb.AppendLine(string.Format(FormatStringConstants.HeaderRankSection, rank));
                foreach (var blessing in rankBlessings)
                {
                    var unlocked = playerData.IsBlessingUnlocked(blessing.BlessingId);
                    var status = unlocked
                        ? FormatStringConstants.LabelChecked
                        : FormatStringConstants.LabelUnchecked;
                    sb.AppendLine($"{status} {blessing.Name}");

                    if (blessing.PrerequisiteBlessings is { Count: > 0 })
                    {
                        sb.Append(FormatStringConstants.LabelRequires);
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

                sb.AppendLine(string.Format(FormatStringConstants.HeaderRankSection, rank));
                foreach (var blessing in rankBlessings)
                {
                    var unlocked = religion?.UnlockedBlessings.TryGetValue(blessing.BlessingId, out var u) == true && u;
                    var status = unlocked
                        ? FormatStringConstants.LabelChecked
                        : FormatStringConstants.LabelUnchecked;
                    sb.AppendLine($"{status} {blessing.Name}");

                    if (blessing.PrerequisiteBlessings is { Count: > 0 })
                    {
                        sb.Append(FormatStringConstants.LabelRequires);
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
        if (player == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorPlayerNotFound);
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
            return TextCommandResult.Error(UsageMessageConstants.UsageBlessingsUnlock);

        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error(string.Format(ErrorMessageConstants.ErrorBlessingNotFound, blessingId));

        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUid);
        var religion = _religionManager.GetPlayerReligion(playerUid);

        var (canUnlock, reason) = _blessingRegistry.CanUnlockBlessing(playerData, religion, blessing);
        if (!canUnlock)
            return TextCommandResult.Error(string.Format(ErrorMessageConstants.ErrorCannotUnlockBlessing, reason));

        // Unlock the blessing
        if (blessing.Kind == BlessingKind.Player)
        {
            if (religion == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorMustBeInReligionToUnlock);

            var success = _playerProgressionDataManager.UnlockPlayerBlessing(playerUid, blessingId);
            if (!success) return TextCommandResult.Error(ErrorMessageConstants.ErrorFailedToUnlock);

            _blessingEffectSystem.RefreshPlayerBlessings(playerUid);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(playerUid);

            // Send blessing unlock notification to client for GUI update
            var player = _sapi.World.PlayerByUid(playerUid) as IServerPlayer;
            if (player != null)
            {
                var packet = new BlessingUnlockResponsePacket(true, $"Unlocked {blessing.Name}!", blessing.BlessingId);
                _serverChannel.SendPacket(packet, player);
            }

            return TextCommandResult.Success(string.Format(SuccessMessageConstants.SuccessUnlockedPlayerBlessing,
                blessing.Name));
        }

        // Religion blessing
        if (religion == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorMustBeInReligionToUnlock);

        // Only founder can unlock religion blessings (optional restriction)
        if (!religion.IsFounder(playerUid))
            return TextCommandResult.Error(ErrorMessageConstants.ErrorOnlyFounderCanUnlock);

        religion.UnlockedBlessings[blessingId] = true;
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
                    string.Format(SuccessMessageConstants.NotificationBlessingUnlocked, blessing.Name),
                    EnumChatType.Notification
                );

                // Notify player data changed (triggers HUD update)
                _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                // Send blessing unlock packet for GUI update
                var packet = new BlessingUnlockResponsePacket(true, $"Religion blessing unlocked: {blessing.Name}!",
                    blessing.BlessingId);
                _serverChannel.SendPacket(packet, member);
            }
        }

        return TextCommandResult.Success(string.Format(SuccessMessageConstants.SuccessUnlockedReligionBlessing,
            blessing.Name));
    }

    /// <summary>
    ///     /blessings active - Shows all active blessings and combined modifiers
    /// </summary>
    internal TextCommandResult OnActive(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error(ErrorMessageConstants.ErrorPlayerNotFound);

        var (playerBlessings, religionBlessings) = _blessingEffectSystem.GetActiveBlessings(player.PlayerUID);
        var combinedModifiers = _blessingEffectSystem.GetCombinedStatModifiers(player.PlayerUID);

        var sb = new StringBuilder();
        sb.AppendLine(FormatStringConstants.HeaderActiveBlessings);
        sb.AppendLine();

        sb.AppendLine(string.Format(FormatStringConstants.LabelPlayerBlessingsSection, playerBlessings.Count));
        if (playerBlessings.Count == 0)
            sb.AppendLine(FormatStringConstants.LabelNone);
        else
            foreach (var blessing in playerBlessings)
                sb.AppendLine(string.Format(FormatStringConstants.LabelBlessingItem, blessing.Name));

        sb.AppendLine();

        sb.AppendLine(string.Format(FormatStringConstants.LabelReligionBlessingsSection, religionBlessings.Count));
        if (religionBlessings.Count == 0)
            sb.AppendLine(FormatStringConstants.LabelNone);
        else
            foreach (var blessing in religionBlessings)
                sb.AppendLine(string.Format(FormatStringConstants.LabelBlessingItem, blessing.Name));

        sb.AppendLine();

        sb.AppendLine(FormatStringConstants.LabelCombinedStatModifiers);
        sb.AppendLine(combinedModifiers.Count == 0
            ? FormatStringConstants.LabelNoActiveModifiers
            : _blessingEffectSystem.FormatStatModifiers(combinedModifiers));

        return TextCommandResult.Success(sb.ToString());
    }

    #region Admin Commands (Privilege.root)

    /// <summary>
    ///     /blessings admin unlock blessingid> [playername] - Force unlock a blessing for a player, bypassing all validation
    /// </summary>
    internal TextCommandResult OnAdminUnlock(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var blessingId = (string)args[0];
        var targetPlayerName = args.Parsers.Count > 1 ? (string?)args[1] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // Validate blessing exists
        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error($"Blessing '{blessingId}' not found");

        // Get target's religion
        var targetReligion = _religionManager.GetPlayerReligion(targetPlayer.PlayerUID);
        if (targetReligion == null || string.IsNullOrEmpty(targetReligion.ReligionUID))
            return TextCommandResult.Error($"{targetPlayer.PlayerName} is not in a religion");

        // Handle based on blessing kind
        if (blessing.Kind == BlessingKind.Player)
        {
            // Player blessing - unlock for target player
            if (targetPlayerData.UnlockedBlessings.Contains(blessing.BlessingId))
                return TextCommandResult.Success(
                    $"{targetPlayer.PlayerName} already has blessing '{blessing.Name}' unlocked");

            targetPlayerData.UnlockedBlessings.Add(blessing.BlessingId);
            _blessingEffectSystem.RefreshPlayerBlessings(targetPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(targetPlayer.PlayerUID);

            // Send blessing unlock notification to target player for GUI update
            var packet =
                new BlessingUnlockResponsePacket(true, $"Admin unlocked {blessing.Name}!", blessing.BlessingId);
            _serverChannel.SendPacket(packet, targetPlayer);

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} unlocked player blessing '{blessing.Name}' for {targetPlayer.PlayerName}");

            return TextCommandResult.Success(
                $"Unlocked player blessing '{blessing.Name}' for {targetPlayer.PlayerName}");
        }
        else
        {
            // Religion blessing - only founder can unlock (admin doesn't bypass this for game balance)
            if (targetReligion.FounderUID != targetPlayer.PlayerUID)
                return TextCommandResult.Error("Only the religion founder can unlock religion blessings");

            if (targetReligion.UnlockedBlessings.ContainsKey(blessing.BlessingId))
                return TextCommandResult.Success(
                    $"{targetReligion.ReligionName} already has blessing '{blessing.Name}' unlocked");

            targetReligion.UnlockedBlessings.Add(blessing.BlessingId, true);
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
                        $"Admin unlocked religion blessing: {blessing.Name}!", blessing.BlessingId);
                    _serverChannel.SendPacket(memberPacket, member);
                }
            }

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} unlocked religion blessing '{blessing.Name}' for {targetReligion.ReligionName}");

            return TextCommandResult.Success(
                $"Unlocked religion blessing '{blessing.Name}' for {targetReligion.ReligionName}");
        }
    }

    /// <summary>
    ///     /blessings admin lock <blessingid> [playername] - Remove a specific unlocked blessing from a player
    /// </summary>
    internal TextCommandResult OnAdminLock(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var blessingId = (string)args[0];
        var targetPlayerName = args.Parsers.Count > 1 ? (string?)args[1] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // Validate blessing exists
        var blessing = _blessingRegistry.GetBlessing(blessingId);
        if (blessing == null)
            return TextCommandResult.Error($"Blessing '{blessingId}' not found");

        // Get target's religion
        var targetReligion = _religionManager.GetPlayerReligion(targetPlayer.PlayerUID);
        if (targetReligion == null || string.IsNullOrEmpty(targetReligion.ReligionUID))
            return TextCommandResult.Error($"{targetPlayer.PlayerName} is not in a religion");

        // Handle based on blessing kind
        if (blessing.Kind == BlessingKind.Player)
        {
            // Player blessing - lock for target player
            if (!targetPlayerData.UnlockedBlessings.Contains(blessing.BlessingId))
                return TextCommandResult.Success(
                    $"{targetPlayer.PlayerName} doesn't have blessing '{blessing.Name}' unlocked");

            targetPlayerData.UnlockedBlessings.Remove(blessing.BlessingId);
            _blessingEffectSystem.RefreshPlayerBlessings(targetPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(targetPlayer.PlayerUID);

            // Send blessing lock notification to target player for GUI update
            var packet = new BlessingUnlockResponsePacket(false, $"Admin locked {blessing.Name}", blessing.BlessingId);
            _serverChannel.SendPacket(packet, targetPlayer);

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} locked player blessing '{blessing.Name}' for {targetPlayer.PlayerName}");

            return TextCommandResult.Success($"Locked player blessing '{blessing.Name}' for {targetPlayer.PlayerName}");
        }
        else
        {
            // Religion blessing - only founder can lock
            if (targetReligion.FounderUID != targetPlayer.PlayerUID)
                return TextCommandResult.Error("Only the religion founder can lock religion blessings");

            if (!targetReligion.UnlockedBlessings.ContainsKey(blessing.BlessingId))
                return TextCommandResult.Success(
                    $"{targetReligion.ReligionName} doesn't have blessing '{blessing.Name}' unlocked");

            targetReligion.UnlockedBlessings.Remove(blessing.BlessingId);
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
                        $"Admin locked religion blessing: {blessing.Name}", blessing.BlessingId);
                    _serverChannel.SendPacket(memberPacket, member);
                }
            }

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} locked religion blessing '{blessing.Name}' for {targetReligion.ReligionName}");

            return TextCommandResult.Success(
                $"Locked religion blessing '{blessing.Name}' for {targetReligion.ReligionName}");
        }
    }

    /// <summary>
    ///     /blessings admin reset [playername] - Clear all unlocked blessings from a player
    /// </summary>
    internal TextCommandResult OnAdminReset(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var targetPlayerName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        var blessingCount = targetPlayerData.UnlockedBlessings.Count;
        if (blessingCount == 0)
            return TextCommandResult.Success($"{targetPlayer.PlayerName} has no blessings to reset");

        targetPlayerData.UnlockedBlessings.Clear();
        _blessingEffectSystem.RefreshPlayerBlessings(targetPlayer.PlayerUID);

        // Notify player data changed (triggers HUD update)
        // Note: For reset, we don't send individual blessing packets since all were removed
        // The NotifyPlayerDataChanged will trigger a full state refresh on the client
        _playerProgressionDataManager.NotifyPlayerDataChanged(targetPlayer.PlayerUID);

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} reset all blessings for {targetPlayer.PlayerName}");

        return TextCommandResult.Success($"Reset {blessingCount} blessing(s) for {targetPlayer.PlayerName}");
    }

    /// <summary>
    ///     /blessings admin unlockall [playername] - Unlock all available blessings for a player
    /// </summary>
    internal TextCommandResult OnAdminUnlockAll(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var targetPlayerName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Resolve target player
        var (targetPlayer, targetPlayerData, errorResult) = CommandHelpers.ResolveTargetPlayer(
            player, targetPlayerName, _sapi, _playerProgressionDataManager, _religionManager);
        if (errorResult is { Status: EnumCommandStatus.Error })
            return errorResult;

        // Get target's deity
        var targetDeity = _religionManager.GetPlayerActiveDeity(targetPlayer.PlayerUID);
        if (targetDeity == DeityType.None)
            return TextCommandResult.Error($"{targetPlayer.PlayerName} is not in a religion");

        // Get target's religion
        var targetReligion = _religionManager.GetPlayerReligion(targetPlayer.PlayerUID);
        if (targetReligion == null || string.IsNullOrEmpty(targetReligion.ReligionUID))
            return TextCommandResult.Error($"{targetPlayer.PlayerName} is not in a religion");

        // Get all blessings for this deity
        var playerBlessings = _blessingRegistry.GetBlessingsForDeity(targetDeity, BlessingKind.Player);
        var religionBlessings = _blessingRegistry.GetBlessingsForDeity(targetDeity, BlessingKind.Religion);

        int playerUnlocked = 0;
        int religionUnlocked = 0;

        // Unlock all player blessings
        foreach (var blessing in playerBlessings)
        {
            if (!targetPlayerData.UnlockedBlessings.Contains(blessing.BlessingId))
            {
                targetPlayerData.UnlockedBlessings.Add(blessing.BlessingId);
                playerUnlocked++;
            }
        }

        // Unlock all religion blessings (only if founder)
        if (targetReligion.FounderUID == targetPlayer.PlayerUID)
        {
            foreach (var blessing in religionBlessings)
            {
                if (!targetReligion.UnlockedBlessings.ContainsKey(blessing.BlessingId))
                {
                    targetReligion.UnlockedBlessings.Add(blessing.BlessingId, true);
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
                                    $"Religion blessing unlocked: {blessing.Name}!", blessing.BlessingId);
                                _serverChannel.SendPacket(memberPacket, member);
                            }
                        }
                    }
                }
            }
        }

        if (playerUnlocked > 0)
        {
            _blessingEffectSystem.RefreshPlayerBlessings(targetPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(targetPlayer.PlayerUID);

            // Send unlock notifications for all player blessings
            foreach (var blessing in playerBlessings)
            {
                if (targetPlayerData.UnlockedBlessings.Contains(blessing.BlessingId))
                {
                    var packet = new BlessingUnlockResponsePacket(true, $"Unlocked {blessing.Name}!",
                        blessing.BlessingId);
                    _serverChannel.SendPacket(packet, targetPlayer);
                }
            }
        }

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} unlocked all blessings for {targetPlayer.PlayerName} ({playerUnlocked} player, {religionUnlocked} religion)");

        return TextCommandResult.Success(
            $"Unlocked {playerUnlocked} player blessing(s) and {religionUnlocked} religion blessing(s) for {targetPlayer.PlayerName}");
    }

    #endregion
}