using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Services;

namespace DivineAscension.Extensions;

/// <summary>
///     Extension methods for enums to provide localized display strings.
/// </summary>
public static class EnumLocalizationExtensions
{
    /// <summary>
    ///     Get the localized display name for a FavorRank.
    /// </summary>
    /// <param name="rank">The favor rank</param>
    /// <returns>Localized rank name</returns>
    public static string ToLocalizedString(this FavorRank rank)
    {
        return rank switch
        {
            FavorRank.Initiate => LocalizationService.Instance.Get(LocalizationKeys.RANK_FAVOR_INITIATE),
            FavorRank.Disciple => LocalizationService.Instance.Get(LocalizationKeys.RANK_FAVOR_DISCIPLE),
            FavorRank.Zealot => LocalizationService.Instance.Get(LocalizationKeys.RANK_FAVOR_ZEALOT),
            FavorRank.Champion => LocalizationService.Instance.Get(LocalizationKeys.RANK_FAVOR_CHAMPION),
            FavorRank.Avatar => LocalizationService.Instance.Get(LocalizationKeys.RANK_FAVOR_AVATAR),
            _ => rank.ToString()
        };
    }

    /// <summary>
    ///     Get the localized display name for a PrestigeRank.
    /// </summary>
    /// <param name="rank">The prestige rank</param>
    /// <returns>Localized rank name</returns>
    public static string ToLocalizedString(this PrestigeRank rank)
    {
        return rank switch
        {
            PrestigeRank.Fledgling => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_FLEDGLING),
            PrestigeRank.Established => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_ESTABLISHED),
            PrestigeRank.Renowned => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_RENOWNED),
            PrestigeRank.Legendary => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_LEGENDARY),
            PrestigeRank.Mythic => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_MYTHIC),
            _ => rank.ToString()
        };
    }

    /// <summary>
    ///     Get the localized name for a DeityDomain.
    /// </summary>
    /// <param name="deity">The deity domain</param>
    /// <returns>Localized domain name</returns>
    public static string ToLocalizedString(this DeityDomain deity)
    {
        return deity switch
        {
            DeityDomain.Craft => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_CRAFT_NAME),
            DeityDomain.Wild => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_WILD_NAME),
            DeityDomain.Harvest => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_HARVEST_NAME),
            DeityDomain.Stone => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_STONE_NAME),
            _ => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_UNKNOWN_NAME)
        };
    }

    /// <summary>
    ///     Get the localized name with title for a DeityDomain.
    ///     Example: "Craft - God of Forge & Craft"
    /// </summary>
    /// <param name="deity">The deity domain</param>
    /// <returns>Localized domain name with title</returns>
    public static string ToLocalizedStringWithTitle(this DeityDomain deity)
    {
        var name = deity.ToLocalizedString();
        var title = deity switch
        {
            DeityDomain.Craft => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_CRAFT_TITLE),
            DeityDomain.Wild => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_WILD_TITLE),
            DeityDomain.Harvest => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_HARVEST_TITLE),
            DeityDomain.Stone => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_STONE_TITLE),
            _ => ""
        };

        return string.IsNullOrEmpty(title) ? name : $"{name} - {title}";
    }

    /// <summary>
    ///     Get the localized description for a DeityDomain.
    /// </summary>
    /// <param name="deity">The deity domain</param>
    /// <returns>Localized domain description</returns>
    public static string ToLocalizedDescription(this DeityDomain deity)
    {
        return deity switch
        {
            DeityDomain.Craft => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_CRAFT_DESCRIPTION),
            DeityDomain.Wild => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_WILD_DESCRIPTION),
            DeityDomain.Harvest => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_HARVEST_DESCRIPTION),
            DeityDomain.Stone => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_STONE_DESCRIPTION),
            _ => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_UNKNOWN_NAME)
        };
    }
}