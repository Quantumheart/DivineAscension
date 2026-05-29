using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using Vintagestory.API.MathTools;

namespace DivineAscension.Models.Enum;

/// <summary>
///     Immutable per-domain facts. Promotes the dozens of per-domain
///     <c>switch</c> expressions scattered across the codebase (loc keys, colors,
///     short codes, ethos, feast day, prayer particle color, prestige keywords)
///     into a single record so a new <see cref="DeityDomain" /> is described in
///     exactly one place (#558).
/// </summary>
/// <param name="Domain">The domain this metadata describes.</param>
/// <param name="ShortCode">One-letter code for the cross-deity summary strip.</param>
/// <param name="NameLocKey">Localization key for the domain's display name.</param>
/// <param name="TitleLocKey">Localization key for the domain's title.</param>
/// <param name="DescriptionLocKey">Localization key for the domain's description.</param>
/// <param name="EpithetLocKey">Localization key for the founder epithet of a civ with this patron.</param>
/// <param name="FeastPatronLocKey">Localization key for the auto-seeded patron feast name.</param>
/// <param name="TitleSuffix">English title fragment, e.g. "of the Craft" (UI tooltips, not yet localized).</param>
/// <param name="TooltipDescription">English long-form tooltip blurb (not yet localized).</param>
/// <param name="PrimaryColor">Manuscript-ink accent color for this domain.</param>
/// <param name="PrayerParticleRgba">ARGB-packed color for prayer particles at this domain's altars.</param>
/// <param name="Ethos">Civilization ethos derived when this domain is the founder's patron.</param>
/// <param name="HolyDay">Fixed patron's-day (in-game month/day) for the auto-seeded feast.</param>
/// <param name="PrestigeActivityKeywords">Action-type substrings that earn prestige for this domain.</param>
public record DeityDomainMetadata(
    DeityDomain Domain,
    string ShortCode,
    string NameLocKey,
    string TitleLocKey,
    string DescriptionLocKey,
    string EpithetLocKey,
    string FeastPatronLocKey,
    string TitleSuffix,
    string TooltipDescription,
    Vector4 PrimaryColor,
    int PrayerParticleRgba,
    CivilizationEthos Ethos,
    (int Month, int Day) HolyDay,
    IReadOnlyList<string> PrestigeActivityKeywords);

