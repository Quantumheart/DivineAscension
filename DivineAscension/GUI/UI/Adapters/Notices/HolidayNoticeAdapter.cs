using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Letters;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Adapters.Notices;

/// <summary>
///     Projects feast-day data from <see cref="PlayerReligionInfoResponsePacket"/>
///     into <see cref="LetterEntry"/> records for the Letters chapter. Pure
///     read model — no new state, no new packets.
///
///     Upcoming (DaysUntil ≤ 7) feast days surface as "letters from the
///     scribes" with timing copy; recent <c>ChronicleKind.FeastDay</c>
///     entries surface as past notices. Order: upcoming first (soonest
///     first), then recent past (newest first). Read-only — no Accept /
///     Refuse buttons.
/// </summary>
internal static class HolidayNoticeAdapter
{
    /// <summary>
    ///     Show upcoming feasts whose <c>DaysUntil</c> is within this window.
    ///     7 in-game days lines up with the chat broadcast that fires on the
    ///     day itself — letters give a week of advance notice.
    /// </summary>
    public const int AdvanceNoticeDays = 7;

    /// <summary>Cap on past notices to keep the page bounded.</summary>
    public const int RecentPastLimit = 10;

    public static IReadOnlyList<LetterEntry> Build(PlayerReligionInfoResponsePacket religion)
    {
        if (religion == null || !religion.HasReligion) return Array.Empty<LetterEntry>();

        var letters = new List<LetterEntry>();
        var loc = LocalizationService.Instance;
        var sender = loc.Get(LocalizationKeys.UI_NOTICES_SENDER_RELIGION, religion.ReligionName);
        var glyphPainter = BuildGlyphPainter(religion.Domain);

        // Upcoming — server pre-sorts FeastDays by DaysUntil ascending, so
        // a take-while on the window threshold gives us the soonest first.
        foreach (var feast in religion.FeastDays)
        {
            if (feast.DaysUntil < 0 || feast.DaysUntil > AdvanceNoticeDays) continue;
            letters.Add(new LetterEntry(
                Id: $"upcoming:{feast.FeastId}",
                SenderText: sender,
                GlyphPainter: glyphPainter,
                QuoteLine: FormatUpcomingQuote(feast),
                ShowActions: false));
        }

        // Past — chronicle is oldest-first; reverse for newest-first.
        var pastQuoteKey = LocalizationKeys.UI_NOTICES_QUOTE_PAST;
        foreach (var entry in religion.Chronicle
                     .Where(e => e.Kind == (int)ChronicleKind.FeastDay)
                     .Reverse()
                     .Take(RecentPastLimit))
        {
            letters.Add(new LetterEntry(
                // Chronicle entries don't carry an id — InGameDay+Kind is unique
                // enough for stable LetterEntry identity within this list.
                Id: $"past:{entry.InGameDay}:{entry.Kind}",
                SenderText: sender,
                GlyphPainter: glyphPainter,
                QuoteLine: loc.Get(pastQuoteKey, entry.Line),
                ShowActions: false));
        }

        return letters;
    }

    private static string FormatUpcomingQuote(PlayerReligionInfoResponsePacket.FeastDayDto feast)
    {
        var loc = LocalizationService.Instance;
        return feast.DaysUntil switch
        {
            0 => loc.Get(LocalizationKeys.UI_NOTICES_QUOTE_UPCOMING_TODAY, feast.Name),
            1 => loc.Get(LocalizationKeys.UI_NOTICES_QUOTE_UPCOMING_TOMORROW, feast.Name),
            _ => loc.Get(LocalizationKeys.UI_NOTICES_QUOTE_UPCOMING_DAYS,
                feast.Name, feast.DaysUntil, FormatDate(feast.Month, feast.Day))
        };
    }

    private static string FormatDate(int month, int day)
    {
        if (month is < 1 or > 12 || day < 1) return string.Empty;
        var monthName = LocalizationService.Instance.Get(LocalizationKeys.CalendarMonth(month));
        return $"{Ordinal(day)} of {monthName}";
    }

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

    private static Action<ImGuiNET.ImDrawListPtr, System.Numerics.Vector2, System.Numerics.Vector2>
        BuildGlyphPainter(string domainString)
    {
        // Same domain glyph the chapter strips use. ParseDeityType is forgiving
        // and returns None for unknown strings, in which case the glyph
        // renderer paints a neutral mark.
        var domain = DomainHelper.ParseDeityType(domainString);
        return (drawList, min, max) => DomainGlyphRenderer.Draw(drawList, domain, min, max);
    }
}
