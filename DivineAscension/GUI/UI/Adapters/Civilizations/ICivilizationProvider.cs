using System.Collections.Generic;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     UI-only data source for listing civilizations. Intended for swapping
///     between a real provider and a dev-only fake provider without touching systems/persistence.
/// </summary>
internal interface ICivilizationProvider
{
    IReadOnlyList<CivilizationVM> GetCivilizations();
    void ConfigureDevSeed(int count, int seed);
    void Refresh();
}