using System;
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

    /// <summary>
    ///     Creates a new ConfigCommands instance
    /// </summary>
    /// <param name="sapi">Server API</param>
    /// <param name="setProfanityFilterEnabled">Callback to set profanity filter state and persist</param>
    /// <param name="getProfanityFilterEnabled">Callback to get current profanity filter state</param>
    public ConfigCommands(
        ICoreServerAPI sapi,
        Action<bool> setProfanityFilterEnabled,
        Func<bool> getProfanityFilterEnabled)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _setProfanityFilterEnabled = setProfanityFilterEnabled ??
                                     throw new ArgumentNullException(nameof(setProfanityFilterEnabled));
        _getProfanityFilterEnabled = getProfanityFilterEnabled ??
                                     throw new ArgumentNullException(nameof(getProfanityFilterEnabled));
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
}