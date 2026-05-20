using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.State;
using DivineAscension.GUI.State.Religion;
using DivineAscension.GUI.UI.Renderers.Sidebar;

namespace DivineAscension.Tests.GUI.UI.Sidebar;

[ExcludeFromCodeCoverage]
public class SidebarNavMapperTests
{
    private static SidebarNavMapper.Context Ctx(
        bool hasReligion = false,
        bool hasCivilization = false,
        bool isCivFounder = false,
        bool isReligionFounder = false,
        int religionInvites = 0,
        int civInvites = 0,
        SidebarNavId currentNav = SidebarNavId.ReligionInfo,
        IReadOnlyDictionary<string, bool>? collapsed = null)
    {
        return new SidebarNavMapper.Context(
            hasReligion,
            hasCivilization,
            isCivFounder,
            isReligionFounder,
            religionInvites,
            civInvites,
            currentNav,
            collapsed);
    }

    private static SidebarItemViewModel Find(SidebarViewModel vm, SidebarNavId id)
    {
        return vm.Groups.SelectMany(g => g.Items).First(i => i.Id == id);
    }

    [Fact]
    public void NoReligion_NoCivilization_RestrictedItemsAreDisabledWithTooltips()
    {
        var vm = SidebarNavMapper.BuildViewModel(Ctx());

        var info = Find(vm, SidebarNavId.ReligionInfo);
        Assert.True(info.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION, info.DisabledTooltipKey);

        var roles = Find(vm, SidebarNavId.ReligionRoles);
        Assert.True(roles.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION, roles.DisabledTooltipKey);

        var invites = Find(vm, SidebarNavId.ReligionInvites);
        Assert.False(invites.IsDisabled);

        var create = Find(vm, SidebarNavId.ReligionCreate);
        Assert.False(create.IsDisabled);

        var civInfo = Find(vm, SidebarNavId.CivilizationInfo);
        Assert.True(civInfo.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION, civInfo.DisabledTooltipKey);

        var civInvites = Find(vm, SidebarNavId.CivilizationInvites);
        Assert.True(civInvites.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION, civInvites.DisabledTooltipKey);
    }

