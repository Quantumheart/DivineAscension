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
    ///     Get the localized name for a DeityType.
    /// </summary>
    /// <param name="deity">The deity type</param>
    /// <returns>Localized deity name</returns>
    public static string ToLocalizedString(this DeityType deity)
    {
        return deity switch
        {
            DeityType.Khoras => LocalizationService.Instance.Get(LocalizationKeys.DEITY_KHORAS_NAME),
            DeityType.Lysa => LocalizationService.Instance.Get(LocalizationKeys.DEITY_LYSA_NAME),
            DeityType.Aethra => LocalizationService.Instance.Get(LocalizationKeys.DEITY_AETHRA_NAME),
            DeityType.Gaia => LocalizationService.Instance.Get(LocalizationKeys.DEITY_GAIA_NAME),
            _ => LocalizationService.Instance.Get(LocalizationKeys.DEITY_UNKNOWN_NAME)
        };
    }

    /// <summary>
    ///     Get the localized name with title for a DeityType.
    ///     Example: "Khoras - God of Forge & Craft"
    /// </summary>
    /// <param name="deity">The deity type</param>
    /// <returns>Localized deity name with title</returns>
    public static string ToLocalizedStringWithTitle(this DeityType deity)
    {
        var name = deity.ToLocalizedString();
        var title = deity switch
        {
            DeityType.Khoras => LocalizationService.Instance.Get(LocalizationKeys.DEITY_KHORAS_TITLE),
            DeityType.Lysa => LocalizationService.Instance.Get(LocalizationKeys.DEITY_LYSA_TITLE),
            DeityType.Aethra => LocalizationService.Instance.Get(LocalizationKeys.DEITY_AETHRA_TITLE),
            DeityType.Gaia => LocalizationService.Instance.Get(LocalizationKeys.DEITY_GAIA_TITLE),
            _ => ""
        };

        return string.IsNullOrEmpty(title) ? name : $"{name} - {title}";
    }

    /// <summary>
    ///     Get the localized description for a DeityType.
    /// </summary>
    /// <param name="deity">The deity type</param>
    /// <returns>Localized deity description</returns>
    public static string ToLocalizedDescription(this DeityType deity)
    {
        return deity switch
        {
            DeityType.Khoras => LocalizationService.Instance.Get(LocalizationKeys.DEITY_KHORAS_DESCRIPTION),
            DeityType.Lysa => LocalizationService.Instance.Get(LocalizationKeys.DEITY_LYSA_DESCRIPTION),
            DeityType.Aethra => LocalizationService.Instance.Get(LocalizationKeys.DEITY_AETHRA_DESCRIPTION),
            DeityType.Gaia => LocalizationService.Instance.Get(LocalizationKeys.DEITY_GAIA_DESCRIPTION),
            _ => LocalizationService.Instance.Get(LocalizationKeys.DEITY_UNKNOWN_NAME)
        };
    }
}