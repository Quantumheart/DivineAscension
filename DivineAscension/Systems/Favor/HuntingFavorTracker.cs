using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class HuntingFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly HashSet<string> _lysaFollowers = new();

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        _sapi.Event.OnEntityDeath -= OnEntityDeath;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _lysaFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;


    public void Initialize()
    {
        _sapi.Event.OnEntityDeath += OnEntityDeath;

        // Cache followers
        RefreshFollowerCache();

        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;
    }

    private void RefreshFollowerCache()
    {
        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers) UpdateFollower(player.PlayerUID);
    }

    private void OnPlayerDataChanged(string playerId)
    {
        UpdateFollower(playerId);
    }

    private void UpdateFollower(string playerId)
    {
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(playerId);
        if (deityType == DeityDomain)
            _lysaFollowers.Add(playerId);
        else
            _lysaFollowers.Remove(playerId);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionId)
    {
        _lysaFollowers.Remove(player.PlayerUID);
    }

    private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
    {
        if (entity == null || damageSource == null) return;

        // Check if the killer is a player
        var killer = damageSource.GetCauseEntity();
        if (killer is EntityPlayer { Player: IServerPlayer player })
        {
            if (!_lysaFollowers.Contains(player.PlayerUID)) return;

            var favor = GetFavorForEntity(entity);
            if (favor > 0) _favorSystem.AwardFavorForAction(player, "hunting " + entity.Code.Path, favor);
        }
    }

    private int GetFavorForEntity(Entity entity)
    {
        if (entity is not EntityAgent || entity is EntityPlayer) return 0;

        var code = entity.Code.Path.ToLower();

        // Filter out non-animals (monsters, constructs, undead)
        if (IsNonAnimal(code)) return 0;

        // Unified pattern-based detection for all animals
        return CalculateAnimalFavor(code);
    }

    internal bool IsNonAnimal(string code)
    {
        // Monsters
        if (code.Contains("drifter") || code.Contains("locust") || code.Contains("bell"))
            return true;

        // Constructs
        if (code.Contains("mechanical") || code.Contains("construct") ||
            code.Contains("automaton") || code.Contains("golem"))
            return true;

        // Undead
        if (code.Contains("undead") || code.Contains("skeleton") ||
            code.Contains("zombie") || code.Contains("ghost") || code.Contains("wraith"))
            return true;

        // Summons
        if (code.Contains("summoned") || code.Contains("illusion") || code.Contains("spirit"))
            return true;

        return false;
    }

    internal int CalculateAnimalFavor(string code)
    {
        // Tier 15: Large predators
        if (code.Contains("bear") ||
            code.Contains("tiger") || code.Contains("lion") || code.Contains("machairodontinae") ||
            code.Contains("predator") || code.Contains("apex"))
            return 15;

        // Tier 12: Large herbivores / medium predators
        if (code.Contains("wolf") || code.Contains("moose") ||
            code.Contains("mammoth") || code.Contains("elephant") || code.Contains("rhino") ||
            code.Contains("bison") || code.Contains("buffalo") || code.Contains("bovinae") || code.Contains("giant"))
            return 12;

        // Tier 10: Scavengers
        if (code.Contains("hyena") ||
            code.Contains("jackal") || code.Contains("vulture") || code.Contains("scavenger"))
            return 10;

        // Tier 8: Medium prey animals
        if (code.Contains("deer") || code.Contains("fox") || code.Contains("vulpini") || code.Contains("urocyonini") ||
            code.Contains("cerdocyonina") || code.Contains("canina") || code.Contains("bighorn") ||
            code.Contains("gazelle") ||
            code.Contains("antelope") || code.Contains("caribou") || code.Contains("elk") ||
            code.Contains("boar") || code.Contains("lynx") || code.Contains("caracal"))
            return 8;

        // Tier 5: Small domesticated / raccoon-sized
        if (code.Contains("pig") || code.Contains("sheep") || code.Contains("raccoon") ||
            code.Contains("goat") || code.Contains("lamb") || code.Contains("calf") ||
            code.Contains("badger") || code.Contains("otter"))
            return 5;

        // Tier 3: Tiny animals (default for any animal)
        if (code.Contains("chicken") || code.Contains("hare") || code.Contains("rabbit") ||
            code.Contains("bird") || code.Contains("chick") || code.Contains("rodent") ||
            code.Contains("squirrel") || code.Contains("rat") || code.Contains("mouse") ||
            code.StartsWith("animal") || code.Contains("/animal/"))
            return 3;

        // Not recognized as an animal
        return 0;
    }
}