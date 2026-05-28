using DivineAscension.API.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Sacred-calendar helpers shared by <see cref="ReligionManager" /> (founding
///     date capture) and <see cref="ReligionCalendarTicker" /> (daily fire check).
///     <c>IGameCalendar</c> exposes <c>Month</c> and <c>DayOfYear</c> in 0-based
///     form; the feast model is 1-based, so we derive both consistently here.
/// </summary>
internal static class SacredCalendar
{
    /// <summary>
    ///     Returns the current in-game (Month, Day) as 1-based values, or
    ///     (0, 0) when the calendar is unavailable (early init or tests).
    /// </summary>
    public static (int Month, int Day) GetCurrentMonthDay(IWorldService worldService)
    {
        try
        {
            var calendar = worldService?.Calendar;
            if (calendar == null || calendar.DaysPerMonth <= 0) return (0, 0);
            var month = calendar.DayOfYear / calendar.DaysPerMonth + 1;
            var day = calendar.DayOfYear % calendar.DaysPerMonth + 1;
            return (month, day);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    ///     Current in-game year, or 0 when the calendar is unavailable.
    /// </summary>
    public static int GetCurrentYear(IWorldService worldService)
    {
        try
        {
            return worldService?.Calendar?.Year ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
