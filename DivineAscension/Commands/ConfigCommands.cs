using System;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Chat commands for mod configuration (admin only)
/// </summary>
public class ConfigCommands
{
    private readonly Func<bool> _getProfanityFilterEnabled;
    private readonly ICoreServerAPI _sapi;
    private readonly Action<bool> _setProfanityFilterEnabled;
    private readonly ModConfigData _modConfig;
    private readonly Action _saveConfig;

    /// <summary>
    ///     Creates a new ConfigCommands instance
    /// </summary>
    /// <param name="sapi">Server API</param>
    /// <param name="setProfanityFilterEnabled">Callback to set profanity filter state and persist</param>
    /// <param name="getProfanityFilterEnabled">Callback to get current profanity filter state</param>
    /// <param name="modConfig">Mod configuration data</param>
    /// <param name="saveConfig">Callback to save configuration</param>
    public ConfigCommands(
        ICoreServerAPI sapi,
        Action<bool> setProfanityFilterEnabled,
        Func<bool> getProfanityFilterEnabled,
        ModConfigData modConfig,
        Action saveConfig)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _setProfanityFilterEnabled = setProfanityFilterEnabled ??
                                     throw new ArgumentNullException(nameof(setProfanityFilterEnabled));
        _getProfanityFilterEnabled = getProfanityFilterEnabled ??
                                     throw new ArgumentNullException(nameof(getProfanityFilterEnabled));
        _modConfig = modConfig ?? throw new ArgumentNullException(nameof(modConfig));
        _saveConfig = saveConfig ?? throw new ArgumentNullException(nameof(saveConfig));
    }

    /// <summary>
    ///     Registers all configuration commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("da")
            .WithDescription("Divine Ascension mod configuration")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.root)
            .BeginSubCommand("config")
            .WithDescription("Manage mod configuration settings")
            .BeginSubCommand("profanityfilter")
            .WithDescription("Enable, disable, or check the profanity filter status")
            .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("action"))
            .HandleWith(OnProfanityFilter)
            .EndSubCommand()
            .BeginSubCommand("cooldown")
            .WithDescription("Manage cooldown system settings")
            .BeginSubCommand("status")
            .WithDescription("Show current cooldown settings")
            .HandleWith(OnCooldownStatus)
            .EndSubCommand()
            .BeginSubCommand("set")
            .WithDescription("Set cooldown duration for a specific operation")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("operation"), _sapi.ChatCommands.Parsers.Int("seconds"))
            .HandleWith(OnCooldownSet)
            .EndSubCommand()
            .BeginSubCommand("enable")
            .WithDescription("Enable the cooldown system globally")
            .HandleWith(OnCooldownEnable)
            .EndSubCommand()
            .BeginSubCommand("disable")
            .WithDescription("Disable the cooldown system globally (WARNING: removes anti-griefing protection)")
            .HandleWith(OnCooldownDisable)
            .EndSubCommand()
            .EndSubCommand()
            .EndSubCommand();

        _sapi.Logger.Notification("[DivineAscension] Config commands registered");
    }

    /// <summary>
    ///     Handler for /da config profanityfilter [on|off|status]
    /// </summary>
    internal TextCommandResult OnProfanityFilter(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error("Command must be run by a player");

        var action = (string?)args[0];

        // Default to status if no action provided
        if (string.IsNullOrWhiteSpace(action))
        {
            action = "status";
        }

        switch (action.ToLowerInvariant())
        {
            case "on":
            case "enable":
            case "true":
            case "1":
                _setProfanityFilterEnabled(true);
                _sapi.Logger.Notification(
                    $"[DivineAscension] Profanity filter enabled by {player.PlayerName}");
                return TextCommandResult.Success("Profanity filter has been enabled.");

            case "off":
            case "disable":
            case "false":
            case "0":
                _setProfanityFilterEnabled(false);
                _sapi.Logger.Notification(
                    $"[DivineAscension] Profanity filter disabled by {player.PlayerName}");
                return TextCommandResult.Success("Profanity filter has been disabled.");

            case "status":
            case "get":
                var isEnabled = _getProfanityFilterEnabled();
                var wordCount = ProfanityFilterService.Instance.WordCount;
                return TextCommandResult.Success(
                    $"Profanity filter is currently {(isEnabled ? "enabled" : "disabled")}. " +
                    $"({wordCount} words loaded)");

            default:
                return TextCommandResult.Error(
                    "Invalid action. Use 'on', 'off', or 'status'. Example: /da config profanityfilter off");
        }
    }

    /// <summary>
    ///     Handler for /da config cooldown status
    /// </summary>
    internal TextCommandResult OnCooldownStatus(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error("Command must be run by a player");

        var status = _modConfig.CooldownsEnabled ? "Enabled" : "Disabled (WARNING: anti-griefing protection removed!)";
        var message = $"Cooldown System: {status}\n\nCurrent Durations:\n";
        message += $"  Religion Deletion: {_modConfig.ReligionDeletionCooldown}s\n";
        message += $"  Member Kick: {_modConfig.MemberKickCooldown}s\n";
        message += $"  Member Ban: {_modConfig.MemberBanCooldown}s\n";
        message += $"  Invite: {_modConfig.InviteCooldown}s\n";
        message += $"  Religion Creation: {_modConfig.ReligionCreationCooldown}s\n";
        message += $"  Diplomatic Proposal: {_modConfig.ProposalCooldown}s\n";
        message += $"  War Declaration: {_modConfig.WarDeclarationCooldown}s";

        return TextCommandResult.Success(message);
    }

    /// <summary>
    ///     Handler for /da config cooldown set <operation> <seconds>
    /// </summary>
    internal TextCommandResult OnCooldownSet(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error("Command must be run by a player");

        var operation = (string?)args[0];
        var seconds = (int?)args[1];

        if (string.IsNullOrWhiteSpace(operation) || !seconds.HasValue)
            return TextCommandResult.Error("Usage: /da config cooldown set <operation> <seconds>");

        if (seconds.Value < 0 || seconds.Value > 3600)
            return TextCommandResult.Error("Seconds must be between 0 and 3600 (1 hour)");

        // Map operation name to config property
        var updated = false;
        var operationLower = operation.ToLowerInvariant();

        switch (operationLower)
        {
            case "religiondeletion":
            case "deletion":
                _modConfig.ReligionDeletionCooldown = seconds.Value;
                updated = true;
                operation = "Religion Deletion";
                break;
            case "memberkick":
            case "kick":
                _modConfig.MemberKickCooldown = seconds.Value;
                updated = true;
                operation = "Member Kick";
                break;
            case "memberban":
            case "ban":
                _modConfig.MemberBanCooldown = seconds.Value;
                updated = true;
                operation = "Member Ban";
                break;
            case "invite":
                _modConfig.InviteCooldown = seconds.Value;
                updated = true;
                operation = "Invite";
                break;
            case "religioncreation":
            case "creation":
                _modConfig.ReligionCreationCooldown = seconds.Value;
                updated = true;
                operation = "Religion Creation";
                break;
            case "proposal":
            case "diplomaticproposal":
                _modConfig.ProposalCooldown = seconds.Value;
                updated = true;
                operation = "Diplomatic Proposal";
                break;
            case "wardeclaration":
            case "war":
                _modConfig.WarDeclarationCooldown = seconds.Value;
                updated = true;
                operation = "War Declaration";
                break;
            default:
                return TextCommandResult.Error(
                    $"Unknown operation '{operation}'. Valid operations: ReligionDeletion, MemberKick, MemberBan, Invite, ReligionCreation, Proposal, WarDeclaration");
        }

        if (updated)
        {
            _saveConfig();
            _sapi.Logger.Notification(
                $"[DivineAscension] {player.PlayerName} set {operation} cooldown to {seconds.Value}s");
            return TextCommandResult.Success($"{operation} cooldown set to {seconds.Value} seconds");
        }

        return TextCommandResult.Error("Failed to update cooldown");
    }

    /// <summary>
    ///     Handler for /da config cooldown enable
    /// </summary>
    internal TextCommandResult OnCooldownEnable(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error("Command must be run by a player");

        _modConfig.CooldownsEnabled = true;
        _saveConfig();
        _sapi.Logger.Notification($"[DivineAscension] Cooldown system enabled by {player.PlayerName}");
        return TextCommandResult.Success("Cooldown system has been enabled.");
    }

    /// <summary>
    ///     Handler for /da config cooldown disable
    /// </summary>
    internal TextCommandResult OnCooldownDisable(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error("Command must be run by a player");

        _modConfig.CooldownsEnabled = false;
        _saveConfig();
        _sapi.Logger.Warning(
            $"[DivineAscension] Cooldown system DISABLED by {player.PlayerName} - anti-griefing protection removed!");
        return TextCommandResult.Success(
            "Cooldown system has been DISABLED. WARNING: This removes anti-griefing protection!");
    }
}