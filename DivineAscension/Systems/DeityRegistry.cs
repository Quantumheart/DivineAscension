using System.Collections.Generic;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
///     Central registry for managing all deities in the game
/// </summary>
public class DeityRegistry : IDeityRegistry
{
    private readonly ICoreAPI _api;
    private readonly Dictionary<DeityType, Deity> _deities = new();

    public DeityRegistry(ICoreAPI api)
    {
        _api = api;
    }

    /// <summary>
    ///     Initializes the registry with all deities
    /// </summary>
    public void Initialize()
    {
        _api.Logger.Notification("[DivineAscension] Initializing Deity Registry...");

        // Register deities (Utility-focused system - 4 deities)
        RegisterDeity(CreateKhoras());
        RegisterDeity(CreateLysa());
        // TODO: Phase 1 - Add CreateAethra() and CreateGaia() implementations
        // RegisterDeity(CreateAethra());
        // RegisterDeity(CreateGaia());

        _api.Logger.Notification($"[DivineAscension] Registered {_deities.Count} deities");
    }

    /// <summary>
    ///     Gets a deity by type
    /// </summary>
    public Deity? GetDeity(DeityType type)
    {
        return _deities.TryGetValue(type, out var deity) ? deity : null;
    }

    /// <summary>
    ///     Gets all registered deities
    /// </summary>
    public IEnumerable<Deity> GetAllDeities()
    {
        return _deities.Values;
    }

    /// <summary>
    ///     Checks if a deity exists
    /// </summary>
    public bool HasDeity(DeityType type)
    {
        return _deities.ContainsKey(type);
    }

    /// <summary>
    ///     Registers a deity in the registry
    /// </summary>
    private void RegisterDeity(Deity deity)
    {
        if (_deities.ContainsKey(deity.Type))
        {
            _api.Logger.Warning($"[DivineAscension] Deity {deity.Name} already registered, skipping");
            return;
        }

        _deities[deity.Type] = deity;
        _api.Logger.Debug($"[DivineAscension] Registered deity: {deity.Name} ({deity.Domain})");
    }

    #region Deity Definitions

    private Deity CreateKhoras()
    {
        return new Deity(DeityType.Khoras, "Khoras", "War")
        {
            Description = "The God of War, Khoras embodies martial prowess and strategic combat. " +
                          "Followers gain powerful offensive abilities and excel in direct confrontation.",
            Alignment = DeityAlignment.Lawful,
            PrimaryColor = "#8B0000", // Dark Red
            SecondaryColor = "#FFD700", // Gold
            Playstyle = "Aggressive melee combat with high damage abilities and tactical buffs",
            AbilityIds = new List<string>
            {
                // To be implemented in Task 6
                "khoras_warbanner",
                "khoras_battlecry",
                "khoras_blade_storm",
                "khoras_last_stand"
            }
        };
    }

    private Deity CreateLysa()
    {
        return new Deity(DeityType.Lysa, "Lysa", "Hunt")
        {
            Description = "The Goddess of the Hunt, Lysa rewards patience, precision, and tracking. " +
                          "Followers gain mobility and ranged combat advantages.",
            Alignment = DeityAlignment.Neutral,
            PrimaryColor = "#228B22", // Forest Green
            SecondaryColor = "#8B4513", // Saddle Brown
            Playstyle = "Mobile ranged combat with tracking abilities and tactical positioning",
            AbilityIds = new List<string>
            {
                // To be implemented in Task 6
                "lysa_hunters_mark",
                "lysa_swift_feet",
                "lysa_arrow_rain",
                "lysa_predator_instinct"
            }
        };
    }

    // TODO: Implement remaining utility-focused deities in Phase 1
    // private Deity CreateAethra() { ... } - Goddess of Agriculture & Light
    // private Deity CreateGaia() { ... } - Goddess of Earth & Stone

    #endregion
}