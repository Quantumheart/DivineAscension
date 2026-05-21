using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Shared header renderer for sidebar destinations and modal/overlay forms.
///     Every pane-level title goes through this so font, color, icon framing,
///     rank-tag placement, and the ornamental divider stay consistent across
///     Religion, Civilization, Blessing, and HolySites content.
///     Pass <paramref name="iconTextureId" /> when the pane represents the
///     player's own entity (identity panes). Pass <paramref name="rankTag" />
///     when the entity has a rank/status to display in brackets after the
///     title. Both are optional.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class PaneHeaderRenderer
{
    public const float IconSize = 32f;
    public const float RowHeight = 36f; // Math.Max(IconSize + 4f, 32f)
    public const float DividerBelowSpacing = 20f;
    public const float TotalHeight = RowHeight + DividerBelowSpacing;

    public static float Draw(
        ImDrawListPtr drawList,
        string title,
        float x, float y, float width,
        IntPtr iconTextureId = default,
        string? rankTag = null,
        Vector4? rankColor = null,
        string? rightTitle = null,
        Action<ImDrawListPtr, Vector2, Vector2>? iconPainter = null)
    {
        var hasIcon = iconTextureId != IntPtr.Zero || iconPainter != null;
        var titleX = hasIcon ? x + IconSize + 12f : x;

        if (hasIcon)
        {
            var iconMin = new Vector2(x, y);
            var iconMax = new Vector2(x + IconSize, y + IconSize);
            if (iconPainter != null)
            {
                iconPainter(drawList, iconMin, iconMax);
            }
            else
            {
                var tint = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
                drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tint);
            }
            var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
            drawList.AddRect(iconMin, iconMax, borderColor, 4f, ImDrawFlags.None, 1f);
        }

        TextRenderer.DrawLabel(drawList, title, titleX, y + 4f, FontSizes.PageTitle, ColorPalette.White);

        if (!string.IsNullOrEmpty(rankTag))
        {
            // CalcTextSize reports at the loaded font's base size (SubsectionLabel-equivalent);
            // scale to match what we actually rendered the title at.
            var nameWidthScaled = ImGui.CalcTextSize(title).X * (FontSizes.PageTitle / FontSizes.SubsectionLabel);
            var rankText = $"[{rankTag}]";
            drawList.AddText(ImGui.GetFont(), FontSizes.SubsectionLabel,
                new Vector2(titleX + nameWidthScaled + 8f, y + 6f),
                ImGui.ColorConvertFloat4ToU32(rankColor ?? ColorPalette.Gold), rankText);
        }

        if (!string.IsNullOrEmpty(rightTitle))
        {
            // Right-aligned entity name at the same baseline as the title.
            var rightScale = FontSizes.PageTitle / FontSizes.SubsectionLabel;
            var rightWidth = ImGui.CalcTextSize(rightTitle).X * rightScale;
            var rightX = x + width - rightWidth;
            drawList.AddText(ImGui.GetFont(), FontSizes.PageTitle,
                new Vector2(rightX, y + 4f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), rightTitle);
        }

        var dividerY = y + RowHeight;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return dividerY + DividerBelowSpacing;
    }
}
