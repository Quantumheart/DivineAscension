using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.PlayerInfo;
using DivineAscension.GUI.Models.PlayerInfo;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Renderers.PlayerInfo;
using DivineAscension.GUI.UI.Renderers.Sidebar;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Layout;

/// <summary>
///     Top-level dispatcher for the dialog body. Splits the window into two
///     rects — sidebar | content — and feeds each to its renderer.
///     Content dispatch reads <see cref="SidebarState.CurrentNav" /> directly.
///     The former right rail now lives as the <c>SidebarNavId.PlayerInfo</c>
///     content destination.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MainLayoutCoordinator
{
    // Layout metrics are authored at base (1.0) scale and returned scaled by
    // UiScale.Factor so the chrome grows proportionally with the fonts (#589).
    private static float OuterPadding => UiScale.Scaled(16f);
    private static float SidebarWidth => UiScale.Scaled(240f);
    private static float SidebarCollapsedWidth => UiScale.Scaled(40f);
    private static float Gap => UiScale.Scaled(8f);
    private static float TopChromeHeight => UiScale.Scaled(32f);
    private static float PageFooterGap => UiScale.Scaled(6f);

    public static void Draw(
        GuiDialogManager manager,
        GuiDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime)
    {
        // Stamp ambient chrome state so pure renderers can tint chapter
        // drop caps in the player's patron ink without holding a manager
        // reference. Null when the player has no religion or hasn't been
        // assigned a domain yet (drop cap then falls back to gold).
        var patronDomain = manager.HasReligion()
                           && manager.ReligionStateManager.CurrentReligionDomain != DeityDomain.None
            ? manager.ReligionStateManager.CurrentReligionDomain
            : (DeityDomain?)null;
        ChromeContext.SetFrame(patronDomain);

        // Any open ConfirmOverlay is modal: the dim backdrop can't stop immediate-mode
        // hit-testing, so suppress all dialog chrome (title close, sidebar nav, page turn)
        // this frame. Only the confirm/cancel buttons the modal itself draws stay live.
        // BeginFrame rolls the previous frame's mark into IsBlocking; the modal persists
        // across frames, so reading it here (before chrome draws) is correct (#453).
        ModalInputGuard.BeginFrame();
        var modalOpen = ModalInputGuard.IsBlocking;

        var windowPos = ImGui.GetWindowPos();
        var outer = new UiRect(windowPos.X, windowPos.Y, windowWidth, windowHeight);

        // Override ImGui's default cool-toned hover / active / check colors
        // so every Selectable, Button, and Checkbox inside the dialog flashes
        // warm gold instead of the stock blueish defaults. Applied once at
        // the layout root so nested renderers don't have to repeat the push.
        var hover = ColorPalette.Gold * 0.25f;
        var active = ColorPalette.Gold * 0.45f;
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hover);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, active);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ColorPalette.DarkBrown);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, hover);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, active);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, ColorPalette.Gold);

        // Title strip occupies the top chrome band of the inset outer rect and
        // hosts the close button on its right edge. Stays visible regardless of
        // sidebar collapsed state because it lives above the column split.
        var inner = outer.Inset(OuterPadding);
        var titleStrip = new UiRect(inner.X, inner.Y, inner.W, TopChromeHeight);
        if (TitleStripRenderer.Draw(titleStrip, manager) && !modalOpen)
        {
            state.RequestClose = true;
        }

        var body = inner.Cut(TopChromeHeight, 0f);
        var sidebarW = state.Sidebar.IsCollapsed ? SidebarCollapsedWidth : SidebarWidth;
        var (sidebar, content) = body.SplitLeft(sidebarW, Gap);

        // --- Sidebar ---
        var sidebarCtx = SidebarNavMapper.ContextFromManager(manager, state.Sidebar);
        var sidebarVm = SidebarNavMapper.BuildViewModel(sidebarCtx);
        var sidebarEvents = SidebarRenderer.Draw(sidebar, sidebarVm);
        if (!modalOpen)
            ApplySidebarEvents(sidebarEvents, manager, state);

        // Reserve a footer strip at the bottom of the content rect for the
        // page-turn affordance. The dispatched content draws into the
        // remaining region above it.
        var pagePosition = PageTurnNavigator.Compute(sidebarVm);
        var hasFooter = pagePosition.Total > 1 && content.H > PageTurnFooterRenderer.FooterHeight + PageFooterGap;
        var contentBody = hasFooter
            ? content.Cut(0f, PageTurnFooterRenderer.FooterHeight + PageFooterGap)
            : content;

        // --- Content dispatch (driven by Sidebar.CurrentNav).
        DispatchContent(manager, state, contentBody, windowWidth, windowHeight, deltaTime);

        if (hasFooter)
        {
            var footerRect = new UiRect(
                content.X,
                content.Bottom - PageTurnFooterRenderer.FooterHeight,
                content.W,
                PageTurnFooterRenderer.FooterHeight);
            var footerEvents = PageTurnFooterRenderer.Draw(footerRect, content, pagePosition);
            if (!modalOpen)
                ApplySidebarEvents(footerEvents, manager, state);
        }

        if (!modalOpen)
            ApplyPageTurnKeyboard(pagePosition, manager, state);

        ImGui.PopStyleColor(8);
    }

    /// <summary>
    ///     Left/right arrow keys flip pages when no text widget or other
    ///     arrow-consuming item is active. Mirrors the click path by emitting
    ///     an <see cref="SidebarEvent.ItemClicked" /> so the page-turn and
    ///     sidebar-click routes stay single-path.
    /// </summary>
    private static void ApplyPageTurnKeyboard(PageTurnNavigator.PagePosition pos,
        GuiDialogManager manager, GuiDialogState state)
    {
        // WantCaptureKeyboard is true whenever an ImGui window has focus, which
        // includes our dialog itself — guard only against active text inputs and
        // items currently being interacted with (sliders, drags, list nav).
        var io = ImGui.GetIO();
        if (io.WantTextInput) return;
        if (ImGui.IsAnyItemActive()) return;

        if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow) && pos.Previous.HasValue)
        {
            SidebarNavMapper.Apply(pos.Previous.Value, state);
            RefreshSidebarDestinationData(pos.Previous.Value, manager);
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow) && pos.Next.HasValue)
        {
            SidebarNavMapper.Apply(pos.Next.Value, state);
            RefreshSidebarDestinationData(pos.Next.Value, manager);
        }
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
            case SidebarNavId.ReligionRoster:
            case SidebarNavId.ReligionChronicle:
            case SidebarNavId.ReligionSacredCalendar:
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
            case SidebarNavId.CivilizationChronicle:
                civ.State.InfoState.ErrorMsg = null;
                civ.RequestCivilizationInfo();
                break;
            case SidebarNavId.CivilizationInvites:
                civ.State.InviteState.ErrorMsg = null;
                civ.RequestCivilizationInfo();
                break;
            case SidebarNavId.CivilizationDiplomacy:
            case SidebarNavId.CivilizationProposeAccord:
                civ.State.DiplomacyState.ErrorMessage = null;
                civ.RequestDiplomacyInfo();
                // Propose page also reads the civ list for the recipient dropdown,
                // so refresh that whenever either accord page is opened.
                civ.RequestCivilizationList(civ.State.BrowseState.DeityFilter);
                break;
            case SidebarNavId.CivilizationHolySites:
                civ.State.HolySitesState.Browse.ErrorMsg = null;
                civ.RequestCivilizationHolySites();
                break;
            case SidebarNavId.CivilizationMilestones:
                civ.State.MilestoneState.ErrorMsg = null;
                civ.RequestMilestoneProgress();
                break;
            case SidebarNavId.CivilizationLeaderboard:
                civ.State.LeaderboardState.ErrorMsg = null;
                civ.RequestLeaderboard();
                break;
        }
    }

    private static PlayerInfoViewModel BuildPlayerInfoViewModel(GuiDialogManager manager,
        GuiDialogState state, UiRect content)
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
            // Bounds get overwritten by PlayerInfoRenderer; leave as content rect.
            content.X,
            content.Y,
            content.W);

        return new PlayerInfoViewModel(header, notifications, state.PlayerInfo.ShowUnreadOnly,
            state.PlayerInfo.ScrollY,
            content.X, content.Y, content.W, content.H);
    }

    private static void ApplyPlayerInfoEvents(IReadOnlyList<PlayerInfoEvent> events,
        GuiDialogManager manager, GuiDialogState state)
    {
        foreach (var ev in events)
        {
            switch (ev)
            {
                case PlayerInfoEvent.MarkNotificationRead mark:
                    manager.NotificationManager.MarkRead(mark.Index);
                    break;
                case PlayerInfoEvent.ClearNotificationHistory:
                    manager.NotificationManager.ClearHistory();
                    break;
                case PlayerInfoEvent.SetUnreadOnly toggle:
                    state.PlayerInfo.ShowUnreadOnly = toggle.Enabled;
                    break;
                case PlayerInfoEvent.ScrollChanged scroll:
                    state.PlayerInfo.ScrollY = scroll.ScrollY;
                    break;
            }
        }
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
            case SidebarNavId.ReligionRoster:
            case SidebarNavId.ReligionActivity:
            case SidebarNavId.ReligionChronicle:
            case SidebarNavId.ReligionSacredCalendar:
            case SidebarNavId.ReligionRoles:
            case SidebarNavId.ReligionInvites:
            case SidebarNavId.ReligionCreate:
                manager.ReligionStateManager.DrawReligionTab(nav, content.X, content.Y, content.W, content.H);
                return;
            case SidebarNavId.ReligionVows:
            {
                var prestigeProgress = manager.ReligionStateManager.GetReligionPrestigeProgress();
                manager.BlessingStateManager.DrawVowsTab(
                    content.X, content.Y, content.W, content.H,
                    windowWidth, windowHeight, deltaTime,
                    manager.ReligionStateManager.CurrentFavor,
                    manager.ReligionStateManager.CurrentPrestige,
                    manager.ReligionStateManager.CurrentReligionDomain,
                    manager.ReligionStateManager.State.InfoState.MyReligionInfo?.IsFounder ?? false,
                    manager.ReligionStateManager.CurrentDeityName,
                    prestigeProgress.RequiredPrestige,
                    manager.ReligionStateManager.FavorByDeity,
                    manager.ReligionStateManager.FavorRanksByDeity,
                    manager.ReligionStateManager.TotalFavorEarnedByDeity,
                    manager.ReligionStateManager.DiscipleThreshold,
                    manager.ReligionStateManager.ZealotThreshold,
                    manager.ReligionStateManager.ChampionThreshold,
                    manager.ReligionStateManager.AvatarThreshold);
                return;
            }
            case SidebarNavId.CivilizationBrowse:
            case SidebarNavId.CivilizationInfo:
            case SidebarNavId.CivilizationInvites:
            case SidebarNavId.CivilizationCreate:
            case SidebarNavId.CivilizationDiplomacy:
            case SidebarNavId.CivilizationProposeAccord:
            case SidebarNavId.CivilizationHolySites:
            case SidebarNavId.CivilizationMilestones:
            case SidebarNavId.CivilizationChronicle:
            case SidebarNavId.CivilizationLeaderboard:
                manager.CivilizationManager.DrawCivilizationTab(nav, content.X, content.Y, content.W, content.H);
                return;
        }

        if (nav == SidebarNavId.PlayerInfo)
        {
            var vm = BuildPlayerInfoViewModel(manager, state, content);
            var events = PlayerInfoRenderer.Draw(vm);
            ApplyPlayerInfoEvents(events, manager, state);
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
                manager.ReligionStateManager.FavorByDeity,
                manager.ReligionStateManager.FavorRanksByDeity,
                manager.ReligionStateManager.TotalFavorEarnedByDeity,
                manager.ReligionStateManager.DiscipleThreshold,
                manager.ReligionStateManager.ZealotThreshold,
                manager.ReligionStateManager.ChampionThreshold,
                manager.ReligionStateManager.AvatarThreshold);
        }
    }
}
