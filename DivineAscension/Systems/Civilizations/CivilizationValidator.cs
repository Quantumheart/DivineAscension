using System;
using System.Linq;
using DivineAscension.Data;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     Pure, lock-free validation rules for civilizations. No side effects and no
///     logging — callers decide what to log on failure, preserving their messages.
/// </summary>
internal static class CivilizationValidator
{
    public const int MinReligions = 1;
    public const int MaxReligions = 4;
    public const int MaxDescriptionLength = 200;
    public const int MinCapitalNameLength = 1;
    public const int MaxCapitalNameLength = 64;

    public static bool IsNameNonEmpty(string name) => !string.IsNullOrWhiteSpace(name);

    public static bool IsNameLengthValid(string name) => name.Length >= 3 && name.Length <= 32;

    public static bool IsDescriptionLengthValid(string description) =>
        description.Length <= MaxDescriptionLength;

    public static bool IsNameTaken(CivilizationWorldData data, string name) =>
        data.Civilizations.Values.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Trims the candidate capital name and reports whether it falls within the
    ///     allowed 1-64 character range.
    /// </summary>
    public static bool TryNormalizeCapitalName(string? capitalName, out string trimmed)
    {
        trimmed = (capitalName ?? string.Empty).Trim();
        return trimmed.Length >= MinCapitalNameLength && trimmed.Length <= MaxCapitalNameLength;
    }
}
