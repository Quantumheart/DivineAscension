using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.State;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Client;
using Xunit;

namespace DivineAscension.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class ReligionStateManagerHudTests
{
    private readonly ReligionStateManager _sut;

    public ReligionStateManagerHudTests()
    {
        var api = new Mock<ICoreClientAPI>();
        var ui = new Mock<IUiService>();
        var sound = new Mock<ISoundManager>();
        _sut = new ReligionStateManager(api.Object, ui.Object, sound.Object);
    }

    [Fact]
    public void BuildHudViewModel_NoReligion_RendersFiveZeroBarsNoPatron()
    {
        var vm = _sut.BuildHudViewModel(1920, 1080, new RankProgressHudState());

        Assert.True(vm.IsVisible);
        Assert.False(vm.HasReligion);
        Assert.Equal(DeityDomain.None, vm.PatronDomain);
        Assert.Equal(5, vm.Deities.Count);
        Assert.All(vm.Deities, s => Assert.False(s.IsPatron));
        Assert.All(vm.Deities, s => Assert.Equal(0, s.TotalFavorEarned));
        Assert.All(vm.Deities, s => Assert.Equal("Initiate", s.RankName));
    }

    [Fact]
    public void BuildHudViewModel_WithReligion_PatronFlagOnlyOnPatronSlice()
    {
        _sut.Initialize("rel-1", DeityDomain.Wild, "Wolves");

        var vm = _sut.BuildHudViewModel(1920, 1080, new RankProgressHudState());

        Assert.True(vm.HasReligion);
        Assert.Equal(DeityDomain.Wild, vm.PatronDomain);
        var patron = vm.Deities.Single(s => s.IsPatron);
        Assert.Equal(DeityDomain.Wild, patron.Domain);
        Assert.Equal(4, vm.Deities.Count(s => !s.IsPatron));
    }

    [Fact]
    public void BuildHudViewModel_FavorByDeity_DrivesCorrectSlice()
    {
        _sut.Initialize("rel-1", DeityDomain.Wild, "Wolves");
        _sut.TotalFavorEarnedByDeity[DeityDomain.Wild] = 600;
        _sut.FavorRanksByDeity[DeityDomain.Wild] = 1; // Disciple

        var vm = _sut.BuildHudViewModel(1920, 1080, new RankProgressHudState());

        var wild = vm.Deities.Single(s => s.Domain == DeityDomain.Wild);
        Assert.Equal("Disciple", wild.RankName);
        Assert.Equal("Zealot", wild.NextRankName);
        Assert.Equal(600, wild.TotalFavorEarned);
        Assert.Equal(2000, wild.FavorRequiredForNext);
        Assert.Equal(0.30f, wild.Progress, 3);
        Assert.False(wild.IsMaxRank);
    }

    [Fact]
    public void BuildHudViewModel_MaxRank_ProgressOneAndMaxFlagTrue()
    {
        _sut.Initialize("rel-1", DeityDomain.Stone, "Granite");
        _sut.TotalFavorEarnedByDeity[DeityDomain.Stone] = 15000;
        _sut.FavorRanksByDeity[DeityDomain.Stone] = 4;

        var vm = _sut.BuildHudViewModel(1920, 1080, new RankProgressHudState());

        var stone = vm.Deities.Single(s => s.Domain == DeityDomain.Stone);
        Assert.True(stone.IsMaxRank);
        Assert.Equal(1.0f, stone.Progress);
        Assert.Equal("Avatar", stone.RankName);
    }

    [Fact]
    public void BuildHudViewModel_FavorRankMissing_DerivedFromTotalFavor()
    {
        _sut.TotalFavorEarnedByDeity[DeityDomain.Craft] = 2500; // Zealot (>= 2000)

        var vm = _sut.BuildHudViewModel(1920, 1080, new RankProgressHudState());

        var craft = vm.Deities.Single(s => s.Domain == DeityDomain.Craft);
        Assert.Equal("Zealot", craft.RankName);
    }

    [Fact]
    public void BuildHudViewModel_CollapsedToPatron_StateFlagPropagates()
    {
        _sut.Initialize("rel-1", DeityDomain.Wild, "Wolves");
        var state = new RankProgressHudState { CollapsedToPatron = true };

        var vm = _sut.BuildHudViewModel(1920, 1080, state);

        Assert.True(vm.CollapsedToPatron);
        Assert.Equal(5, vm.Deities.Count); // viewmodel still carries all 5; renderer filters
    }

    [Fact]
    public void BuildHudViewModel_DeityOrderIsStable()
    {
        var vm = _sut.BuildHudViewModel(1920, 1080, new RankProgressHudState());

        var order = vm.Deities.Select(s => s.Domain).ToArray();
        Assert.Equal(new[]
        {
            DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest,
            DeityDomain.Harvest, DeityDomain.Stone
        }, order);
    }
}
