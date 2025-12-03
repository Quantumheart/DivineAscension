using System.Collections.Generic;
using PantheonWars.Models.Enum;
using Vintagestory.GameContent;

namespace PantheonWars.Models;

/// <summary>
///     Represents a passive blessing that can be unlocked by players or religions
/// </summary>
public class Blessing
{
    /// <summary>
    ///     Creates a new blessing with the specified ID, name, and deity
    /// </summary>
    public Blessing(string blessingId, string name, DeityType deity)
    {
        BlessingId = blessingId;
        Name = name;
        Deity = deity;
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public Blessing()
    {
    }

    /// <summary>
    ///     Unique identifier for this blessing (e.g., "khoras_warriors_resolve")
    /// </summary>
    public string BlessingId { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the blessing
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Detailed description of what the blessing does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Which deity this blessing belongs to
    /// </summary>
    public DeityType Deity { get; set; }

    /// <summary>
    ///     Type of blessing (Player or Religion)
    /// </summary>
    public BlessingKind Kind { get; set; }

    /// <summary>
    ///     Category for organization (Combat, Defense, etc.)
    /// </summary>
    public BlessingCategory Category { get; set; }

    /// <summary>
    ///     Required player favor rank to unlock (for Player blessings)
    /// </summary>
    public int RequiredFavorRank { get; set; }

    /// <summary>
    ///     Required religion prestige rank to unlock (for Religion blessings)
    /// </summary>
    public int RequiredPrestigeRank { get; set; }

    /// <summary>
    ///     List of prerequisite blessing IDs that must be unlocked first
    /// </summary>
    public List<string>? PrerequisiteBlessings { get; set; } = new();

    /// <summary>
    ///     Dictionary of stat modifiers this blessing provides
    ///     Key: stat name (e.g., "walkspeed", "meleeDamageMultiplier")
    ///     Value: modifier value (additive)
    /// </summary>
    public Dictionary<string, float> StatModifiers { get; set; } = new();

    /// <summary>
    ///     List of special effect identifiers for complex blessing behaviors
    /// </summary>
    public List<string>? SpecialEffects { get; set; } = new();
}