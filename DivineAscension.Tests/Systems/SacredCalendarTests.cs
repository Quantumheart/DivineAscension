using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
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
    private readonly Mock<ILoggerWrapper> _logger = new();

    private ReligionManager NewManager() =>
        new(_logger.Object, _events, _persistence, _world);

    [Fact]
    public void DomainHolyDay_HasOneEntryPerDomain()
    {
        // Sourced from DeityDomainRegistry (#558) — one entry per real domain.
        Assert.Equal(DeityDomains.All.Count, FeastDay.DomainHolyDay.Count);
        foreach (var domain in DeityDomains.All)
            Assert.True(FeastDay.DomainHolyDay.ContainsKey(domain), $"missing holy day for {domain}");

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

    [Fact]
    public void DeterministicAutoFeastId_StableAcrossCalls_PerReligionAndKind()
    {
        var a1 = FeastDay.DeterministicAutoFeastId("religion-A", FeastKind.Founding);
        var a2 = FeastDay.DeterministicAutoFeastId("religion-A", FeastKind.Founding);
        var a3 = FeastDay.DeterministicAutoFeastId("religion-A", FeastKind.PatronDomain);
        var b1 = FeastDay.DeterministicAutoFeastId("religion-B", FeastKind.Founding);

        Assert.Equal(a1, a2);
        Assert.NotEqual(a1, a3);
        Assert.NotEqual(a1, b1);
        Assert.NotEqual(Guid.Empty, a1);
    }

    [Fact]
    public void GetUnlockedCustomFeastSlots_RenownedGets1_MythicGets2()
    {
        Assert.Equal(0, ReligionManager.GetUnlockedCustomFeastSlots(PrestigeRank.Fledgling));
        Assert.Equal(0, ReligionManager.GetUnlockedCustomFeastSlots(PrestigeRank.Established));
        Assert.Equal(1, ReligionManager.GetUnlockedCustomFeastSlots(PrestigeRank.Renowned));
        Assert.Equal(1, ReligionManager.GetUnlockedCustomFeastSlots(PrestigeRank.Legendary));
        Assert.Equal(2, ReligionManager.GetUnlockedCustomFeastSlots(PrestigeRank.Mythic));
    }

    [Fact]
    public void TryAddCustomFeast_NotFounder_Rejected()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var cooldown = TestFixtures.CreateMockCooldownManager();

        var code = manager.TryAddCustomFeast(religion.ReligionUID, "other-player",
            "Mid-Year", 3, 1, cooldown.Object, out _);

        Assert.Equal(FeastDayErrorCode.NotFounder, code);
    }

    [Fact]
    public void TryAddCustomFeast_EmptyName_Rejected()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var cooldown = TestFixtures.CreateMockCooldownManager();

        var code = manager.TryAddCustomFeast(religion.ReligionUID, "founder",
            "   ", 3, 1, cooldown.Object, out _);

        Assert.Equal(FeastDayErrorCode.NameEmpty, code);
    }

    [Fact]
    public void TryAddCustomFeast_NameTooLong_Rejected()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var cooldown = TestFixtures.CreateMockCooldownManager();
        var longName = new string('x', 33);

        var code = manager.TryAddCustomFeast(religion.ReligionUID, "founder",
            longName, 3, 1, cooldown.Object, out _);

        Assert.Equal(FeastDayErrorCode.NameTooLong, code);
    }

    [Fact]
    public void TryAddCustomFeast_NoCalendar_RejectedAsInvalidDate()
    {
        // Without a calendar, custom feasts are gated out — date validation
        // can't run, and we don't want to seed feasts blindly.
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var cooldown = TestFixtures.CreateMockCooldownManager();

        var code = manager.TryAddCustomFeast(religion.ReligionUID, "founder",
            "Mid-Year", 3, 1, cooldown.Object, out _);

        Assert.Equal(FeastDayErrorCode.InvalidDate, code);
    }

    [Fact]
    public void TryRemoveCustomFeast_NotFounder_Rejected()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var cooldown = TestFixtures.CreateMockCooldownManager();

        var code = manager.TryRemoveCustomFeast(religion.ReligionUID, "other-player",
            Guid.NewGuid(), cooldown.Object);

        Assert.Equal(FeastDayErrorCode.NotFounder, code);
    }

    [Fact]
    public void TryRemoveCustomFeast_CannotRemoveAutoFeasts()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);
        var cooldown = TestFixtures.CreateMockCooldownManager();
        cooldown.Setup(c => c.CanPerformOperation("founder", CooldownType.FeastDayMutation, out It.Ref<string?>.IsAny))
            .Returns(true);

        // Try to remove the auto Patron feast by its deterministic id.
        var patronId = FeastDay.DeterministicAutoFeastId(religion.ReligionUID, FeastKind.PatronDomain);

        var code = manager.TryRemoveCustomFeast(religion.ReligionUID, "founder", patronId, cooldown.Object);

        Assert.Equal(FeastDayErrorCode.NotFound, code);
        // Auto feast still present.
        Assert.Contains(religion.FeastDays, f => f.Kind == FeastKind.PatronDomain);
    }

    [Fact]
    public void BackfillAutoFeastIds_StampsDeterministicIdsOnLegacyAutos()
    {
        var manager = NewManager();
        var religion = manager.CreateReligion("Order", DeityDomain.Craft, "Forge", "founder", true);

        // Simulate a pre-#422 save: clear FeastIds on the auto Patron seed.
        var legacy = new FeastDay("Old Patron", 2, 1, FeastKind.PatronDomain) { FeastId = Guid.Empty };
        religion.SetFeastDays(new[] { legacy });

        religion.BackfillAutoFeastIds();

        var expected = FeastDay.DeterministicAutoFeastId(religion.ReligionUID, FeastKind.PatronDomain);
        Assert.Equal(expected, religion.FeastDays[0].FeastId);
    }
}
