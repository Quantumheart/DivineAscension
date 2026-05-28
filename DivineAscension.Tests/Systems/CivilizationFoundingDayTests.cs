using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Tests for the civilization annual Founding Day holiday. The full
///     ticker pipeline is integration-level (needs an IGameCalendar fake),
///     so these cover the load-bearing data + manager pieces: founding
///     date capture, first-year suppression stamp, and the idempotency
///     accessor used by the ticker.
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationFoundingDayTests
{
    private readonly CivilizationManager _civ;
    private readonly Mock<IReligionManager> _religions = new();
    private readonly Mock<ILoggerWrapper> _logger = new();
    private readonly FakeEventService _events = new();
    private readonly FakePersistenceService _persistence = new();
    private readonly FakeWorldService _world = new();

    public CivilizationFoundingDayTests()
    {
        _civ = new CivilizationManager(_logger.Object, _events, _persistence, _world, _religions.Object);
    }

    private Civilization CreateCiv(string founderUid = "founder", string religionId = "religion")
    {
        var religion = TestFixtures.CreateTestReligion(religionId, "R", DeityDomain.Craft, "D", founderUid);
        _religions.Setup(r => r.GetReligion(religionId)).Returns(religion);
        return _civ.CreateCivilization("Test Realm", founderUid, religionId)!;
    }

    [Fact]
    public void CreateCivilization_WithoutCalendar_LeavesFoundingDateUncaptured()
    {
        // FakeWorldService throws on Calendar when unset — the membership
        // service must swallow that and leave FoundingMonth/Day at 0 so the
        // ticker simply skips this civ.
        var civ = CreateCiv();

        Assert.Equal(0, civ.FoundingMonth);
        Assert.Equal(0, civ.FoundingDay);
    }

    [Fact]
    public void TryMarkFoundingDayFired_FiresOncePerYear()
    {
        var civ = CreateCiv();
        // Simulate a captured date + clean stamp so the first call succeeds.
        civ.FoundingMonth = 3;
        civ.FoundingDay = 7;
        civ.FoundingDayLastFiredYear = 0;

        Assert.True(_civ.TryMarkFoundingDayFired(civ.CivId, 1387));
        Assert.False(_civ.TryMarkFoundingDayFired(civ.CivId, 1387));
        Assert.True(_civ.TryMarkFoundingDayFired(civ.CivId, 1388));
    }

    [Fact]
    public void TryMarkFoundingDayFired_RespectsFirstYearSuppressionStamp()
    {
        // A civ founded in year Y has FoundingDayLastFiredYear stamped to Y,
        // so any attempt to fire in Y must be rejected.
        var civ = CreateCiv();
        civ.FoundingDayLastFiredYear = 1387;

        Assert.False(_civ.TryMarkFoundingDayFired(civ.CivId, 1387));
        Assert.True(_civ.TryMarkFoundingDayFired(civ.CivId, 1388));
    }

    [Fact]
    public void TryMarkFoundingDayFired_UnknownCiv_ReturnsFalse()
    {
        Assert.False(_civ.TryMarkFoundingDayFired("nonexistent-civ", 1387));
    }
}
