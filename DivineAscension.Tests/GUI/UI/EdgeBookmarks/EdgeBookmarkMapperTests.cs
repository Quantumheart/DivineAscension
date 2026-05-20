using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Renderers.EdgeBookmarks;

namespace DivineAscension.Tests.GUI.UI.EdgeBookmarks;

[ExcludeFromCodeCoverage]
public class EdgeBookmarkMapperTests
{
    private static EdgeBookmarkViewModel FindByStamp(EdgeBookmarkRibbonStack stack, string stamp)
    {
        return stack.Bookmarks.First(b => b.Stamp == stamp);
    }

    [Fact]
    public void BuildViewModel_ReturnsFourBookmarksInRBCQOrder()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(false, false, SidebarNavId.ReligionBrowse));

        Assert.Equal(4, stack.Bookmarks.Count);
        Assert.Equal("R", stack.Bookmarks[0].Stamp);
        Assert.Equal("B", stack.Bookmarks[1].Stamp);
        Assert.Equal("C", stack.Bookmarks[2].Stamp);
        Assert.Equal("?", stack.Bookmarks[3].Stamp);
    }

    [Fact]
    public void ReligionBookmark_TargetsBrowse_WhenPlayerHasNoReligion()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(false, false, SidebarNavId.Blessings));
        Assert.Equal(SidebarNavId.ReligionBrowse, FindByStamp(stack, "R").Target);
    }

    [Fact]
    public void ReligionBookmark_TargetsInfo_WhenPlayerHasReligion()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(true, false, SidebarNavId.Blessings));
        Assert.Equal(SidebarNavId.ReligionInfo, FindByStamp(stack, "R").Target);
    }

    [Fact]
    public void CivilizationBookmark_TargetsBrowse_WhenPlayerHasNoCivilization()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(true, false, SidebarNavId.ReligionInfo));
        Assert.Equal(SidebarNavId.CivilizationBrowse, FindByStamp(stack, "C").Target);
    }

    [Fact]
    public void CivilizationBookmark_TargetsInfo_WhenPlayerHasCivilization()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(true, true, SidebarNavId.ReligionInfo));
        Assert.Equal(SidebarNavId.CivilizationInfo, FindByStamp(stack, "C").Target);
    }

    [Theory]
    [InlineData(SidebarNavId.ReligionBrowse, "R")]
    [InlineData(SidebarNavId.ReligionInfo, "R")]
    [InlineData(SidebarNavId.ReligionActivity, "R")]
    [InlineData(SidebarNavId.ReligionRoles, "R")]
    [InlineData(SidebarNavId.ReligionInvites, "R")]
    [InlineData(SidebarNavId.ReligionCreate, "R")]
    [InlineData(SidebarNavId.Blessings, "B")]
    [InlineData(SidebarNavId.CivilizationBrowse, "C")]
    [InlineData(SidebarNavId.CivilizationInfo, "C")]
    [InlineData(SidebarNavId.CivilizationInvites, "C")]
    [InlineData(SidebarNavId.CivilizationCreate, "C")]
    [InlineData(SidebarNavId.CivilizationDiplomacy, "C")]
    [InlineData(SidebarNavId.CivilizationHolySites, "C")]
    [InlineData(SidebarNavId.CivilizationMilestones, "C")]
    public void ActiveBookmark_TracksCurrentNavSection(SidebarNavId nav, string activeStamp)
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(true, true, nav));

        foreach (var bm in stack.Bookmarks)
        {
            Assert.Equal(bm.Stamp == activeStamp, bm.IsActive);
        }
    }

    [Fact]
    public void HelpBookmark_IsDisabled()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(true, true, SidebarNavId.ReligionInfo));
        var help = FindByStamp(stack, "?");
        Assert.True(help.IsDisabled);
        Assert.False(help.IsActive);
    }

    [Fact]
    public void NonHelpBookmarks_AreEnabled()
    {
        var stack = EdgeBookmarkMapper.BuildViewModel(
            new EdgeBookmarkMapper.Context(false, false, SidebarNavId.ReligionBrowse));
        Assert.False(FindByStamp(stack, "R").IsDisabled);
        Assert.False(FindByStamp(stack, "B").IsDisabled);
        Assert.False(FindByStamp(stack, "C").IsDisabled);
    }
}
