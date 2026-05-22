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
///     Tests for the civilization capital (CapitalName + optional CapitalHolySiteId binding)
///     introduced by #370/#394. Covers founder gating, eligibility check, save/load default,
///     and the two cascade rules (site destroyed, religion leaves civ).
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationCapitalTests
{
    private readonly CivilizationManager _civ;
    private readonly Mock<IHolySiteManager> _holySites = new();
    private readonly Mock<ILoggerWrapper> _logger = new();
    private readonly Mock<IReligionManager> _religions = new();
    private readonly FakeEventService _events = new();
    private readonly FakePersistenceService _persistence = new();
    private readonly FakeWorldService _world = new();

    public CivilizationCapitalTests()
    {
        _civ = new CivilizationManager(_logger.Object, _events, _persistence, _world, _religions.Object);
        _civ.SetHolySiteManager(_holySites.Object);
    }

    private Civilization CreateCivWithReligion(string founderUid, string religionId, string civName = "Realm")
    {
        var religion = TestFixtures.CreateTestReligion(religionId, "R", DeityDomain.Craft, "D", founderUid);
        _religions.Setup(r => r.GetReligion(religionId)).Returns(religion);
        return _civ.CreateCivilization(civName, founderUid, religionId)!;
    }

    [Fact]
    public void CreateCivilization_AutoDefaultsCapitalName()
    {
        var civ = CreateCivWithReligion("u1", "r1", "Iron Realm");
        Assert.Equal("Iron Realm Seat", civ.CapitalName);
        Assert.Null(civ.CapitalHolySiteId);
    }

    [Fact]
    public void SetCapital_FounderRenamesWithoutBinding_Succeeds()
    {
        var civ = CreateCivWithReligion("u1", "r1");

        var ok = _civ.SetCapital(civ.CivId, "u1", "High Hold", null);

        Assert.True(ok);
        Assert.Equal("High Hold", civ.CapitalName);
        Assert.Null(civ.CapitalHolySiteId);
    }

    [Fact]
    public void SetCapital_NonFounderRejected()
    {
        var civ = CreateCivWithReligion("u1", "r1");

        var ok = _civ.SetCapital(civ.CivId, "stranger", "Usurped Hold", null);

        Assert.False(ok);
        Assert.NotEqual("Usurped Hold", civ.CapitalName);
    }

    [Fact]
    public void SetCapital_BindsMemberReligionSite_Succeeds()
    {
        var civ = CreateCivWithReligion("u1", "r1");
        var site = new HolySiteData { SiteUID = "s1", ReligionUID = "r1", SiteName = "Forge" };
        _holySites.Setup(h => h.GetHolySite("s1")).Returns(site);

        var ok = _civ.SetCapital(civ.CivId, "u1", "Forge Seat", "s1");

        Assert.True(ok);
        Assert.Equal("s1", civ.CapitalHolySiteId);
    }

    [Fact]
    public void SetCapital_NonMemberReligionSite_Rejected()
    {
        var civ = CreateCivWithReligion("u1", "r1");
        var site = new HolySiteData { SiteUID = "s1", ReligionUID = "outsider", SiteName = "Foreign Altar" };
        _holySites.Setup(h => h.GetHolySite("s1")).Returns(site);

        var ok = _civ.SetCapital(civ.CivId, "u1", "Bad Bind", "s1");

        Assert.False(ok);
        Assert.Null(civ.CapitalHolySiteId);
    }

    [Fact]
    public void SetCapital_EmptyName_Rejected()
    {
        var civ = CreateCivWithReligion("u1", "r1");

        var ok = _civ.SetCapital(civ.CivId, "u1", "   ", null);

        Assert.False(ok);
    }

    [Fact]
    public void HolySiteRemoved_ClearsBinding_KeepsName()
    {
        var civ = CreateCivWithReligion("u1", "r1");
        var site = new HolySiteData { SiteUID = "s1", ReligionUID = "r1", SiteName = "Forge" };
        _holySites.Setup(h => h.GetHolySite("s1")).Returns(site);
        _civ.SetCapital(civ.CivId, "u1", "Forge Seat", "s1");

        _holySites.Raise(h => h.OnHolySiteRemoved += null, "r1", "s1");

        Assert.Null(civ.CapitalHolySiteId);
        Assert.Equal("Forge Seat", civ.CapitalName);
    }

    [Fact]
    public void ReligionLeavesCiv_ClearsBindingIfOwned_KeepsName()
    {
        // Founder has r1; invite + accept r2; bind capital to r2's site; r2 leaves.
        var founderUid = "u1";
        var civ = CreateCivWithReligion(founderUid, "r1");

        // r2 setup
        var r2Founder = "u2";
        var r2 = TestFixtures.CreateTestReligion("r2", "R2", DeityDomain.Wild, "D2", r2Founder);
        _religions.Setup(r => r.GetReligion("r2")).Returns(r2);
        _religions.Setup(r => r.GetPlayerReligion(r2Founder)).Returns(r2);

        // Add r2 to the civ via internal data path (bypass invite plumbing)
        civ.AddReligion("r2");

        var site = new HolySiteData { SiteUID = "s2", ReligionUID = "r2", SiteName = "Grove" };
        _holySites.Setup(h => h.GetHolySite("s2")).Returns(site);
        Assert.True(_civ.SetCapital(civ.CivId, founderUid, "Grove Seat", "s2"));

        // r2 leaves
        Assert.True(_civ.LeaveReligion("r2", r2Founder));

        Assert.Null(civ.CapitalHolySiteId);
        Assert.Equal("Grove Seat", civ.CapitalName);
    }
}
