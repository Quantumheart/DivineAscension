using System;
using System.Linq;
using System.Text;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
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
    IServerNetworkChannel serverChannel)
{
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    private readonly IReligionPrestigeManager _religionPrestigeManager =
        religionPrestigeManager ?? throw new ArgumentNullException(nameof(religionPrestigeManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly IServerNetworkChannel? _serverChannel =
        serverChannel ?? throw new ArgumentNullException(nameof(serverChannel));

    /// <summary>
    ///     Registers all religion commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("religion")
            .WithDescription("Manage religions and congregation membership")
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("create")
            .WithDescription("Create a new religion")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"),
                _sapi.ChatCommands.Parsers.Word("deity"),
                _sapi.ChatCommands.Parsers.OptionalWord("visibility"))
            .HandleWith(OnCreateReligion)
            .EndSubCommand()
            .BeginSubCommand("join")
            .WithDescription("Join a religion")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnJoinReligion)
            .EndSubCommand()
            .BeginSubCommand("leave")
            .WithDescription("Leave your current religion")
            .HandleWith(OnLeaveReligion)
            .EndSubCommand()
            .BeginSubCommand("list")
            .WithDescription("List all religions")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("deity"))
            .HandleWith(OnListReligions)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription("Show religion information")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnReligionInfo)
            .EndSubCommand()
            .BeginSubCommand("members")
            .WithDescription("Show members of your religion")
            .HandleWith(OnListMembers)
            .EndSubCommand()
            .BeginSubCommand("invite")
            .WithDescription("Invite a player to your religion")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnInvitePlayer)
            .EndSubCommand()
            .BeginSubCommand("kick")
            .WithDescription("Kick a player from your religion")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnKickPlayer)
            .EndSubCommand()
            .BeginSubCommand("ban")
            .WithDescription("Ban a player from your religion")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"),
                _sapi.ChatCommands.Parsers.OptionalAll("reason"),
                _sapi.ChatCommands.Parsers.OptionalInt("days"))
            .HandleWith(OnBanPlayer)
            .EndSubCommand()
            .BeginSubCommand("unban")
            .WithDescription("Unban a player from your religion")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnUnbanPlayer)
            .EndSubCommand()
            .BeginSubCommand("banlist")
            .WithDescription("List all banned players")
            .HandleWith(OnListBannedPlayers)
            .EndSubCommand()
            .BeginSubCommand("disband")
            .WithDescription("Disband your religion")
            .HandleWith(OnDisbandReligion)
            .EndSubCommand()
            .BeginSubCommand("description")
            .WithDescription("Set your religion's description")
            .WithArgs(_sapi.ChatCommands.Parsers.All("text"))
            .HandleWith(OnSetDescription)
            .EndSubCommand()
            .BeginSubCommand("prestige")
            .WithDescription("Manage religion prestige")
            .BeginSubCommand("info")
            .WithDescription("View detailed prestige information for a religion")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("religionname"))
            .HandleWith(OnPrestigeInfo)
            .EndSubCommand()
            .BeginSubCommand("add")
            .WithDescription("Add prestige to a religion (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("religionname"),
                _sapi.ChatCommands.Parsers.Int("amount"),
                _sapi.ChatCommands.Parsers.OptionalAll("reason"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnPrestigeAdd)
            .EndSubCommand()
            .BeginSubCommand("set")
            .WithDescription("Set religion prestige to a specific amount (Admin only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("religionname"),
                _sapi.ChatCommands.Parsers.Int("amount"))
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnPrestigeSet)
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player already has a religion
        var playerId = player.PlayerUID;
        if (_religionManager.HasReligion(playerId))
            return TextCommandResult.Error("You are already in a religion. Use /religion leave first.");

        // Parse deity type
        if (!Enum.TryParse(deityName, true, out DeityType deity) || deity == DeityType.None)
        {
            var validDeities = string.Join(", ", Enum.GetNames(typeof(DeityType)).Where(d => d != "None"));
            return TextCommandResult.Error($"Invalid deity. Valid options: {validDeities}");
        }

        // Parse visibility
        var isPublic = visibility?.ToLower() != "private";

        // Check if religion name already exists
        if (_religionManager.GetReligionByName(religionName) != null)
            return TextCommandResult.Error($"A religion named '{religionName}' already exists");

        // Create the religion
        var religion = _religionManager.CreateReligion(religionName, deity, player.PlayerUID, isPublic);

        // Set up founder's player religion data (already added to Members via constructor)
        _playerProgressionDataManager.SetPlayerReligionData(player.PlayerUID, religion.ReligionUID);

        return TextCommandResult.Success(
            $"Religion '{religionName}' created! You are now the founder serving {deity}.");
    }

    /// <summary>
    ///     Handler for /religion join <name>
    /// </summary>
    internal TextCommandResult OnJoinReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Find the religion
        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null) return TextCommandResult.Error($"Religion '{religionName}' not found");

        // Check if player can join
        if (!_religionManager.CanJoinReligion(religion.ReligionUID, player.PlayerUID))
            return TextCommandResult.Error("This religion is private and you have not been invited");

        // Apply switching penalty if needed
        if (_religionManager.HasReligion(player.PlayerUID))
            _playerProgressionDataManager.HandleReligionSwitch(player.PlayerUID);

        // Join the religion
        _playerProgressionDataManager.JoinReligion(player.PlayerUID, religion.ReligionUID);

        // Remove invitation if exists
        _religionManager.RemoveInvitation(player.PlayerUID, religion.ReligionUID);

        return TextCommandResult.Success($"You have joined {religion.ReligionName}! May {religion.Deity} guide you.");
    }

    /// <summary>
    ///     Handler for /religion leave
    /// </summary>
    internal TextCommandResult OnLeaveReligion(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        // Get religion info before leaving
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        var religionName = religion?.ReligionName ?? "Unknown";

        // Prevent founders from leaving (use role-based check for consistency)
        if (religion != null && religion.GetPlayerRole(player.PlayerUID) == RoleDefaults.FOUNDER_ROLE_ID)
        {
            return TextCommandResult.Error(
                "Founders cannot leave their religion. Transfer founder status or disband the religion instead.");
        }

        // Leave the religion
        _playerProgressionDataManager.LeaveReligion(player.PlayerUID);

        return TextCommandResult.Success($"You have left {religionName}");
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
                return TextCommandResult.Error($"Invalid deity: {deityFilter}");
            religions = _religionManager.GetReligionsByDeity(deity);
        }

        if (religions.Count == 0) return TextCommandResult.Success("No religions found");

        var sb = new StringBuilder();
        sb.AppendLine("=== Religions ===");
        foreach (var religion in religions.OrderByDescending(r => r.TotalPrestige))
        {
            var visibility = religion.IsPublic ? "Public" : "Private";
            sb.AppendLine(
                $"- {religion.ReligionName} ({religion.Deity}) | {visibility} | {religion.GetMemberCount()} members | Rank: {religion.PrestigeRank}");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion info [name]
    /// </summary>
    internal TextCommandResult OnReligionInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var religionName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Get the religion
        ReligionData? religion;
        if (!string.IsNullOrEmpty(religionName))
        {
            religion = _religionManager.GetReligionByName(religionName);
            if (religion == null) return TextCommandResult.Error($"Religion '{religionName}' not found");
        }
        else
        {
            // Show current religion
            if (!_religionManager.HasReligion(player.PlayerUID))
                return TextCommandResult.Error("You are not in any religion. Specify a religion name to view.");
            religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion == null) return TextCommandResult.Error("Could not find your religion data");
        }

        // Build info display
        var sb = new StringBuilder();
        sb.AppendLine($"=== {religion.ReligionName} ===");
        sb.AppendLine($"Deity: {religion.Deity}");
        sb.AppendLine($"Visibility: {(religion.IsPublic ? "Public" : "Private")}");
        sb.AppendLine($"Members: {religion.GetMemberCount()}");
        sb.AppendLine($"Prestige Rank: {religion.PrestigeRank}");
        sb.AppendLine($"Prestige: {religion.Prestige} (Total: {religion.TotalPrestige})");
        sb.AppendLine($"Created: {religion.CreationDate:yyyy-MM-dd}");

        // Use cached founder name
        sb.AppendLine($"Founder: {religion.FounderName}");

        if (!string.IsNullOrEmpty(religion.Description)) sb.AppendLine($"Description: {religion.Description}");

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion members
    /// </summary>
    internal TextCommandResult OnListMembers(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        var sb = new StringBuilder();
        sb.AppendLine($"=== {religion.ReligionName} Members ({religion.GetMemberCount()}) ===");

        foreach (var memberUID in religion.MemberUIDs)
        {
            var memberPlayer = _sapi.World.PlayerByUid(memberUID);
            var memberName = memberPlayer?.PlayerName ?? "Unknown";

            var memberData = _playerProgressionDataManager.GetOrCreatePlayerData(memberUID);
            var role = religion.IsFounder(memberUID) ? "Founder" : "Member";

            sb.AppendLine($"- {memberName} ({role}) | Rank: {memberData.FavorRank} | Favor: {memberData.Favor}");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to invite
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.INVITE_PLAYERS))
            return TextCommandResult.Error("You don't have permission to invite players");

        // Find target player
        var targetPlayer = _sapi.World.AllOnlinePlayers
                .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase)) as
            IServerPlayer;

        if (targetPlayer == null) return TextCommandResult.Error($"Player '{targetPlayerName}' not found online");

        // Check if target is already a member
        if (religion.IsMember(targetPlayer.PlayerUID))
            return TextCommandResult.Error($"{targetPlayerName} is already a member of {religion.ReligionName}");

        // Send invitation
        var success = _religionManager.InvitePlayer(religion.ReligionUID, targetPlayer.PlayerUID, player.PlayerUID);

        if (!success)
        {
            return TextCommandResult.Error("Failed to send invitation. They may already have a pending invite.");
        }

        // Notify target player (only if successful)
        targetPlayer.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"You have been invited to join {religion.ReligionName}! Use /religion join {religion.ReligionName} to accept.",
            EnumChatType.Notification
        );

        return TextCommandResult.Success($"Invitation sent to {targetPlayerName}");
    }

    /// <summary>
    ///     Handler for /religion kick <playername>
    /// </summary>
    internal TextCommandResult OnKickPlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to kick members
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.KICK_MEMBERS))
            return TextCommandResult.Error("You don't have permission to kick members");

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null) return TextCommandResult.Error($"Player '{targetPlayerName}' not found");

        // Check if target is a member
        if (!religion.IsMember(targetPlayer.PlayerUID))
            return TextCommandResult.Error($"{targetPlayerName} is not a member of {religion.ReligionName}");

        // Cannot kick yourself
        if (targetPlayer.PlayerUID == player.PlayerUID)
            return TextCommandResult.Error(
                "You cannot kick yourself. Use /religion disband or /religion leave instead.");

        // Kick the player
        _playerProgressionDataManager.LeaveReligion(targetPlayer.PlayerUID);

        // Notify target if online
        var targetServerPlayer = targetPlayer as IServerPlayer;
        if (targetServerPlayer != null)
            targetServerPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"You have been removed from {religion.ReligionName}",
                EnumChatType.Notification
            );

        return TextCommandResult.Success($"{targetPlayerName} has been removed from {religion.ReligionName}");
    }

    /// <summary>
    ///     Handler for /religion ban <playername> [reason] [days]
    /// </summary>
    internal TextCommandResult OnBanPlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];
        var reason = args.Parsers.Count > 1 ? (string?)args[1] : "No reason provided";
        var expiryDays = args.Parsers.Count > 2 ? (int?)args[2] : null;

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to ban players
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.BAN_PLAYERS))
            return TextCommandResult.Error("You don't have permission to ban players");

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null) return TextCommandResult.Error($"Player '{targetPlayerName}' not found");

        // Cannot ban yourself
        if (targetPlayer.PlayerUID == player.PlayerUID)
            return TextCommandResult.Error("You cannot ban yourself.");

        // Kick the player if they're still a member
        if (religion.IsMember(targetPlayer.PlayerUID))
            _playerProgressionDataManager.LeaveReligion(targetPlayer.PlayerUID);

        // Ban the player
        _religionManager.BanPlayer(
            religion.ReligionUID,
            targetPlayer.PlayerUID,
            player.PlayerUID,
            reason ?? "No reason provided",
            expiryDays
        );

        // Notify target if online
        var targetServerPlayer = targetPlayer as IServerPlayer;
        if (targetServerPlayer != null)
            targetServerPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"You have been banned from {religion.ReligionName}. Reason: {reason}",
                EnumChatType.Notification
            );

        var expiryText = expiryDays.HasValue ? $" for {expiryDays} days" : " permanently";
        return TextCommandResult.Success(
            $"{targetPlayerName} has been banned from {religion.ReligionName}{expiryText}");
    }

    /// <summary>
    ///     Handler for /religion unban <playername>
    /// </summary>
    internal TextCommandResult OnUnbanPlayer(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to ban/unban players
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.BAN_PLAYERS))
            return TextCommandResult.Error("You don't have permission to unban players");

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null) return TextCommandResult.Error($"Player '{targetPlayerName}' not found");

        // Unban the player
        if (_religionManager.UnbanPlayer(religion.ReligionUID, targetPlayer.PlayerUID))
            return TextCommandResult.Success($"{targetPlayerName} has been unbanned from {religion.ReligionName}");
        return TextCommandResult.Error($"{targetPlayerName} is not banned from {religion.ReligionName}");
    }

    /// <summary>
    ///     Handler for /religion banlist
    /// </summary>
    internal TextCommandResult OnListBannedPlayers(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to view ban list
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.VIEW_BAN_LIST))
            return TextCommandResult.Error("You don't have permission to view the ban list");

        var bannedPlayers = _religionManager.GetBannedPlayers(religion.ReligionUID);

        if (bannedPlayers.Count == 0)
            return TextCommandResult.Success("No players are currently banned from your religion");

        var sb = new StringBuilder();
        sb.AppendLine($"Banned players in {religion.ReligionName}:");
        sb.AppendLine();

        foreach (var ban in bannedPlayers)
        {
            var playerName = _sapi.World.PlayerByUid(ban.PlayerUID)?.PlayerName ?? "Unknown";
            var bannedBy = _sapi.World.PlayerByUid(ban.BannedByUID)?.PlayerName ?? "Unknown";
            var expiry = ban.ExpiresAt.HasValue
                ? $"expires on {ban.ExpiresAt.Value:yyyy-MM-dd HH:mm}"
                : "permanent";

            sb.AppendLine($"  • {playerName}");
            sb.AppendLine($"    Reason: {ban.Reason}");
            sb.AppendLine($"    Banned by: {bannedBy} on {ban.BannedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"    Status: {expiry}");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId)) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to disband the religion
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.DISBAND_RELIGION))
            return TextCommandResult.Error("You don't have permission to disband the religion");

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
                        $"{religionName} has been disbanded by its founder",
                        EnumChatType.Notification
                    );

                // Send religion state changed packet to all members (including founder)
                if (_serverChannel != null)
                {
                    var statePacket = new ReligionStateChangedPacket
                    {
                        Reason = $"{religionName} has been disbanded",
                        HasReligion = false
                    };
                    _serverChannel.SendPacket(statePacket, memberPlayer);
                }
            }
        }

        // Delete the religion
        _religionManager.DeleteReligion(religion.ReligionUID, player.PlayerUID);

        return TextCommandResult.Success($"{religionName} has been disbanded");
    }

    /// <summary>
    ///     Handler for /religion description <text>
    /// </summary>
    internal TextCommandResult OnSetDescription(TextCommandCallingArgs args)
    {
        var description = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Check if player is in a religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId)) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to edit description
        if (!religion.HasPermission(playerId, RolePermissions.EDIT_DESCRIPTION))
            return TextCommandResult.Error("You don't have permission to edit the religion description");

        // Set description
        religion.Description = description;

        return TextCommandResult.Success($"Description set for {religion.ReligionName}");
    }

    /// <summary>
    ///     Handler for /religion prestige info [religionname]
    /// </summary>
    internal TextCommandResult OnPrestigeInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var religionName = args.Parsers.Count > 0 ? (string?)args[0] : null;

        // Get the religion
        ReligionData? religion;
        if (!string.IsNullOrEmpty(religionName))
        {
            religion = _religionManager.GetReligionByName(religionName);
            if (religion == null) return TextCommandResult.Error($"Religion '{religionName}' not found");
        }
        else
        {
            // Show current religion's prestige
            if (!_religionManager.HasReligion(player.PlayerUID))
                return TextCommandResult.Error("You are not in any religion. Specify a religion name to view.");
            religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion == null) return TextCommandResult.Error("Could not find your religion data");
        }

        // Get prestige progress
        var (current, nextThreshold, nextRank) = _religionPrestigeManager.GetPrestigeProgress(religion.ReligionUID);
        var currentRank = religion.PrestigeRank;

        // Build info display
        var sb = new StringBuilder();
        sb.AppendLine($"=== {religion.ReligionName} Prestige ===");
        sb.AppendLine($"Current Rank: {(int)currentRank} ({currentRank})");
        sb.AppendLine($"Current Prestige: {current}");
        sb.AppendLine($"Total Prestige Earned: {religion.TotalPrestige}");

        if (currentRank < PrestigeRank.Mythic)
        {
            sb.AppendLine($"Next Rank: {nextRank} ({(PrestigeRank)nextRank})");
            sb.AppendLine($"Progress: {current}/{nextThreshold} ({current * 100 / nextThreshold}%)");
            sb.AppendLine($"Remaining: {nextThreshold - current}");
        }
        else
        {
            sb.AppendLine("Maximum rank achieved!");
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
        var reason = args.Parsers.Count > 2 ? (string?)args[2] : "Admin command";

        if (amount <= 0) return TextCommandResult.Error("Amount must be positive");

        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null) return TextCommandResult.Error($"Religion '{religionName}' not found");

        var oldPrestige = religion.Prestige;
        var oldRank = religion.PrestigeRank;

        _religionPrestigeManager.AddPrestige(religion.ReligionUID, amount, reason ?? "Admin command");

        var newPrestige = religion.Prestige;
        var newRank = religion.PrestigeRank;

        var rankChanged = newRank > oldRank ? $" (Rank: {oldRank} → {newRank})" : "";

        return TextCommandResult.Success(
            $"Added {amount} prestige to {religionName}. Total: {oldPrestige} → {newPrestige}{rankChanged}");
    }

    /// <summary>
    ///     Handler for /religion prestige set <religionname> <amount>
    /// </summary>
    internal TextCommandResult OnPrestigeSet(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];
        var amount = (int)args[1];

        if (amount < 0) return TextCommandResult.Error("Amount cannot be negative");

        var religion = _religionManager.GetReligionByName(religionName);
        if (religion == null) return TextCommandResult.Error($"Religion '{religionName}' not found");

        var oldPrestige = religion.Prestige;
        var oldRank = religion.PrestigeRank;

        // Set prestige directly and update rank
        religion.Prestige = amount;
        religion.TotalPrestige = Math.Max(religion.TotalPrestige, amount); // Ensure total is at least as high
        _religionPrestigeManager.UpdatePrestigeRank(religion.ReligionUID);

        var newRank = religion.PrestigeRank;
        var rankChanged = newRank != oldRank ? $" (Rank: {oldRank} → {newRank})" : "";

        return TextCommandResult.Success(
            $"Set {religionName} prestige to {amount} (was {oldPrestige}){rankChanged}");
    }

    #endregion
}