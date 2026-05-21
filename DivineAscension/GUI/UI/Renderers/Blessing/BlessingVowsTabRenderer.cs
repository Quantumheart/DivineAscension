using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Actions;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.GUI.Models.Blessing.Tree;
using DivineAscension.GUI.UI.Renderers.Blessing.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     I.iii — Vows of the Order. Hosts the religion-side blessing tree
///     (migrated off III.ii — Blessings, see #336). The page renders as a
///     single-panel tree with a serif title, prose intro, dotted-leader
///     cross-domain summary ("sworn"), and the [Swear] unlock action which is
///     founder-gated on the UI side (server enforces).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingVowsTabRenderer
{
    private const float TitleHeight = 28f;
    private const float IntroHeight = 36f;
    private const float SummaryRowHeight = 18f;
    private const float SummaryGap = 14f;
    private const float PatronHeadingHeight = 22f;
    private const float StrapSpacing = 8f;
    private const float DividerHeight = 14f;
    private const float Padding = 16f;

    internal static BlessingTabRenderResult Draw(BlessingTabViewModel vm)
    {
        const float infoPanelHeight = 200f;
        const float actionButtonHeight = 36f;
        const float actionButtonPadding = 16f;

        string? hoveringBlessingId = null;

        var drawList = ImGui.GetWindowDrawList();
        var topY = vm.Y;

        // Title — serif voice ("VOWS OF THE ORDER"). Font swap is unresolved (#336
        // notes Cinzel is off the table); use the standard PageTitle size here so
        // the typography PR can wire the chosen serif into one place.
        DrawCenteredText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_TITLE),
            vm.X, topY, vm.Width, TitleHeight, PageTitle, ColorPalette.Gold);
        topY += TitleHeight + 4f;

        // Prose intro — wrapped under the title.
        DrawWrappedText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_INTRO),
            vm.X + Padding, topY, vm.Width - Padding * 2, Secondary, ColorPalette.White);
        topY += IntroHeight;

        DrawOrnamentalDivider(drawList, vm.X, topY, vm.Width);
        topY += DividerHeight;

        // Across the Domains — dotted-leader rows, religion-only counts.
        DrawText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_ACROSS_DOMAINS),
            vm.X + Padding, topY, SectionHeader, ColorPalette.Gold);
        topY += SectionHeader + 6f;

        foreach (var summary in vm.DeitySummaries)
        {
            DrawDottedLeaderRow(drawList, vm.X + Padding, topY, vm.Width - Padding * 2, summary);
            topY += SummaryRowHeight;
        }
        topY += SummaryGap;

        DrawOrnamentalDivider(drawList, vm.X, topY, vm.Width);
        topY += DividerHeight;

        // Patron sub-heading + right-aligned prestige balance.
        var patronName = string.IsNullOrEmpty(vm.PatronDeityName)
            ? vm.PatronDomain.ToString()
            : vm.PatronDeityName!;
        var patronHeading = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_VOWS_PATRON_HEADING, patronName);
        DrawText(drawList, patronHeading, vm.X + Padding, topY, TableHeader, ColorPalette.Gold);

        var prestigeBalance = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_VOWS_PRESTIGE_BALANCE,
            vm.ReligionPrestige,
            vm.PrestigeNextThreshold > 0 ? vm.PrestigeNextThreshold : vm.ReligionPrestige);
        DrawRightAlignedText(drawList, prestigeBalance,
            vm.X + Padding, topY, vm.Width - Padding * 2, TableHeader, ColorPalette.Gold);
        topY += PatronHeadingHeight + 4f;

        // Deity sub-index.
        var requestedDeity = DeitySelectorRenderer.Draw(vm.X + Padding, topY, vm.ActiveDeity, vm.PatronDomain);
        topY += DeitySelectorRenderer.Height + StrapSpacing;

        DrawOrnamentalDivider(drawList, vm.X, topY, vm.Width);
        topY += DividerHeight;

        // Tree pane (single panel, religion-only).
        var consumedTop = topY - vm.Y;
        var treeHeight = vm.Height - infoPanelHeight - Padding - consumedTop - DividerHeight - 8f;
        if (treeHeight < 80f) treeHeight = 80f;

        var treeVm = new BlessingTreeViewModel(
            vm.PlayerTreeScrollState,
            vm.ReligionTreeScrollState,
            vm.PlayerBlessingStates,
            vm.ReligionBlessingStates,
            vm.X, topY, vm.Width, treeHeight,
            vm.DeltaTime,
            vm.SelectedBlessingId,
            vm.PlayerFavor,
            vm.ReligionPrestige
        );
        var treeResult = BlessingTreeRenderer.Draw(treeVm, BlessingKind.Religion, showBalanceHeader: false);

        foreach (var ev in treeResult.Events)
            if (ev is TreeEvent.Hovered hovered)
                hoveringBlessingId = hovered.BlessingId;

        var infoY = topY + treeHeight + 4f;
        DrawOrnamentalDivider(drawList, vm.X, infoY, vm.Width);
        infoY += DividerHeight;

        // Info pane — heading + reused BlessingInfoRenderer over religion-only states.
        DrawText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_VOWS_SELECTED_HEADING),
            vm.X + Padding, infoY, TableHeader, ColorPalette.Gold);

        var religionOnlyStates = new Dictionary<string, BlessingNodeState>(vm.ReligionBlessingStates);
        var communalSelected = vm.SelectedBlessingState != null
                               && vm.SelectedBlessingState.Blessing.Kind == BlessingKind.Religion
            ? vm.SelectedBlessingState
            : null;

        var infoVm = new BlessingInfoViewModel(
            communalSelected,
            religionOnlyStates,
            vm.X, infoY + TableHeader + 4f,
            vm.Width, infoPanelHeight - TableHeader - 4f,
            vm.PlayerFavor,
            vm.ReligionPrestige);
        BlessingInfoRenderer.Draw(infoVm);

        // Actions — [Swear], founder-gated.
        var buttonX = vm.X + vm.Width - actionButtonPadding;
        var buttonY = vm.Y + vm.Height - actionButtonHeight - actionButtonPadding;
        var actionsVm = new BlessingActionsViewModel(
            communalSelected,
            buttonX, buttonY,
            vm.PlayerFavor, vm.ReligionPrestige,
            isReligionFounder: vm.IsReligionFounder
        );
        var actionsResult = BlessingActionsRenderer.Draw(actionsVm);

        // Hover tooltip — religion-only state.
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

            var mousePos = ImGui.GetMousePos();
            TooltipRenderer.Draw(tooltipData, mousePos.X, mousePos.Y, vm.WindowWidth, vm.WindowHeight);
        }

        return new BlessingTabRenderResult(
            treeResult.Events,
            actionsResult.Events,
            hoveringBlessingId,
            vm.Height,
            requestedDeity);
    }

    private static void DrawCenteredText(ImDrawListPtr drawList, string text,
        float x, float y, float width, float height, float fontSize, Vector4 color)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(ImGui.GetFont(), fontSize, pos, ImGui.ColorConvertFloat4ToU32(color), text);
    }

    private static void DrawText(ImDrawListPtr drawList, string text, float x, float y,
        float fontSize, Vector4 color)
    {
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(color), text);
    }

    private static void DrawRightAlignedText(ImDrawListPtr drawList, string text,
        float x, float y, float width, float fontSize, Vector4 color)
    {
        var size = ImGui.CalcTextSize(text);
        drawList.AddText(ImGui.GetFont(), fontSize,
            new Vector2(x + width - size.X, y),
            ImGui.ColorConvertFloat4ToU32(color), text);
    }

    private static void DrawWrappedText(ImDrawListPtr drawList, string text,
        float x, float y, float width, float fontSize, Vector4 color)
    {
        // Simple single-line draw with cap to avoid pulling in a wrap helper;
        // the intro line fits on one row at typical dialog widths. If it
        // exceeds the width ImGui will clip — acceptable for this MVP page.
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(color), text);
    }

    private static void DrawOrnamentalDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.5f));
        var glyph = "─── ✦ ─── ✦ ─── ✦ ───";
        var size = ImGui.CalcTextSize(glyph);
        var pos = new Vector2(x + (width - size.X) / 2f, y + 2f);
        drawList.AddText(ImGui.GetFont(), Secondary, pos, color, glyph);
    }

    private static void DrawDottedLeaderRow(ImDrawListPtr drawList,
        float x, float y, float width, DeityBlessingSummary summary)
    {
        var domainLabel = summary.Domain.ToString();
        var rightText = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_VOWS_SWORN_ROW,
            summary.UnlockedReligion,
            summary.TotalReligion);
        var textColor = ImGui.ColorConvertFloat4ToU32(
            summary.IsPatron ? ColorPalette.Gold : ColorPalette.White);

        var leftSize = ImGui.CalcTextSize(domainLabel);
        var rightSize = ImGui.CalcTextSize(rightText);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, y), textColor, domainLabel);
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(x + width - rightSize.X, y), textColor, rightText);

        // Dotted leader between the two ends.
        var dotsStart = x + leftSize.X + 8f;
        var dotsEnd = x + width - rightSize.X - 8f;
        if (dotsEnd > dotsStart)
        {
            var dotColor = ImGui.ColorConvertFloat4ToU32(
                ColorPalette.WithAlpha(ColorPalette.White, 0.5f));
            const float dotSpacing = 6f;
            for (var dx = dotsStart; dx < dotsEnd; dx += dotSpacing)
                drawList.AddCircleFilled(new Vector2(dx, y + Body / 2f), 1.0f, dotColor, 6);
        }
    }
}
