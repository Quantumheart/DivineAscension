using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;

namespace DivineAscension.Systems;

/// <summary>
///     Builds and appends chronicle entries for a religion, owning the in-game day
///     capture and the localized chronicle-voice wording (#373). Mirrors
///     <see cref="Civilizations.CivilizationChronicler" />: keeps calendar and
///     localization concerns out of the manager. The chronicle is the permanent,
///     narrative history of rare events — distinct from the FIFO activity log.
/// </summary>
internal sealed class ReligionChronicler
{
    private readonly IWorldService _worldService;

    public ReligionChronicler(IWorldService worldService)
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
        try
        {
            var calendar = _worldService.Calendar;
            if (calendar != null && calendar.HoursPerDay > 0f)
                inGameDay = (int)(calendar.TotalHours / calendar.HoursPerDay);
        }
        catch
        {
            inGameDay = 0;
        }

        return new ChronicleEntry(kind, line, relatedId, DateTime.UtcNow, inGameDay);
    }

    public void RecordFounded(ReligionData religion)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.Founded,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_FOUNDED,
                religion.ReligionName, religion.FounderName, religion.PatronName),
            null));
    }

    public void RecordFirstHolySite(ReligionData religion)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.FirstHolySite,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_FIRST_HOLY_SITE,
                religion.ReligionName),
            null));
    }

    public void RecordBlessingUnlocked(ReligionData religion, string blessingName, string? blessingId)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.BlessingUnlocked,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_BLESSING_UNLOCKED, blessingName),
            blessingId));
    }

    public void RecordFounderTransferred(ReligionData religion, string newFounderName)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.FounderTransferred,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_FOUNDER_TRANSFERRED, newFounderName),
            null));
    }

    public void RecordCivilizationJoined(ReligionData religion, string civName, string? civId)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.JoinedCivilization,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_JOINED_CIVILIZATION, civName),
            civId));
    }

    public void RecordCivilizationLeft(ReligionData religion, string civName, string? civId)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.LeftCivilization,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_LEFT_CIVILIZATION, civName),
            civId));
    }

    public void RecordDisbanded(ReligionData religion)
    {
        religion.AddChronicleEntry(BuildEntry(ChronicleKind.Disbanded,
            LocalizationService.Instance.Get(LocalizationKeys.CHRONICLE_RELIGION_DISBANDED, religion.ReligionName),
            null));
    }
}
