namespace DivineAscension.Models.Enum;

/// <summary>
///     Narrative identity axis for a civilization, picked once at founding.
///     Derived by default from the founder religion's <see cref="DeityDomain" />;
///     may be overridden by the founder if creation UI exposes the choice.
/// </summary>
/// <remarks>
///     Sovereign is the safe default for legacy saves that pre-date this field.
///     Domain → Ethos mapping is defined in <see cref="CivilizationEthosDeriver" />.
/// </remarks>
public enum CivilizationEthos
{
    Sovereign = 0,
    Mercantile = 1,
    Martial = 2,
    Mystic = 3,
    Ascetic = 4
}
