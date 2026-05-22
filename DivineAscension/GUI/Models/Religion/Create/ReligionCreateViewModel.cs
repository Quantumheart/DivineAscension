using System;

namespace DivineAscension.GUI.Models.Religion.Create;

/// <summary>
/// Immutable view model for religion creation form
/// Contains only the data needed to render the creation UI
/// </summary>
public readonly struct ReligionCreateViewModel(
    string religionName,
    string domain,
    string deityName,
    bool isPublic,
    string[] availableDomains,
    string? errorMessage,
    string? religionNameProfanityWord,
    string? deityNameProfanityWord,
    float x,
    float y,
    float width,
    float height,
    string motto = "",
    string? mottoProfanityWord = null)
{
    public string ReligionName { get; } = religionName;

    /// <summary>
    /// The domain (Craft, Wild, Harvest, Stone)
    /// </summary>
    public string Domain { get; } = domain;

    /// <summary>
    /// The custom name for the deity this religion worships
    /// </summary>
    public string DeityName { get; } = deityName;

    /// <summary>
    /// Optional motto/creed at creation (#361)
    /// </summary>
    public string Motto { get; } = motto;

    public bool IsPublic { get; } = isPublic;
    public string[] AvailableDomains { get; } = availableDomains;
    public string? ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Profanity word found in religion name, if any
    /// </summary>
    public string? ReligionNameProfanityWord { get; } = religionNameProfanityWord;

    /// <summary>
    /// Profanity word found in deity name, if any
    /// </summary>
    public string? DeityNameProfanityWord { get; } = deityNameProfanityWord;

    /// <summary>
    /// Profanity word found in motto, if any
    /// </summary>
    public string? MottoProfanityWord { get; } = mottoProfanityWord;

    // Layout
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    /// <summary>
    /// Indicates if the religion name contains profanity
    /// </summary>
    public bool ReligionNameHasProfanity => !string.IsNullOrEmpty(ReligionNameProfanityWord);

    /// <summary>
    /// Indicates if the deity name contains profanity
    /// </summary>
    public bool DeityNameHasProfanity => !string.IsNullOrEmpty(DeityNameProfanityWord);

    /// <summary>
    /// Indicates if the motto contains profanity
    /// </summary>
    public bool MottoHasProfanity => !string.IsNullOrEmpty(MottoProfanityWord);

    /// <summary>
    /// Indicates if any field contains profanity
    /// </summary>
    public bool HasProfanity => ReligionNameHasProfanity || DeityNameHasProfanity || MottoHasProfanity;

    /// <summary>
    /// Indicates the motto exceeds the max length
    /// </summary>
    public bool MottoTooLong => !string.IsNullOrEmpty(Motto) && Motto.Length > 80;

    /// <summary>
    /// Gets the index of the currently selected domain in the available domains array
    /// </summary>
    public int GetCurrentDomainIndex()
    {
        var index = Array.IndexOf(AvailableDomains, Domain);
        return index == -1 ? 0 : index; // Default to first domain if not found
    }

    /// <summary>
    /// Checks if the form is valid and can be submitted
    /// </summary>
    public bool CanCreate =>
        !string.IsNullOrWhiteSpace(ReligionName)
        && ReligionName.Length >= 3
        && ReligionName.Length <= 32
        && !string.IsNullOrWhiteSpace(DeityName)
        && DeityName.Length >= 2
        && DeityName.Length <= 48
        && !MottoTooLong
        && !HasProfanity;

    /// <summary>
    /// Gets the info text to display based on public/private selection
    /// </summary>
    public string InfoText => IsPublic
        ? "Public religions appear in the browser and anyone can join."
        : "Private religions require an invitation from the founder.";
}