using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Constants;

/// <summary>
///     Constants for Vintage Story's stat system.
///     These are the actual stat names used by VS's entity.Stats API.
///     Use these constants in blessing definitions to ensure consistency.
/// </summary>
[ExcludeFromCodeCoverage]
public static class VintageStoryStats
{
    // Combat Stats
    public const string MeleeWeaponsDamage = "meleeWeaponsDamage";
    public const string RangedWeaponsDamage = "rangedWeaponsDamage";
    public const string MeleeWeaponsSpeed = "meleeWeaponsSpeed";

    // Defense Stats
    public const string MeleeWeaponArmor = "meleeWeaponArmor";
    public const string MaxHealthExtraPoints = "maxhealthExtraPoints";

    // Movement Stats
    public const string WalkSpeed = "walkspeed";

    // Utility Stats
    public const string HealingEffectiveness = "healingeffectivness";
    public const string HungerRate = "hungerrate";
    public const string MiningSpeedMul = "miningSpeedMul";

    // Ranged Stats
    public const string RangedWeaponsAcc = "rangedWeaponsAcc";
    public const string RangedWeaponsSpeed = "rangedWeaponsSpeed";
}