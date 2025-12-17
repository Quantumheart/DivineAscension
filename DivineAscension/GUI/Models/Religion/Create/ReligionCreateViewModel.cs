using System;

namespace PantheonWars.GUI.Models.Religion.Create;

/// <summary>
/// Immutable view model for religion creation form
/// Contains only the data needed to render the creation UI
/// </summary>
public readonly struct ReligionCreateViewModel(
    string religionName,
    string deityName,
    bool isPublic,
    string[] availableDeities,
    string? errorMessage,
    float x,
    float y,
    float width,
    float height)
{
    public string ReligionName { get; } = religionName;
    public string DeityName { get; } = deityName;
    public bool IsPublic { get; } = isPublic;
    public string[] AvailableDeities { get; } = availableDeities;
    public string? ErrorMessage { get; } = errorMessage;

    // Layout
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    /// <summary>
    /// Gets the index of the currently selected deity in the available deities array
    /// </summary>
    public int GetCurrentDeityIndex()
    {
        var index = Array.IndexOf(AvailableDeities, DeityName);
        return index == -1 ? 0 : index; // Default to first deity if not found
    }

    /// <summary>
    /// Checks if the form is valid and can be submitted
    /// </summary>
    public bool CanCreate =>
        !string.IsNullOrWhiteSpace(ReligionName)
        && ReligionName.Length >= 3
        && ReligionName.Length <= 32;

    /// <summary>
    /// Gets the info text to display based on public/private selection
    /// </summary>
    public string InfoText => IsPublic
        ? "Public religions appear in the browser and anyone can join."
        : "Private religions require an invitation from the founder.";
}