using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Server;

namespace PantheonWars.Models;

/// <summary>
///     Base class for all deity abilities
/// </summary>
public abstract class Ability
{
    protected Ability(string id, string name, string description, DeityType deity, AbilityType type)
    {
        Id = id;
        Name = name;
        Description = description;
        Deity = deity;
        Type = type;
        MinimumRank = DevotionRank.Initiate; // Default: available from start
    }

    /// <summary>
    ///     Unique identifier for the ability (e.g., "khoras_warbanner")
    /// </summary>
    public string Id { get; protected set; }

    /// <summary>
    ///     Display name of the ability
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    ///     Description of what the ability does
    /// </summary>
    public string Description { get; protected set; }

    /// <summary>
    ///     Cooldown duration in seconds
    /// </summary>
    public float CooldownSeconds { get; protected set; }

    /// <summary>
    ///     Divine favor cost to use this ability
    /// </summary>
    public int FavorCost { get; protected set; }

    /// <summary>
    ///     Type/category of this ability
    /// </summary>
    public AbilityType Type { get; protected set; }

    /// <summary>
    ///     Which deity this ability belongs to
    /// </summary>
    public DeityType Deity { get; protected set; }

    /// <summary>
    ///     Minimum devotion rank required to use this ability
    /// </summary>
    public DevotionRank MinimumRank { get; protected set; }

    /// <summary>
    ///     Executes the ability. Returns true if successful, false if failed.
    /// </summary>
    /// <param name="caster">The player using the ability</param>
    /// <param name="sapi">Server API for accessing game state</param>
    /// <param name="buffManager">BuffManager for applying buffs/debuffs (optional for MVP abilities)</param>
    /// <returns>True if ability executed successfully, false otherwise</returns>
    public abstract bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null);

    /// <summary>
    ///     Validates if the ability can be used (override for custom validation logic)
    /// </summary>
    /// <param name="caster">The player attempting to use the ability</param>
    /// <param name="sapi">Server API</param>
    /// <param name="failureReason">Output parameter explaining why validation failed</param>
    /// <returns>True if ability can be used, false otherwise</returns>
    public virtual bool CanExecute(IServerPlayer caster, ICoreServerAPI sapi, out string failureReason)
    {
        failureReason = string.Empty;
        return true;
    }
}