    [Fact]
    public void HasReligion_NotFounder_RolesDisabledWithFounderOnly()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, isReligionFounder: false));

        Assert.False(Find(vm, SidebarNavId.ReligionInfo).IsDisabled);
        Assert.False(Find(vm, SidebarNavId.ReligionActivity).IsDisabled);

        var roles = Find(vm, SidebarNavId.ReligionRoles);
        Assert.True(roles.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_FOUNDER_ONLY, roles.DisabledTooltipKey);
    }

    [Fact]
    public void HasReligion_AsFounder_RolesEnabled()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, isReligionFounder: true));

        Assert.False(Find(vm, SidebarNavId.ReligionRoles).IsDisabled);
    }

    [Fact]
    public void HasReligion_InvitesAndCreateBlockedAsAlreadyInReligion()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, isReligionFounder: false));

        var invites = Find(vm, SidebarNavId.ReligionInvites);
        Assert.True(invites.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_RELIGION, invites.DisabledTooltipKey);

        var create = Find(vm, SidebarNavId.ReligionCreate);
        Assert.True(create.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_RELIGION, create.DisabledTooltipKey);
    }

    [Fact]
    public void HasReligion_NoCivilization_CivilizationInvitesEnabled()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true));

        var invites = Find(vm, SidebarNavId.CivilizationInvites);
        Assert.False(invites.IsDisabled);

        var create = Find(vm, SidebarNavId.CivilizationCreate);
        Assert.False(create.IsDisabled);
    }

    [Fact]
    public void HasCivilization_InvitesAndCreateBlockedAsAlreadyInCivilization()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, hasCivilization: true));

        var invites = Find(vm, SidebarNavId.CivilizationInvites);
        Assert.True(invites.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_CIVILIZATION, invites.DisabledTooltipKey);

        var create = Find(vm, SidebarNavId.CivilizationCreate);
        Assert.True(create.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_CIVILIZATION, create.DisabledTooltipKey);
    }

    [Fact]
    public void HasCivilization_AllCivilizationContentItemsEnabled()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, hasCivilization: true));

        Assert.False(Find(vm, SidebarNavId.CivilizationInfo).IsDisabled);
        Assert.False(Find(vm, SidebarNavId.CivilizationDiplomacy).IsDisabled);
        Assert.False(Find(vm, SidebarNavId.CivilizationHolySites).IsDisabled);
        Assert.False(Find(vm, SidebarNavId.CivilizationMilestones).IsDisabled);
    }

    [Fact]
    public void Blessings_AlwaysEnabled()
    {
        var vmOut = SidebarNavMapper.BuildViewModel(Ctx());
        Assert.False(Find(vmOut, SidebarNavId.Blessings).IsDisabled);

        var vmFull = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, hasCivilization: true,
                isCivFounder: true, isReligionFounder: true));
        Assert.False(Find(vmFull, SidebarNavId.Blessings).IsDisabled);
    }

    [Fact]
    public void BadgeCounts_FlowFromContextToInviteItems()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, religionInvites: 3, civInvites: 5));

        // Religion invites is disabled (already in religion) but badge still passes through.
        Assert.Equal(3, Find(vm, SidebarNavId.ReligionInvites).Badge);
        Assert.Equal(5, Find(vm, SidebarNavId.CivilizationInvites).Badge);
    }

    [Fact]
    public void CurrentNav_MarksMatchingItemActive()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(currentNav: SidebarNavId.Blessings));

        Assert.True(Find(vm, SidebarNavId.Blessings).IsActive);
        Assert.False(Find(vm, SidebarNavId.ReligionInfo).IsActive);
    }

    [Fact]
    public void CollapsedGroups_FlowIntoGroupViewModel()
    {
        var collapsed = new Dictionary<string, bool>
        {
            [SidebarNavMapper.GroupReligionKey] = true
        };
        var vm = SidebarNavMapper.BuildViewModel(Ctx(collapsed: collapsed));

        var religionGroup = vm.Groups.First(g => g.Key == SidebarNavMapper.GroupReligionKey);
        Assert.True(religionGroup.IsCollapsed);

        var civGroup = vm.Groups.First(g => g.Key == SidebarNavMapper.GroupCivilizationKey);
        Assert.False(civGroup.IsCollapsed);
    }

    [Fact]
    public void Apply_BlessingsNav_SetsCurrentNav()
    {
        var state = new GuiDialogState();

        SidebarNavMapper.Apply(SidebarNavId.Blessings, state);

        Assert.Equal(SidebarNavId.Blessings, state.Sidebar.CurrentNav);
    }

    [Fact]
    public void Apply_ReligionRoles_SetsCurrentNav()
    {
        var state = new GuiDialogState();

        SidebarNavMapper.Apply(SidebarNavId.ReligionRoles, state);

        Assert.Equal(SidebarNavId.ReligionRoles, state.Sidebar.CurrentNav);
    }

    [Fact]
    public void Apply_CivilizationMilestones_SetsCurrentNav()
    {
        var state = new GuiDialogState();

        SidebarNavMapper.Apply(SidebarNavId.CivilizationMilestones, state);

        Assert.Equal(SidebarNavId.CivilizationMilestones, state.Sidebar.CurrentNav);
    }

    [Fact]
    public void ToReligionSubTab_MapsReligionNavs()
    {
        Assert.Equal(SubTab.Browse, SidebarNavMapper.ToReligionSubTab(SidebarNavId.ReligionBrowse));
        Assert.Equal(SubTab.Info, SidebarNavMapper.ToReligionSubTab(SidebarNavId.ReligionInfo));
        Assert.Equal(SubTab.Roles, SidebarNavMapper.ToReligionSubTab(SidebarNavId.ReligionRoles));
        Assert.Null(SidebarNavMapper.ToReligionSubTab(SidebarNavId.Blessings));
        Assert.Null(SidebarNavMapper.ToReligionSubTab(SidebarNavId.CivilizationInfo));
    }

    [Fact]
    public void ToCivilizationSubTab_MapsCivilizationNavs()
    {
        Assert.Equal(CivilizationSubTab.Browse, SidebarNavMapper.ToCivilizationSubTab(SidebarNavId.CivilizationBrowse));
        Assert.Equal(CivilizationSubTab.Milestones, SidebarNavMapper.ToCivilizationSubTab(SidebarNavId.CivilizationMilestones));
        Assert.Equal(CivilizationSubTab.HolySites, SidebarNavMapper.ToCivilizationSubTab(SidebarNavId.CivilizationHolySites));
        Assert.Null(SidebarNavMapper.ToCivilizationSubTab(SidebarNavId.Blessings));
        Assert.Null(SidebarNavMapper.ToCivilizationSubTab(SidebarNavId.ReligionInfo));
    }
}
