using DivineAscension.Constants;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
/// Formats a chronicle entry's date for display. Entries carry the in-game
/// calendar date (year / month / day) captured at write time and render as
/// "the 3rd of January, Year 1387". Entries written before calendar capture
/// existed have year 0 and fall back to the "Day {N}" presentation.
/// </summary>
public static class ChronicleDateFormatter
{
    /// <param name="dayFallbackKey">
    ///     Area-specific "Day {0}" localization key (civ vs religion) used when the
    ///     entry predates calendar capture.
    /// </param>
    public static string Format(int year, int month, int dayOfMonth, int inGameDay, string dayFallbackKey)
    {
        var loc = LocalizationService.Instance;
        // Month is the capture sentinel: valid months are 1..12, so 0 means the entry
        // predates calendar capture. Year is NOT a sentinel — year 0 is a valid first
        // year in some world configs and must still render.
        if (month is < 1 or > 12 || dayOfMonth < 1)
            return loc.Get(dayFallbackKey, inGameDay);

        var monthName = loc.Get(LocalizationKeys.CalendarMonth(month));
        return loc.Get(LocalizationKeys.UI_CALENDAR_CHRONICLE_DATE, Ordinal(dayOfMonth), monthName, year);
    }

    /// <summary>English ordinal: 1 -> "1st", 2 -> "2nd", 3 -> "3rd", 4 -> "4th", 11 -> "11th".</summary>
    private static string Ordinal(int n)
    {
        var suffix = (n % 100) is >= 11 and <= 13
            ? "th"
            : (n % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        return $"{n}{suffix}";
    }
}
