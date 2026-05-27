using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using ProtoBuf;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Tests for the civilization chronicle (#369): chronicle entries are written
///     on founding, membership changes, and explicit records, and persist across
///     a ProtoBuf round-trip.
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationChronicleTests
{
    private readonly CivilizationManager _civilizationManager;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public CivilizationChronicleTests()
    {
        _mockReligionManager = new Mock<IReligionManager>();
        _civilizationManager = new CivilizationManager(
            new Mock<ILoggerWrapper>().Object,
            new FakeEventService(),
            new FakePersistenceService(),
            new FakeWorldService(),
            _mockReligionManager.Object);
    }

    [Fact]
    public void CreateCivilization_WritesFoundedEntry()
    {
        var founderUID = "founder-1";
        var religionId = "religion-1";
        var religion = TestFixtures.CreateTestReligion(religionId, "Founder Religion", DeityDomain.Craft, "Deity",
            founderUID);
        _mockReligionManager.Setup(r => r.GetReligion(religionId)).Returns(religion);

        var civ = _civilizationManager.CreateCivilization("Grand Realm", founderUID, religionId);

        Assert.NotNull(civ);
        var entry = Assert.Single(civ.Chronicle);
        Assert.Equal(ChronicleKind.Founded, entry.Kind);
        Assert.False(string.IsNullOrEmpty(entry.Line));
    }

    [Fact]
    public void AcceptInvite_WritesReligionJoinedEntry()
    {
        var founderUID = "founder-1";
        var founderReligionId = "religion-1";
        var targetUID = "target-2";
        var targetReligionId = "religion-2";

        var founderReligion = TestFixtures.CreateTestReligion(founderReligionId, "Founder Religion",
            DeityDomain.Craft, "Deity", founderUID);
        var targetReligion = TestFixtures.CreateTestReligion(targetReligionId, "Target Religion",
            DeityDomain.Wild, "Deity", targetUID);
        _mockReligionManager.Setup(r => r.GetReligion(founderReligionId)).Returns(founderReligion);
        _mockReligionManager.Setup(r => r.GetReligion(targetReligionId)).Returns(targetReligion);

        var civ = _civilizationManager.CreateCivilization("Grand Realm", founderUID, founderReligionId);
        Assert.NotNull(civ);
        _civilizationManager.InviteReligion(civ.CivId, targetReligionId, founderUID);
        var invite = _civilizationManager.GetInvitesForReligion(targetReligionId).First();

        var result = _civilizationManager.AcceptInvite(invite.InviteId, targetUID);

        Assert.True(result);
        var joined = _civilizationManager.GetCivilization(civ.CivId)!.Chronicle
            .Where(e => e.Kind == ChronicleKind.ReligionJoined)
            .ToList();
        var entry = Assert.Single(joined);
        Assert.Equal(targetReligionId, entry.RelatedId);
    }

    [Fact]
    public void LeaveReligion_WritesReligionLeftEntry()
    {
        var founderUID = "founder-1";
        var founderReligionId = "religion-1";
        var targetUID = "target-2";
        var targetReligionId = "religion-2";

        var founderReligion = TestFixtures.CreateTestReligion(founderReligionId, "Founder Religion",
            DeityDomain.Craft, "Deity", founderUID);
        var targetReligion = TestFixtures.CreateTestReligion(targetReligionId, "Target Religion",
            DeityDomain.Wild, "Deity", targetUID);
        _mockReligionManager.Setup(r => r.GetReligion(founderReligionId)).Returns(founderReligion);
        _mockReligionManager.Setup(r => r.GetReligion(targetReligionId)).Returns(targetReligion);

        var civ = _civilizationManager.CreateCivilization("Grand Realm", founderUID, founderReligionId);
        Assert.NotNull(civ);
        _civilizationManager.InviteReligion(civ.CivId, targetReligionId, founderUID);
        var invite = _civilizationManager.GetInvitesForReligion(targetReligionId).First();
        _civilizationManager.AcceptInvite(invite.InviteId, targetUID);

        var result = _civilizationManager.LeaveReligion(targetReligionId, targetUID);

        Assert.True(result);
        var left = _civilizationManager.GetCivilization(civ.CivId)!.Chronicle
            .Where(e => e.Kind == ChronicleKind.ReligionLeft)
            .ToList();
        var entry = Assert.Single(left);
        Assert.Equal(targetReligionId, entry.RelatedId);
    }

    [Fact]
    public void RecordChronicleEntry_AppendsEntry()
    {
        var founderUID = "founder-1";
        var religionId = "religion-1";
        var religion = TestFixtures.CreateTestReligion(religionId, "Founder Religion", DeityDomain.Craft, "Deity",
            founderUID);
        _mockReligionManager.Setup(r => r.GetReligion(religionId)).Returns(religion);
        var civ = _civilizationManager.CreateCivilization("Grand Realm", founderUID, religionId);
        Assert.NotNull(civ);

        _civilizationManager.RecordChronicleEntry(civ.CivId, ChronicleKind.WarDeclared, "War upon the north.",
            "other-civ");

        var war = Assert.Single(civ.Chronicle.Where(e => e.Kind == ChronicleKind.WarDeclared));
        Assert.Equal("War upon the north.", war.Line);
        Assert.Equal("other-civ", war.RelatedId);
    }

    [Fact]
    public void RecordChronicleEntry_UnknownCiv_DoesNotThrow()
    {
        _civilizationManager.RecordChronicleEntry("missing", ChronicleKind.PeaceSigned, "Peace.", null);
    }

    [Fact]
    public void Chronicle_RoundTripsThroughProtoBuf()
    {
        var civ = new Civilization("civ-1", "Grand Realm", "founder-1", "religion-1");
        civ.AddChronicleEntry(new ChronicleEntry(ChronicleKind.Founded, "Founded.", null,
            new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc), 3));
        civ.AddChronicleEntry(new ChronicleEntry(ChronicleKind.WarDeclared, "War.", "civ-2",
            new System.DateTime(2026, 1, 2, 0, 0, 0, System.DateTimeKind.Utc), 7));

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, civ);
        ms.Position = 0;
        var restored = Serializer.Deserialize<Civilization>(ms);

        Assert.Equal(2, restored.Chronicle.Count);
        Assert.Equal(ChronicleKind.Founded, restored.Chronicle[0].Kind);
        Assert.Equal("War.", restored.Chronicle[1].Line);
        Assert.Equal("civ-2", restored.Chronicle[1].RelatedId);
        Assert.Equal(7, restored.Chronicle[1].InGameDay);
    }

    [Fact]
    public void AddChronicleEntry_PreservesOldestFirstOrder()
    {
        var civ = new Civilization("civ-1", "Grand Realm", "founder-1", "religion-1");
        civ.AddChronicleEntry(new ChronicleEntry(ChronicleKind.Founded, "First.", null, System.DateTime.UtcNow, 1));
        civ.AddChronicleEntry(new ChronicleEntry(ChronicleKind.MilestoneAwarded, "Second.", null,
            System.DateTime.UtcNow, 2));

        Assert.Equal("First.", civ.Chronicle[0].Line);
        Assert.Equal("Second.", civ.Chronicle[1].Line);
    }
}
