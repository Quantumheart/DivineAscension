using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
    private const float TabSize = 56f;
    private const float TabSpacing = 6f;
    private const float PatronBorderThickness = 2.5f;
    private const float ActiveBorderThickness = 2f;
    public const float Height = TabSize + 4f;

    private static readonly DeityDomain[] Order =
    {
        DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest, DeityDomain.Harvest, DeityDomain.Stone
    };

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

        for (var i = 0; i < Order.Length; i++)
        {
            var domain = Order[i];
            var isPatron = patronDomain != DeityDomain.None && domain == patronDomain;
            var isActive = domain == activeDeity;

            var min = new Vector2(cursorX, y);
            var max = new Vector2(cursorX + TabSize, y + TabSize);

            var hovered = mouse.X >= min.X && mouse.X <= max.X && mouse.Y >= min.Y && mouse.Y <= max.Y;

            var bgColor = ImGui.ColorConvertFloat4ToU32(
                hovered ? ColorPalette.LightBrown : ColorPalette.DarkBrown);
            drawList.AddRectFilled(min, max, bgColor, 4f);

            var textureId = DeityIconLoader.GetDeityTextureId(domain);
            if (textureId != IntPtr.Zero)
            {
                const float pad = 6f;
                var imgMin = new Vector2(min.X + pad, min.Y + pad);
                var imgMax = new Vector2(max.X - pad, max.Y - pad);
                drawList.AddImage(textureId, imgMin, imgMax,
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            }
            else
            {
                var center = new Vector2((min.X + max.X) / 2f, (min.Y + max.Y) / 2f);
                var fallback = ImGui.ColorConvertFloat4ToU32(DomainHelper.GetDeityColor(domain));
                drawList.AddCircleFilled(center, TabSize / 3f, fallback, 16);
            }

            if (isPatron)
            {
                var patronBorder = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                drawList.AddRect(min, max, patronBorder, 4f, ImDrawFlags.None, PatronBorderThickness);
                var star = "*";
                drawList.AddText(ImGui.GetFont(), Secondary,
                    new Vector2(max.X - 12f, min.Y + 2f),
                    patronBorder, star);
            }
            else if (isActive)
            {
                var border = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.6f));
                drawList.AddRect(min, max, border, 4f, ImDrawFlags.None, ActiveBorderThickness);
            }

            if (isActive)
            {
                var underline = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                drawList.AddLine(
                    new Vector2(min.X, max.Y + 1f),
                    new Vector2(max.X, max.Y + 1f),
                    underline, 2f);
            }

            if (hovered && clicked)
                requested = domain;

            cursorX += TabSize + TabSpacing;
        }

        return requested;
    }
}
