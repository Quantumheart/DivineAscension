using System.Collections.Generic;
using DivineAscension.Network.Diplomacy;

namespace DivineAscension.GUI.UI.Adapters.Diplomacy;

/// <summary>
///     UI-only data source for diplomatic relationships and pending proposals.
///     Lets the Accords + Propose pages render seeded data in DEBUG without
///     a server round-trip. Mirrors the
///     <see cref="DivineAscension.GUI.UI.Adapters.Civilizations.ICivilizationProvider" />
///     pattern.
/// </summary>
internal interface IDiplomacyProvider
{
    /// <summary>
    ///     Synthesise diplomatic state for <paramref name="currentCivId" /> against
    ///     the supplied roster of other realms (typically the fake civ list).
    /// </summary>
    DiplomacyInfoResponsePacket GetDiplomacyInfo(
        string currentCivId,
        IReadOnlyList<(string CivId, string Name)> otherRealms);

    void ConfigureDevSeed(int seed);
}
