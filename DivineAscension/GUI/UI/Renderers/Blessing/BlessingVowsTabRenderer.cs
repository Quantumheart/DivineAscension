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
///     I.iii — Vows of the Order, rendered as a scrollable ledger chapter.
///     Title strip, prose intro, dotted-leader cross-domain summary,
///     patron sub-heading with prestige balance, deity selector, and the
///     communal tree pane. Unlock is by double-click (founder-gated and
///     confirmed in the manager), mirroring the Blessings page — no detail
///     pane or action footer.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingVowsTabRenderer
{
    private const float Padding = 16f;
    private const float SectionLabelHeight = 22f;
    private const float LeaderRowHeight = 22f;
    private const float DividerSpacing = 18f;
    private const float ScrollbarWidth = 16f;
    private const float TreePaneHeight = 360f;

    internal static BlessingTabRenderResult Draw(BlessingTabViewModel vm)
    {
        string? hoveringBlessingId = null;

        var drawList = ImGui.GetWindowDrawList();
        // Reserve scrollbar gutter (shared with sibling chapter pages) so
        // every divider, leader row, and ornament lines up cross-pane.
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        var contentHeight = ComputeContentHeight(vm.DeitySummaries.Count, contentWidth,
            vm.ReligionBlessingSlotCap > 0);
        var maxScroll = MathF.Max(0f, contentHeight - vm.Height);

        // --- Mouse wheel scroll — only when hovering content outside the tree panel
        //     (tree has its own ImGui child scroll for pan/zoom).
        var mousePos = ImGui.GetMousePos();
        var paneHover = mousePos.X >= vm.X && mousePos.X <= vm.X + vm.Width
                                          && mousePos.Y >= vm.Y && mousePos.Y <= vm.Y + vm.Height;
        var scrollY = vm.VowsPageScrollY;
        float? requestedScrollY = null;

        // Approximate tree rect for wheel-exclusion. Real position depends on cumulative
        // section heights — pre-compute the same way the body does below.
        var slotsRowHeight = vm.ReligionBlessingSlotCap > 0 ? LeaderRowHeight : 0f;
        var preTreeOffset = HeaderHeight() + IntroHeight(vm, contentWidth) + 8f + DividerSpacing
                            + SectionLabelHeight + vm.DeitySummaries.Count * LeaderRowHeight + 4f
                            + DividerSpacing + LeaderRowHeight + 4f + slotsRowHeight
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
                    requestedScrollY = newScrollY;
                }
            }
        }

        // Clip to pane and offset by scroll for all drawList primitives.
        drawList.PushClipRect(new Vector2(vm.X, vm.Y),
            new Vector2(vm.X + vm.Width, vm.Y + vm.Height), true);

        // --- Title strip (shared chapter chrome).
        var rightTitle = string.IsNullOrEmpty(vm.PatronDeityName)
            ? vm.PatronDomain.ToString()
            : vm.PatronDeityName!;
        var strip = ChapterStripRenderer.Draw(drawList, vm.X, vm.Y, vm.Width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_TITLE),
            rightTitle: rightTitle);
        var topY = strip.BodyY;

        // --- Prose intro.
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_INTRO);
        var introWidth = contentWidth - Padding * 2;
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, introWidth, Secondary);
        TextRenderer.DrawInfoText(drawList, intro, vm.X + Padding, topY, introWidth, Secondary,
            ColorPalette.White);
        topY += introHeight + 8f;

        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // --- Across the Domains.
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_ACROSS_DOMAINS),
            vm.X + Padding, topY, SubsectionLabel, ColorPalette.White);
        topY += SectionLabelHeight;

        foreach (var summary in vm.DeitySummaries)
        {
            var sworn = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_SWORN_ROW,
                summary.UnlockedReligion, summary.TotalReligion);
            ChromeRenderer.DrawLeader(drawList,
                summary.Domain.ToString(), sworn,
                vm.X + Padding, topY, contentWidth - Padding * 2,
                labelColor: summary.IsPatron ? ColorPalette.Gold : ColorPalette.White,
                valueColor: summary.IsPatron ? ColorPalette.Gold : ColorPalette.White);
            topY += LeaderRowHeight;
        }

        topY += 4f;
        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // --- "Of {Patron}" + right-aligned prestige balance.
        var patronHeading = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_VOWS_PATRON_HEADING, rightTitle);
        var prestigeBalance = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_VOWS_PRESTIGE_BALANCE,
            vm.ReligionPrestige,
            vm.PrestigeNextThreshold > 0 ? vm.PrestigeNextThreshold : vm.ReligionPrestige);
        ChromeRenderer.DrawLeader(drawList, patronHeading, prestigeBalance,
            vm.X + Padding, topY, contentWidth - Padding * 2,
            labelColor: ColorPalette.Gold, valueColor: ColorPalette.White);
        topY += LeaderRowHeight + 4f;

        // --- "Inscribed: X/Y" religion slot counter (#479). Hidden until the cap is synced
        //     (cap 0 = unknown). Turns gold when the religion is at its inscribe cap.
        if (vm.ReligionBlessingSlotCap > 0)
        {
            var atCap = vm.ReligionBlessingSlotUsed >= vm.ReligionBlessingSlotCap;
            var slotsText = LocalizationService.Instance.Get(
                LocalizationKeys.UI_BLESSING_VOWS_SLOTS_ROW,
                vm.ReligionBlessingSlotUsed, vm.ReligionBlessingSlotCap);
            TextRenderer.DrawLabel(drawList, slotsText, vm.X + Padding, topY, SubsectionLabel,
                atCap ? ColorPalette.Gold : ColorPalette.White);
            topY += LeaderRowHeight;
        }

        // --- Deity sub-index (selector strip, glyph primitives) — horizontally centered.
        var stripX = vm.X + (contentWidth - DeitySelectorRenderer.StripWidth) * 0.5f;
        var requestedDeity = DeitySelectorRenderer.Draw(stripX, topY, vm.ActiveDeity, vm.PatronDomain);
        topY += DeitySelectorRenderer.Height + 6f;

        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // --- Tree pane.
        var treeVm = new BlessingTreeViewModel(
            vm.ReligionTreeScrollState,
            vm.ReligionBlessingStates,
            vm.X, topY, contentWidth, TreePaneHeight,
            vm.DeltaTime,
            vm.SelectedBlessingId,
            PanelId: "blessing_tree_religion",
            PanelLabel: string.Empty,
            BalanceText: string.Empty,
            ShowBalanceHeader: false
        );
        var treeResult = BlessingTreeRenderer.Draw(treeVm);

        // Translate the kind-neutral ScrollChanged into the religion-side variant the manager
        // expects. Selected/Hovered pass through unchanged.
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
                    treeEvents.Add(new TreeEvent.ReligionTreeScrollChanged(sc.ScrollX, sc.ScrollY));
                    break;
                default:
                    // Double-click unlocks the vow, mirroring the Blessings page; the
                    // founder gate and confirmation live in the manager (#453).
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

        // --- Hover tooltip — religion-only state.
        if (!string.IsNullOrEmpty(hoveringBlessingId)
            && vm.ReligionBlessingStates.TryGetValue(hoveringBlessingId!, out var hoveringState)
            && hoveringState != null)
        {
            var allBlessings = new Dictionary<string, DivineAscension.Models.Blessing>();
            foreach (var s in vm.ReligionBlessingStates.Values)
                allBlessings.TryAdd(s.Blessing.BlessingId, s.Blessing);

            var tooltipData = BlessingTooltipData.FromBlessingAndState(
                hoveringState.Blessing,
                hoveringState,
                allBlessings
            );

            var mp = ImGui.GetMousePos();
            TooltipRenderer.Draw(tooltipData, mp.X, mp.Y, vm.WindowWidth, vm.WindowHeight);
        }

        // --- Unlock confirmation modal (#453). Drawn last (over scrollbar/tooltip) so the
        // dim backdrop and dialog paint above the chapter; the unlock request only dispatches
        // once the player confirms here.
        IReadOnlyList<ActionsEvent> actionsEvents = System.Array.Empty<ActionsEvent>();
        if (vm.PendingUnlockState != null)
        {
            var confirmEvents = new List<ActionsEvent>(2);
            BlessingUnlockConfirmRenderer.Draw(vm.PendingUnlockState, confirmEvents);
            actionsEvents = confirmEvents;
        }
        else if (vm.PendingUnlearnState != null)
        {
            // Founder strike confirm (#484), mirroring the Blessings page unlearn modal.
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
            requestedVowsScrollY: requestedScrollY,
            requestedPageScrollY: null);
    }

    private static float HeaderHeight() =>
        ChapterStripRenderer.TopPadding + PaneHeaderRenderer.TotalHeight;

    private static float IntroHeight(BlessingTabViewModel vm, float contentWidth)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_INTRO);
        return TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);
    }

    private static float ComputeContentHeight(int summaryRows, float contentWidth, bool hasSlotsRow)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_INTRO);
        var introH = TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);

        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += introH + 8f;
        h += DividerSpacing;
        h += SectionLabelHeight;
        h += summaryRows * LeaderRowHeight;
        h += 4f + DividerSpacing;
        h += LeaderRowHeight + 4f;
        if (hasSlotsRow) h += LeaderRowHeight;
        h += DeitySelectorRenderer.Height + 6f;
        h += DividerSpacing;
        h += TreePaneHeight + 6f;
        return h;
    }
}
