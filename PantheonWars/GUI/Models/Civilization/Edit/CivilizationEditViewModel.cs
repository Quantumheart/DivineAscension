namespace PantheonWars.GUI.Models.Civilization.Edit;

public readonly struct CivilizationEditViewModel(
    string civilizationId,
    string civilizationName,
    string currentIcon,
    string editingIcon,
    float x,
    float y,
    float width,
    float height)
{
    public string CivilizationId { get; } = civilizationId;
    public string CivilizationName { get; } = civilizationName;
    public string CurrentIcon { get; } = currentIcon;
    public string EditingIcon { get; } = editingIcon;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
}