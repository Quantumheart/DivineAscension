using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.SacredCalendar;

/// <summary>
///     Immutable view model for the Sacred Calendar chapter (#375 + #422).
///     Read fields come from <see cref="PlayerReligionInfoResponsePacket"/>;
///     editor fields come from <c>InfoState.SacredCalendar</c>.
/// </summary>
public readonly struct SacredCalendarViewModel(
    bool isLoading,
    bool hasReligion,
    bool isFounder,
    string religionUID,
    string religionName,
    string deity,
    IReadOnlyList<PlayerReligionInfoResponsePacket.FeastDayDto> feasts,
    int daysPerMonth,
    int monthsPerYear,
    int currentMonth,
    int currentDay,
    int customCount,
    int unlockedSlots,
    bool addDialogOpen,
    string addName,
    int addMonth,
    int addDay,
    Guid? removeConfirmFeastId,
    string? removeConfirmFeastName,
    string? lastErrorMessage,
    float x,
    float y,
    float width,
    float height,
    float scrollY)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public bool IsFounder { get; } = isFounder;
    public string ReligionUID { get; } = religionUID;
    public string ReligionName { get; } = religionName;
    public string Deity { get; } = deity;
    public IReadOnlyList<PlayerReligionInfoResponsePacket.FeastDayDto> Feasts { get; } = feasts;
    public bool HasFeasts => Feasts is { Count: > 0 };
    public int DaysPerMonth { get; } = daysPerMonth;
    public int MonthsPerYear { get; } = monthsPerYear;
    public int CurrentMonth { get; } = currentMonth;
    public int CurrentDay { get; } = currentDay;
    public int CustomCount { get; } = customCount;
    public int UnlockedSlots { get; } = unlockedSlots;

    /// <summary>True when an empty custom slot is available *and* unlocked.</summary>
    public bool CanAdd => IsFounder && CustomCount < UnlockedSlots;

    /// <summary>True when the founder is at the cap.</summary>
    public bool AtCap => CustomCount >= 2;

    public bool AddDialogOpen { get; } = addDialogOpen;
    public string AddName { get; } = addName;
    public int AddMonth { get; } = addMonth;
    public int AddDay { get; } = addDay;
    public Guid? RemoveConfirmFeastId { get; } = removeConfirmFeastId;
    public string? RemoveConfirmFeastName { get; } = removeConfirmFeastName;
    public string? LastErrorMessage { get; } = lastErrorMessage;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;
}
