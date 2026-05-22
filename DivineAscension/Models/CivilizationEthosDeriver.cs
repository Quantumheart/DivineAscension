using DivineAscension.Constants;
using DivineAscension.Models.Enum;

namespace DivineAscension.Models;

/// <summary>
///     Maps a founder religion's patron <see cref="DeityDomain" /> to the
///     civilization's <see cref="CivilizationEthos" /> and founder epithet
///     localization key. Pure function, computed once at civ creation.
/// </summary>
public static class CivilizationEthosDeriver
{
    /// <summary>
    ///     Derives the ethos and the localization key for the founder epithet
    ///     from the founder religion's patron domain.
    /// </summary>
    public static (CivilizationEthos Ethos, string EpithetLocKey) Derive(DeityDomain patronDomain)
    {
        return patronDomain switch
        {
            DeityDomain.Craft => (CivilizationEthos.Mercantile, LocalizationKeys.CIVILIZATION_EPITHET_CRAFT),
            DeityDomain.Conquest => (CivilizationEthos.Martial, LocalizationKeys.CIVILIZATION_EPITHET_CONQUEST),
            DeityDomain.Wild => (CivilizationEthos.Mystic, LocalizationKeys.CIVILIZATION_EPITHET_WILD),
            DeityDomain.Harvest => (CivilizationEthos.Ascetic, LocalizationKeys.CIVILIZATION_EPITHET_HARVEST),
            DeityDomain.Stone => (CivilizationEthos.Sovereign, LocalizationKeys.CIVILIZATION_EPITHET_STONE),
            _ => (CivilizationEthos.Sovereign, LocalizationKeys.CIVILIZATION_EPITHET_DEFAULT)
        };
    }
}
