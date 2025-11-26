using System;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class HuntingFavorTracker(IPlayerReligionDataManager playerReligionDataManager, ICoreServerAPI sapi, FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Lysa;
    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    
    public void Initialize()
    {
        _sapi.Event.OnEntityDeath += OnEntityDeath;
    }
    
    private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
    {
        if (entity == null || damageSource == null) return;

        // Check if the killer is a player
        Entity killer = damageSource.GetCauseEntity();
        if (killer is EntityPlayer { Player: IServerPlayer player })
        {
            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
            if (religionData?.ActiveDeity != DeityType) return;
             // Check if the victim is an animal
             if (IsAnimal(entity))
             {
                 _favorSystem.AwardFavorForAction(player, "hunting " + entity.Code.Path, 5);
             }
        }
    }

    private bool IsAnimal(Entity? entity)
    {
        if (entity == null) return false;
        // Heuristic: It's an agent (living), not a player
        if (entity is not EntityAgent) return false;
        if (entity is EntityPlayer) return false;
        
        // Common animal keywords in path
        string code = entity.Code.Path.ToLower();
        
        // Known animals
        string[] animals = new string[] { 
            "wolf", "pig", "sheep", "chicken", "hare", "fox", "bear", 
            "deer", "raccoon", "hyena", "gazelle", "bighorn", "moose"
        };
        
        // Known monsters to exclude
        string[] monsters = new string[] { "drifter", "locust", "bell" };
        
        foreach (var monster in monsters)
        {
            if (code.Contains(monster)) return false;
        }

        foreach (var animal in animals)
        {
            if (code.Contains(animal)) return true;
        }
        
        // Fallback: if it starts with "animal-" or contains "/animal/"
        if (code.StartsWith("animal") || code.Contains("/animal/")) return true;
        
        return false;
    }

    public void Dispose()
    {
        _sapi.Event.OnEntityDeath -= OnEntityDeath;
    }
}
