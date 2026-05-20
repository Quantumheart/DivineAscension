using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.State;

namespace DivineAscension.Tests.GUI.State;

[ExcludeFromCodeCoverage]
public class SidebarStateTests
{
    [Fact]
    public void Constructor_DefaultsToReligionInfoAndExpanded()
    {
        var state = new SidebarState();

        Assert.False(state.IsCollapsed);
        Assert.Empty(state.CollapsedGroups);
        Assert.Equal(SidebarNavId.ReligionInfo, state.CurrentNav);
    }

    [Fact]
    public void CollapsedGroups_RoundTrip()
    {
        var state = new SidebarState();

        state.CollapsedGroups["religion"] = true;
        state.CollapsedGroups["blessings"] = false;

        Assert.True(state.CollapsedGroups["religion"]);
        Assert.False(state.CollapsedGroups["blessings"]);
    }

    [Fact]
    public void Reset_ClearsGroupsAndRestoresDefaults()
    {
        var state = new SidebarState
        {
            IsCollapsed = true,
            CurrentNav = SidebarNavId.CivilizationDiplomacy
        };
        state.CollapsedGroups["religion"] = true;

        state.Reset();

        Assert.False(state.IsCollapsed);
        Assert.Empty(state.CollapsedGroups);
        Assert.Equal(SidebarNavId.ReligionInfo, state.CurrentNav);
    }
}
