using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.SacredCalendar;

/// <summary>
///     Immutable view model for the Sacred Calendar chapter (#375). The feast
///     list and calendar context come from
///     <see cref="PlayerReligionInfoResponsePacket" />; the chapter is read-only.
/// </summary>
public readonly struct SacredCalendarViewModel(
    bool isLoading,
    bool hasReligion,
    string religionName,
    string deity,
    IReadOnlyList<PlayerReligionInfoResponsePacket.FeastDayDto> feasts,
    int daysPerMonth,
    int monthsPerYear,
    int currentMonth,
    int currentDay,
    float x,
    float y,
    float width,
    float height,
    float scrollY)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public string ReligionName { get; } = religionName;
    public string Deity { get; } = deity;
    public IReadOnlyList<PlayerReligionInfoResponsePacket.FeastDayDto> Feasts { get; } = feasts;
    public bool HasFeasts => Feasts is { Count: > 0 };
    public int DaysPerMonth { get; } = daysPerMonth;
    public int MonthsPerYear { get; } = monthsPerYear;
    public int CurrentMonth { get; } = currentMonth;
    public int CurrentDay { get; } = currentDay;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;
}