/// <summary>
///     The single source of truth for per-domain metadata. A real domain
///     (everything in <see cref="DeityDomains.All" />) without an entry here
///     throws on first lookup — much louder than the old default-case
///     "Unknown" silent degrade. The <c>DeityDomainRegistry_CoversAllDomains</c>
///     guard test fails CI the moment a new domain is added without an entry.
/// </summary>
public static class DeityDomainRegistry
{
    private static readonly Dictionary<DeityDomain, DeityDomainMetadata> Map = new()
    {
        [DeityDomain.Craft] = new DeityDomainMetadata(
            DeityDomain.Craft, "C",
            LocalizationKeys.DOMAIN_CRAFT_NAME, LocalizationKeys.DOMAIN_CRAFT_TITLE,
            LocalizationKeys.DOMAIN_CRAFT_DESCRIPTION, LocalizationKeys.CIVILIZATION_EPITHET_CRAFT,
            LocalizationKeys.FEAST_PATRON_CRAFT_NAME,
            "of the Craft",
            "The domain of the Forge and Craft. Followers are rewarded for their dedication to metalworking, smithing, and the creation of tools and weapons. Those who shape raw materials into works of utility and art earn the favor of this domain.",
            new Vector4(0.698f, 0.416f, 0.165f, 1.0f), // #B26A2A copper — Forge & Craft
            ColorUtil.ToRgba(255, 255, 60, 40), // red/orange for forging
            CivilizationEthos.Mercantile, (2, 1),
            new[] { "mining", "smithing", "smelting", "anvil" }),

        [DeityDomain.Wild] = new DeityDomainMetadata(
            DeityDomain.Wild, "W",
            LocalizationKeys.DOMAIN_WILD_NAME, LocalizationKeys.DOMAIN_WILD_TITLE,
            LocalizationKeys.DOMAIN_WILD_DESCRIPTION, LocalizationKeys.CIVILIZATION_EPITHET_WILD,
            LocalizationKeys.FEAST_PATRON_WILD_NAME,
            "of the Wild",
            "The domain of the Hunt and Wild. Followers are rewarded for patience, precision, and tracking. Those who master the hunt and live in harmony with the wilderness earn the favor of this domain.",
            new Vector4(0.361f, 0.431f, 0.165f, 1.0f), // #5C6E2A olive — Hunt & Wild
            ColorUtil.ToRgba(255, 50, 220, 80), // green for nature
            CivilizationEthos.Mystic, (4, 15),
            new[] { "hunting", "foraging", "skinning", "exploration" }),

        [DeityDomain.Conquest] = new DeityDomainMetadata(
            DeityDomain.Conquest, "Q",
            LocalizationKeys.DOMAIN_CONQUEST_NAME, LocalizationKeys.DOMAIN_CONQUEST_TITLE,
            LocalizationKeys.DOMAIN_CONQUEST_DESCRIPTION, LocalizationKeys.CIVILIZATION_EPITHET_CONQUEST,
            LocalizationKeys.FEAST_PATRON_CONQUEST_NAME,
            "of Conquest",
            "The domain of Domination and Victory. Followers are rewarded for martial prowess, defeating enemies, and expanding their dominance through strength. Those who prove their superiority in combat and claim victory earn the favor of this domain.",
            new Vector4(0.557f, 0.180f, 0.122f, 1.0f), // #8E2E1F dried-blood red — Domination & Victory
            ColorUtil.ToRgba(255, 140, 20, 30), // crimson/blood for battle
            CivilizationEthos.Martial, (7, 4),
            new[] { "combat", "battle", "fight", "discovered", "ruin", "patrol" }),

        [DeityDomain.Harvest] = new DeityDomainMetadata(
            DeityDomain.Harvest, "H",
            LocalizationKeys.DOMAIN_HARVEST_NAME, LocalizationKeys.DOMAIN_HARVEST_TITLE,
            LocalizationKeys.DOMAIN_HARVEST_DESCRIPTION, LocalizationKeys.CIVILIZATION_EPITHET_HARVEST,
            LocalizationKeys.FEAST_PATRON_HARVEST_NAME,
            "of the Harvest",
            "The domain of Agriculture and Light. Followers are rewarded for cultivation and nurturing growth through light and warmth. Those who tend the land and bring forth abundance earn the favor of this domain.",
            new Vector4(0.627f, 0.463f, 0.157f, 1.0f), // #A07628 wheat ochre — Agriculture & Light
            ColorUtil.ToRgba(255, 255, 200, 50), // golden for crops
            CivilizationEthos.Ascetic, (9, 12),
            new[] { "harvest", "planting", "cooking" }),

        [DeityDomain.Stone] = new DeityDomainMetadata(
            DeityDomain.Stone, "S",
            LocalizationKeys.DOMAIN_STONE_NAME, LocalizationKeys.DOMAIN_STONE_TITLE,
            LocalizationKeys.DOMAIN_STONE_DESCRIPTION, LocalizationKeys.CIVILIZATION_EPITHET_STONE,
            LocalizationKeys.FEAST_PATRON_STONE_NAME,
            "of the Stone",
            "The domain of Earth and Stone. Followers are rewarded for the transformative art of working with clay and earth. Those who shape pottery, form clay, and honor the elements of the ground earn the favor of this domain.",
            new Vector4(0.369f, 0.329f, 0.282f, 1.0f), // #5E5448 warm slate — Earth & Stone
            ColorUtil.ToRgba(255, 160, 140, 120), // brown/tan for earth
            CivilizationEthos.Sovereign, (11, 1),
            new[] { "pottery", "brick", "clay", "carving" }),

        [DeityDomain.Caravan] = new DeityDomainMetadata(
            DeityDomain.Caravan, "R",
            LocalizationKeys.DOMAIN_CARAVAN_NAME, LocalizationKeys.DOMAIN_CARAVAN_TITLE,
            LocalizationKeys.DOMAIN_CARAVAN_DESCRIPTION, LocalizationKeys.CIVILIZATION_EPITHET_CARAVAN,
            LocalizationKeys.FEAST_PATRON_CARAVAN_NAME,
            "of the Caravan",
            "The domain of Trade and Wayfaring. Followers are rewarded for honest exchange and miles travelled under open sky. Those who barter with strangers, carry goods between distant hearths, and walk the unmapped roads earn the favor of this domain.",
            new Vector4(0.761f, 0.541f, 0.118f, 1.0f), // #C28A1E road ochre — Trade & Wayfaring
            ColorUtil.ToRgba(255, 200, 150, 40), // amber for road dust
            CivilizationEthos.Mercantile, (6, 21),
            new[] { "trade", "discovered chunk", "encountered trader" })
    };

    /// <summary>All registered metadata, in <see cref="DeityDomains.All" /> order.</summary>
    public static readonly IReadOnlyList<DeityDomainMetadata> All =
        DeityDomains.All.Select(d => Map[d]).ToArray();

    /// <summary>
    ///     Metadata for a domain. Throws for a real domain with no entry (a bug a
    ///     guard test should already have caught) and for <see cref="DeityDomain.None" />.
    /// </summary>
    public static DeityDomainMetadata Get(DeityDomain domain) =>
        Map.TryGetValue(domain, out var meta)
            ? meta
            : throw new ArgumentException($"DeityDomain {domain} has no registered metadata", nameof(domain));

    /// <summary>
    ///     Metadata for a domain, or false for <see cref="DeityDomain.None" /> / any
    ///     unregistered value. Use on UI paths that legitimately receive None.
    /// </summary>
    public static bool TryGet(DeityDomain domain, out DeityDomainMetadata meta) =>
        Map.TryGetValue(domain, out meta!);
}
