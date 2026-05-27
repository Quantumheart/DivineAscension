using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Chronicle;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for the Chronicle chapter (#373). Sibling page to "This Order"
/// (II.i) and the Roster (II.ii). Chapter title strip with the order name and
/// domain glyph, an ornamental divider, then dated prose entries oldest-first (a
/// chronicle reads forward). Shows an italic-feeling empty line until the order's
/// history begins. Read-only: scrolling is the only interaction.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionChronicleRenderer
{
    private const float OrnateDividerHeight = 18f;
    private const float OrnateDividerYPadding = 6f;
    private const float DiamondHalfSize = 3.5f;
    private const float DiamondLeftPadding = 4f;
    private const float ProseIndent = 18f;
    private const float EntryGap = 8f;
    private const float ScrollbarWidth = 16f;
    private const float EmptyTopPadding = 12f;

    public static ReligionChronicleRenderResult Draw(ReligionChronicleViewModel vm, ImDrawListPtr drawList)
    {
        var events = new List<ChronicleEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_LOADING),
                x, y, width, height);
            return new ReligionChronicleRenderResult(events, height);
        }

        if (!vm.HasReligion)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_NO_RELIGION),
                x, y, width, height);
            return new ReligionChronicleRenderResult(events, height);
        }

        var contentHeight = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeight - height);

        var scrollY = vm.ScrollY;
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new ChronicleEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        // === CHAPTER STRIP (title + order name + domain glyph) ===
        var deityDomain = DomainHelper.ParseDeityType(vm.Deity);
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_CHRONICLE_HEADING),
            rightTitle: vm.ReligionName,
            rightGlyph: deityDomain);
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        ChromeRenderer.DrawDividerOrnate(drawList, x, currentY + OrnateDividerYPadding, contentWidth);
        currentY += OrnateDividerHeight;

        if (!vm.HasChronicle)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_CHRONICLE_EMPTY),
                x, currentY + EmptyTopPadding, contentWidth, Secondary, ColorPalette.Grey);
        }
        else
        {
            var proseWidth = contentWidth - ProseIndent;
            foreach (var entry in vm.Chronicle)
            {
                var centerY = currentY + (Secondary + 6f) / 2f;
                ChromeRenderer.DrawDiamond(drawList,
                    x + DiamondLeftPadding + DiamondHalfSize, centerY,
                    DiamondHalfSize,
                    ColorPalette.Gold * 0.6f);

                var text = ComposeLine(entry);
                TextRenderer.DrawInfoText(drawList, text, x + ProseIndent, currentY, proseWidth,
                    Secondary, ColorPalette.LightText);

                currentY += TextRenderer.MeasureWrappedHeight(text, proseWidth, Secondary) + EntryGap;
            }
        }

        drawList.PopClipRect();

        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new ReligionChronicleRenderResult(events, height);
    }

    private static float ComputeContentHeight(ReligionChronicleViewModel vm)
    {
        // Chapter strip body offset is unknown without drawing; approximate with the
        // pane header height plus the ornate divider, which is what the strip reserves.
        var h = PaneHeaderRenderer.TotalHeight + OrnateDividerHeight;

        if (!vm.HasChronicle)
            return h + EmptyTopPadding + Secondary + 8f;

        var proseWidth = vm.Width - ScrollbarWidth - ProseIndent;
        foreach (var entry in vm.Chronicle)
            h += TextRenderer.MeasureWrappedHeight(ComposeLine(entry), proseWidth, Secondary) + EntryGap;

        return h + 8f;
    }

    private static string ComposeLine(PlayerReligionInfoResponsePacket.ChronicleEntryDto entry)
    {
        var day = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_CHRONICLE_DAY,
            entry.InGameDay);
        return $"{day} · {entry.Line}";
    }

    private static void DrawCentered(ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(pos, color, text);
    }
}
