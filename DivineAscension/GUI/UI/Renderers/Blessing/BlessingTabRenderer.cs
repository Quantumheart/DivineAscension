using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Tab;
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
///     III.ii — The Blessings, rendered as a scrollable ledger chapter. Title strip,
///     prose intro, cross-domain leader rows, patron heading + favor balance,
///     deity sub-index, and the tier-grouped ledger of personal blessings
///     (double-click a row to inscribe).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingTabRenderer
{
    private const float Padding = 16f;
    private const float SectionLabelHeight = 22f;
    private const float LeaderRowHeight = 22f;
    private const float DividerSpacing = 18f;
    private const float ScrollbarWidth = 16f;

    internal static BlessingTabRenderResult DrawBlessingsTab(BlessingTabViewModel vm)
    {
        string? hoveringBlessingId = null;

        var drawList = ImGui.GetWindowDrawList();
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        var contentHeight = ComputeContentHeight(vm, contentWidth);
        var maxScroll = MathF.Max(0f, contentHeight - vm.Height);

        var mousePos = ImGui.GetMousePos();
        var paneHover = mousePos.X >= vm.X && mousePos.X <= vm.X + vm.Width
                                          && mousePos.Y >= vm.Y && mousePos.Y <= vm.Y + vm.Height;
        var scrollY = vm.BlessingsPageScrollY;
        float? requestedPageScrollY = null;

        if (paneHover && maxScroll > 0f)
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

        var hasPatron = vm.PatronDomain != DeityDomain.None;
        var headingName = hasPatron
            ? (string.IsNullOrEmpty(vm.PatronDeityName) ? vm.PatronDomain.ToString() : vm.PatronDeityName!)
            : vm.ActiveDeity.ToString();
        var strip = ChapterStripRenderer.Draw(drawList, vm.X, vm.Y, vm.Width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_TITLE),
            rightTitle: hasPatron ? headingName : null);
        var topY = strip.BodyY;

        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        var introWidth = contentWidth - Padding * 2;
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, introWidth, Secondary);
        TextRenderer.DrawInfoText(drawList, intro, vm.X + Padding, topY, introWidth, Secondary,
            ColorPalette.White);
        topY += introHeight + 8f;

        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

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

        // "Of {Patron}" + right-aligned favor balance.
        var patronHeading = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_PAGE_PATRON_HEADING, headingName);
        var favorBalance = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_FAVOR_BALANCE, vm.PlayerFavor);
        ChromeRenderer.DrawLeader(drawList, patronHeading, favorBalance,
            vm.X + Padding, topY, contentWidth - Padding * 2,
            labelColor: ColorPalette.Gold, valueColor: ColorPalette.White);
        topY += LeaderRowHeight + 4f;

        var stripX = vm.X + (contentWidth - DeitySelectorRenderer.StripWidth) * 0.5f;
        var requestedDeity = DeitySelectorRenderer.Draw(stripX, topY, vm.ActiveDeity, vm.PatronDomain);
        topY += DeitySelectorRenderer.Height + 6f;

        ChromeRenderer.DrawDivider(drawList, vm.X, topY, contentWidth);
        topY += DividerSpacing;

        // Tier-grouped ledger replaces the old node-graph tree.
        var ledgerResult = BlessingsLedgerRenderer.Draw(
            vm.X, topY, contentWidth,
            vm.PlayerBlessingStates,
            costUnit: "favor",
            selectedBlessingId: vm.SelectedBlessingId);

        var treeEvents = new List<TreeEvent>(ledgerResult.Events.Count);
        foreach (var ev in ledgerResult.Events)
        {
            if (ev is TreeEvent.Hovered hovered)
                hoveringBlessingId = hovered.BlessingId;
            treeEvents.Add(ev);
        }

        topY += ledgerResult.Height + 6f;

        drawList.PopClipRect();

        if (maxScroll > 0f)
        {
            Scrollbar.Draw(drawList,
                vm.X + vm.Width - ScrollbarWidth, vm.Y,
                ScrollbarWidth, vm.Height,
                scrollY, maxScroll);
        }

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
            hoveringBlessingId,
            vm.Height,
            requestedDeity,
            requestedVowsScrollY: null,
            requestedPageScrollY: requestedPageScrollY);
    }

    private static float ComputeContentHeight(BlessingTabViewModel vm, float contentWidth)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PAGE_INTRO);
        var introH = TextRenderer.MeasureWrappedHeight(intro, contentWidth - Padding * 2, Secondary);

        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += introH + 8f;
        h += DividerSpacing;
        h += SectionLabelHeight;
        h += vm.DeitySummaries.Count * LeaderRowHeight;
        h += 4f + DividerSpacing;
        h += LeaderRowHeight + 4f;
        h += DeitySelectorRenderer.Height + 6f;
        h += DividerSpacing;
        h += BlessingsLedgerRenderer.MeasureHeight(vm.PlayerBlessingStates);
        h += 6f;
        return h;
    }
}
