using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Constants;

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

    #region Aethra (Agriculture & Light) - 10 Blessings

    // Player Blessings (6)
    public const string AethraSunsBlessing = "aethra_suns_blessing";
    public const string AethraBountifulHarvest = "aethra_bountiful_harvest";
    public const string AethraBakersTouch = "aethra_bakers_touch";
    public const string AethraMasterFarmer = "aethra_master_farmer";
    public const string AethraDivineKitchen = "aethra_divine_kitchen";
    public const string AethraAvatarOfAbundance = "aethra_avatar_of_abundance";

    // Religion Blessings (4)
    public const string AethraCommunityFarm = "aethra_community_farm";
    public const string AethraHarvestFestival = "aethra_harvest_festival";
    public const string AethraLandOfPlenty = "aethra_land_of_plenty";
    public const string AethraPantheonOfLight = "aethra_pantheon_of_light";

    #endregion


    #region Gaia (Pottery & Clay) - 10 Blessings

    // Player Blessings (6)
    public const string GaiaClayShaper = "gaia_clay_shaper";
    public const string GaiaMasterPotter = "gaia_master_potter";
    public const string GaiaEarthenBuilder = "gaia_earthen_builder";
    public const string GaiaKilnMaster = "gaia_kiln_master";
    public const string GaiaClayArchitect = "gaia_clay_architect";
    public const string GaiaAvatarOfClay = "gaia_avatar_of_clay";

    // Religion Blessings (4)
    public const string GaiaPottersCircle = "gaia_potters_circle";
    public const string GaiaClayGuild = "gaia_clay_guild";
    public const string GaiaEarthenCommunity = "gaia_earthen_community";
    public const string GaiaPantheonOfClay = "gaia_pantheon_of_clay";

    #endregion
}