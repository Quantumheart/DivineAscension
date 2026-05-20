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
///     The legacy <c>CurrentMainTab</c> / <c>CurrentSubTab</c> fields are kept
///     in sync by <see cref="SidebarNavMapper.Apply" /> until Phase 4 retires them.
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

        // --- Content dispatch (still driven by the legacy CurrentMainTab,
        //     which SidebarNavMapper.Apply keeps in sync with CurrentNav).
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
                    var previousMainTab = state.CurrentMainTab;
                    SidebarNavMapper.Apply(itemClicked.Id, state,
                        manager.ReligionStateManager.State,
                        manager.CivilizationManager.State);
                    if (state.CurrentMainTab != previousMainTab)
                    {
                        // Mirror the old top-tab click-load: kicks off the
                        // server requests so the new content has data to draw.
                        RefreshTabData(state.CurrentMainTab, manager);
                    }
                    // Browse always re-fires the list request, even on same
                    // main tab — the old sub-tab strip did this on every click.
                    if (itemClicked.Id == SidebarNavId.ReligionBrowse)
                    {
                        manager.ReligionStateManager.State.BrowseState.IsBrowseLoading = true;
                        manager.ReligionStateManager.RequestReligionList(
                            manager.ReligionStateManager.State.BrowseState.DeityFilter);
                    }
                    else if (itemClicked.Id == SidebarNavId.CivilizationBrowse)
                    {
                        manager.CivilizationManager.RequestCivilizationList(
                            manager.CivilizationManager.State.BrowseState.DeityFilter);
                    }
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

    private static void RefreshTabData(MainDialogTab tab, GuiDialogManager manager)
    {
        switch (tab)
        {
            case MainDialogTab.Religion:
                manager.ReligionStateManager.State.BrowseState.IsBrowseLoading = true;
                manager.ReligionStateManager.RequestReligionList(
                    manager.ReligionStateManager.State.BrowseState.DeityFilter);
                if (manager.HasReligion())
                    manager.ReligionStateManager.State.InfoState.Loading = true;
                else
                    manager.ReligionStateManager.State.InvitesState.Loading = true;
                manager.ReligionStateManager.RequestPlayerReligionInfo();
                break;
            case MainDialogTab.Civilization:
                manager.CivilizationManager.RequestCivilizationList(
                    manager.CivTabState.BrowseState.DeityFilter);
                manager.CivilizationManager.RequestCivilizationInfo();
                manager.ReligionStateManager.RequestPlayerReligionInfo();
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

        switch (state.CurrentMainTab)
        {
            case MainDialogTab.Religion:
                manager.ReligionStateManager.DrawReligionTab(content.X, content.Y, content.W, content.H);
                break;
            case MainDialogTab.Blessings:
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
                break;
            case MainDialogTab.Civilization:
                manager.CivilizationManager.DrawCivilizationTab(content.X, content.Y, content.W, content.H);
                break;
        }
    }
}
