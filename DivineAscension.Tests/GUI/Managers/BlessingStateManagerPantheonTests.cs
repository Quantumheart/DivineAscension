using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Client;
using Xunit;

namespace DivineAscension.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class BlessingStateManagerPantheonTests
{
    private readonly BlessingStateManager _sut;

    public BlessingStateManagerPantheonTests()
    {
        var api = new Mock<ICoreClientAPI>();
        var ui = new Mock<IUiService>();
        var sound = new Mock<ISoundManager>();
        _sut = new BlessingStateManager(api.Object, ui.Object, sound.Object);
    }

    private static Blessing B(string id, DeityDomain domain, BlessingKind kind = BlessingKind.Player,
        bool requiresPatron = false, int cost = 10)
    {
        return new Blessing
        {
            BlessingId = id,
            Name = id,
            Domain = domain,
            Kind = kind,
            RequiresPatron = requiresPatron,
            Cost = cost,
            PrerequisiteBlessings = new List<string>()
        };
    }

    [Fact]
    public void LoadBlessingStates_PopulatesAllFiveDeityBuckets()
    {
        var players = new List<Blessing>
        {
            B("c1", DeityDomain.Craft),
            B("w1", DeityDomain.Wild),
            B("q1", DeityDomain.Conquest),
            B("h1", DeityDomain.Harvest),
            B("s1", DeityDomain.Stone)
        };
        var religion = new List<Blessing>
        {
            B("rc1", DeityDomain.Craft, BlessingKind.Religion),
            B("rw1", DeityDomain.Wild, BlessingKind.Religion)
        };

        _sut.LoadBlessingStates(players, religion);

        Assert.Equal(5, _sut.State.PlayerBlessingStatesByDeity.Count);
        foreach (var domain in new[] { DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest, DeityDomain.Harvest, DeityDomain.Stone })
            Assert.Single(_sut.State.PlayerBlessingStatesByDeity[domain]);
        Assert.Single(_sut.State.ReligionBlessingStatesByDeity[DeityDomain.Craft]);
        Assert.Single(_sut.State.ReligionBlessingStatesByDeity[DeityDomain.Wild]);
        Assert.Empty(_sut.State.ReligionBlessingStatesByDeity[DeityDomain.Stone]);
    }

    [Fact]
    public void RefreshAllBlessingStates_NonPatronCostMultiplier_AppliedPerDeity()
    {
        _sut.LoadBlessingStates(new List<Blessing>
        {
            B("craft", DeityDomain.Craft),
            B("wild", DeityDomain.Wild)
        }, new List<Blessing>());

        var ranks = new Dictionary<DeityDomain, int>();
        _sut.RefreshAllBlessingStates(ranks, 0, DeityDomain.Wild);

        var craft = _sut.State.PlayerBlessingStatesByDeity[DeityDomain.Craft]["craft"];
        var wild = _sut.State.PlayerBlessingStatesByDeity[DeityDomain.Wild]["wild"];
        Assert.Equal(1.5f, craft.NonPatronCostMultiplier);
        Assert.Equal(1.0f, wild.NonPatronCostMultiplier);
    }

    [Fact]
    public void RefreshAllBlessingStates_RequiresPatronCapstone_LockedOnNonPatron()
    {
        _sut.LoadBlessingStates(new List<Blessing>
        {
            B("avatar_of_craft", DeityDomain.Craft, requiresPatron: true)
        }, new List<Blessing>());

        // Patron is Wild, so the Craft capstone must stay locked even with maxed Craft rank.
        var ranks = new Dictionary<DeityDomain, int> { [DeityDomain.Craft] = 4 };
        _sut.RefreshAllBlessingStates(ranks, 0, DeityDomain.Wild);

        var capstone = _sut.State.PlayerBlessingStatesByDeity[DeityDomain.Craft]["avatar_of_craft"];
        Assert.False(capstone.CanUnlock);
    }

    [Fact]
    public void RequiresPatronCapstone_UnlockableWhenPatronMatches()
    {
        _sut.LoadBlessingStates(new List<Blessing>
        {
            B("avatar_of_wild", DeityDomain.Wild, requiresPatron: true)
        }, new List<Blessing>());

        var ranks = new Dictionary<DeityDomain, int> { [DeityDomain.Wild] = 4 };
        _sut.RefreshAllBlessingStates(ranks, 0, DeityDomain.Wild);

        var capstone = _sut.State.PlayerBlessingStatesByDeity[DeityDomain.Wild]["avatar_of_wild"];
        Assert.True(capstone.CanUnlock);
    }

    [Fact]
    public void SetActiveDeity_SwapsActiveAndResetsSelection()
    {
        _sut.LoadBlessingStates(new List<Blessing>
        {
            B("craft", DeityDomain.Craft),
            B("wild", DeityDomain.Wild)
        }, new List<Blessing>());
        _sut.SetActiveDeity(DeityDomain.Craft);
        _sut.SelectBlessing("craft");
        Assert.Equal("craft", _sut.State.TreeState.SelectedBlessingId);

        _sut.SetActiveDeity(DeityDomain.Wild);

        Assert.Equal(DeityDomain.Wild, _sut.State.ActiveDeity);
        Assert.Null(_sut.State.TreeState.SelectedBlessingId);
        Assert.Single(_sut.ActivePlayerBlessings);
        Assert.True(_sut.ActivePlayerBlessings.ContainsKey("wild"));
    }

    [Fact]
    public void SetActiveDeity_DoesNotResetUnlockedFlags()
    {
        _sut.LoadBlessingStates(new List<Blessing> { B("craft", DeityDomain.Craft) }, new List<Blessing>());
        _sut.SetBlessingUnlocked("craft", true);

        _sut.SetActiveDeity(DeityDomain.Wild);
        _sut.SetActiveDeity(DeityDomain.Craft);

        Assert.True(_sut.ActivePlayerBlessings["craft"].IsUnlocked);
    }

    [Fact]
    public void ActiveBlessings_EmptyWhenDeityHasNone()
    {
        _sut.LoadBlessingStates(new List<Blessing> { B("craft", DeityDomain.Craft) }, new List<Blessing>());

        _sut.SetActiveDeity(DeityDomain.Stone);

        Assert.Empty(_sut.ActivePlayerBlessings);
        Assert.Empty(_sut.ActiveReligionBlessings);
    }
}
