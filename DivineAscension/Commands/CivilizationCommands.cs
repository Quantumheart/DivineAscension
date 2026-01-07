using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DivineAscension.Data;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Handles all civilization-related chat commands
/// </summary>
public class CivilizationCommands(
    ICoreServerAPI sapi,
    CivilizationManager civilizationManager,
    IReligionManager religionManager)
{
    private readonly CivilizationManager _civilizationManager =
        civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    /// <summary>
    ///     Registers all civilization commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("civ")
            .WithDescription("Manage civilizations - alliances of 1-4 different-deity religions")
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("create")
            .WithDescription("Create a new civilization")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnCreateCivilization)
            .EndSubCommand()
            .BeginSubCommand("invite")
            .WithDescription("Invite a religion to your civilization (founder only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("religionname"))
            .HandleWith(OnInviteReligion)
            .EndSubCommand()
            .BeginSubCommand("accept")
            .WithDescription("Accept a civilization invitation")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("inviteid"))
            .HandleWith(OnAcceptInvite)
            .EndSubCommand()
            .BeginSubCommand("leave")
            .WithDescription("Leave your civilization")
            .HandleWith(OnLeaveCivilization)
            .EndSubCommand()
            .BeginSubCommand("kick")
            .WithDescription("Kick a religion from your civilization (founder only)")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("religionname"))
            .HandleWith(OnKickReligion)
            .EndSubCommand()
            .BeginSubCommand("disband")
            .WithDescription("Disband your civilization (founder only)")
            .HandleWith(OnDisbandCivilization)
            .EndSubCommand()
            .BeginSubCommand("list")
            .WithDescription("List all civilizations")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("deity"))
            .HandleWith(OnListCivilizations)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription("Show civilization information")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("name"))
            .HandleWith(OnCivilizationInfo)
            .EndSubCommand()
            .BeginSubCommand("invites")
            .WithDescription("Show your pending civilization invitations")
            .HandleWith(OnListInvites)
            .EndSubCommand()
            .BeginSubCommand("cleanup")
            .WithDescription("Clean up orphaned civilizations and diplomacy data (Admin only)")
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnCleanupOrphanedData)
            .EndSubCommand();
    }

    /// <summary>
    ///     Handler for /civ create <name>
    /// </summary>
    internal TextCommandResult OnCreateCivilization(TextCommandCallingArgs args)
    {
        var civName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Get player's religion
        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You must be in a religion to create a civilization");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error("Your religion was not found");

        // Check if player is founder
        if (religion.FounderUID != player.PlayerUID)
            return TextCommandResult.Error("Only religion founders can create civilizations");

        // Create civilization
        var civ = _civilizationManager.CreateCivilization(civName, player.PlayerUID, religion.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error(
                "Failed to create civilization. Check name requirements (3-32 characters, unique)");

        return TextCommandResult.Success(
            $"Civilization '{civName}' created! You can now invite 1-3 more religions with different deities.");
    }

    /// <summary>
    ///     Handler for /civ invite <religionname>
    /// </summary>
    internal TextCommandResult OnInviteReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You must be in a religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        // Get player's civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error("You are not in a civilization. Use /civ create first");

        // Check if player is founder
        if (civ.FounderUID != player.PlayerUID)
            return TextCommandResult.Error("Only the civilization founder can send invitations");

        // Find target religion
        var targetReligion = _religionManager.GetReligionByName(religionName);
        if (targetReligion == null)
            return TextCommandResult.Error($"Religion '{religionName}' not found");

        // Send invitation
        var success = _civilizationManager.InviteReligion(civ.CivId, targetReligion.ReligionUID, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error(
                "Failed to send invitation. Check: civilization not full (max 4), different deity required");

        return TextCommandResult.Success($"Invitation sent to '{religionName}'. It will expire in 7 days.");
    }

    /// <summary>
    ///     Handler for /civ accept <inviteid>
    /// </summary>
    internal TextCommandResult OnAcceptInvite(TextCommandCallingArgs args)
    {
        var inviteId = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");


        if (!_religionManager.HasReligion(player.PlayerUID))
            return TextCommandResult.Error("You must be in a religion");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error("Your religion was not found");

        // Check if player is founder
        if (religion.FounderUID != player.PlayerUID)
            return TextCommandResult.Error("Only religion founders can accept civilization invitations");

        // Accept invitation
        var success = _civilizationManager.AcceptInvite(inviteId, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error(
                "Failed to accept invitation. It may have expired or the civilization is full");

        return TextCommandResult.Success("You have joined the civilization!");
    }

    /// <summary>
    ///     Handler for /civ leave
    /// </summary>
    internal TextCommandResult OnLeaveCivilization(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error("You must be in a religion");

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error("Your religion was not found");

        // Check if player is founder of their religion
        if (religion.FounderUID != playerId)
            return TextCommandResult.Error("Only religion founders can leave civilizations");

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error("You are not in a civilization");

        // Check if player is civilization founder
        if (civ.FounderUID == player.PlayerUID)
            return TextCommandResult.Error("Civilization founders cannot leave. Use /civ disband instead");

        // Leave civilization
        var success = _civilizationManager.LeaveReligion(religion.ReligionUID, player.PlayerUID);
        if (!success)
            return TextCommandResult.Error("Failed to leave civilization");

        return TextCommandResult.Success("You have left the civilization.");
    }

    /// <summary>
    ///     Handler for /civ kick <religionname>
    /// </summary>
    internal TextCommandResult OnKickReligion(TextCommandCallingArgs args)
    {
        var religionName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error("You must be in a religion");

        var religion = _religionManager.GetPlayerReligion(playerId);

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error("You are not in a civilization");

        // Check if player is founder
        if (civ.FounderUID != player.PlayerUID)
            return TextCommandResult.Error("Only the civilization founder can kick religions");

        // Find target religion
        var targetReligion = _religionManager.GetReligionByName(religionName);
        if (targetReligion == null)
            return TextCommandResult.Error($"Religion '{religionName}' not found");

        // Kick religion
        var success = _civilizationManager.KickReligion(civ.CivId, targetReligion.ReligionUID, playerId);
        if (!success)
            return TextCommandResult.Error("Failed to kick religion. You cannot kick your own religion");

        return TextCommandResult.Success($"'{religionName}' has been kicked from the civilization.");
    }

    /// <summary>
    ///     Handler for /civ disband
    /// </summary>
    internal TextCommandResult OnDisbandCivilization(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error("You must be in a religion");

        var religion = _religionManager.GetPlayerReligion(playerId);

        // Get civilization
        var civ = _civilizationManager.GetCivilizationByReligion(religion!.ReligionUID);
        if (civ == null)
            return TextCommandResult.Error("You are not in a civilization");

        // Check if player is founder
        if (civ.FounderUID != playerId)
            return TextCommandResult.Error("Only the civilization founder can disband it");

        // Disband
        var success = _civilizationManager.DisbandCivilization(civ.CivId, playerId);
        if (!success)
            return TextCommandResult.Error("Failed to disband civilization");

        return TextCommandResult.Success("Civilization disbanded. All member religions have been freed.");
    }

    /// <summary>
    ///     Handler for /civ list [deity]
    /// </summary>
    internal TextCommandResult OnListCivilizations(TextCommandCallingArgs args)
    {
        var deityFilter = args.Parsers.Count > 0 ? (string?)args[0] : null;

        var civilizations = _civilizationManager.GetAllCivilizations();

        if (!civilizations.Any())
            return TextCommandResult.Success("No civilizations exist yet. Create one with /civ create!");

        var sb = new StringBuilder();
        sb.AppendLine("=== Civilizations ===");

        foreach (var civ in civilizations)
        {
            var religions = _civilizationManager.GetCivReligions(civ.CivId);
            var deities = religions.Select(r => r.Deity.ToString()).Distinct().ToList();

            // Apply deity filter if specified
            if (!string.IsNullOrEmpty(deityFilter) &&
                !deities.Any(d => d.Contains(deityFilter, StringComparison.OrdinalIgnoreCase)))
                continue;

            sb.AppendLine($"• {civ.Name} ({civ.MemberReligionIds.Count}/4 religions)");
            sb.AppendLine($"  Deities: {string.Join(", ", deities)}");
            sb.AppendLine($"  Religions: {string.Join(", ", religions.Select(r => r.ReligionName))}");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /civ info [name]
    /// </summary>
    internal TextCommandResult OnCivilizationInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        Civilization? civ;

        // If name provided, look up by name
        if (args.Parsers.Count > 0)
        {
            var civName = (string)args[0];
            civ = _civilizationManager.GetAllCivilizations()
                .FirstOrDefault(c => c.Name.Equals(civName, StringComparison.OrdinalIgnoreCase));
            if (civ == null)
                return TextCommandResult.Error($"Civilization '{civName}' not found");
        }
        else
        {
            // Get player's civilization
            var playerId = player.PlayerUID;
            if (!_religionManager.HasReligion(playerId))
                return TextCommandResult.Error("You must be in a religion. Specify a civilization name to view others");

            var religion = _religionManager.GetPlayerReligion(playerId);
            civ = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
            if (civ == null)
                return TextCommandResult.Error(
                    "You are not in a civilization. Specify a civilization name to view others");
        }

        // Build info display
        var sb = new StringBuilder();
        sb.AppendLine($"=== {civ.Name} ===");
        sb.AppendLine($"Founded: {civ.CreatedDate:yyyy-MM-dd}");
        sb.AppendLine($"Members: {civ.MemberReligionIds.Count}/4 religions");
        sb.AppendLine();

        sb.AppendLine("Member Religions:");
        var religions = _civilizationManager.GetCivReligions(civ.CivId);
        foreach (var religion in religions)
        {
            var isFounder = religion.ReligionUID == civ.MemberReligionIds[0] ? " [Founder]" : "";
            sb.AppendLine(
                $"  • {religion.ReligionName} ({religion.Deity}) - {religion.MemberUIDs.Count} members{isFounder}");
        }

        // Show pending invites only to founder
        if (civ.FounderUID == player.PlayerUID)
        {
            sb.AppendLine();
            var invites = _civilizationManager.GetInvitesForCiv(civ.CivId);
            if (invites.Any())
            {
                sb.AppendLine("Pending Invitations:");
                foreach (var invite in invites)
                {
                    var targetReligion = _religionManager.GetReligion(invite.ReligionId);
                    if (targetReligion != null)
                    {
                        var daysLeft = (invite.ExpiresDate - DateTime.UtcNow).Days;
                        sb.AppendLine(
                            $"  • {targetReligion.ReligionName} (expires in {daysLeft} days) - ID: {invite.InviteId}");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        // Get player's religion
        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error("You must be in a religion to receive invitations");

        var religion = _religionManager.GetPlayerReligion(playerId);

        if (religion == null)
            return TextCommandResult.Error("Your religion was not found");

        // Check if player is founder
        if (religion.FounderUID != playerId)
            return TextCommandResult.Error("Only religion founders can view civilization invitations");

        // Get invites
        var invites = _civilizationManager.GetInvitesForReligion(religion.ReligionUID);

        if (!invites.Any())
            return TextCommandResult.Success("You have no pending civilization invitations");

        var sb = new StringBuilder();
        sb.AppendLine("=== Pending Civilization Invitations ===");

        foreach (var invite in invites)
        {
            var civ = _civilizationManager.GetCivilization(invite.CivId);
            if (civ != null)
            {
                var daysLeft = (invite.ExpiresDate - DateTime.UtcNow).Days;
                sb.AppendLine($"• {civ.Name} ({civ.MemberReligionIds.Count}/4 religions) - expires in {daysLeft} days");
                sb.AppendLine($"  Invite ID: {invite.InviteId}");
                sb.AppendLine($"  Use: /civ accept {invite.InviteId}");
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
            return TextCommandResult.Success("No orphaned civilizations found.");
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
            $"Cleaned up {orphanedCivs.Count} orphaned civilization(s). Associated diplomacy data was also removed.");
    }
}