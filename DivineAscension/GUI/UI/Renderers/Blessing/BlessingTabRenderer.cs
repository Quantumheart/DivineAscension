using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.GUI.Models.Blessing.Tree;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     III.ii — The Blessings, rendered as a scrollable ledger chapter. Personal blessings
///     only — the religion-side tree migrated to I.iii — Vows of the Order in #335. Title
///     strip, prose intro, dotted-leader cross-domain summary, patron sub-heading, deity
///     sub-index, and the personal tree pane. Unlock is by double-click (confirmed in the
///     manager, #453); node details show on hover via the tooltip.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingTabRenderer
{
    private const float Padding = 16f;
    private const float SectionLabelHeight = 22f;
    private const float LeaderRowHeight = 22f;
    private const float DividerSpacing = 18f;
    private const float ScrollbarWidth = 16f;
    private const float TreePaneHeight = 340f;
    private const float BannerHeight = 26f;

    internal static BlessingTabRenderResult DrawBlessingsTab(BlessingTabViewModel vm)
    {
        string? hoveringBlessingId = null;

        var drawList = ImGui.GetWindowDrawList();
        // Reserve scrollbar gutter (shared with sibling chapter pages) so every divider,
        // leader row, and ornament lines up cross-pane.
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        // The slot-usage row only renders when the server has reported a real cap, i.e. while in a
        // religion. Non-religion players (and ex-members) get no PlayerReligionDataPacket, so the
        // value would be stale/zero — hide the row entirely rather than show "/ 0" (#446).
        var showSlots = vm.MaxBlessingSlots > 0;

        var contentHeight = ComputeContentHeight(vm.DeitySummaries.Count, contentWidth, showSlots, vm.FreeRespecActive);
        var maxScroll = MathF.Max(0f, contentHeight - vm.Height);

        // --- Mouse wheel scroll — only when hovering content outside the tree panel
        //     (tree has its own ImGui child scroll for pan/zoom).
        var mousePos = ImGui.GetMousePos();
        var paneHover = mousePos.X >= vm.X && mousePos.X <= vm.X + vm.Width
                                          && mousePos.Y >= vm.Y && mousePos.Y <= vm.Y + vm.Height;
        var scrollY = vm.BlessingsPageScrollY;
        float? requestedPageScrollY = null;

        // Approximate tree rect for wheel-exclusion. Mirrors the body layout below.
        var preTreeOffset = HeaderHeight() + (vm.FreeRespecActive ? BannerHeight : 0f)
                            + IntroHeight(vm, contentWidth) + 8f + DividerSpacing
                            + SectionLabelHeight + vm.DeitySummaries.Count * LeaderRowHeight + 4f
                            + DividerSpacing + LeaderRowHeight + (showSlots ? LeaderRowHeight : 0f) + 4f
                            + DeitySelectorRenderer.Height + 6f + DividerSpacing;
        var treeScreenTop = vm.Y + preTreeOffset - scrollY;
        var overTree = mousePos.X >= vm.X && mousePos.X <= vm.X + contentWidth
                                          && mousePos.Y >= treeScreenTop
                                          && mousePos.Y <= treeScreenTop + TreePaneHeight;

        if (paneHover && !overTree && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0f)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (MathF.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    requestedPageScrollY = newScrollY;
                }
            }
        }

        drawList.PushClipRect(new Vector2(vm.X, vm.Y),
            new Vector2(vm.X + vm.Width, vm.Y + vm.Height), true);

        // --- Title strip (shared chapter chrome). Use the religion-set deity name when
        // available; fall back to the patron domain name; if there's no patron at all,
        // fall back to the currently-viewed deity from the sub-index.
        var hasPatron = vm.PatronDomain != DeityDomain.None;
        var headingName = hasPatron
            ? (string.IsNullOrEmpty(vm.PatronDeityName) ? vm.PatronDomain.ToString() : vm.PatronDeityName!)
            : vm.ActiveDeity.ToString();
        var strip = ChapterStripRenderer.Draw(drawList, vm.X, vm.Y, vm.Width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_TITLE),
            rightTitle: hasPatron ? headingName : null);
        var topY = strip.BodyY;

        // --- Free-respec banner (#462). Shown only while the admin window is open; gold to read
        // as a boon. Folded into the scrollable body so all offsets below shift with it.
        if (vm.FreeRespecActive)
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_FREE_RESPEC_ACTIVE),
                vm.X + Padding, topY, SubsectionLabel, ColorPalette.Gold);
            topY += BannerHeight;
        }

        // --- Prose intro.
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        var introWidth = contentWidth - Padding * 2;
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, introWidth, Secondary);
        TextRenderer.DrawInfoText(drawList, intro, vm.X + Padding, topY, introWidth, Secondary,
            ColorPalette.White);
        topY += introHeight + 8f;

        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // --- Across the Domains.
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_ACROSS_DOMAINS),
            vm.X + Padding, topY, SubsectionLabel, ColorPalette.White);
        topY += SectionLabelHeight;

        foreach (var summary in vm.DeitySummaries)
        {
            // Show per-domain spendable favor alongside the inscription count — favor is
            // tracked separately per domain and gates cross-domain unlock costs (1.5x for
            // non-patron domains), so the patron-only balance below isn't enough on its own.
            var inscribed = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INSCRIBED_ROW_FAVOR,
                summary.CurrentFavor, summary.UnlockedPlayer, summary.TotalPlayer);
            ChromeRenderer.DrawLeader(drawList,
                summary.Domain.ToString(), inscribed,
                vm.X + Padding, topY, contentWidth - Padding * 2,
                labelColor: summary.IsPatron ? ColorPalette.Gold : ColorPalette.White,
                valueColor: summary.IsPatron ? ColorPalette.Gold : ColorPalette.White);
            topY += LeaderRowHeight;
        }

        topY += 4f;
        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // --- "Of {Patron}" heading + right-aligned favor balance (matches vows page).
        var patronHeading = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_PAGE_PATRON_HEADING, headingName);
        var favorBalance = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_FAVOR_BALANCE, vm.PlayerFavor);
        ChromeRenderer.DrawLeader(drawList, patronHeading, favorBalance,
            vm.X + Padding, topY, contentWidth - Padding * 2,
            labelColor: ColorPalette.Gold, valueColor: ColorPalette.White);
        topY += LeaderRowHeight;

        // --- Unlock-slot usage (#446). Global across domains: "Blessing Slots .... Unlocked: X / max".
        // Only shown in a religion (real cap); value turns red at the cap to mirror the gated unlock.
        if (showSlots)
        {
            var slotsHeading = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_SLOTS_HEADING);
            var slotsValue = LocalizationService.Instance.Get(
                LocalizationKeys.UI_BLESSING_PAGE_SLOTS, vm.UnlockedPlayerCount, vm.MaxBlessingSlots);
            var atCap = vm.UnlockedPlayerCount >= vm.MaxBlessingSlots;
            ChromeRenderer.DrawLeader(drawList, slotsHeading, slotsValue,
                vm.X + Padding, topY, contentWidth - Padding * 2,
                labelColor: ColorPalette.White,
                valueColor: atCap ? ColorPalette.ErrorRed : ColorPalette.White);
            topY += LeaderRowHeight;
        }

        topY += 4f;

        // --- Deity sub-index (selector strip, glyph primitives) — horizontally centered.
        var stripX = vm.X + (contentWidth - DeitySelectorRenderer.StripWidth) * 0.5f;
        var requestedDeity = DeitySelectorRenderer.Draw(stripX, topY, vm.ActiveDeity, vm.PatronDomain);
        topY += DeitySelectorRenderer.Height + 6f;

        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // --- Tree pane (personal only). Favor balance moved into the patron-heading
        // leader row above; no dark panel header on the tree itself.
        var treeVm = new BlessingTreeViewModel(
            vm.PlayerTreeScrollState,
            vm.PlayerBlessingStates,
            vm.X, topY, contentWidth, TreePaneHeight,
            vm.DeltaTime,
            vm.SelectedBlessingId,
            PanelId: "blessing_tree_player",
            PanelLabel: string.Empty,
            BalanceText: string.Empty,
            ShowBalanceHeader: false
        );
        var treeResult = BlessingTreeRenderer.Draw(treeVm);

        // Translate the kind-neutral ScrollChanged into the player-side variant the manager
        // expects. Selected/Hovered events pass through unchanged.
        var treeEvents = new List<TreeEvent>(treeResult.Events.Count);
        foreach (var ev in treeResult.Events)
        {
            switch (ev)
            {
                case TreeEvent.Hovered hovered:
                    hoveringBlessingId = hovered.BlessingId;
                    treeEvents.Add(hovered);
                    break;
                case TreeEvent.ScrollChanged sc:
                    treeEvents.Add(new TreeEvent.PlayerTreeScrollChanged(sc.ScrollX, sc.ScrollY));
                    break;
                default:
                    treeEvents.Add(ev);
                    break;
            }
        }

        topY += TreePaneHeight + 6f;

        drawList.PopClipRect();

        // --- Scrollbar (outside the clip rect so it always paints).
        if (maxScroll > 0f)
        {
            Scrollbar.Draw(drawList,
                vm.X + vm.Width - ScrollbarWidth, vm.Y,
                ScrollbarWidth, vm.Height,
                scrollY, maxScroll);
        }

        // --- Hover tooltip — player blessings only.
        if (!string.IsNullOrEmpty(hoveringBlessingId)
            && vm.PlayerBlessingStates.TryGetValue(hoveringBlessingId!, out var hoveringState)
            && hoveringState != null)
        {
            var allBlessings = new Dictionary<string, DivineAscension.Models.Blessing>();
            foreach (var s in vm.PlayerBlessingStates.Values)
                allBlessings.TryAdd(s.Blessing.BlessingId, s.Blessing);

            var tooltipData = BlessingTooltipData.FromBlessingAndState(
                hoveringState.Blessing,
                hoveringState,
                allBlessings
            );

            var mp = ImGui.GetMousePos();
            TooltipRenderer.Draw(tooltipData, mp.X, mp.Y, vm.WindowWidth, vm.WindowHeight);
        }

        // --- Unlock confirmation modal (#453). Drawn last so the dim backdrop and dialog
        // paint over the chapter content. The unlock request is only dispatched once the
        // player confirms here.
        IReadOnlyList<ActionsEvent> actionsEvents = System.Array.Empty<ActionsEvent>();
        if (vm.PendingUnlockState != null)
        {
            var confirmEvents = new List<ActionsEvent>(2);
            BlessingUnlockConfirmRenderer.Draw(vm.PendingUnlockState, confirmEvents);
            actionsEvents = confirmEvents;
        }
        else if (vm.PendingUnlearnState != null)
        {
            // Unlearn confirmation (#459) — same modal chrome as unlock; only one can be open.
            var confirmEvents = new List<ActionsEvent>(2);
            BlessingUnlearnConfirmRenderer.Draw(
                vm.PendingUnlearnState,
                vm.PendingUnlearnCascadeNames,
                vm.PendingUnlearnRefundTotal,
                confirmEvents);
            actionsEvents = confirmEvents;
        }

        return new BlessingTabRenderResult(
            treeEvents,
            actionsEvents,
            hoveringBlessingId,
            vm.Height,
            requestedDeity,
            requestedVowsScrollY: null,
            requestedPageScrollY: requestedPageScrollY);
    }

    private static float HeaderHeight() =>
        ChapterStripRenderer.TopPadding + PaneHeaderRenderer.TotalHeight;

    private static float IntroHeight(BlessingTabViewModel vm, float contentWidth)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        return TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);
    }

    private static float ComputeContentHeight(int summaryRows, float contentWidth, bool showSlots, bool freeRespec)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        var introH = TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);

        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += freeRespec ? BannerHeight : 0f;
        h += introH + 8f;
        h += DividerSpacing;
        h += SectionLabelHeight;
        h += summaryRows * LeaderRowHeight;
        h += 4f + DividerSpacing;
        h += LeaderRowHeight + (showSlots ? LeaderRowHeight : 0f) + 4f;
        h += DeitySelectorRenderer.Height + 6f;
        h += DividerSpacing;
        h += TreePaneHeight + 6f;
        return h;
    }
}
