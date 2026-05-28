using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.UI.Adapters.Notices;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;

namespace DivineAscension.Tests.GUI;

/// <summary>
///     Tests for the pure-read projection of feast days + chronicle into
///     <see cref="DivineAscension.GUI.Models.Letters.LetterEntry"/> rows for
///     the Letters chapter. Covers the advance-notice window, the past
///     cap, ordering, and the no-religion guard.
/// </summary>
[ExcludeFromCodeCoverage]
public class HolidayNoticeAdapterTests
{
    public HolidayNoticeAdapterTests()
    {
        // Adapter resolves localization keys. LocalizationService falls back
        // to the key string when not initialized — that's fine for assertions
        // that only check letter structure, not the exact resolved copy.
        _ = LocalizationService.Instance;
    }

    private static PlayerReligionInfoResponsePacket NewReligion(string name = "Order of the Forge",
        string domain = "Craft")
    {
        return new PlayerReligionInfoResponsePacket
        {
            HasReligion = true,
            ReligionName = name,
            Domain = domain,
            FeastDays = new List<PlayerReligionInfoResponsePacket.FeastDayDto>(),
            Chronicle = new List<PlayerReligionInfoResponsePacket.ChronicleEntryDto>()
        };
    }

    [Fact]
    public void Build_NoReligion_ReturnsEmpty()
    {
        var letters = HolidayNoticeAdapter.Build(new PlayerReligionInfoResponsePacket { HasReligion = false });
        Assert.Empty(letters);
    }

    [Fact]
    public void Build_OnlyFeastsWithinWindow_AreShown()
    {
        var religion = NewReligion();
        religion.FeastDays.Add(new PlayerReligionInfoResponsePacket.FeastDayDto
        {
            Name = "Within", Month = 1, Day = 1, Kind = (int)FeastKind.Founding, DaysUntil = 3
        });
        religion.FeastDays.Add(new PlayerReligionInfoResponsePacket.FeastDayDto
        {
            Name = "OutsideWindow", Month = 6, Day = 1, Kind = (int)FeastKind.PatronDomain,
            DaysUntil = HolidayNoticeAdapter.AdvanceNoticeDays + 5
        });

        var letters = HolidayNoticeAdapter.Build(religion);

        Assert.Single(letters);
        Assert.StartsWith("upcoming:", letters[0].Id);
    }

    [Fact]
    public void Build_PastChronicleEntries_AreCappedAndNewestFirst()
    {
        var religion = NewReligion();
        // Chronicle is oldest-first server-side; build 15 entries, expect 10 newest.
        for (var i = 0; i < 15; i++)
        {
            religion.Chronicle.Add(new PlayerReligionInfoResponsePacket.ChronicleEntryDto
            {
                InGameDay = i,
                Kind = (int)ChronicleKind.FeastDay,
                Line = $"entry-{i}"
            });
        }

        var letters = HolidayNoticeAdapter.Build(religion);

        Assert.Equal(HolidayNoticeAdapter.RecentPastLimit, letters.Count);
        // Newest (highest InGameDay) first; entry-14 should be at the top.
        Assert.Equal("past:14:16", letters.First().Id);
    }

    [Fact]
    public void Build_UpcomingBeforePast()
    {
        var religion = NewReligion();
        religion.FeastDays.Add(new PlayerReligionInfoResponsePacket.FeastDayDto
        {
            Name = "Coming", Month = 1, Day = 1, Kind = (int)FeastKind.Founding, DaysUntil = 1
        });
        religion.Chronicle.Add(new PlayerReligionInfoResponsePacket.ChronicleEntryDto
        {
            InGameDay = 5, Kind = (int)ChronicleKind.FeastDay, Line = "past entry"
        });

        var letters = HolidayNoticeAdapter.Build(religion);

        Assert.Equal(2, letters.Count);
        Assert.StartsWith("upcoming:", letters[0].Id);
        Assert.StartsWith("past:", letters[1].Id);
    }

    [Fact]
    public void Build_ChronicleEntriesOfOtherKinds_AreIgnored()
    {
        // Only FeastDay chronicle entries get projected; Founded / disbanded / etc. don't.
        var religion = NewReligion();
        religion.Chronicle.Add(new PlayerReligionInfoResponsePacket.ChronicleEntryDto
        {
            InGameDay = 5, Kind = (int)ChronicleKind.Founded, Line = "founded"
        });
        religion.Chronicle.Add(new PlayerReligionInfoResponsePacket.ChronicleEntryDto
        {
            InGameDay = 7, Kind = (int)ChronicleKind.FeastDay, Line = "a feast"
        });

        var letters = HolidayNoticeAdapter.Build(religion);

        Assert.Single(letters);
        Assert.Equal("past:7:16", letters[0].Id);
    }

    [Fact]
    public void Build_AllLettersAreReadOnly()
    {
        var religion = NewReligion();
        religion.FeastDays.Add(new PlayerReligionInfoResponsePacket.FeastDayDto
        {
            Name = "Soon", Month = 1, Day = 1, Kind = (int)FeastKind.Founding, DaysUntil = 0
        });
        religion.Chronicle.Add(new PlayerReligionInfoResponsePacket.ChronicleEntryDto
        {
            InGameDay = 1, Kind = (int)ChronicleKind.FeastDay, Line = "kept"
        });

        var letters = HolidayNoticeAdapter.Build(religion);

        Assert.All(letters, l => Assert.False(l.ShowActions));
    }
}
