using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DivineAscension.Commands.Parsers;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Extensions;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Handles all civilization-related chat commands
/// </summary>
public class CivilizationCommands(
    ICoreServerAPI sapi,
    ICivilizationManager civilizationManager,
    IReligionManager religionManager,
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICooldownManager cooldownManager)
{
    private readonly ICivilizationManager _civilizationManager =
        civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly ICooldownManager _cooldownManager =
        cooldownManager ?? throw new ArgumentNullException(nameof(cooldownManager));

    /// <summary>
    ///     Registers all civilization commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("civ")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_DESC))
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("create")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_CREATE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("name"))
            .HandleWith(OnCreateCivilization)
            .EndSubCommand()
            .BeginSubCommand("invite")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_INVITE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("religionname"))
            .HandleWith(OnInviteReligion)
            .EndSubCommand()
            .BeginSubCommand("accept")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ACCEPT_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("inviteid"))
            .HandleWith(OnAcceptInvite)
            .EndSubCommand()
            .BeginSubCommand("decline")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_DECLINE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("inviteid"))
            .HandleWith(OnDeclineInvite)
            .EndSubCommand()
            .BeginSubCommand("leave")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LEAVE_DESC))
            .HandleWith(OnLeaveCivilization)
            .EndSubCommand()
            .BeginSubCommand("kick")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_KICK_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("religionname"))
            .HandleWith(OnKickReligion)
            .EndSubCommand()
            .BeginSubCommand("disband")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_DISBAND_DESC))
            .HandleWith(OnDisbandCivilization)
            .EndSubCommand()
            .BeginSubCommand("description")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_DESCRIPTION_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.All("text"))
            .HandleWith(OnSetDescription)
            .EndSubCommand()
            .BeginSubCommand("list")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LIST_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("deity"))
            .HandleWith(OnListCivilizations)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_INFO_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalQuotedString("name"))
            .HandleWith(OnCivilizationInfo)
            .EndSubCommand()
            .BeginSubCommand("invites")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_INVITES_DESC))
            .HandleWith(OnListInvites)
            .EndSubCommand()
            .BeginSubCommand("admin")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ADMIN_DESC))
            .RequiresPrivilege(Privilege.root)
            .BeginSubCommand("create")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ADMIN_CREATE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("civname"),
                _sapi.ChatCommands.Parsers.QuotedString("religion1"),
                _sapi.ChatCommands.Parsers.OptionalQuotedString("religion2"),
                _sapi.ChatCommands.Parsers.OptionalQuotedString("religion3"),
                _sapi.ChatCommands.Parsers.OptionalQuotedString("religion4"))
            .HandleWith(OnAdminCreate)
            .EndSubCommand()
            .BeginSubCommand("dissolve")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ADMIN_DISSOLVE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.QuotedString("civname"))
            .HandleWith(OnAdminDissolve)
            .EndSubCommand()
            .BeginSubCommand("cleanup")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ADMIN_CLEANUP_DESC))
            .HandleWith(OnCleanupOrphanedData)
            .EndSubCommand()
            .EndSubCommand();
    }

    /// <summary>
    ///     Handler for /civ create <name>
    /// </summary>
    internal TextCommandResult OnCreateCivilization(TextCommandCallingArgs args)
    {
        var civName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Get player's religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION_TO_CREATE));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_NOT_FOUND_GENERIC));

        // Check if player is founder
        if (religion.FounderUID != player.PlayerUID)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDERS_CREATE));

        // Check for profanity in civilization name
        if (ProfanityFilterService.Instance.ContainsProfanity(civName))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NAME_PROFANITY));

        // Create civilization
        var civ = _civilizationManager.CreateCivilization(civName, player.PlayerUID, religion.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_CREATE_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_CREATED, civName));
    }

    /// <summary>
    ///     Handler for /civ invite <religionname>
    /// </summary>
    internal TextCommandResult OnInviteReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Check cooldown (2s for civilization invites)
        if (!_cooldownManager.CanPerformOperation(player.PlayerUID, CooldownType.Invite, out var cooldownError))
            return TextCommandResult.Error(cooldownError!);

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        // Get player's civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NOT_IN_CIV_USE_CREATE));

        // Check if player is founder
        if (!civ.IsFounder(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDER_INVITE));

        // Find target religion
        var targetReligion = _religionManager.GetReligionByName(religionName);
        if (targetReligion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_RELIGION_NOT_FOUND, religionName));

        // Send invitation
        var success = _civilizationManager.InviteReligion(civ.CivId, targetReligion.ReligionUID, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_INVITE_FAILED));

        // Record cooldown after successful invite
        _cooldownManager.RecordOperation(player.PlayerUID, CooldownType.Invite);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_INVITE_SENT, religionName));
    }

    /// <summary>
    ///     Handler for /civ accept <inviteid>
    /// </summary>
    internal TextCommandResult OnAcceptInvite(TextCommandCallingArgs args)
    {
        var inviteId = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));


        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_NOT_FOUND_GENERIC));

        // Check if player is founder
        if (religion.FounderUID != player.PlayerUID)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDERS_ACCEPT));

        // Accept invitation
        var success = _civilizationManager.AcceptInvite(inviteId, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ACCEPT_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_JOINED));
    }

    /// <summary>
    ///     Handler for /civ decline <inviteid>
    /// </summary>
    internal TextCommandResult OnDeclineInvite(TextCommandCallingArgs args)
    {
        var inviteId = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_NOT_FOUND_GENERIC));

        // Check if player is founder
        if (religion.FounderUID != player.PlayerUID)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDERS_DECLINE));

        // Decline invitation
        var success = _civilizationManager.DeclineInvite(inviteId, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_DECLINE_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_DECLINED));
    }

    /// <summary>
    ///     Handler for /civ leave
    /// </summary>
    internal TextCommandResult OnLeaveCivilization(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_NOT_FOUND_GENERIC));

        // Check if player is founder of their religion
        if (religion.FounderUID != playerId)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDERS_LEAVE));

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NOT_IN_CIV));

        // Check if player is civilization founder
        if (civ.IsFounder(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_FOUNDER_CANNOT_LEAVE));

        // Leave civilization
        var success = _civilizationManager.LeaveReligion(religion.ReligionUID, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_LEAVE_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_LEFT));
    }

    /// <summary>
    ///     Handler for /civ kick <religionname>
    /// </summary>
    internal TextCommandResult OnKickReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NOT_IN_CIV));

        // Check if player is founder
        if (!civ.IsFounder(player.PlayerUID))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDER_KICK));

        // Find target religion
        var targetReligion = _religionManager.GetReligionByName(religionName);
        if (targetReligion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_RELIGION_NOT_FOUND, religionName));

        // Kick religion
        var success = _civilizationManager.KickReligion(civ.CivId, targetReligion.ReligionUID, playerId);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_KICK_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_KICKED, religionName));
    }

    /// <summary>
    ///     Handler for /civ disband
    /// </summary>
    internal TextCommandResult OnDisbandCivilization(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NOT_IN_CIV));

        // Check if player is founder
        if (!civ.IsFounder(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDER_DISBAND));

        // Disband
        var success = _civilizationManager.DisbandCivilization(civ.CivId, playerId);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_DISBAND_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_DISBANDED));
    }

    /// <summary>
    ///     Handler for /civ description <text>
    /// </summary>
    internal TextCommandResult OnSetDescription(TextCommandCallingArgs args)
    {
        var description = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NOT_IN_CIV));

        // Check if player is founder
        if (!civ.IsFounder(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDER_DESCRIPTION));

        // Validate description length
        if (description.Length > 200)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_DESCRIPTION_TOO_LONG));

        // Check for profanity
        if (ProfanityFilterService.Instance.ContainsProfanity(description))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_DESCRIPTION_PROFANITY));

        // Update description
        var success = _civilizationManager.UpdateCivilizationDescription(civ.CivId, playerId, description);
        if (!success)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_DESCRIPTION_UPDATE_FAILED));

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.NET_CIV_DESCRIPTION_UPDATED));
    }

    /// <summary>
    ///     Handler for /civ list [deity]
    /// </summary>
    internal TextCommandResult OnListCivilizations(TextCommandCallingArgs args)
    {
        var deityFilter = args.Parsers.Count > 0 ? (string?)args[0] : null;

        var civilizations = _civilizationManager.GetAllCivilizations();

        if (!civilizations.Any())
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_NO_CIVS));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_HEADER_LIST));

        foreach (var civ in civilizations)
        {
            var religions = _civilizationManager.GetCivReligions(civ.CivId);
            var deities = religions.Select(r => r.Domain.ToString()).Distinct().ToList();

            // Apply deity filter if specified
            if (!string.IsNullOrEmpty(deityFilter) &&
                !deities.Any(d => d.Contains(deityFilter, StringComparison.OrdinalIgnoreCase)))
                continue;

            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_FORMAT_LIST_ITEM,
                civ.Name, civ.MemberReligionIds.Count));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_DEITIES,
                string.Join(", ", deities)));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_RELIGIONS,
                string.Join(", ", religions.Select(r => r.ReligionName))));
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /civ info [name]
    /// </summary>
    internal TextCommandResult OnCivilizationInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        Civilization? civ;

        // If name provided, look up by name
        if (args.Parsers.Count > 0)
        {
            var civName = (string)args[0];
            civ = _civilizationManager.GetAllCivilizations()
                .FirstOrDefault(c => c.Name.Equals(civName, StringComparison.OrdinalIgnoreCase));
            if (civ == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_CIV_NOT_FOUND, civName));
        }
        else
        {
            // Get player's civilization
            var playerId = player.PlayerUID;
            if (!_religionManager.HasReligion(playerId))
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_MUST_BE_IN_RELIGION_SPECIFY_NAME));

            var religion = _religionManager.GetPlayerReligion(playerId);
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_MUST_BE_IN_RELIGION_SPECIFY_NAME));

            civ = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
            if (civ == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NOT_IN_CIV_SPECIFY_NAME));
        }

        // Build info display
        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_HEADER_INFO, civ.Name));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_FOUNDED,
            civ.CreatedDate.ToString("yyyy-MM-dd")));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_MEMBERS,
            civ.MemberReligionIds.Count));
        sb.AppendLine();

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_MEMBER_RELIGIONS));
        var religions = _civilizationManager.GetCivReligions(civ.CivId);
        foreach (var religion in religions)
        {
            var isFounder = religion.ReligionUID == civ.MemberReligionIds[0]
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_FOUNDER)
                : "";
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_FORMAT_MEMBER_RELIGION,
                religion.ReligionName, religion.Domain, religion.MemberUIDs.Count, isFounder));
        }

        // Show pending invites only to founder
        if (civ.IsFounder(player.PlayerUID))
        {
            sb.AppendLine();
            var invites = _civilizationManager.GetInvitesForCiv(civ.CivId);
            if (invites.Any())
            {
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_PENDING_INVITES));
                foreach (var invite in invites)
                {
                    var targetReligion = _religionManager.GetReligion(invite.ReligionId);
                    if (targetReligion != null)
                    {
                        var daysLeft = (invite.ExpiresDate - DateTime.UtcNow).Days;
                        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_FORMAT_PENDING_INVITE,
                            targetReligion.ReligionName, daysLeft, invite.InviteId));
                    }
                }
            }
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /civ invites
    /// </summary>
    internal TextCommandResult OnListInvites(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_MUST_BE_IN_RELIGION_FOR_INVITES));

        var religion = _religionManager.GetPlayerReligion(playerId);

        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_NOT_FOUND_GENERIC));

        // Check if player is founder
        if (religion.FounderUID != playerId)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ONLY_FOUNDERS_VIEW_INVITES));

        // Get invites
        var invites = _civilizationManager.GetInvitesForReligion(religion.ReligionUID);

        if (!invites.Any())
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_NO_INVITES));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_HEADER_INVITES));

        foreach (var invite in invites)
        {
            var civ = _civilizationManager.GetCivilization(invite.CivId);
            if (civ != null)
            {
                var daysLeft = (invite.ExpiresDate - DateTime.UtcNow).Days;
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_FORMAT_INVITE_ITEM,
                    civ.Name, civ.MemberReligionIds.Count, daysLeft));
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_INVITE_ID,
                    invite.InviteId));
                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_LABEL_USE_ACCEPT,
                    invite.InviteId));
            }
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /civ cleanup (Admin only)
    ///     Removes orphaned civilizations and their associated diplomacy data
    /// </summary>
    internal TextCommandResult OnCleanupOrphanedData(TextCommandCallingArgs args)
    {
        var orphanedCivs = new List<string>();

        // Find civilizations with 0 religions or invalid state
        foreach (var civ in _civilizationManager.GetAllCivilizations())
        {
            if (civ.MemberReligionIds.Count == 0 || !civ.IsValid)
            {
                orphanedCivs.Add(civ.CivId);
            }
        }

        if (orphanedCivs.Count == 0)
        {
            return TextCommandResult.Success(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_NO_ORPHANED));
        }

        // Disband orphaned civilizations
        foreach (var civId in orphanedCivs)
        {
            var civ = _civilizationManager.GetCivilization(civId);
            if (civ != null)
            {
                _civilizationManager.DisbandCivilization(civ.CivId, civ.FounderUID);
            }
        }

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_CLEANUP, orphanedCivs.Count));
    }

    #region Admin Commands (Privilege.root)

    /// <summary>
    ///     /civ admin create <civname> <religion1> [religion2] [religion3] [religion4] - Force create civilization with religions
    /// </summary>
    internal TextCommandResult OnAdminCreate(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var civName = (string)args[0];
        var religion1Name = (string)args[1];
        var religion2Name = args.Parsers.Count > 2 ? (string?)args[2] : null;
        var religion3Name = args.Parsers.Count > 3 ? (string?)args[3] : null;
        var religion4Name = args.Parsers.Count > 4 ? (string?)args[4] : null;

        // Collect all religion names
        var religionNames = new List<string> { religion1Name };
        if (religion2Name != null) religionNames.Add(religion2Name);
        if (religion3Name != null) religionNames.Add(religion3Name);
        if (religion4Name != null) religionNames.Add(religion4Name);

        // Validate all religions exist
        var religions = new List<ReligionData>();
        foreach (var name in religionNames)
        {
            var religion = _religionManager.GetReligionByName(name);
            if (religion == null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_RELIGION_NOT_FOUND, name));
            religions.Add(religion);
        }

        // Check deity uniqueness
        var deitySet = new HashSet<DeityDomain>();
        var duplicateDeities = new List<DeityDomain>();
        foreach (var religion in religions)
        {
            if (!deitySet.Add(religion.Domain))
            {
                if (!duplicateDeities.Contains(religion.Domain))
                    duplicateDeities.Add(religion.Domain);
            }
        }

        if (duplicateDeities.Count > 0)
        {
            var deityNames = string.Join(", ", duplicateDeities.Select(d => d.ToLocalizedString()));
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_DUPLICATE_DEITIES, deityNames));
        }

        // Check if any religion is already in a civilization
        foreach (var religion in religions)
        {
            var existingCiv = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
            if (existingCiv != null)
                return TextCommandResult.Error(
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_RELIGION_ALREADY_IN_CIV,
                        religion.ReligionName, existingCiv.Name));
        }

        // Create civilization with first religion as founder
        var founderReligion = religions[0];
        var civilization = _civilizationManager.CreateCivilization(civName, founderReligion.FounderUID,
            founderReligion.ReligionUID);

        if (civilization == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_ADMIN_CREATE_FAILED));

        // Add remaining religions directly (bypass invitation system)
        for (int i = 1; i < religions.Count; i++)
        {
            var religion = religions[i];
            civilization.AddReligion(religion.ReligionUID);
        }

        // Update member count
        int totalMembers = 0;
        foreach (var religionId in civilization.MemberReligionIds)
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion != null) totalMembers += religion.MemberUIDs.Count;
        }

        civilization.MemberCount = totalMembers;

        // Notify all members of all affected religions (not just founders)
        foreach (var religion in religions)
        {
            foreach (var memberUid in religion.MemberUIDs)
            {
                var member = _sapi.World.PlayerByUid(memberUid) as IServerPlayer;
                if (member != null)
                {
                    // Send chat notification
                    member.SendMessage(GlobalConstants.GeneralChatGroup,
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_RELIGION_JOINED_NOTIFICATION,
                            religion.ReligionName, civName),
                        EnumChatType.Notification);

                    // Trigger player data refresh (updates HUD)
                    _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);
                }
            }
        }

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} created civilization '{civName}' with {religions.Count} religion(s)");

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_ADMIN_CREATED,
                civName, religions.Count, totalMembers));
    }

    /// <summary>
    ///     /civ admin dissolve <civname> - Force dissolve a civilization
    /// </summary>
    internal TextCommandResult OnAdminDissolve(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var civName = (string)args[0];

        // Find civilization by name
        var civilization = _civilizationManager.GetAllCivilizations()
            .FirstOrDefault(c => string.Equals(c.Name, civName, StringComparison.OrdinalIgnoreCase));

        if (civilization == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_ERROR_CIV_NOT_FOUND, civName));

        // Get all member religions before disbanding
        var memberReligionIds = civilization.MemberReligionIds.ToList();
        var memberCount = memberReligionIds.Count;

        // Use DisbandCivilization with founder UID (admin override)
        _civilizationManager.DisbandCivilization(civilization.CivId, civilization.FounderUID);

        // Notify all members of all affected religions
        foreach (var religionUID in memberReligionIds)
        {
            var religion = _religionManager.GetReligion(religionUID);
            if (religion == null) continue;

            foreach (var memberUid in religion.MemberUIDs)
            {
                var member = _sapi.World.PlayerByUid(memberUid) as IServerPlayer;
                if (member != null)
                {
                    // Send chat notification
                    member.SendMessage(GlobalConstants.GeneralChatGroup,
                        LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_ADMIN_DISBANDED_NOTIFICATION,
                            civName),
                        EnumChatType.Notification);

                    // Trigger player data refresh
                    _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);
                }
            }
        }

        _sapi.Logger.Notification(
            $"[DivineAscension] Admin: {player.PlayerName} disbanded civilization '{civName}' ({memberCount} religion(s))");

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_CIV_SUCCESS_ADMIN_DISBANDED, civName));
    }

    #endregion
}