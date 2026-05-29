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
        return DeityDomainRegistry.TryGet(patronDomain, out var meta)
            ? (meta.Ethos, meta.EpithetLocKey)
            : (CivilizationEthos.Sovereign, LocalizationKeys.CIVILIZATION_EPITHET_DEFAULT);
    }
}
