using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Tests.Helpers;
using Moq;
using ProtoBuf;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Tests for the religion chronicle (#373): chronicle entries are written on
///     founding, founder transfer, the first holy site, blessing unlocks, civ
///     join/leave, and disband, and persist across a ProtoBuf round-trip.
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionChronicleTests
{
    private readonly ReligionManager _religionManager;

    public ReligionChronicleTests()
    {
        var mockLogger = new Mock<ILogger>();
        _religionManager = new ReligionManager(mockLogger.Object, new FakeEventService(),
            new FakePersistenceService(), new FakeWorldService());
    }

    [Fact]
    public void CreateReligion_WritesFoundedEntry()
    {
        var religion = _religionManager.CreateReligion("Order of the Forge", DeityDomain.Craft, "Smith",
            "founder-1", false);

        var entry = Assert.Single(religion.Chronicle);
        Assert.Equal(ChronicleKind.Founded, entry.Kind);
        Assert.False(string.IsNullOrEmpty(entry.Line));
    }

    [Fact]
    public void RecordFirstHolySite_WritesEntry()
    {
        var religion = _religionManager.CreateReligion("Order", DeityDomain.Wild, "Hunter", "founder-1", false);

        _religionManager.RecordFirstHolySite(religion.ReligionUID);

        var entry = Assert.Single(religion.Chronicle, e => e.Kind == ChronicleKind.FirstHolySite);
        Assert.False(string.IsNullOrEmpty(entry.Line));
    }

    [Fact]
    public void RecordBlessingUnlocked_WritesEntryWithBlessingId()
    {
        var religion = _religionManager.CreateReligion("Order", DeityDomain.Stone, "Mason", "founder-1", false);

        _religionManager.RecordBlessingUnlocked(religion.ReligionUID, "Stoneward", "blessing-7");

        var entry = Assert.Single(religion.Chronicle, e => e.Kind == ChronicleKind.BlessingUnlocked);
        Assert.Equal("blessing-7", entry.RelatedId);
    }

    [Fact]
    public void RecordCivilizationJoinedAndLeft_WriteEntries()
    {
        var religion = _religionManager.CreateReligion("Order", DeityDomain.Harvest, "Reaper", "founder-1", false);

        _religionManager.RecordCivilizationJoined(religion.ReligionUID, "Grand Realm", "civ-1");
        _religionManager.RecordCivilizationLeft(religion.ReligionUID, "Grand Realm", "civ-1");

        var joined = Assert.Single(religion.Chronicle, e => e.Kind == ChronicleKind.JoinedCivilization);
        var left = Assert.Single(religion.Chronicle, e => e.Kind == ChronicleKind.LeftCivilization);
        Assert.Equal("civ-1", joined.RelatedId);
        Assert.Equal("civ-1", left.RelatedId);
    }

    [Fact]
    public void RemoveFounder_WithRemainingMembers_WritesFounderTransferredEntry()
    {
        var religion = _religionManager.CreateReligion("Order", DeityDomain.Conquest, "Warlord", "founder-1", false);
        _religionManager.AddMember(religion.ReligionUID, "member-2");

        _religionManager.RemoveMember(religion.ReligionUID, "founder-1");

        var entry = Assert.Single(religion.Chronicle, e => e.Kind == ChronicleKind.FounderTransferred);
        Assert.False(string.IsNullOrEmpty(entry.Line));
    }

    [Fact]
    public void DeleteReligion_WritesDisbandedEntry()
    {
        var religion = _religionManager.CreateReligion("Order", DeityDomain.Craft, "Smith", "founder-1", false);

        var deleted = _religionManager.DeleteReligion(religion.ReligionUID, "founder-1");

        Assert.True(deleted);
        // The religion is removed from the manager, but the closing entry is written
        // onto the object before removal — assert via the retained reference.
        Assert.Single(religion.Chronicle, e => e.Kind == ChronicleKind.Disbanded);
    }

    [Fact]
    public void RecordChronicle_UnknownReligion_DoesNotThrow()
    {
        _religionManager.RecordFirstHolySite("missing");
        _religionManager.RecordBlessingUnlocked("missing", "Blessing", "b-1");
        _religionManager.RecordCivilizationJoined("missing", "Realm", "civ-1");
        _religionManager.RecordCivilizationLeft("missing", "Realm", "civ-1");
    }

    [Fact]
    public void Chronicle_RoundTripsThroughProtoBuf()
    {
        var religion = new ReligionData("rel-1", "Order", DeityDomain.Craft, "Smith", "founder-1", "Founder");
        religion.AddChronicleEntry(new ChronicleEntry(ChronicleKind.Founded, "Founded.", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 3));
        religion.AddChronicleEntry(new ChronicleEntry(ChronicleKind.FirstHolySite, "Consecrated.", "site-2",
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), 7));

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, religion);
        ms.Position = 0;
        var restored = Serializer.Deserialize<ReligionData>(ms);

        Assert.Equal(2, restored.Chronicle.Count);
        Assert.Equal(ChronicleKind.Founded, restored.Chronicle[0].Kind);
        Assert.Equal("Consecrated.", restored.Chronicle[1].Line);
        Assert.Equal("site-2", restored.Chronicle[1].RelatedId);
        Assert.Equal(7, restored.Chronicle[1].InGameDay);
    }

    [Fact]
    public void AddChronicleEntry_PreservesOldestFirstOrder()
    {
        var religion = new ReligionData("rel-1", "Order", DeityDomain.Craft, "Smith", "founder-1", "Founder");
        religion.AddChronicleEntry(new ChronicleEntry(ChronicleKind.Founded, "First.", null, DateTime.UtcNow, 1));
        religion.AddChronicleEntry(new ChronicleEntry(ChronicleKind.FirstHolySite, "Second.", null,
            DateTime.UtcNow, 2));

        Assert.Equal("First.", religion.Chronicle[0].Line);
        Assert.Equal("Second.", religion.Chronicle[1].Line);
    }
}
