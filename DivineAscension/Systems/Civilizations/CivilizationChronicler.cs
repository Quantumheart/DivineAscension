using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     Builds and appends chronicle entries for a civilization, owning the in-game
///     day capture and the localized chronicle-voice wording. Keeps calendar and
///     localization concerns out of the manager and membership logic.
/// </summary>
internal sealed class CivilizationChronicler
{
    private readonly IWorldService _worldService;

    public CivilizationChronicler(IWorldService worldService)
    {
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    /// <summary>
    ///     Builds a chronicle entry, capturing the current in-game day. The calendar
    ///     may be unavailable during early init (or in tests); day 0 is used then.
    /// </summary>
    public ChronicleEntry BuildEntry(ChronicleKind kind, string line, string? relatedId)
    {
        var inGameDay = 0;
        var year = 0;
        var month = 0;
        var dayOfMonth = 0;
        try
        {
            var calendar = _worldService.Calendar;
            if (calendar != null && calendar.HoursPerDay > 0f)
            {
                inGameDay = (int)(calendar.TotalHours / calendar.HoursPerDay);
                year = calendar.Year;
                month = calendar.Month;
                if (calendar.DaysPerMonth > 0)
                    dayOfMonth = calendar.DayOfYear % calendar.DaysPerMonth + 1;
            }
        }
        catch
        {
            inGameDay = 0;
        }

        return new ChronicleEntry(kind, line, relatedId, DateTime.UtcNow, inGameDay, year, month, dayOfMonth);
    }

    /// <summary>
    ///     Appends a pre-built chronicle line. Used by collaborating systems that
    ///     supply their own wording (milestones, diplomacy).
    /// </summary>
    public void Record(Civilization civ, ChronicleKind kind, string line, string? relatedId)
    {
        civ.AddChronicleEntry(BuildEntry(kind, line, relatedId));
    }

    public void RecordFounded(Civilization civ, string founderName)
    {
        var line = string.IsNullOrEmpty(civ.FounderEpithet)
            ? LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_FOUNDED, civ.Name, founderName)
            : LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_FOUNDED_EPITHET, civ.Name,
                founderName, civ.FounderEpithet);
        civ.AddChronicleEntry(BuildEntry(ChronicleKind.Founded, line, null));
    }

    public void RecordReligionJoined(Civilization civ, string religionName, string religionId)
    {
        civ.AddChronicleEntry(BuildEntry(ChronicleKind.ReligionJoined,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_JOINED, religionName),
            religionId));
    }

    public void RecordReligionLeft(Civilization civ, string religionName, string religionId)
    {
        civ.AddChronicleEntry(BuildEntry(ChronicleKind.ReligionLeft,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_LEFT, religionName),
            religionId));
    }

    /// <summary>
    ///     Records the annual Founding Day "kept by the realm" entry. Fired
    ///     by <see cref="CivilizationCalendarTicker"/> on the day each year.
    /// </summary>
    public void RecordFoundingDay(Civilization civ)
    {
        civ.AddChronicleEntry(BuildEntry(ChronicleKind.FeastDay,
            LocalizationService.Instance.Get(LocalizationKeys.CIV_FOUNDING_DAY_CHRONICLE, civ.Name),
            null));
    }

    public void RecordDisbanded(Civilization civ)
    {
        civ.AddChronicleEntry(BuildEntry(ChronicleKind.Disbanded,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_DISBANDED, civ.Name), null));
    }
}
