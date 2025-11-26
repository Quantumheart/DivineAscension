using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Constants;

/// <summary>
///     Constants for all blessing identifiers across all deities.
///     Use these constants instead of hardcoded strings to prevent typos and enable refactoring.
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingIds
{
    #region Khoras (Forge & Craft) - 10 Blessings

    // Player Blessings (6)
    public const string KhorasCraftsmansTouch = "khoras_craftsmans_touch";
    public const string KhorasMasterworkTools = "khoras_masterwork_tools";
    public const string KhorasForgebornEndurance = "khoras_forgeborn_endurance";
    public const string KhorasLegendarySmith = "khoras_legendary_smith";
    public const string KhorasUnyielding = "khoras_unyielding";
    public const string KhorasAvatarOfForge = "khoras_avatar_of_forge";

    // Religion Blessings (4)
    public const string KhorasSharedWorkshop = "khoras_shared_workshop";
    public const string KhorasGuildOfSmiths = "khoras_guild_of_smiths";
    public const string KhorasMasterCraftsmen = "khoras_master_craftsmen";
    public const string KhorasPantheonOfCreation = "khoras_pantheon_of_creation";

    #endregion

    #region Lysa (Hunt & Wild) - 10 Blessings - Utility Focus

    // Player Blessings (6)
    public const string LysaHuntersInstinct = "lysa_hunters_instinct";
    public const string LysaMasterForager = "lysa_master_forager";
    public const string LysaApexPredator = "lysa_apex_predator";
    public const string LysaAbundanceOfWild = "lysa_abundance_of_wild";
    public const string LysaSilentDeath = "lysa_silent_death";
    public const string LysaAvatarOfWild = "lysa_avatar_of_wild";

    // Religion Blessings (4)
    public const string LysaHuntingParty = "lysa_hunting_party";
    public const string LysaWildernessTribe = "lysa_wilderness_tribe";
    public const string LysaChildrenOfForest = "lysa_children_of_forest";
    public const string LysaPantheonOfHunt = "lysa_pantheon_of_hunt";

    #endregion

    #region Morthen (Death) - 10 Blessings

    // Player Blessings (6)
    public const string MorthenDeathsEmbrace = "morthen_deaths_embrace";
    public const string MorthenSoulReaper = "morthen_soul_reaper";
    public const string MorthenUndying = "morthen_undying";
    public const string MorthenPlagueBearer = "morthen_plague_bearer";
    public const string MorthenDeathless = "morthen_deathless";
    public const string MorthenLordOfDeath = "morthen_lord_of_death";

    // Religion Blessings (4)
    public const string MorthenDeathCult = "morthen_death_cult";
    public const string MorthenNecromanticCovenant = "morthen_necromantic_covenant";
    public const string MorthenDeathlessLegion = "morthen_deathless_legion";
    public const string MorthenEmpireOfDeath = "morthen_empire_of_death";

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

    #region Umbros (Shadows) - 10 Blessings

    // Player Blessings (6)
    public const string UmbrosShadowBlend = "umbros_shadow_blend";
    public const string UmbrosAssassinate = "umbros_assassinate";
    public const string UmbrosPhantomDodge = "umbros_phantom_dodge";
    public const string UmbrosDeadlyAmbush = "umbros_deadly_ambush";
    public const string UmbrosVanish = "umbros_vanish";
    public const string UmbrosAvatarOfShadows = "umbros_avatar_of_shadows";

    // Religion Blessings (4)
    public const string UmbrosShadowCult = "umbros_shadow_cult";
    public const string UmbrosCloak = "umbros_cloak";
    public const string UmbrosNightAssassins = "umbros_night_assassins";
    public const string UmbrosEternalDarkness = "umbros_eternal_darkness";

    #endregion

    #region Tharos (Storms) - 10 Blessings

    // Player Blessings (6)
    public const string TharosStormborn = "tharos_stormborn";
    public const string TharosLightningStrike = "tharos_lightning_strike";
    public const string TharosStormRider = "tharos_storm_rider";
    public const string TharosThunderlord = "tharos_thunderlord";
    public const string TharosTempest = "tharos_tempest";
    public const string TharosAvatarOfStorms = "tharos_avatar_of_storms";

    // Religion Blessings (4)
    public const string TharosStormCallers = "tharos_storm_callers";
    public const string TharosLightningChain = "tharos_lightning_chain";
    public const string TharosThunderstorm = "tharos_thunderstorm";
    public const string TharosEyeOfTheStorm = "tharos_eye_of_the_storm";

    #endregion

    #region Gaia (Earth) - 10 Blessings

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

    #region Vex (Madness) - 10 Blessings

    // Player Blessings (6)
    public const string VexMaddeningWhispers = "vex_maddening_whispers";
    public const string VexChaoticFury = "vex_chaotic_fury";
    public const string VexDeliriumShield = "vex_delirium_shield";
    public const string VexPandemonium = "vex_pandemonium";
    public const string VexMindFortress = "vex_mind_fortress";
    public const string VexAvatarOfMadness = "vex_avatar_of_madness";

    // Religion Blessings (4)
    public const string VexCultOfChaos = "vex_cult_of_chaos";
    public const string VexSharedMadness = "vex_shared_madness";
    public const string VexInsanityAura = "vex_insanity_aura";
    public const string VexRealmOfMadness = "vex_realm_of_madness";

    #endregion
}