namespace DivineAscension.GUI.UI.Adapters.Religions;

/// <summary>
///     UI-only data source for religion detail view. Intended for swapping
///     between a real provider and a dev-only fake provider without touching systems/persistence.
/// </summary>
internal interface IReligionDetailProvider
{
    ReligionDetailVM? GetReligionDetail(string religionUID);
    void Refresh();
}