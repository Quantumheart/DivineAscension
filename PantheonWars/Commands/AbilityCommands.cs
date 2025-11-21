using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Commands;

/// <summary>
///     Chat commands for ability management
/// </summary>
[ExcludeFromCodeCoverage]
public class AbilityCommands
{
    private readonly AbilitySystem _abilitySystem;
    private readonly PlayerDataManager _playerDataManager;
    private readonly IPlayerReligionDataManager _religionDataManager;
    private readonly ICoreServerAPI _sapi;

    public AbilityCommands(
        ICoreServerAPI sapi,
        AbilitySystem abilitySystem,
        PlayerDataManager playerDataManager,
        IPlayerReligionDataManager religionDataManager)
    {
        _sapi = sapi;
        _abilitySystem = abilitySystem;
        _playerDataManager = playerDataManager;
        _religionDataManager = religionDataManager;
    }

    /// <summary>
    ///     Registers all ability-related commands
    /// </summary>
    public void RegisterCommands()
    {
        _sapi.ChatCommands.Create("ability")
            .WithDescription("Manage and use your deity's abilities")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("list")
            .WithDescription("List your available abilities")
            .HandleWith(OnListAbilities)
            .EndSubCommand()
            .BeginSubCommand("info")
            .WithDescription("Get information about a specific ability")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("ability"))
            .HandleWith(OnAbilityInfo)
            .EndSubCommand()
            .BeginSubCommand("use")
            .WithDescription("Activate an ability")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("ability"))
            .HandleWith(OnUseAbility)
            .EndSubCommand()
            .BeginSubCommand("cooldowns")
            .WithDescription("Show cooldown status for all your abilities")
            .HandleWith(OnShowCooldowns)
            .EndSubCommand();

        _sapi.Logger.Notification("[PantheonWars] Ability commands registered");
    }

    private TextCommandResult OnListAbilities(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var religionData = _religionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None)
            return TextCommandResult.Success(
                "You are not in a religion or do not have an active deity. Use /deity list to see available deities.");

        var abilities = _abilitySystem.GetPlayerAbilities(player).ToList();
        if (!abilities.Any()) return TextCommandResult.Success("No abilities available.");

        var sb = new StringBuilder();
        sb.AppendLine("=== Your Abilities ===");

        foreach (var ability in abilities)
        {
            var cooldown = _abilitySystem.GetAbilityCooldown(player, ability.Id);
            var cooldownStatus = cooldown > 0 ? $" [CD: {cooldown:F1}s]" : " [READY]";
            var rankReq = ability.MinimumRank > 0 ? $" (Requires rank: {ability.MinimumRank})" : "";

            sb.AppendLine($"{ability.Name}{cooldownStatus} - {ability.FavorCost} favor{rankReq}");
            sb.AppendLine($"  {ability.Description}");
            sb.AppendLine($"  Use: /ability use {ability.Id}");
            sb.AppendLine();
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnAbilityInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var abilityId = args[0] as string ?? string.Empty;
        var ability = _abilitySystem.GetAbility(abilityId);

        if (ability == null)
            return TextCommandResult.Error(
                $"Ability '{abilityId}' not found. Use /ability list to see your abilities.");

        var playerData = _playerDataManager.GetOrCreatePlayerData(player);
        var religionData = _religionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        var canUse = religionData.ActiveDeity != DeityType.None && ability.Deity == religionData.ActiveDeity;

        var sb = new StringBuilder();
        sb.AppendLine($"=== {ability.Name} ===");
        sb.AppendLine($"Type: {ability.Type}");
        sb.AppendLine($"Deity: {ability.Deity}");
        sb.AppendLine($"Description: {ability.Description}");
        sb.AppendLine($"Favor Cost: {ability.FavorCost}");
        sb.AppendLine($"Cooldown: {ability.CooldownSeconds} seconds");
        sb.AppendLine($"Minimum Rank: {ability.MinimumRank}");

        if (!canUse)
        {
            sb.AppendLine();
            sb.AppendLine($"[!] You cannot use this ability (requires {ability.Deity} deity)");
        }
        else
        {
            var cooldown = _abilitySystem.GetAbilityCooldown(player, ability.Id);
            if (cooldown > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"[!] On cooldown: {cooldown:F1} seconds remaining");
            }
            else if (playerData.DivineFavor < ability.FavorCost)
            {
                sb.AppendLine();
                sb.AppendLine($"[!] Insufficient favor (need {ability.FavorCost}, have {playerData.DivineFavor})");
            }
            else if (playerData.DevotionRank < ability.MinimumRank)
            {
                sb.AppendLine();
                sb.AppendLine($"[!] Rank too low (need rank {ability.MinimumRank}, you have rank {playerData.DevotionRank})");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("[READY] Use: /ability use " + ability.Id);
            }
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnUseAbility(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var abilityId = args[0] as string ?? string.Empty;

        var success = _abilitySystem.ExecuteAbility(player, abilityId);

        if (success) return TextCommandResult.Success(); // Ability execution already sends messages

        return TextCommandResult.Error(""); // Error messages already sent by AbilitySystem
    }

    private TextCommandResult OnShowCooldowns(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command must be used by a player");

        var religionData = _religionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None) return TextCommandResult.Success("You are not in a religion or do not have an active deity.");

        var abilities = _abilitySystem.GetPlayerAbilities(player).ToList();
        if (!abilities.Any()) return TextCommandResult.Success("No abilities available.");

        var sb = new StringBuilder();
        sb.AppendLine("=== Ability Cooldowns ===");

        var onCooldown = 0;
        var ready = 0;

        foreach (var ability in abilities)
        {
            var cooldown = _abilitySystem.GetAbilityCooldown(player, ability.Id);
            if (cooldown > 0)
            {
                sb.AppendLine($"{ability.Name}: {cooldown:F1} seconds");
                onCooldown++;
            }
            else
            {
                sb.AppendLine($"{ability.Name}: READY");
                ready++;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"{ready} ready, {onCooldown} on cooldown");

        return TextCommandResult.Success(sb.ToString());
    }
}