using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Commands;

/// <summary>
///     Chat commands for deity management
/// </summary>
[ExcludeFromCodeCoverage]
public class DeityCommands
{
    private readonly IDeityRegistry _deityRegistry;
    private readonly IPlayerReligionDataManager _religionDataManager;
    private readonly ICoreServerAPI _sapi;

    public DeityCommands(ICoreServerAPI sapi, IDeityRegistry deityRegistry,
        IPlayerReligionDataManager religionDataManager)
    {
        _sapi = sapi;
        _deityRegistry = deityRegistry;
        _religionDataManager = religionDataManager;
    }

    /// <summary>
    ///     Registers all deity-related commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("deity")
            .WithDescription("Manage your deity and divine favor")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("list")
            .WithDescription("List all available deities")
            .HandleWith(OnListDeities)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription("Get information about a specific deity")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("deity"))
            .HandleWith(OnDeityInfo)
            .EndSubCommand()
            .BeginSubCommand("select")
            .WithDescription("Pledge to a deity")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("deity"))
            .HandleWith(OnSelectDeity)
            .EndSubCommand()
            .BeginSubCommand("status")
            .WithDescription("View your current deity status")
            .HandleWith(OnDeityStatus)
            .EndSubCommand();

        _sapi.Logger.Notification("[PantheonWars] Deity commands registered");
    }

    private TextCommandResult OnListDeities(TextCommandCallingArgs args)
    {
        var deities = _deityRegistry.GetAllDeities().ToList();
        var sb = new StringBuilder();
        sb.AppendLine("=== Available Deities ===");

        foreach (var deity in deities)
        {
            sb.AppendLine($"{deity.Name} ({deity.Domain}) - {deity.Alignment}");
            sb.AppendLine($"  {deity.Description}");
            sb.AppendLine();
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnDeityInfo(TextCommandCallingArgs args)
    {
        var deityName = args[0] as string ?? string.Empty;

        // Try to find deity by name
        var deity = _deityRegistry.GetAllDeities()
            .FirstOrDefault(d => d.Name.Equals(deityName, StringComparison.OrdinalIgnoreCase));

        if (deity == null)
            return TextCommandResult.Error($"Deity '{deityName}' not found. Use /deity list to see available deities.");

        var sb = new StringBuilder();
        sb.AppendLine($"=== {deity.Name} - God/Goddess of {deity.Domain} ===");
        sb.AppendLine($"Alignment: {deity.Alignment}");
        sb.AppendLine($"Description: {deity.Description}");
        sb.AppendLine($"Playstyle: {deity.Playstyle}");
        sb.AppendLine();

        // Show relationships
        if (deity.Relationships.Any())
        {
            sb.AppendLine("Relationships:");
            foreach (var relationship in deity.Relationships)
            {
                var relatedDeity = _deityRegistry.GetDeity(relationship.Key);
                var relatedName = relatedDeity?.Name ?? relationship.Key.ToString();
                sb.AppendLine($"  {relationship.Value}: {relatedName}");
            }
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnSelectDeity(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var deityName = args[0] as string ?? string.Empty;

        // Try to find deity by name
        var deity = _deityRegistry.GetAllDeities()
            .FirstOrDefault(d => d.Name.Equals(deityName, StringComparison.OrdinalIgnoreCase));

        if (deity == null)
            return TextCommandResult.Error($"Deity '{deityName}' not found. Use /deity list to see available deities.");

        // Get current player religion data
        var religionData = _religionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        // Check if already pledged to this deity through religion
        if (religionData.ActiveDeity == deity.Type)
            return TextCommandResult.Error($"You are already pledged to {deity.Name} through your religion!");

        // Warn if already in a religion with a deity
        if (religionData.ActiveDeity != DeityType.None)
        {
            var currentDeity = _deityRegistry.GetDeity(religionData.ActiveDeity);
            var currentName = currentDeity?.Name ?? religionData.ActiveDeity.ToString();
            return TextCommandResult.Error(
                $"You are already pledged to {currentName} through your religion. " +
                "Please use the religion system to join a different religion or create your own."
            );
        }

        // Inform player to use religion system
        return TextCommandResult.Error(
            $"To pledge to {deity.Name}, you must join or create a religion that worships this deity. " +
            "Use the religion commands to create or join a religion."
        );
    }

    private TextCommandResult OnDeityStatus(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var religionData = _religionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None)
            return TextCommandResult.Success(
                "You are not in a religion or do not have an active deity. Use /deity list to see available deities.");

        var deity = _deityRegistry.GetDeity(religionData.ActiveDeity);
        var deityName = deity?.Name ?? religionData.ActiveDeity.ToString();

        var sb = new StringBuilder();
        sb.AppendLine("=== Your Divine Status ===");
        sb.AppendLine($"Deity: {deityName}");
        sb.AppendLine($"Divine Favor: {religionData.Favor}");
        sb.AppendLine($"Favor Rank: {religionData.FavorRank}");
        sb.AppendLine($"Kills: {religionData.KillCount}");
        sb.AppendLine($"Total Favor Earned: {religionData.TotalFavorEarned}");

        if (religionData.LastReligionSwitch.HasValue)
        {
            var daysServed = (DateTime.UtcNow - religionData.LastReligionSwitch.Value).Days;
            sb.AppendLine($"Days Served: {daysServed}");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnCheckFavor(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var religionData = _religionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None)
            return TextCommandResult.Success("You are not in a religion or do not have an active deity.");

        var deity = _deityRegistry.GetDeity(religionData.ActiveDeity);
        var deityName = deity?.Name ?? religionData.ActiveDeity.ToString();

        return TextCommandResult.Success(
            $"You have {religionData.Favor} favor with {deityName} (Rank: {religionData.FavorRank})"
        );
    }
}