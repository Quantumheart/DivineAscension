using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Five-icon tab strip for switching the active deity in the Blessing tab.
///     Patron deity is marked with a gold border and ★ glyph.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DeitySelectorRenderer
{
    private static float TabSize => UiScale.Scaled(56f);
    private static float TabSpacing => UiScale.Scaled(6f);
    private static float PatronBorderThickness => UiScale.Scaled(2.5f);
    private static float ActiveBorderThickness => UiScale.Scaled(2f);
    public static float Height => TabSize + UiScale.Scaled(4f);
    // The selectable domains drive both the tab order and the count; Caravan is
    // included only when its feature flag is on (see DeityDomains.Selectable).
    private static readonly IReadOnlyList<DeityDomain> Order = DeityDomains.Selectable;
    public static readonly int TabCount = Order.Count;
    public static float StripWidth => TabCount * TabSize + (TabCount - 1) * TabSpacing;

    /// <summary>
    ///     Draw the deity selector. Returns the deity the user clicked this frame, or null.
    /// </summary>
    public static DeityDomain? Draw(float x, float y, DeityDomain activeDeity, DeityDomain patronDomain)
    {
        var drawList = ImGui.GetWindowDrawList();
        var mouse = ImGui.GetMousePos();
        var clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        DeityDomain? requested = null;
        var cursorX = x;

        for (var i = 0; i < Order.Count; i++)
        {
            var domain = Order[i];
            var isPatron = patronDomain != DeityDomain.None && domain == patronDomain;
            var isActive = domain == activeDeity;

            var min = new Vector2(cursorX, y);
            var max = new Vector2(cursorX + TabSize, y + TabSize);

            var hovered = mouse.X >= min.X && mouse.X <= max.X && mouse.Y >= min.Y && mouse.Y <= max.Y;

            var bgColor = ImGui.ColorConvertFloat4ToU32(
                hovered ? ColorPalette.LightBrown : ColorPalette.DarkBrown);
            drawList.AddRectFilled(min, max, bgColor, UiScale.Scaled(4f));

            var pad = UiScale.Scaled(10f);
            var glyphMin = new Vector2(min.X + pad, min.Y + pad);
            var glyphMax = new Vector2(max.X - pad, max.Y - pad);
            DomainGlyphRenderer.Draw(drawList, domain, glyphMin, glyphMax, ColorPalette.LightText);

            if (isPatron)
            {
                var patronBorder = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                drawList.AddRect(min, max, patronBorder, UiScale.Scaled(4f), ImDrawFlags.None, PatronBorderThickness);
                var star = "*";
                drawList.AddText(ImGui.GetFont(), Secondary,
                    new Vector2(max.X - UiScale.Scaled(12f), min.Y + UiScale.Scaled(2f)),
                    patronBorder, star);
            }
            else if (isActive)
            {
                var border = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.6f));
                drawList.AddRect(min, max, border, UiScale.Scaled(4f), ImDrawFlags.None, ActiveBorderThickness);
            }

            if (isActive)
            {
                var underline = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                drawList.AddLine(
                    new Vector2(min.X, max.Y + UiScale.Scaled(1f)),
                    new Vector2(max.X, max.Y + UiScale.Scaled(1f)),
                    underline, UiScale.Scaled(2f));
            }

            if (hovered && clicked)
                requested = domain;

            cursorX += TabSize + TabSpacing;
        }

        return requested;
    }
}
