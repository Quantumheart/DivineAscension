using System.Collections.Generic;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;

namespace PantheonWars.Systems;

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
    ///     3-deity system: Aethra (Light/Good), Gaia (Nature/Neutral), Morthen (Shadow & Death/Evil)
    /// </summary>
    public void Initialize()
    {
        _api.Logger.Notification("[PantheonWars] Initializing Deity Registry...");

        // Register the 3 deities
        RegisterDeity(CreateAethra());
        RegisterDeity(CreateGaia());
        RegisterDeity(CreateMorthen());

        _api.Logger.Notification($"[PantheonWars] Registered {_deities.Count} deities");
    }

    /// <summary>
    ///     Registers a deity in the registry
    /// </summary>
    private void RegisterDeity(Deity deity)
    {
        if (_deities.ContainsKey(deity.Type))
        {
            _api.Logger.Warning($"[PantheonWars] Deity {deity.Name} already registered, skipping");
            return;
        }

        _deities[deity.Type] = deity;
        _api.Logger.Debug($"[PantheonWars] Registered deity: {deity.Name} ({deity.Domain})");
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
    ///     Gets the relationship between two deities
    /// </summary>
    public DeityRelationshipType GetRelationship(DeityType deity1, DeityType deity2)
    {
        if (deity1 == deity2) return DeityRelationshipType.Neutral;

        var deity = GetDeity(deity1);
        if (deity == null) return DeityRelationshipType.Neutral;

        return deity.Relationships.TryGetValue(deity2, out var relationship)
            ? relationship
            : DeityRelationshipType.Neutral;
    }

    /// <summary>
    ///     Gets the favor multiplier based on deity relationship
    ///     Allied: 0.5x favor, Rival: 2x favor, Neutral: 1x favor
    /// </summary>
    public float GetFavorMultiplier(DeityType attackerDeity, DeityType victimDeity)
    {
        var relationship = GetRelationship(attackerDeity, victimDeity);
        return relationship switch
        {
            DeityRelationshipType.Allied => 0.5f,
            DeityRelationshipType.Rival => 2.0f,
            _ => 1.0f
        };
    }

    #region Deity Definitions

    private Deity CreateAethra()
    {
        return new Deity(DeityType.Aethra, "Aethra", "Light")
        {
            Description = "The Goddess of Light, Aethra embodies divine protection, healing, and holy power. " +
                          "Followers gain defensive abilities and support their allies with healing and shields.",
            Alignment = DeityAlignment.Lawful,
            PrimaryColor = "#FFFFE0", // Light Yellow
            SecondaryColor = "#FFD700", // Gold
            Playstyle = "Support and tank playstyle with healing, shields, and defensive buffs",
            Relationships = new Dictionary<DeityType, DeityRelationshipType>
            {
                { DeityType.Gaia, DeityRelationshipType.Neutral },
                { DeityType.Morthen, DeityRelationshipType.Rival }
            },
            AbilityIds = new List<string>()
        };
    }

    private Deity CreateGaia()
    {
        return new Deity(DeityType.Gaia, "Gaia", "Nature")
        {
            Description = "The Goddess of Nature, Gaia embodies the earth's strength, durability, and regeneration. " +
                          "Followers become immovable tanks with powerful regeneration and defensive abilities.",
            Alignment = DeityAlignment.Neutral,
            PrimaryColor = "#8B7355", // Brown
            SecondaryColor = "#228B22", // Forest Green
            Playstyle = "Tank playstyle focused on durability, regeneration, and outlasting enemies",
            Relationships = new Dictionary<DeityType, DeityRelationshipType>
            {
                { DeityType.Aethra, DeityRelationshipType.Neutral },
                { DeityType.Morthen, DeityRelationshipType.Neutral }
            },
            AbilityIds = new List<string>()
        };
    }

    private Deity CreateMorthen()
    {
        return new Deity(DeityType.Morthen, "Morthen", "Shadow & Death")
        {
            Description = "The God of Shadow & Death, Morthen embodies darkness, lifesteal, decay, and shadow magic. " +
                          "Followers drain life from enemies, spread poison and plague, and strike from the shadows.",
            Alignment = DeityAlignment.Chaotic,
            PrimaryColor = "#4B0082", // Dark Purple
            SecondaryColor = "#2F4F4F", // Dark Slate Gray
            Playstyle = "Sustain DPS playstyle with lifesteal, poison, shadow magic, and draining abilities",
            Relationships = new Dictionary<DeityType, DeityRelationshipType>
            {
                { DeityType.Gaia, DeityRelationshipType.Neutral },
                { DeityType.Aethra, DeityRelationshipType.Rival }
            },
            AbilityIds = new List<string>()
        };
    }

    #endregion
}