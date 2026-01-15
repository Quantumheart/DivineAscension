using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Constants;

/// <summary>
///     Constants for all blessing identifiers across all domains.
///     Use these constants instead of hardcoded strings to prevent typos and enable refactoring.
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingIds
{
    #region Craft (Forge & Craft) - 10 Blessings

    // Player Blessings (6)
    public const string CraftCraftsmansTouch = "khoras_craftsmans_touch";
    public const string CraftMasterworkTools = "khoras_masterwork_tools";
    public const string CraftForgebornEndurance = "khoras_forgeborn_endurance";
    public const string CraftLegendarySmith = "khoras_legendary_smith";
    public const string CraftUnyielding = "khoras_unyielding";
    public const string CraftAvatarOfForge = "khoras_avatar_of_forge";

    // Religion Blessings (4)
    public const string CraftSharedWorkshop = "khoras_shared_workshop";
    public const string CraftGuildOfSmiths = "khoras_guild_of_smiths";
    public const string CraftMasterCraftsmen = "khoras_master_craftsmen";
    public const string CraftPantheonOfCreation = "khoras_pantheon_of_creation";

    #endregion

    #region Wild (Hunt & Wild) - 10 Blessings - Utility Focus

    // Player Blessings (6)
    public const string WildHuntersInstinct = "lysa_hunters_instinct";
    public const string WildMasterForager = "lysa_master_forager";
    public const string WildApexPredator = "lysa_apex_predator";
    public const string WildAbundanceOfWild = "lysa_abundance_of_wild";
    public const string WildSilentDeath = "lysa_silent_death";
    public const string WildAvatarOfWild = "lysa_avatar_of_wild";

    // Religion Blessings (4)
    public const string WildHuntingParty = "lysa_hunting_party";
    public const string WildWildernessTribe = "lysa_wilderness_tribe";
    public const string WildChildrenOfForest = "lysa_children_of_forest";
    public const string WildPantheonOfHunt = "lysa_pantheon_of_hunt";

    #endregion

    #region War (Blood & Battle) - 10 Blessings

    // Player Blessings (6)
    public const string WarBloodthirst = "ares_bloodthirst";
    public const string WarBerserkerRage = "ares_berserker_rage";
    public const string WarIronWill = "ares_iron_will";
    public const string WarWarlordsStrike = "ares_warlords_strike";
    public const string WarUnyieldingFortitude = "ares_unyielding_fortitude";
    public const string WarAvatarOfWar = "ares_avatar_of_war";

    // Religion Blessings (4)
    public const string WarWarband = "ares_warband";
    public const string WarLegionOfBlood = "ares_legion_of_blood";
    public const string WarConquerorsBanner = "ares_conquerors_banner";
    public const string WarPantheonOfWar = "ares_pantheon_of_war";

    #endregion

    #region Harvest (Agriculture & Light) - 10 Blessings

    // Player Blessings (6)
    public const string HarvestSunsBlessing = "aethra_suns_blessing";
    public const string HarvestBountifulHarvest = "aethra_bountiful_harvest";
    public const string HarvestBakersTouch = "aethra_bakers_touch";
    public const string HarvestMasterFarmer = "aethra_master_farmer";
    public const string HarvestDivineKitchen = "aethra_divine_kitchen";
    public const string HarvestAvatarOfAbundance = "aethra_avatar_of_abundance";

    // Religion Blessings (4)
    public const string HarvestCommunityFarm = "aethra_community_farm";
    public const string HarvestHarvestFestival = "aethra_harvest_festival";
    public const string HarvestLandOfPlenty = "aethra_land_of_plenty";
    public const string HarvestPantheonOfLight = "aethra_pantheon_of_light";

    #endregion


    #region Stone (Pottery & Clay) - 10 Blessings

    // Player Blessings (6)
    public const string StoneClayShaper = "gaia_clay_shaper";
    public const string StoneMasterPotter = "gaia_master_potter";
    public const string StoneEarthenBuilder = "gaia_earthen_builder";
    public const string StoneKilnMaster = "gaia_kiln_master";
    public const string StoneClayArchitect = "gaia_clay_architect";
    public const string StoneAvatarOfClay = "gaia_avatar_of_clay";

    // Religion Blessings (4)
    public const string StonePottersCircle = "gaia_potters_circle";
    public const string StoneClayGuild = "gaia_clay_guild";
    public const string StoneEarthenCommunity = "gaia_earthen_community";
    public const string StonePantheonOfClay = "gaia_pantheon_of_clay";

    #endregion
}