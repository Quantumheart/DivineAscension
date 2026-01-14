namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     UI-only data source for civilization details. Intended for swapping
///     between a real provider and a dev-only fake provider without touching systems/persistence.
/// </summary>
internal interface ICivilizationDetailProvider
{
    CivilizationDetailVM? GetCivilizationDetail(string civId);
    void Refresh();
}