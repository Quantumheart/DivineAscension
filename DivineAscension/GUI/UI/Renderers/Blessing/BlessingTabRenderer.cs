using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Actions;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.GUI.Models.Blessing.Tree;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Blessing.Info;
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
///     sub-index, personal tree pane, selected-blessing detail with Read more affordance,
///     and an [Inscribe] action.
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
    private const float InfoPanelHeight = 220f;
    private const float ActionButtonHeight = 36f;

    internal static BlessingTabRenderResult DrawBlessingsTab(BlessingTabViewModel vm)
    {
        string? hoveringBlessingId = null;

        var drawList = ImGui.GetWindowDrawList();
        // Reserve scrollbar gutter (shared with sibling chapter pages) so every divider,
        // leader row, and ornament lines up cross-pane.
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        var contentHeight = ComputeContentHeight(vm.DeitySummaries.Count, contentWidth);
        var maxScroll = MathF.Max(0f, contentHeight - vm.Height);

        // --- Mouse wheel scroll — only when hovering content outside the tree panel
        //     (tree has its own ImGui child scroll for pan/zoom).
        var mousePos = ImGui.GetMousePos();
        var paneHover = mousePos.X >= vm.X && mousePos.X <= vm.X + vm.Width
                                          && mousePos.Y >= vm.Y && mousePos.Y <= vm.Y + vm.Height;
        var scrollY = vm.BlessingsPageScrollY;
        float? requestedPageScrollY = null;

        // Approximate tree rect for wheel-exclusion. Mirrors the body layout below.
        var preTreeOffset = HeaderHeight() + IntroHeight(vm, contentWidth) + 8f + DividerSpacing
                            + SectionLabelHeight + vm.DeitySummaries.Count * LeaderRowHeight + 4f
                            + DividerSpacing + LeaderRowHeight + 4f
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
            var inscribed = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INSCRIBED_ROW,
                summary.UnlockedPlayer, summary.TotalPlayer);
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
        topY += LeaderRowHeight + 4f;

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

        return new BlessingTabRenderResult(
            treeEvents,
            System.Array.Empty<ActionsEvent>(),
            hoveringBlessingId,
            vm.Height,
            requestedDeity,
            requestedVowsScrollY: null,
            requestedPageScrollY: requestedPageScrollY,
            infoEvents: System.Array.Empty<InfoEvent>());
    }

    private static float HeaderHeight() =>
        ChapterStripRenderer.TopPadding + PaneHeaderRenderer.TotalHeight;

    private static float IntroHeight(BlessingTabViewModel vm, float contentWidth)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        return TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);
    }

    private static float ComputeContentHeight(int summaryRows, float contentWidth)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        var introH = TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);

        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += introH + 8f;
        h += DividerSpacing;
        h += SectionLabelHeight;
        h += summaryRows * LeaderRowHeight;
        h += 4f + DividerSpacing;
        h += LeaderRowHeight + 4f;
        h += DeitySelectorRenderer.Height + 6f;
        h += DividerSpacing;
        h += TreePaneHeight + 6f;
        return h;
    }
}
