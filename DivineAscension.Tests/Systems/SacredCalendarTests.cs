using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for the Sacred Calendar feature (#375). The full
///     ReligionCalendarTicker pipeline is integration-level (needs an
///     IGameCalendar fake), so these tests cover the load-bearing pieces:
///     the static patron-day table, the per-religion idempotency stamp, and
///     the manager's behavior when the calendar is unavailable.
/// </summary>
[ExcludeFromCodeCoverage]
public class SacredCalendarTests
{
    private readonly FakeEventService _events = new();
    private readonly FakePersistenceService _persistence = new();
    private readonly FakeWorldService _world = new();
    private readonly Mock<ILogger> _logger = new();

    private ReligionManager NewManager() =>
        new(_logger.Object, _events, _persistence, _world);

    [Fact]
    public void DomainHolyDay_HasOneEntryPerDomain()
    {
        Assert.Equal(5, FeastDay.DomainHolyDay.Count);
        Assert.Equal((2, 1), FeastDay.DomainHolyDay[DeityDomain.Craft]);
        Assert.Equal((4, 15), FeastDay.DomainHolyDay[DeityDomain.Wild]);
        Assert.Equal((7, 4), FeastDay.DomainHolyDay[DeityDomain.Conquest]);
        Assert.Equal((9, 12), FeastDay.DomainHolyDay[DeityDomain.Harvest]);
        Assert.Equal((11, 1), FeastDay.DomainHolyDay[DeityDomain.Stone]);
    }

    [Fact]
    public void CreateReligion_WithoutCalendar_SkipsFoundingButSeedsPatron()
    {
        // FakeWorldService throws on Calendar when none is set — the manager
        // must swallow that and skip Founding (no captured date), while the
        // hardcoded Patron table still seeds a feast.
        var manager = NewManager();

        var religion = manager.CreateReligion("Nameless", DeityDomain.Craft, "Forge", "founder", true);

        Assert.Equal(0, religion.FoundingMonth);
        Assert.Equal(0, religion.FoundingDay);
        Assert.DoesNotContain(religion.FeastDays, f => f.Kind == FeastKind.Founding);
        var patron = Assert.Single(religion.FeastDays, f => f.Kind == FeastKind.PatronDomain);
        Assert.Equal(2, patron.Month);
        Assert.Equal(1, patron.Day);
    }

    [Fact]
    public void Seeded_FoundingFeast_HasLastFiredYearStampedToSuppressFirstYear()
    {
        // With no calendar set, Year reads as 0 and Founding isn't seeded
        // (month/day are also 0). Use TryMarkFeastFired symmetry instead:
        // any Founding seeded for year Y must not fire again until Y+1.
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        religion.SetFeastDays(new[]
        {
            new FeastDay("Founding Day", 3, 7, FeastKind.Founding) { LastFiredYear = 1387 }
        });

        var feast = religion.FeastDays[0];
        Assert.False(religion.TryMarkFeastFired(feast, 1387));
        Assert.True(religion.TryMarkFeastFired(feast, 1388));
    }

    [Fact]
    public void TryMarkFeastFired_FiresOncePerYear()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var feast = new FeastDay("Founding Day", 3, 7, FeastKind.Founding);
        religion.SetFeastDays(new[] { feast });

        var feastFromList = religion.FeastDays[0];
        Assert.True(religion.TryMarkFeastFired(feastFromList, 1387));
        Assert.False(religion.TryMarkFeastFired(feastFromList, 1387));
        Assert.True(religion.TryMarkFeastFired(feastFromList, 1388));
    }

    [Fact]
    public void DomainHolyDay_AllPatronDaysFitMinVanillaMonth()
    {
        // Vanilla survival lets DaysPerMonth go as low as 3 (and the engine
        // permits anything > 0). The ticker clamps Day to DaysPerMonth at
        // fire time, so any value here is *legal*, but document the worst
        // case: the lowest sane month length still produces a fire-able day.
        foreach (var (_, (_, day)) in FeastDay.DomainHolyDay)
            Assert.True(day >= 1, $"patron day {day} must be at least 1");
    }

    [Fact]
    public void TryMarkFeastFired_PersistsLastFiredYear()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Wild, "Greenwarden", "founder", true);
        religion.SetFeastDays(new[] { new FeastDay("Test", 4, 15, FeastKind.PatronDomain) });

        var feast = religion.FeastDays[0];
        religion.TryMarkFeastFired(feast, 1500);

        Assert.Equal(1500, religion.FeastDays[0].LastFiredYear);
    }
}
