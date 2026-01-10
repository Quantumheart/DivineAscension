using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DivineAscension.Commands.Parsers;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Extensions;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Handles all religion-related chat commands
/// </summary>
public class ReligionCommands(
    ICoreServerAPI sapi,
    IReligionManager religionManager,
    IPlayerProgressionDataManager playerReligionDataManager,
    IReligionPrestigeManager religionPrestigeManager,
    IServerNetworkChannel serverChannel,
    IRoleManager roleManager)
{
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    private readonly IReligionPrestigeManager _religionPrestigeManager =
        religionPrestigeManager ?? throw new ArgumentNullException(nameof(religionPrestigeManager));

    private readonly IRoleManager _roleManager =
        roleManager ?? throw new ArgumentNullException(nameof(roleManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly IServerNetworkChannel? _serverChannel =
        serverChannel ?? throw new ArgumentNullException(nameof(serverChannel));

    /// <summary>
    ///     Registers all religion commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("religion")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_DESC))
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("create")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_CREATE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("name"),
                _sapi.ChatCommands.Parsers.Word("deity"),
                _sapi.ChatCommands.Parsers.OptionalWord("visibility"))
            .HandleWith(OnCreateReligion)
            .EndSubCommand()
            .BeginSubCommand("join")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_JOIN_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("name"))
            .HandleWith(OnJoinReligion)
            .EndSubCommand()
            .BeginSubCommand("leave")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_LEAVE_DESC))
            .HandleWith(OnLeaveReligion)
            .EndSubCommand()
            .BeginSubCommand("list")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_LIST_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("deity"))
            .HandleWith(OnListReligions)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalQuotedString("name"))
            .HandleWith(OnReligionInfo)
            .EndSubCommand()
            .BeginSubCommand("members")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_MEMBERS_DESC))
            .HandleWith(OnListMembers)
            .EndSubCommand()
            .BeginSubCommand("invite")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INVITE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnInvitePlayer)
            .EndSubCommand()
            .BeginSubCommand("kick")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_KICK_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnKickPlayer)
            .EndSubCommand()
            .BeginSubCommand("ban")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_BAN_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"),
                _sapi.ChatCommands.Parsers.OptionalAll("reason"),
                _sapi.ChatCommands.Parsers.OptionalInt("days"))
            .HandleWith(OnBanPlayer)
            .EndSubCommand()
            .BeginSubCommand("unban")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_UNBAN_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnUnbanPlayer)
            .EndSubCommand()
            .BeginSubCommand("banlist")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_BANLIST_DESC))
            .HandleWith(OnListBannedPlayers)
            .EndSubCommand()
            .BeginSubCommand("disband")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_DISBAND_DESC))
            .HandleWith(OnDisbandReligion)
            .EndSubCommand()
            .BeginSubCommand("description")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_DESCRIPTION_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.All("text"))
            .HandleWith(OnSetDescription)
            .EndSubCommand()
            .BeginSubCommand("prestige")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_PRESTIGE_DESC))
            .BeginSubCommand("info")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_PRESTIGE_INFO_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalQuotedString("religionname"))
            .HandleWith(OnPrestigeInfo)
            .EndSubCommand()
            .BeginSubCommand("add")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_PRESTIGE_ADD_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("religionname"),
                _sapi.ChatCommands.Parsers.Int("amount"),
                _sapi.ChatCommands.Parsers.OptionalAll("reason"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnPrestigeAdd)
            .EndSubCommand()
            .BeginSubCommand("set")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_PRESTIGE_SET_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("religionname"),
                _sapi.ChatCommands.Parsers.Int("amount"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnPrestigeSet)
            .EndSubCommand()
            .EndSubCommand()
            .BeginSubCommand("admin")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ADMIN_DESC))
            .RequiresPrivilege(Privilege.root)
            .BeginSubCommand("repair")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ADMIN_REPAIR_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminRepair)
            .EndSubCommand()
            .BeginSubCommand("join")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ADMIN_JOIN_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("religionname"),
                _sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminJoin)
            .EndSubCommand()
            .BeginSubCommand("leave")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ADMIN_LEAVE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("playername"))
            .HandleWith(OnAdminLeave)
            .EndSubCommand()
            .EndSubCommand();

        _sapi.Logger.Notification("[DivineAscension] Religion commands registered");
    }

    #region Command Handlers

    /// <summary>
    ///     Handler for /religion create
    ///     <name></name>
    ///     <deity> [public/private]</deity>
    /// </summary>
    internal TextCommandResult OnCreateReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];
        var deityName = (string)args[1];
        var visibility = args.Parsers.Count > 2 ? (string?)args[2] : "public";

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player already has a religion
        var playerId = player.PlayerUID;
        if (_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_ALREADY_IN_RELIGION));

        // Parse deity type
        if (!Enum.TryParse(deityName, true, out DeityType deity) || deity == DeityType.None)
        {
            var validDeities = string.Join(", ", Enum.GetNames(typeof(DeityType)).Where(d => d != "None"));
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INVALID_DEITY, validDeities));
        }

        // Parse visibility
        var isPublic = visibility?.ToLower() != "private";

        // Check if religion name already exists
        if (_religionManager.GetReligionByName(religionName) != null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NAME_EXISTS, religionName));

        // Create the religion
        var religion = _religionManager.CreateReligion(religionName, deity, player.PlayerUID, isPublic);

        // Set up founder's player religion data (already added to Members via constructor)
        _playerProgressionDataManager.SetPlayerReligionData(player.PlayerUID, religion.ReligionUID);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_CREATED,
                religionName, deity.ToLocalizedString()));
    }

    /// <summary>
    ///     Handler for /religion join <name>
    /// </summary>
    internal TextCommandResult OnJoinReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Find the religion
        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_FOUND, religionName));

        // Check if player can join
        if (!_religionManager.CanJoinReligion(religion.ReligionUID, player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PRIVATE_NO_INVITE));

        // Apply switching penalty if needed
        if (_religionManager.HasReligion(player.PlayerUID))
            _playerProgressionDataManager.HandleReligionSwitch(player.PlayerUID);

        // Join the religion
        _playerProgressionDataManager.JoinReligion(player.PlayerUID, religion.ReligionUID);

        // Remove invitation if exists
        _religionManager.RemoveInvitation(player.PlayerUID, religion.ReligionUID);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_JOINED,
                religion.ReligionName, religion.Deity.ToLocalizedString()));
    }

    /// <summary>
    ///     Handler for /religion leave
    /// </summary>
    internal TextCommandResult OnLeaveReligion(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        // Get religion info before leaving
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        var religionName = religion?.ReligionName ??
                           LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_UNKNOWN);

        // Prevent founders from leaving (use role-based check for consistency)
        if (religion != null && religion.GetPlayerRole(player.PlayerUID) == RoleDefaults.FOUNDER_ROLE_ID)
        {
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_FOUNDER_CANNOT_LEAVE));
        }

        // Leave the religion
        _playerProgressionDataManager.LeaveReligion(player.PlayerUID);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_LEFT, religionName));
    }

    /// <summary>
    ///     Handler for /religion list [deity]
    /// </summary>
    internal TextCommandResult OnListReligions(TextCommandCallingArgs args)
    {
        var deityFilter = args.Parsers.Count > 0 ? (string?)args[0] : null;

        var religions = _religionManager.GetAllReligions();

        // Apply deity filter if specified
        if (!string.IsNullOrEmpty(deityFilter))
        {
            if (!Enum.TryParse(deityFilter, true, out DeityType deity))
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INVALID_DEITY_FILTER,
                        deityFilter));
            religions = _religionManager.GetReligionsByDeity(deity);
        }

        if (religions.Count == 0)
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_NO_RELIGIONS));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_LIST, religions.Count));
        foreach (var religion in religions.OrderByDescending(r => r.TotalPrestige))
        {
            var visibility = religion.IsPublic
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_VISIBILITY_PUBLIC)
                : LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_VISIBILITY_PRIVATE);
            sb.AppendLine(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_LIST_ENTRY,
                    religion.ReligionName,
                    religion.Deity.ToLocalizedString(),
                    visibility,
                    religion.GetMemberCount(),
                    religion.PrestigeRank.ToLocalizedString()));
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion info [name]
    /// </summary>
    internal TextCommandResult OnReligionInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var religionName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Get the religion
        ReligionData? religion;
        if (!string.IsNullOrEmpty(religionName))
        {
            religion = _religionManager.GetReligionByName(religionName);
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_FOUND, religionName));
        }
        else
        {
            // Show current religion
            if (!_religionManager.HasReligion(player.PlayerUID))
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION_SPECIFY));
            religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));
        }

        // Build info display
        var sb = new StringBuilder();
        sb.AppendLine(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_INFO, religion.ReligionName));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_DEITY,
            religion.Deity.ToLocalizedString()));
        var visibility = religion.IsPublic
            ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_VISIBILITY_PUBLIC)
            : LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_VISIBILITY_PRIVATE);
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_VISIBILITY, visibility));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_MEMBERS,
            religion.GetMemberCount()));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_RANK,
            religion.PrestigeRank.ToLocalizedString()));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE,
            religion.Prestige, religion.TotalPrestige));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_CREATED,
            religion.CreationDate.ToString("yyyy-MM-dd")));

        // Use cached founder name
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_FOUNDER,
            religion.FounderName));

        if (!string.IsNullOrEmpty(religion.Description))
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_DESCRIPTION,
                religion.Description));

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion members
    /// </summary>
    internal TextCommandResult OnListMembers(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_MEMBERS,
            religion.ReligionName, religion.GetMemberCount()));

        foreach (var memberUID in religion.MemberUIDs)
        {
            var memberPlayer = _sapi.World.PlayerByUid(memberUID);
            var memberName = memberPlayer?.PlayerName ??
                             LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_UNKNOWN);

            var memberData = _playerProgressionDataManager.GetOrCreatePlayerData(memberUID);
            var role = religion.IsFounder(memberUID)
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_ROLE_FOUNDER)
                : LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_ROLE_MEMBER);

            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_MEMBER,
                memberName, role, memberData.FavorRank.ToLocalizedString(), memberData.Favor));
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion invite <playername>
    /// </summary>
    internal TextCommandResult OnInvitePlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to invite
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.INVITE_PLAYERS))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_INVITE));

        // Find target player
        var targetPlayer = _sapi.World.AllOnlinePlayers
                .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase)) as
            IServerPlayer;

        if (targetPlayer == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND_ONLINE,
                    targetPlayerName));

        // Check if target is already a member
        if (religion.IsMember(targetPlayer.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_ALREADY_MEMBER,
                    targetPlayerName, religion.ReligionName));

        // Send invitation
        var success = _religionManager.InvitePlayer(religion.ReligionUID, targetPlayer.PlayerUID, player.PlayerUID);

        if (!success)
        {
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INVITE_FAILED));
        }

        // Notify target player (only if successful)
        targetPlayer.SendMessage(
            GlobalConstants.GeneralChatGroup,
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_NOTIFICATION_INVITED,
                religion.ReligionName, religion.ReligionName),
            EnumChatType.Notification
        );

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_INVITE_SENT, targetPlayerName));
    }

    /// <summary>
    ///     Handler for /religion kick <playername>
    /// </summary>
    internal TextCommandResult OnKickPlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to kick members
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.KICK_MEMBERS))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_KICK));

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND,
                    targetPlayerName));

        // Check if target is a member
        if (!religion.IsMember(targetPlayer.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_MEMBER,
                    targetPlayerName, religion.ReligionName));

        // Cannot kick yourself
        if (targetPlayer.PlayerUID == player.PlayerUID)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_CANNOT_KICK_SELF));

        // Kick the player
        _playerProgressionDataManager.LeaveReligion(targetPlayer.PlayerUID);

        // Notify target if online
        var targetServerPlayer = targetPlayer as IServerPlayer;
        if (targetServerPlayer != null)
            targetServerPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_NOTIFICATION_KICKED,
                    religion.ReligionName),
                EnumChatType.Notification
            );

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_KICKED,
                targetPlayerName, religion.ReligionName));
    }

    /// <summary>
    ///     Handler for /religion ban <playername> [reason] [days]
    /// </summary>
    internal TextCommandResult OnBanPlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];
        var reason = args.Parsers.Count > 1 ? (string?)args[1] : null;
        var expiryDays = args.Parsers.Count > 2 ? (int?)args[2] : null;

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to ban players
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.BAN_PLAYERS))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_BAN));

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND,
                    targetPlayerName));

        // Cannot ban yourself
        if (targetPlayer.PlayerUID == player.PlayerUID)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_CANNOT_BAN_SELF));

        // Kick the player if they're still a member
        if (religion.IsMember(targetPlayer.PlayerUID))
            _playerProgressionDataManager.LeaveReligion(targetPlayer.PlayerUID);

        // Ban the player
        var finalReason = reason ??
                          LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_NO_REASON);
        _religionManager.BanPlayer(
            religion.ReligionUID,
            targetPlayer.PlayerUID,
            player.PlayerUID,
            finalReason,
            expiryDays
        );

        // Notify target if online
        var targetServerPlayer = targetPlayer as IServerPlayer;
        if (targetServerPlayer != null)
            targetServerPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_NOTIFICATION_BANNED,
                    religion.ReligionName, finalReason),
                EnumChatType.Notification
            );

        var expiryText = expiryDays.HasValue
            ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_TEMPORARY, expiryDays.Value)
            : LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_PERMANENT);
        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_BANNED,
                targetPlayerName, religion.ReligionName, expiryText));
    }

    /// <summary>
    ///     Handler for /religion unban <playername>
    /// </summary>
    internal TextCommandResult OnUnbanPlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to ban/unban players
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.BAN_PLAYERS))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_UNBAN));

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND,
                    targetPlayerName));

        // Unban the player
        if (_religionManager.UnbanPlayer(religion.ReligionUID, targetPlayer.PlayerUID))
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_UNBANNED,
                    targetPlayerName, religion.ReligionName));
        return TextCommandResult.Error(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_BANNED,
                targetPlayerName, religion.ReligionName));
    }

    /// <summary>
    ///     Handler for /religion banlist
    /// </summary>
    internal TextCommandResult OnListBannedPlayers(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to view ban list
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.VIEW_BAN_LIST))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_VIEW_BANLIST));

        var bannedPlayers = _religionManager.GetBannedPlayers(religion.ReligionUID);

        if (bannedPlayers.Count == 0)
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_NO_BANNED_PLAYERS));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_BANLIST,
            religion.ReligionName));
        sb.AppendLine();

        foreach (var ban in bannedPlayers)
        {
            var playerName = _sapi.World.PlayerByUid(ban.PlayerUID)?.PlayerName ??
                             LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_UNKNOWN);
            var bannedBy = _sapi.World.PlayerByUid(ban.BannedByUID)?.PlayerName ??
                           LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_UNKNOWN);
            var expiry = ban.ExpiresAt.HasValue
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_EXPIRES,
                    ban.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm"))
                : LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_PERMANENT);

            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_ENTRY_HEADER,
                playerName));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_ENTRY_REASON,
                ban.Reason));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_ENTRY_BANNEDBY,
                bannedBy, ban.BannedAt.ToString("yyyy-MM-dd HH:mm")));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_BAN_ENTRY_STATUS,
                expiry));
            sb.AppendLine();
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion disband
    /// </summary>
    internal TextCommandResult OnDisbandReligion(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to disband the religion
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.DISBAND_RELIGION))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_DISBAND));

        var religionName = religion.ReligionName;

        // Remove all members
        var members = religion.MemberUIDs.ToList(); // Copy to avoid modification during iteration
        foreach (var memberUID in members)
        {
            _playerProgressionDataManager.LeaveReligion(memberUID);

            // Notify member if online
            var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
            if (memberPlayer != null)
            {
                // Send chat notification to other members
                if (memberUID != player.PlayerUID)
                    memberPlayer.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        LocalizationService.Instance.Get(
                            LocalizationKeys.CMD_RELIGION_NOTIFICATION_DISBANDED_BY_FOUNDER,
                            religionName),
                        EnumChatType.Notification
                    );

                // Send religion state changed packet to all members (including founder)
                if (_serverChannel != null)
                {
                    var statePacket = new ReligionStateChangedPacket
                    {
                        Reason = LocalizationService.Instance.Get(
                            LocalizationKeys.CMD_RELIGION_NOTIFICATION_DISBANDED, religionName),
                        HasReligion = false
                    };
                    _serverChannel.SendPacket(statePacket, memberPlayer);
                }
            }
        }

        // Delete the religion
        _religionManager.DeleteReligion(religion.ReligionUID, player.PlayerUID);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_DISBANDED, religionName));
    }

    /// <summary>
    ///     Handler for /religion description <text>
    /// </summary>
    internal TextCommandResult OnSetDescription(TextCommandCallingArgs args)
    {
        var description = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check if player is in a religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));

        // Check if player has permission to edit description
        if (!religion.HasPermission(playerId, RolePermissions.EDIT_DESCRIPTION))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_PERMISSION_EDIT_DESC));

        // Set description
        religion.Description = description;

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_DESCRIPTION_SET,
                religion.ReligionName));
    }

    /// <summary>
    ///     Handler for /religion prestige info [religionname]
    /// </summary>
    internal TextCommandResult OnPrestigeInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var religionName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Get the religion
        ReligionData? religion;
        if (!string.IsNullOrEmpty(religionName))
        {
            religion = _religionManager.GetReligionByName(religionName);
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_FOUND, religionName));
        }
        else
        {
            // Show current religion's prestige
            if (!_religionManager.HasReligion(player.PlayerUID))
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NO_RELIGION_SPECIFY));
            religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_DATA_NOT_FOUND));
        }

        // Get prestige progress
        var (current, nextThreshold, nextRank) = _religionPrestigeManager.GetPrestigeProgress(religion.ReligionUID);
        var currentRank = religion.PrestigeRank;

        // Build info display
        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_PRESTIGE,
            religion.ReligionName));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_CURRENT_RANK,
            (int)currentRank, currentRank.ToLocalizedString()));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_CURRENT,
            current));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_TOTAL,
            religion.TotalPrestige));

        if (currentRank < PrestigeRank.Mythic)
        {
            var nextRankEnum = (PrestigeRank)nextRank;
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_NEXT_RANK,
                nextRank, nextRankEnum.ToLocalizedString()));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_PROGRESS,
                current, nextThreshold, current * 100 / nextThreshold));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_REMAINING,
                nextThreshold - current));
        }
        else
        {
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_MAX_RANK));
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion prestige add <religionname> <amount> [reason]
    /// </summary>
    internal TextCommandResult OnPrestigeAdd(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];
        var amount = (int)args[1];
        var reason = args.Parsers.Count > 2 ? (string?)args[2] : null;

        if (amount <= 0)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_AMOUNT_POSITIVE));

        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_FOUND, religionName));

        var oldPrestige = religion.Prestige;
        var oldRank = religion.PrestigeRank;

        var finalReason = reason ??
                          LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_ADMIN_COMMAND);
        _religionPrestigeManager.AddPrestige(religion.ReligionUID, amount, finalReason);

        var newPrestige = religion.Prestige;
        var newRank = religion.PrestigeRank;

        var rankChanged = newRank > oldRank
            ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_RANK_CHANGE,
                oldRank.ToLocalizedString(), newRank.ToLocalizedString())
            : "";

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_PRESTIGE_ADDED,
                amount, religionName, oldPrestige, newPrestige, rankChanged));
    }

    /// <summary>
    ///     Handler for /religion prestige set <religionname> <amount>
    /// </summary>
    internal TextCommandResult OnPrestigeSet(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];
        var amount = (int)args[1];

        if (amount < 0)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_AMOUNT_NON_NEGATIVE));

        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_FOUND, religionName));

        var oldPrestige = religion.Prestige;
        var oldRank = religion.PrestigeRank;

        // Set prestige directly and update rank
        religion.Prestige = amount;
        religion.TotalPrestige = Math.Max(religion.TotalPrestige, amount); // Ensure total is at least as high
        _religionPrestigeManager.UpdatePrestigeRank(religion.ReligionUID);

        var newRank = religion.PrestigeRank;
        var rankChanged = newRank != oldRank
            ? LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_PRESTIGE_RANK_CHANGE,
                oldRank.ToLocalizedString(), newRank.ToLocalizedString())
            : "";

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_PRESTIGE_SET,
                religionName, amount, oldPrestige, rankChanged));
    }

    /// <summary>
    ///     Repairs a specific player's membership state
    /// </summary>
    private TextCommandResult RepairSpecificPlayer(string playerName)
    {
        // Find player by name (check both online and offline)
        var player = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        if (player == null)
        {
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND, playerName));
        }

        // Cast to concrete type for validation method
        var religionManager = _religionManager;
        var playerDataManager = _playerProgressionDataManager as PlayerProgressionDataManager;

        if (religionManager == null || playerDataManager == null)
        {
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INTERNAL));
        }

        // Validate consistency
        var (isConsistent, issues) =
            religionManager.ValidateMembershipConsistency(player.PlayerUID);

        if (isConsistent)
        {
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_REPAIR_NO_ISSUES, playerName));
        }

        // Repair inconsistency
        var wasRepaired =
            religionManager.RepairMembershipConsistency(player.PlayerUID);

        if (wasRepaired)
        {
            // Trigger save and notify player if online
            _playerProgressionDataManager.NotifyPlayerDataChanged(player.PlayerUID);
            religionManager.TriggerSave();

            var serverPlayer = player as IServerPlayer;
            if (serverPlayer != null)
            {
                serverPlayer.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    LocalizationService.Instance.Get(
                        LocalizationKeys.CMD_RELIGION_NOTIFICATION_MEMBERSHIP_REPAIRED),
                    EnumChatType.Notification
                );
            }

            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_REPAIR_PLAYER,
                    playerName, issues));
        }

        return TextCommandResult.Error(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_REPAIR_FAILED, playerName));
    }

    /// <summary>
    ///     Scans and repairs ALL players' membership state
    /// </summary>
    private TextCommandResult RepairAllPlayers()
    {
        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_REPAIR_SCAN));

        int scanned = 0;
        int consistent = 0;
        int repaired = 0;
        int failed = 0;

        // Cast to concrete type for validation method
        var religionManager = _religionManager as ReligionManager;
        var playerDataManager = _playerProgressionDataManager as PlayerProgressionDataManager;

        if (religionManager == null || playerDataManager == null)
        {
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INTERNAL));
        }

        // Get all players (online and offline)
        // Note: This only checks players who have PlayerReligionData saved
        // We also need to check ReligionManager for all members
        var allPlayerUIDs = new HashSet<string>();

        // Collect UIDs from all religions
        foreach (var religion in _religionManager.GetAllReligions())
        {
            foreach (var memberUID in religion.MemberUIDs)
            {
                allPlayerUIDs.Add(memberUID);
            }
        }

        // Collect UIDs from all online players
        foreach (var player in _sapi.World.AllPlayers)
        {
            allPlayerUIDs.Add(player.PlayerUID);
        }

        // Scan each player
        foreach (var playerUID in allPlayerUIDs)
        {
            scanned++;

            var (isConsistent, issues) =
                religionManager.ValidateMembershipConsistency(playerUID);

            if (isConsistent)
            {
                consistent++;
                continue;
            }

            // Found inconsistency - attempt repair
            var player = _sapi.World.PlayerByUid(playerUID);
            var playerName = player?.PlayerName ?? playerUID;

            var wasRepaired =
                religionManager.RepairMembershipConsistency(playerUID);

            if (wasRepaired)
            {
                repaired++;
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_SUCCESS,
                    playerName));
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_ISSUE,
                    issues));
                sb.AppendLine();

                // Notify player if online
                _playerProgressionDataManager.NotifyPlayerDataChanged(playerUID);
                var serverPlayer = player as IServerPlayer;
                if (serverPlayer != null)
                {
                    serverPlayer.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        LocalizationService.Instance.Get(
                            LocalizationKeys.CMD_RELIGION_NOTIFICATION_MEMBERSHIP_REPAIRED),
                        EnumChatType.Notification
                    );
                }
            }
            else
            {
                failed++;
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_FAILED_ENTRY,
                    playerName));
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_ISSUE,
                    issues));
                sb.AppendLine();
            }
        }

        // Trigger save after all repairs
        if (repaired > 0)
        {
            religionManager.TriggerSave();
        }

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_HEADER_REPAIR_SUMMARY));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_SCANNED, scanned));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_CONSISTENT,
            consistent));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_REPAIRED, repaired));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_FORMAT_REPAIR_FAILED_COUNT,
            failed));

        if (repaired == 0 && failed == 0)
        {
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_ALL_CONSISTENT));
        }

        return TextCommandResult.Success(sb.ToString());
    }

    #endregion

    #region Admin Command Handlers (Privilege.root)

    /// <summary>
    ///     /religion admin repair [playername] - Repair religion membership inconsistencies
    /// </summary>
    internal TextCommandResult OnAdminRepair(TextCommandCallingArgs args)
    {
        var playerName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        if (playerName != null)
        {
            // Repair specific player
            return RepairSpecificPlayer(playerName);
        }
        else
        {
            // Repair all players
            return RepairAllPlayers();
        }
    }

    /// <summary>
    ///     /religion admin join religionname> [playername] - Force join a player to a religion
    /// </summary>
    internal TextCommandResult OnAdminJoin(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var religionName = (string)args[0];
        var targetPlayerName = args.Parsers.Count > 1 ? (string?)args[1] : null;

        // Find the target player (default to caller)
        IServerPlayer resolvedTargetPlayer;
        if (targetPlayerName != null)
        {
            var found = _sapi.World.AllPlayers
                .FirstOrDefault(p => string.Equals(p.PlayerName, targetPlayerName, StringComparison.OrdinalIgnoreCase));
            if (found is null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND,
                        targetPlayerName));
            if (found is not IServerPlayer serverPlayer)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INTERNAL));
            resolvedTargetPlayer = serverPlayer;
        }
        else
        {
            resolvedTargetPlayer = player;
        }

        // Find the religion
        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_NOT_FOUND, religionName));

        // Check if player already in this religion
        var currentReligion = _religionManager.GetPlayerReligion(resolvedTargetPlayer.PlayerUID);
        if (currentReligion != null && currentReligion.ReligionUID == religion.ReligionUID)
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_INFO_ALREADY_MEMBER_ADMIN,
                    resolvedTargetPlayer.PlayerName, religionName));

        // If player is in a different religion, force leave first
        if (currentReligion != null && !string.IsNullOrEmpty(currentReligion.ReligionUID))
        {
            _playerProgressionDataManager.LeaveReligion(resolvedTargetPlayer.PlayerUID);
            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} removed {resolvedTargetPlayer.PlayerName} from {currentReligion.ReligionName}");
        }

        // Join the new religion (bypass CanJoinReligion check)
        _playerProgressionDataManager.JoinReligion(resolvedTargetPlayer.PlayerUID, religion.ReligionUID);

        // Remove any pending invitation if exists
        if (_religionManager.HasInvitation(resolvedTargetPlayer.PlayerUID, religion.ReligionUID))
        {
            _religionManager.RemoveInvitation(resolvedTargetPlayer.PlayerUID, religion.ReligionUID);
        }

        // Notify player data changed (triggers HUD update)
        _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedTargetPlayer.PlayerUID);

        // Send religion state changed packet to target player
        if (_serverChannel != null)
        {
            var statePacket = new ReligionStateChangedPacket
            {
                Reason = LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_NOTIFICATION_ADMIN_ADDED,
                    religionName),
                HasReligion = true
            };
            _serverChannel.SendPacket(statePacket, resolvedTargetPlayer);
        }

        // Broadcast role updates to all religion members (so they see the new member in the member list)
        var targetReligion = _religionManager.GetReligion(religion.ReligionUID);
        if (targetReligion != null && _serverChannel != null)
        {
            var rolesResponse = new ReligionRolesResponse
            {
                Success = true,
                Roles = _roleManager.GetReligionRoles(targetReligion.ReligionUID),
                MemberRoles = targetReligion.MemberRoles,
                MemberNames = new Dictionary<string, string>()
            };

            foreach (var uid in targetReligion.MemberUIDs)
                rolesResponse.MemberNames[uid] = targetReligion.GetMemberName(uid);

            // Send to all online members
            foreach (var memberUID in targetReligion.MemberUIDs)
            {
                var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
                if (memberPlayer != null) _serverChannel.SendPacket(rolesResponse, memberPlayer);
            }
        }

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} added {resolvedTargetPlayer.PlayerName} to {religionName}");

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_ADMIN_ADDED,
                resolvedTargetPlayer.PlayerName, religionName));
    }

    /// <summary>
    ///     /religion admin leave [playername] - Force remove a player from their religion
    /// </summary>
    internal TextCommandResult OnAdminLeave(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var targetPlayerName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Find the target player (default to caller)
        IServerPlayer resolvedTargetPlayer;
        if (targetPlayerName != null)
        {
            var found = _sapi.World.AllPlayers
                .FirstOrDefault(p => string.Equals(p.PlayerName, targetPlayerName, StringComparison.OrdinalIgnoreCase));
            if (found is null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NOT_FOUND,
                        targetPlayerName));
            if (found is not IServerPlayer serverPlayer)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_INTERNAL));
            resolvedTargetPlayer = serverPlayer;
        }
        else
        {
            resolvedTargetPlayer = player;
        }

        // Check if player is in a religion
        if (!_religionManager.HasReligion(resolvedTargetPlayer.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NO_RELIGION,
                    resolvedTargetPlayer.PlayerName));

        // Get the religion
        var religion = _religionManager.GetPlayerReligion(resolvedTargetPlayer.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_PLAYER_NO_RELIGION,
                    resolvedTargetPlayer.PlayerName));

        var religionName = religion.ReligionName;

        // Handle founder special cases
        if (religion.FounderUID == resolvedTargetPlayer.PlayerUID)
        {
            if (religion.MemberUIDs.Count > 1)
            {
                // Transfer founder to oldest member first
                var newFounderUID = religion.MemberUIDs[1]; // First member (oldest)
                var result = _roleManager.TransferFounder(religion.ReligionUID, resolvedTargetPlayer.PlayerUID,
                    newFounderUID);

                if (!result.success)
                    return TextCommandResult.Error(
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_ERROR_TRANSFER_FOUNDER_FAILED,
                            result.error));

                // Now leave
                _playerProgressionDataManager.LeaveReligion(resolvedTargetPlayer.PlayerUID);

                // Notify player data changed (triggers HUD update)
                _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedTargetPlayer.PlayerUID);

                // Send religion state changed packet to target player
                if (_serverChannel != null)
                {
                    var statePacket = new ReligionStateChangedPacket
                    {
                        Reason = LocalizationService.Instance.Get(
                            LocalizationKeys.CMD_RELIGION_NOTIFICATION_ADMIN_REMOVED, religionName),
                        HasReligion = false
                    };
                    _serverChannel.SendPacket(statePacket, resolvedTargetPlayer);
                }

                // Broadcast role updates to remaining religion members (so they see updated member list and new founder)
                var updatedReligion = _religionManager.GetReligion(religion.ReligionUID);
                if (updatedReligion != null && _serverChannel != null)
                {
                    var rolesResponse = new ReligionRolesResponse
                    {
                        Success = true,
                        Roles = _roleManager.GetReligionRoles(updatedReligion.ReligionUID),
                        MemberRoles = updatedReligion.MemberRoles,
                        MemberNames = new Dictionary<string, string>()
                    };

                    foreach (var uid in updatedReligion.MemberUIDs)
                        rolesResponse.MemberNames[uid] = updatedReligion.GetMemberName(uid);

                    // Send to all online members (including new founder)
                    foreach (var memberUID in updatedReligion.MemberUIDs)
                    {
                        var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
                        if (memberPlayer != null) _serverChannel.SendPacket(rolesResponse, memberPlayer);
                    }
                }

                var newFounderName = religion.GetMemberName(newFounderUID);
                _sapi.Logger.Notification(
                    $"[DivineAscension] Admin: {player.PlayerName} removed founder {resolvedTargetPlayer.PlayerName} from {religionName}. Founder transferred to {newFounderName}");

                return TextCommandResult.Success(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_ADMIN_LEFT_FOUNDER_TRANSFER,
                        resolvedTargetPlayer.PlayerName, religionName, newFounderName));
            }
            else
            {
                // Sole member - disband religion
                _religionManager.DeleteReligion(religion.ReligionUID, resolvedTargetPlayer.PlayerUID);

                // Notify player data changed (triggers HUD update)
                _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedTargetPlayer.PlayerUID);

                // Send religion disbanded notification
                if (_serverChannel != null)
                {
                    var statePacket = new ReligionStateChangedPacket
                    {
                        Reason = LocalizationService.Instance.Get(
                            LocalizationKeys.CMD_RELIGION_NOTIFICATION_ADMIN_DISBANDED, religionName),
                        HasReligion = false
                    };
                    _serverChannel.SendPacket(statePacket, resolvedTargetPlayer);
                }

                _sapi.Logger.Notification(
                    $"[DivineAscension] Admin: {player.PlayerName} removed sole founder {resolvedTargetPlayer.PlayerName} from {religionName}. Religion disbanded");

                return TextCommandResult.Success(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_ADMIN_LEFT_DISBANDED,
                        resolvedTargetPlayer.PlayerName, religionName));
            }
        }
        else
        {
            // Regular member - just leave
            _playerProgressionDataManager.LeaveReligion(resolvedTargetPlayer.PlayerUID);

            // Notify player data changed (triggers HUD update)
            _playerProgressionDataManager.NotifyPlayerDataChanged(resolvedTargetPlayer.PlayerUID);

            // Send religion state changed packet to target player
            if (_serverChannel != null)
            {
                var statePacket = new ReligionStateChangedPacket
                {
                    Reason = LocalizationService.Instance.Get(
                        LocalizationKeys.CMD_RELIGION_NOTIFICATION_ADMIN_REMOVED, religionName),
                    HasReligion = false
                };
                _serverChannel.SendPacket(statePacket, resolvedTargetPlayer);
            }

            // Broadcast role updates to remaining religion members (so they see updated member list)
            var updatedReligion = _religionManager.GetReligion(religion.ReligionUID);
            if (updatedReligion != null && _serverChannel != null)
            {
                var rolesResponse = new ReligionRolesResponse
                {
                    Success = true,
                    Roles = _roleManager.GetReligionRoles(updatedReligion.ReligionUID),
                    MemberRoles = updatedReligion.MemberRoles,
                    MemberNames = new Dictionary<string, string>()
                };

                foreach (var uid in updatedReligion.MemberUIDs)
                    rolesResponse.MemberNames[uid] = updatedReligion.GetMemberName(uid);

                // Send to all online members
                foreach (var memberUID in updatedReligion.MemberUIDs)
                {
                    var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
                    if (memberPlayer != null) _serverChannel.SendPacket(rolesResponse, memberPlayer);
                }
            }

            _sapi.Logger.Notification(
                $"[DivineAscension] Admin: {player.PlayerName} removed {resolvedTargetPlayer.PlayerName} from {religionName}");

            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_RELIGION_SUCCESS_ADMIN_LEFT,
                    resolvedTargetPlayer.PlayerName, religionName));
        }
    }

    #endregion
}