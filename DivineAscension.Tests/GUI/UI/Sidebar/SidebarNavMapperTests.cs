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
        int unreadNotifications = 0,
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
            unreadNotifications,
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
    public void HasReligion_LettersStaysEnabledAndCreateBlocked()
    {
        // Letters (the renamed/repurposed ReligionInvites slot) is always
        // visible — content adapts from invites to holiday notices when
        // the player has a religion. Create stays gated as before.
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, isReligionFounder: false));

        var letters = Find(vm, SidebarNavId.ReligionInvites);
        Assert.False(letters.IsDisabled);

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
        // Religion-less: invite count surfaces on the Letters badge.
        var noReligion = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: false, religionInvites: 3, civInvites: 5));
        Assert.Equal(3, Find(noReligion, SidebarNavId.ReligionInvites).Badge);
        Assert.Equal(5, Find(noReligion, SidebarNavId.CivilizationInvites).Badge);

        // In a religion: Letters now shows holiday notices, not invites,
        // so the religion-invite badge is suppressed (would be misleading).
        // Civilization invites are independent of religion membership.
        var withReligion = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, religionInvites: 3, civInvites: 5));
        Assert.Equal(0, Find(withReligion, SidebarNavId.ReligionInvites).Badge);
        Assert.Equal(5, Find(withReligion, SidebarNavId.CivilizationInvites).Badge);
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
    public void Groups_AreOrdered_PersonalReligionCivilization()
    {
        var vm = SidebarNavMapper.BuildViewModel(Ctx());

        var keys = vm.Groups.Select(g => g.Key).ToList();
        Assert.Equal(new[]
        {
            SidebarNavMapper.GroupPersonalKey,
            SidebarNavMapper.GroupReligionKey,
            SidebarNavMapper.GroupCivilizationKey
        }, keys);
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
    public void Vows_RequiresReligion_DisabledWithNeedReligionTooltipWhenAbsent()
    {
        var vm = SidebarNavMapper.BuildViewModel(Ctx());

        var vows = Find(vm, SidebarNavId.ReligionVows);
        Assert.True(vows.IsDisabled);
        Assert.Equal(LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION, vows.DisabledTooltipKey);
    }

    [Fact]
    public void Vows_EnabledForAnyMember_NotFounderGatedInSidebar()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, isReligionFounder: false));

        var vows = Find(vm, SidebarNavId.ReligionVows);
        Assert.False(vows.IsDisabled);
    }

    [Fact]
    public void Vows_AppearsBetweenInfoAndActivity_InReligionGroup()
    {
        var vm = SidebarNavMapper.BuildViewModel(
            Ctx(hasReligion: true, isReligionFounder: true));

        var religionGroup = vm.Groups.First(g => g.Key == SidebarNavMapper.GroupReligionKey);
        var ids = religionGroup.Items.Select(i => i.Id).ToList();

        var infoIdx = ids.IndexOf(SidebarNavId.ReligionInfo);
        var vowsIdx = ids.IndexOf(SidebarNavId.ReligionVows);
        var activityIdx = ids.IndexOf(SidebarNavId.ReligionActivity);

        Assert.True(infoIdx >= 0);
        Assert.True(vowsIdx >= 0);
        Assert.True(activityIdx >= 0);
        Assert.True(infoIdx < vowsIdx, "Vows should follow Info in sidebar order.");
        Assert.True(vowsIdx < activityIdx, "Vows should precede Activity in sidebar order.");
    }

    [Fact]
    public void ResolveRestoreNav_EnabledPage_IsRestored()
    {
        // CivilizationDiplomacy is enabled when the player has a civilization.
        var nav = SidebarNavMapper.ResolveRestoreNav(
            SidebarNavId.CivilizationDiplomacy,
            Ctx(hasReligion: true, hasCivilization: true));

        Assert.Equal(SidebarNavId.CivilizationDiplomacy, nav);
    }

    [Fact]
    public void ResolveRestoreNav_DisabledPage_FallsBackToPlayerInfo()
    {
        // CivilizationDiplomacy is disabled with no civilization (e.g. player left it).
        var nav = SidebarNavMapper.ResolveRestoreNav(
            SidebarNavId.CivilizationDiplomacy,
            Ctx());

        Assert.Equal(SidebarNavId.PlayerInfo, nav);
    }

    [Fact]
    public void ResolveRestoreNav_PlayerInfo_AlwaysRestored()
    {
        Assert.Equal(SidebarNavId.PlayerInfo,
            SidebarNavMapper.ResolveRestoreNav(SidebarNavId.PlayerInfo, Ctx()));
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

}
