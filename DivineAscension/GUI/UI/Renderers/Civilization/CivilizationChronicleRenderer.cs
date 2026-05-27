using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Chronicle;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
/// Ledger-chapter renderer for the realm Chronicle (#369). Sibling page to
/// "This Realm" and "Laurels". Chapter strip with the realm name, a prose intro,
/// an ornamental divider, dated prose entries oldest-first (a chronicle reads
/// forward), a closing divider, and a centered closing line. Read-only:
/// scrolling is the only interaction.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationChronicleRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float IntroBottomSpacing = 10f;
    private const float IntroLineHeight = 18f;
    private const float DiamondHalfSize = 3.5f;
    private const float DiamondLeftPadding = 4f;
    private const float ProseIndent = 18f;
    private const float EntryGap = 8f;
    private const float ScrollbarWidth = 16f;
    private const float ClosingLineHeight = 24f;
    private const float ClosingLineTopSpacing = 6f;

    public static CivilizationChronicleRenderResult Draw(CivilizationChronicleViewModel vm, ImDrawListPtr drawList)
    {
        var events = new List<CivilizationChronicleEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LOADING),
                x, y, width, height);
            return new CivilizationChronicleRenderResult(events, height);
        }

        if (!vm.HasCivilization)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_NOT_IN_CIV),
                x, y, width, height);
            return new CivilizationChronicleRenderResult(events, height);
        }

        var contentHeight = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeight - height);

        var scrollY = vm.ScrollY;
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        if (isHover && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new CivilizationChronicleEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        // === CHAPTER STRIP (title + realm name) ===
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CHRONICLE_HEADING),
            rightTitle: vm.CivilizationName);
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === EMPTY STATE — strip + centered message, as the civ chapters do ===
        if (!vm.HasChronicle)
        {
            drawList.PopClipRect();
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CHRONICLE_EMPTY),
                x, currentY, width, height - (currentY - y));
            return new CivilizationChronicleRenderResult(events, height);
        }

        // === PROSE INTRO ===
        currentY = DrawIntro(drawList, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === ENTRIES (oldest-first) ===
        var proseWidth = contentWidth - ProseIndent;
        foreach (var entry in vm.Chronicle)
        {
            var centerY = currentY + (Secondary + 6f) / 2f;
            ChromeRenderer.DrawDiamond(drawList,
                x + DiamondLeftPadding + DiamondHalfSize, centerY,
                DiamondHalfSize,
                ColorPalette.Gold * 0.6f);

            // Entry prose sits on the parchment page → iron-gall ink (palette §2),
            // not LightText (cream, for dark surfaces only).
            var text = ComposeLine(entry);
            TextRenderer.DrawInfoText(drawList, text, x + ProseIndent, currentY, proseWidth,
                Secondary, ColorPalette.White);

            currentY += TextRenderer.MeasureWrappedHeight(text, proseWidth, Secondary) + EntryGap;
        }

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === CLOSING LINE ===
        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (maxScroll > 0f)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new CivilizationChronicleRenderResult(events, height);
    }

    private static float DrawIntro(ImDrawListPtr drawList, float x, float y, float width)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CHRONICLE_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, x, y, width, Body, ColorPalette.White);
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, width, Body);
        return y + (introHeight > 0 ? introHeight : IntroLineHeight) + IntroBottomSpacing;
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static void DrawClosingLine(ImDrawListPtr drawList, float x, float y, float width)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CHRONICLE_FOOTER_CLOSING);
        var size = ImGui.CalcTextSize(text);
        var textX = x + (width - size.X) / 2f;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static float ComputeContentHeight(CivilizationChronicleViewModel vm)
    {
        var h = PaneHeaderRenderer.TotalHeight;

        if (!vm.HasChronicle)
            return h;

        h += IntroLineHeight + IntroBottomSpacing;
        h += DividerHeight;

        var proseWidth = vm.Width - ScrollbarWidth - ProseIndent;
        foreach (var entry in vm.Chronicle)
            h += TextRenderer.MeasureWrappedHeight(ComposeLine(entry), proseWidth, Secondary) + EntryGap;

        h += DividerHeight + ClosingLineTopSpacing + ClosingLineHeight;
        return h;
    }

    private static string ComposeLine(CivilizationInfoResponsePacket.ChronicleEntryDto entry)
    {
        var date = ChronicleDateFormatter.Format(entry.Year, entry.Month, entry.DayOfMonth, entry.InGameDay,
            LocalizationKeys.UI_CIVILIZATION_INFO_CHRONICLE_DAY);
        return $"{date} · {entry.Line}";
    }

    private static void DrawCentered(ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(pos, color, text);
    }
}
