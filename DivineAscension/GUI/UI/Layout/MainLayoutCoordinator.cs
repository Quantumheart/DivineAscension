using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.RightRail;
using DivineAscension.GUI.UI.Renderers.Sidebar;
using DivineAscension.Network.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Layout;

/// <summary>
///     Top-level dispatcher for the dialog body. Splits the window into three
///     rects — sidebar | content | rightRail — and feeds each to its renderer.
///     Content dispatch reads <see cref="SidebarState.CurrentNav" /> directly.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MainLayoutCoordinator
{
    private const float OuterPadding = 16f;
    private const float SidebarWidth = 240f;
    private const float SidebarCollapsedWidth = 40f;
    private const float RailWidth = 340f;
    private const float Gap = 8f;
    private const float CloseButtonSize = 24f;
    private const float CloseButtonInset = 12f;
    private const float TopChromeHeight = 32f;

    public static void Draw(
        GuiDialogManager manager,
        GuiDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime)
    {
        var windowPos = ImGui.GetWindowPos();
        var outer = new UiRect(windowPos.X, windowPos.Y, windowWidth, windowHeight);
        var drawList = ImGui.GetWindowDrawList();

        // Top-right close button sits outside any child region so it stays
        // clickable regardless of sidebar/rail layout.
        var closeX = outer.Right - CloseButtonInset - CloseButtonSize;
        var closeY = outer.Y + CloseButtonInset;
        if (ButtonRenderer.DrawCloseButton(drawList, closeX, closeY, CloseButtonSize))
        {
            state.RequestClose = true;
        }

        // Reserve a strip at the top for chrome (close button + future top bar),
        // then split the remainder into the three columns.
        var body = outer.Inset(OuterPadding).Cut(TopChromeHeight, 0f);
        var sidebarW = state.Sidebar.IsCollapsed ? SidebarCollapsedWidth : SidebarWidth;
        var (sidebar, afterSidebar) = body.SplitLeft(sidebarW, Gap);
        var (content, rail) = afterSidebar.SplitRight(RailWidth, Gap);

        // --- Sidebar ---
        var sidebarCtx = SidebarNavMapper.ContextFromManager(manager, state.Sidebar);
        var sidebarVm = SidebarNavMapper.BuildViewModel(sidebarCtx);
        var sidebarEvents = SidebarRenderer.Draw(sidebar, sidebarVm);
        ApplySidebarEvents(sidebarEvents, manager, state);

        // --- Right rail ---
        var railVm = BuildRailViewModel(manager, rail);
        RightRailRenderer.Draw(rail, railVm);

        // --- Content dispatch (driven by Sidebar.CurrentNav).
        DispatchContent(manager, state, content, windowWidth, windowHeight, deltaTime);
    }

    private static void ApplySidebarEvents(IReadOnlyList<SidebarEvent> events,
        GuiDialogManager manager, GuiDialogState state)
    {
        foreach (var ev in events)
        {
            switch (ev)
            {
                case SidebarEvent.ItemClicked itemClicked:
                    SidebarNavMapper.Apply(itemClicked.Id, state);
                    // Per-destination data request + matching context-error clear.
                    RefreshSidebarDestinationData(itemClicked.Id, manager);
                    break;
                case SidebarEvent.GroupToggled group:
                    var groups = state.Sidebar.CollapsedGroups;
                    groups[group.Key] = !(groups.TryGetValue(group.Key, out var c) && c);
                    break;
                case SidebarEvent.SidebarToggled:
                    state.Sidebar.IsCollapsed = !state.Sidebar.IsCollapsed;
                    break;
            }
        }
    }

    /// <summary>
    ///     Per-destination data refresh — mirrors the bodies of the legacy
    ///     <c>SubTabEvent.TabChanged</c> handlers in
    ///     <c>ReligionStateManager</c> / <c>CivilizationStateManager</c>. Fires
    ///     the appropriate request and clears the matching context error so
    ///     each sub-view has fresh data when the user navs in.
    /// </summary>
    private static void RefreshSidebarDestinationData(SidebarNavId id, GuiDialogManager manager)
    {
        var religion = manager.ReligionStateManager;
        var civ = manager.CivilizationManager;
        switch (id)
        {
            case SidebarNavId.ReligionBrowse:
                religion.State.ErrorState.BrowseError = null;
                religion.State.BrowseState.IsBrowseLoading = true;
                religion.RequestReligionList(religion.State.BrowseState.DeityFilter);
                break;
            case SidebarNavId.ReligionInfo:
                religion.State.ErrorState.InfoError = null;
                religion.State.InfoState.Loading = true;
                religion.RequestPlayerReligionInfo();
                break;
            case SidebarNavId.ReligionActivity:
                religion.State.ErrorState.ActivityError = null;
                // Activity log loads via RequestPlayerReligionInfo's response handler
                // path; refresh that to pull the latest activity rows alongside.
                religion.RequestPlayerReligionInfo();
                break;
            case SidebarNavId.ReligionRoles:
                religion.RequestReligionRoles();
                break;
            case SidebarNavId.ReligionInvites:
                religion.State.InvitesState.InvitesError = null;
                religion.State.InvitesState.Loading = true;
                religion.RequestPlayerReligionInfo();
                break;
            case SidebarNavId.ReligionCreate:
                religion.State.ErrorState.CreateError = null;
                religion.RequestPlayerReligionInfo();
                break;
            case SidebarNavId.CivilizationBrowse:
                if (civ.State.DetailState.ViewingCivilizationId != null)
                    civ.State.DetailState.ErrorMsg = null;
                else
                    civ.State.BrowseState.ErrorMsg = null;
                civ.RequestCivilizationList(civ.State.BrowseState.DeityFilter);
                break;
            case SidebarNavId.CivilizationInfo:
                civ.State.InfoState.ErrorMsg = null;
                civ.RequestCivilizationInfo();
                break;
            case SidebarNavId.CivilizationInvites:
                civ.State.InviteState.ErrorMsg = null;
                civ.RequestCivilizationInfo();
                break;
            case SidebarNavId.CivilizationDiplomacy:
                civ.State.DiplomacyState.ErrorMessage = null;
                civ.RequestDiplomacyInfo();
                break;
            case SidebarNavId.CivilizationHolySites:
                civ.State.HolySitesState.Browse.ErrorMsg = null;
                civ.RequestCivilizationHolySites();
                break;
            case SidebarNavId.CivilizationMilestones:
                civ.State.MilestoneState.ErrorMsg = null;
                civ.RequestMilestoneProgress();
                break;
        }
    }

    private static RightRailViewModel BuildRailViewModel(GuiDialogManager manager, UiRect rail)
    {
        var notifications = manager.NotificationManager.State.History;
        var civMembers = manager.CivilizationManager.CivilizationMemberReligions
                         ?? new List<CivilizationInfoResponsePacket.MemberReligion>();
        var header = new ReligionHeaderViewModel(
            manager.HasReligion(),
            manager.HasCivilization(),
            manager.CivilizationManager.CurrentCivilizationName,
            civMembers,
            manager.ReligionStateManager.CurrentReligionDomain,
            manager.ReligionStateManager.CurrentDeityName,
            manager.ReligionStateManager.CurrentReligionName,
            manager.ReligionStateManager.ReligionMemberCount,
            manager.ReligionStateManager.PlayerRoleInReligion,
            manager.ReligionStateManager.GetPlayerFavorProgress(),
            manager.ReligionStateManager.GetReligionPrestigeProgress(),
            manager.IsCivilizationFounder,
            manager.CivilizationManager.CivilizationIcon,
            manager.CivilizationManager.CivilizationRank,
            rail.X,
            rail.Y,
            rail.W);

        return new RightRailViewModel(header, notifications, ShowUnreadOnly: false);
    }

    private static void DispatchContent(GuiDialogManager manager, GuiDialogState state,
        UiRect content, int windowWidth, int windowHeight, float deltaTime)
    {
        if (content.W <= 0f || content.H <= 0f) return;

        var nav = state.Sidebar.CurrentNav;
        switch (nav)
        {
            case SidebarNavId.ReligionBrowse:
            case SidebarNavId.ReligionInfo:
            case SidebarNavId.ReligionActivity:
            case SidebarNavId.ReligionRoles:
            case SidebarNavId.ReligionInvites:
            case SidebarNavId.ReligionCreate:
                manager.ReligionStateManager.DrawReligionTab(nav, content.X, content.Y, content.W, content.H);
                return;
            case SidebarNavId.CivilizationBrowse:
            case SidebarNavId.CivilizationInfo:
            case SidebarNavId.CivilizationInvites:
            case SidebarNavId.CivilizationCreate:
            case SidebarNavId.CivilizationDiplomacy:
            case SidebarNavId.CivilizationHolySites:
            case SidebarNavId.CivilizationMilestones:
                manager.CivilizationManager.DrawCivilizationTab(nav, content.X, content.Y, content.W, content.H);
                return;
        }

        if (nav == SidebarNavId.Blessings)
        {
            manager.BlessingStateManager.DrawBlessingsTab(
                content.X, content.Y, content.W, content.H,
                windowWidth, windowHeight, deltaTime,
                manager.ReligionStateManager.CurrentFavor,
                manager.ReligionStateManager.CurrentPrestige,
                manager.ReligionStateManager.CurrentReligionDomain,
                manager.ReligionStateManager.FavorRanksByDeity,
                manager.ReligionStateManager.TotalFavorEarnedByDeity,
                manager.ReligionStateManager.DiscipleThreshold,
                manager.ReligionStateManager.ZealotThreshold,
                manager.ReligionStateManager.ChampionThreshold,
                manager.ReligionStateManager.AvatarThreshold);
        }
    }
}
