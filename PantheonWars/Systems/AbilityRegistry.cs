using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using Vintagestory.API.Common;

namespace PantheonWars.Systems;

/// <summary>
///     Central registry for managing all deity abilities
/// </summary>
public class AbilityRegistry
{
    private readonly Dictionary<string, Ability> _abilities = new();
    private readonly Dictionary<DeityType, List<Ability>> _abilitiesByDeity = new();
    private readonly ICoreAPI _api;

    public AbilityRegistry(ICoreAPI api)
    {
        _api = api;
    }

    /// <summary>
    ///     Initializes the registry and registers all abilities
    /// </summary>
    public void Initialize()
    {
        _api.Logger.Notification("[PantheonWars] Initializing Ability Registry...");

        // Initialize deity ability lists
        foreach (DeityType deityType in Enum.GetValues(typeof(DeityType)))
            if (deityType != DeityType.None)
                _abilitiesByDeity[deityType] = new List<Ability>();

        // Register abilities (3-deity system - abilities not yet implemented for new deities)
        // TODO: Implement abilities for Aethra, Gaia, and Morthen

        _api.Logger.Notification($"[PantheonWars] Registered {_abilities.Count} abilities");
    }

    /// <summary>
    ///     Registers an ability in the registry
    /// </summary>
    private void RegisterAbility(Ability ability)
    {
        if (_abilities.ContainsKey(ability.Id))
        {
            _api.Logger.Warning($"[PantheonWars] Ability {ability.Id} already registered, skipping");
            return;
        }

        _abilities[ability.Id] = ability;

        if (_abilitiesByDeity.TryGetValue(ability.Deity, out var list)) list.Add(ability);

        _api.Logger.Debug($"[PantheonWars] Registered ability: {ability.Name} ({ability.Id})");
    }

    /// <summary>
    ///     Gets an ability by its ID
    /// </summary>
    public Ability? GetAbility(string abilityId)
    {
        return _abilities.TryGetValue(abilityId, out var ability) ? ability : null;
    }

    /// <summary>
    ///     Gets all abilities for a specific deity
    /// </summary>
    public IEnumerable<Ability> GetAbilitiesForDeity(DeityType deityType)
    {
        if (_abilitiesByDeity.TryGetValue(deityType, out var abilities)) return abilities;
        return Enumerable.Empty<Ability>();
    }

    /// <summary>
    ///     Gets all registered abilities
    /// </summary>
    public IEnumerable<Ability> GetAllAbilities()
    {
        return _abilities.Values;
    }

    /// <summary>
    ///     Checks if an ability exists
    /// </summary>
    public bool HasAbility(string abilityId)
    {
        return _abilities.ContainsKey(abilityId);
    }

    /// <summary>
    ///     Validates that an ability belongs to a specific deity
    /// </summary>
    public bool AbilityBelongsToDeity(string abilityId, DeityType deityType)
    {
        var ability = GetAbility(abilityId);
        return ability != null && ability.Deity == deityType;
    }

    #region Ability Registration

    // TODO: Implement abilities for 3-deity system
    // private void RegisterAethraAbilities() { }
    // private void RegisterGaiaAbilities() { }
    // private void RegisterMorthenAbilities() { }

    #endregion
}