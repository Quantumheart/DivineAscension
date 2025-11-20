using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Constants;

/// <summary>
///     Constants for all blessing identifiers across all deities.
///     Use these constants instead of hardcoded strings to prevent typos and enable refactoring.
///     3-deity system: Aethra (Light), Gaia (Nature), Morthen (Shadow & Death)
///     Total: 30 blessings (3 deities × 10 blessings each)
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingIds
{
    #region Morthen (Shadow & Death) - 10 Blessings

    // Player Blessings (6)
    public const string MorthenDeathsEmbrace = "morthen_deaths_embrace";
    public const string MorthenSoulReaper = "morthen_soul_reaper";
    public const string MorthenUndying = "morthen_undying";
    public const string MorthenPlagueBearer = "morthen_plague_bearer";
    public const string MorthenDeathless = "morthen_deathless";
    public const string MorthenLordOfDeath = "morthen_lord_of_death";

    // Religion Blessings (4)
    public const string MorthenShadowCult = "morthen_shadow_cult";
    public const string MorthenNecromanticCovenant = "morthen_necromantic_covenant";
    public const string MorthenDeathlessLegion = "morthen_deathless_legion";
    public const string MorthenEmpireOfDarkness = "morthen_empire_of_darkness";

    #endregion

    #region Aethra (Light) - 10 Blessings

    // Player Blessings (6)
    public const string AethraDivineGrace = "aethra_divine_grace";
    public const string AethraRadiantStrike = "aethra_radiant_strike";
    public const string AethraBlessedShield = "aethra_blessed_shield";
    public const string AethraPurifyingLight = "aethra_purifying_light";
    public const string AethraAegisOfLight = "aethra_aegis_of_light";
    public const string AethraAvatarOfLight = "aethra_avatar_of_light";

    // Religion Blessings (4)
    public const string AethraBlessingOfLight = "aethra_blessing_of_light";
    public const string AethraDivineSanctuary = "aethra_divine_sanctuary";
    public const string AethraSacredBond = "aethra_sacred_bond";
    public const string AethraCathedralOfLight = "aethra_cathedral_of_light";

    #endregion

    #region Gaia (Nature) - 10 Blessings

    // Player Blessings (6)
    public const string GaiaEarthenResilience = "gaia_earthen_resilience";
    public const string GaiaStoneForm = "gaia_stone_form";
    public const string GaiaNaturesBlessing = "gaia_natures_blessing";
    public const string GaiaMountainGuard = "gaia_mountain_guard";
    public const string GaiaLifebloom = "gaia_lifebloom";
    public const string GaiaAvatarOfEarth = "gaia_avatar_of_earth";

    // Religion Blessings (4)
    public const string GaiaEarthwardens = "gaia_earthwardens";
    public const string GaiaLivingFortress = "gaia_living_fortress";
    public const string GaiaNaturesWrath = "gaia_natures_wrath";
    public const string GaiaWorldTree = "gaia_world_tree";

    #endregion
}