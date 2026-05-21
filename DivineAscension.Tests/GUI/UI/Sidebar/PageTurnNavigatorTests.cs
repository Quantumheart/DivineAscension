using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Renderers.Sidebar;

namespace DivineAscension.Tests.GUI.UI.Sidebar;

[ExcludeFromCodeCoverage]
public class PageTurnNavigatorTests
{
    private static SidebarItemViewModel Item(SidebarNavId id, bool disabled = false)
    {
        return new SidebarItemViewModel(
            id,
            id.ToString(),
            "icon",
            0,
            IsActive: false,
            IsDisabled: disabled,
            DisabledTooltipKey: null);
    }

    private static SidebarViewModel Vm(SidebarNavId current, params SidebarItemViewModel[] items)
    {
        var group = new SidebarGroupViewModel("g", "Group", false, items);
        return new SidebarViewModel(false, current, new List<SidebarGroupViewModel> { group });
    }

    [Fact]
    public void EmptyEnabledList_ReportsZeroTotalAndNoNeighbours()
    {
        var vm = new SidebarViewModel(false, SidebarNavId.ReligionInfo,
            new List<SidebarGroupViewModel>());

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(0, pos.Total);
        Assert.Equal(-1, pos.Index);
        Assert.Null(pos.Previous);
        Assert.Null(pos.Next);
    }

    [Fact]
    public void FirstPage_HasNoPrevious_HasNext()
    {
        var vm = Vm(SidebarNavId.ReligionBrowse,
            Item(SidebarNavId.ReligionBrowse),
            Item(SidebarNavId.ReligionInfo),
            Item(SidebarNavId.Blessings));

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(0, pos.Index);
        Assert.Equal(3, pos.Total);
        Assert.Null(pos.Previous);
        Assert.Equal(SidebarNavId.ReligionInfo, pos.Next);
    }

    [Fact]
    public void LastPage_HasPrevious_NoNext()
    {
        var vm = Vm(SidebarNavId.Blessings,
            Item(SidebarNavId.ReligionBrowse),
            Item(SidebarNavId.ReligionInfo),
            Item(SidebarNavId.Blessings));

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(2, pos.Index);
        Assert.Equal(3, pos.Total);
        Assert.Equal(SidebarNavId.ReligionInfo, pos.Previous);
        Assert.Null(pos.Next);
    }

    [Fact]
    public void DisabledItems_AreSkippedInPageChain()
    {
        var vm = Vm(SidebarNavId.ReligionInfo,
            Item(SidebarNavId.ReligionBrowse),
            Item(SidebarNavId.ReligionInfo),
            Item(SidebarNavId.ReligionRoles, disabled: true),
            Item(SidebarNavId.Blessings));

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(3, pos.Total);
        Assert.Equal(1, pos.Index);
        Assert.Equal(SidebarNavId.ReligionBrowse, pos.Previous);
        // Next skips the disabled ReligionRoles.
        Assert.Equal(SidebarNavId.Blessings, pos.Next);
    }

    [Fact]
    public void OrderFollowsSidebarGroupsThenItems()
    {
        var religion = new SidebarGroupViewModel("religion", "Religion", false,
            new List<SidebarItemViewModel>
            {
                Item(SidebarNavId.ReligionBrowse),
                Item(SidebarNavId.ReligionInfo)
            });
        var personal = new SidebarGroupViewModel("personal", "Personal", false,
            new List<SidebarItemViewModel>
            {
                Item(SidebarNavId.PlayerInfo),
                Item(SidebarNavId.Blessings)
            });

        var vm = new SidebarViewModel(false, SidebarNavId.ReligionInfo,
            new List<SidebarGroupViewModel> { religion, personal });

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(4, pos.Total);
        Assert.Equal(1, pos.Index);
        Assert.Equal(SidebarNavId.ReligionBrowse, pos.Previous);
        // Crossing the group boundary still walks to the next enabled item.
        Assert.Equal(SidebarNavId.PlayerInfo, pos.Next);
    }

    [Fact]
    public void CurrentNavOnDisabledItem_IndexIsNegativeButNeighboursResolve()
    {
        // Dialog opens on a founder-only page for a non-founder — the user
        // should still be able to step off via the page-turn buttons.
        var vm = Vm(SidebarNavId.ReligionRoles,
            Item(SidebarNavId.ReligionBrowse),
            Item(SidebarNavId.ReligionRoles, disabled: true),
            Item(SidebarNavId.Blessings));

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(-1, pos.Index);
        Assert.Equal(2, pos.Total);
        Assert.Equal(SidebarNavId.ReligionBrowse, pos.Previous);
        Assert.Equal(SidebarNavId.Blessings, pos.Next);
    }

    [Fact]
    public void OnlyOneEnabledPage_HasNeitherPrevNorNext()
    {
        var vm = Vm(SidebarNavId.Blessings,
            Item(SidebarNavId.ReligionBrowse, disabled: true),
            Item(SidebarNavId.Blessings),
            Item(SidebarNavId.ReligionInfo, disabled: true));

        var pos = PageTurnNavigator.Compute(vm);

        Assert.Equal(1, pos.Total);
        Assert.Equal(0, pos.Index);
        Assert.Null(pos.Previous);
        Assert.Null(pos.Next);
    }
}
