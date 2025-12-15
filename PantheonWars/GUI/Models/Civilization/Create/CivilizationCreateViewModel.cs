namespace PantheonWars.GUI.Models.Civilization.Create;

public readonly struct CivilizationCreateViewModel(
    string civilizationName,
    string selectedIcon,
    string? errorMessage,
    bool userIsReligionFounder,
    bool userInCivilization,
    float x,
    float y,
    float width,
    float height)
{
    public string CivilizationName { get; } = civilizationName;
    public string SelectedIcon { get; } = selectedIcon;
    public string? ErrorMessage { get; } = errorMessage;
    public bool UserIsReligionFounder { get; } = userIsReligionFounder;
    public bool UserInCivilization { get; } = userInCivilization;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods (no side effects)
    public bool CanCreate => UserIsReligionFounder
                             && !UserInCivilization
                             && !string.IsNullOrWhiteSpace(CivilizationName);
}