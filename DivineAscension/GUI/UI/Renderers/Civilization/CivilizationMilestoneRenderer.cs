using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Milestones;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Pure renderer for the II.vii Chronicles ledger chapter (#332). Title
///     strip + refresh glyph, prose intro, Standing leader rows, manuscript
///     Boons of Standing, then a Deeds Recorded list sorted incomplete-first.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationMilestoneRenderer
{
    private static float DividerHeight => UiScale.Scaled(18f);
    private static float DividerYPadding => UiScale.Scaled(6f);
    private static float SectionLabelHeight => UiScale.Scaled(22f);
    private static float StatRowHeight => UiScale.Scaled(22f);
    private static float ProseBottomSpacing => UiScale.Scaled(12f);
    private static float BoonLineHeight => UiScale.Scaled(22f);
    private static float BoonProseHeight => UiScale.Scaled(20f);
    private static float DeedNameHeight => UiScale.Scaled(22f);
    private static float DeedProgressRowHeight => UiScale.Scaled(22f);
    private static float DeedItemSpacing => UiScale.Scaled(10f);
    private static float ProgressBarHeight => UiScale.Scaled(12f);
    private static float ProgressBarIndent => UiScale.Scaled(22f);
    private static float ProgressBarWidth => UiScale.Scaled(160f);
    private static float RefreshGlyphSize => UiScale.Scaled(22f);
    private static float ScrollbarWidth => UiScale.Scaled(16f);

    public static CivilizationMilestoneRenderResult Draw(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<MilestoneEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_LOADING),
                x, y, width, height, ColorPalette.Grey);
            return new CivilizationMilestoneRenderResult(events, height);
        }

        if (!string.IsNullOrEmpty(viewModel.ErrorMsg))
        {
            var errText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_ERROR)
                .Replace("{0}", viewModel.ErrorMsg!);
            DrawCenteredStateText(drawList, errText, x, y, width, height, ColorPalette.ErrorRed);
            return new CivilizationMilestoneRenderResult(events, height);
        }

        var contentHeightEstimate = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width &&
                      mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = viewModel.ScrollY;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new MilestoneEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_MILESTONES));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // Refresh glyph button — painted on top of the strip at the right edge.
        DrawRefreshButton(viewModel, drawList, x, y - scrollY, contentWidth, events);

        if (viewModel.Milestones.Count == 0)
        {
            drawList.PopClipRect();
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_EMPTY),
                x, currentY, width, height - (currentY - y), ColorPalette.Grey);
            return new CivilizationMilestoneRenderResult(events, height);
        }

        currentY = DrawProseIntro(viewModel, drawList, x, currentY, contentWidth);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        currentY = DrawStandingSection(viewModel, drawList, x, currentY, contentWidth);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        currentY = DrawBoonsSection(viewModel, drawList, x, currentY, contentWidth);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        DrawDeedsSection(viewModel, drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new CivilizationMilestoneRenderResult(events, height);
    }

    private static void DrawRefreshButton(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float paneX,
        float stripTopY,
        float contentWidth,
        List<MilestoneEvent> events)
    {
        var bx = paneX + contentWidth - RefreshGlyphSize;
        var by = stripTopY + ChapterStripRenderer.TopPadding + UiScale.Scaled(6f);
        if (ButtonRenderer.DrawButton(drawList, string.Empty,
                bx, by, RefreshGlyphSize, RefreshGlyphSize,
                isPrimary: false, enabled: !viewModel.IsLoading))
        {
            events.Add(new MilestoneEvent.RefreshClicked());
        }
        ChromeRenderer.DrawRefreshArrow(drawList,
            bx + RefreshGlyphSize / 2f,
            by + RefreshGlyphSize / 2f,
            RefreshGlyphSize - UiScale.Scaled(6f),
            ColorPalette.LightText);
    }

    private static float DrawProseIntro(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var realm = string.IsNullOrWhiteSpace(viewModel.RealmName)
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_TITLE)
            : viewModel.RealmName;
        var prose = LocalizationService.Instance.Get(
            LocalizationKeys.UI_CIVILIZATION_MILESTONES_INTRO, realm);

        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var lines = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (lines > 0 ? lines : Body + UiScale.Scaled(6f)) + ProseBottomSpacing;
    }

    private static float DrawStandingSection(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_STANDING_HEADING),
            x, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + SectionLabelHeight;

        var rankName = RankRequirements.GetCivilizationRankName(viewModel.Rank);
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_RANK_LABEL),
            rankName,
            x, currentY, width);
        currentY += StatRowHeight;

        var completed = viewModel.Milestones.Count(m => m.IsCompleted);
        var total = viewModel.Milestones.Count;
        var deedsValue = LocalizationService.Instance.Get(
            LocalizationKeys.UI_CIVILIZATION_MILESTONES_DEEDS_VALUE, completed, total);
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_DEEDS_LABEL),
            deedsValue,
            x, currentY, width);
        currentY += StatRowHeight;

        return currentY + UiScale.Scaled(6f);
    }

    private static float DrawBoonsSection(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_BOONS_HEADING),
            x, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + SectionLabelHeight;

        var phrases = MilestonePhrases.ActiveBonusPhrases(viewModel.Bonuses).ToList();
        if (phrases.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_BOONS_EMPTY),
                x, currentY, width, Secondary, ColorPalette.Grey);
            return currentY + BoonLineHeight + UiScale.Scaled(6f);
        }

        foreach (var boon in phrases)
        {
            DrawDiamondBullet(drawList, x + UiScale.Scaled(4f), currentY + Body / 2f - UiScale.Scaled(1f));
            drawList.AddText(ImGui.GetFont(), Body,
                new Vector2(x + ProgressBarIndent, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
                boon.Value);
            currentY += BoonLineHeight;

            drawList.AddText(ImGui.GetFont(), Secondary,
                new Vector2(x + ProgressBarIndent, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
                boon.Prose);
            currentY += BoonProseHeight;
        }

        return currentY + UiScale.Scaled(6f);
    }

    private static void DrawDiamondBullet(ImDrawListPtr drawList, float cx, float cy)
    {
        ChromeRenderer.DrawDiamond(drawList, cx + UiScale.Scaled(4f), cy, UiScale.Scaled(4f), ColorPalette.Gold);
        // Inner spark for the fleuron — small centre highlight.
        drawList.AddCircleFilled(new Vector2(cx + UiScale.Scaled(4f), cy), UiScale.Scaled(1.2f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText));
    }

    private static void DrawDeedsSection(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_DEEDS_HEADING),
            x, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + SectionLabelHeight;

        var sorted = viewModel.Milestones
            .OrderBy(m => m.IsCompleted)
            .ThenByDescending(m => m.TargetValue > 0 ? (float)m.CurrentValue / m.TargetValue : 0f)
            .ToList();

        foreach (var deed in sorted)
        {
            currentY = DrawDeedItem(drawList, deed, x, currentY, width);
        }
    }

    private static float DrawDeedItem(
        ImDrawListPtr drawList,
        MilestoneProgressDto deed,
        float x, float y, float width)
    {
        var markCx = x + UiScale.Scaled(8f);
        var markCy = y + DeedNameHeight / 2f;
        if (deed.IsCompleted)
            DrawCheckmark(drawList, markCx, markCy);
        else
            DrawOpenCircle(drawList, markCx, markCy);

        var nameColor = deed.IsCompleted
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel,
            new Vector2(x + ProgressBarIndent, y + UiScale.Scaled(2f)),
            nameColor, deed.MilestoneName);

        var currentY = y + DeedNameHeight;

        if (deed.IsCompleted)
        {
            var setDown = LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_MILESTONES_DEEDS_SET_DOWN);
            drawList.AddText(ImGui.GetFont(), Secondary,
                new Vector2(x + ProgressBarIndent, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
                setDown);
            currentY += DeedProgressRowHeight;
        }
        else
        {
            var barX = x + ProgressBarIndent;
            var barY = currentY + UiScale.Scaled(4f);
            var pct = deed.TargetValue > 0
                ? Math.Clamp((float)deed.CurrentValue / deed.TargetValue, 0f, 1f)
                : 0f;
            ProgressBarRenderer.DrawProgressBar(drawList, barX, barY,
                ProgressBarWidth, ProgressBarHeight, pct,
                ColorPalette.Gold, ColorPalette.TableBackground, " ");

            var verb = MilestonePhrases.GetVerbPhrase(deed.TriggerType);
            var countText = LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_MILESTONES_DEEDS_COUNT,
                deed.CurrentValue, deed.TargetValue, verb);
            drawList.AddText(ImGui.GetFont(), Secondary,
                new Vector2(barX + ProgressBarWidth + UiScale.Scaled(10f), currentY + UiScale.Scaled(2f)),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
                countText);
            currentY += DeedProgressRowHeight;
        }

        return currentY + DeedItemSpacing;
    }

    private static void DrawCheckmark(ImDrawListPtr drawList, float cx, float cy)
    {
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        var s = UiScale.Scaled(5f);
        var a = new Vector2(cx - s, cy);
        var b = new Vector2(cx - UiScale.Scaled(1f), cy + s - UiScale.Scaled(1f));
        var c = new Vector2(cx + s, cy - s + UiScale.Scaled(1f));
        drawList.AddLine(a, b, color, UiScale.Scaled(1.8f));
        drawList.AddLine(b, c, color, UiScale.Scaled(1.8f));
    }

    private static void DrawOpenCircle(ImDrawListPtr drawList, float cx, float cy)
    {
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f);
        drawList.AddCircle(new Vector2(cx, cy), UiScale.Scaled(5f), color, 0, UiScale.Scaled(1.4f));
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDividerOrnate(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float ComputeContentHeight(CivilizationMilestoneViewModel viewModel)
    {
        var h = PaneHeaderRenderer.TotalHeight;

        // Prose intro (~2 lines)
        h += UiScale.Scaled(36f) + ProseBottomSpacing;

        // Standing: heading + 2 leader rows + bottom pad
        h += DividerHeight;
        h += SectionLabelHeight + StatRowHeight * 2 + UiScale.Scaled(6f);

        // Boons: heading + N lines (or empty single line)
        h += DividerHeight;
        var boonCount = MilestonePhrases.ActiveBonusPhrases(viewModel.Bonuses).Count();
        h += SectionLabelHeight + (boonCount > 0
            ? boonCount * (BoonLineHeight + BoonProseHeight)
            : BoonLineHeight) + UiScale.Scaled(6f);

        // Deeds: heading + per-item (name + progress/set-down row + spacing)
        h += DividerHeight;
        h += SectionLabelHeight;
        h += viewModel.Milestones.Count * (DeedNameHeight + DeedProgressRowHeight + DeedItemSpacing);

        return h;
    }

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text,
        float x, float y, float width, float height, Vector4 color)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(color), text);
    }
}
