namespace DivineAscension.GUI.Models.Civilization.Create;

public readonly struct CivilizationCreateViewModel(
    string civilizationName,
    string selectedIcon,
    string? errorMessage,
    bool userIsReligionFounder,
    bool userInCivilization,
    string? profanityMatchedWord,
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
    public string? ProfanityMatchedWord { get; } = profanityMatchedWord;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    /// <summary>
    /// Indicates if the civilization name contains profanity
    /// </summary>
    public bool HasProfanity => !string.IsNullOrEmpty(ProfanityMatchedWord);

    /// <summary>
    /// Checks if the form is valid and can be submitted
    /// </summary>
    public bool CanCreate => UserIsReligionFounder
                             && !UserInCivilization
                             && !string.IsNullOrWhiteSpace(CivilizationName)
                             && CivilizationName.Length >= 3
                             && CivilizationName.Length <= 32
                             && !HasProfanity;
}