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
    public static float IconSize => UiScale.Scaled(32f);
    public static float RowHeight => UiScale.Scaled(36f); // Math.Max(IconSize + 4f, 32f)
    public static float DropCapRowHeight => UiScale.Scaled(44f); // Math.Max(ChromeRenderer.DropCapSize + 4f, RowHeight)
    public static float DividerBelowSpacing => UiScale.Scaled(20f);
    public static float TotalHeight => RowHeight + DividerBelowSpacing;
    private static float DropCapGap => UiScale.Scaled(12f);

    public static float Draw(
        ImDrawListPtr drawList,
        string title,
        float x, float y, float width,
        IntPtr iconTextureId = default,
        string? rankTag = null,
        Vector4? rankColor = null,
        string? rightTitle = null,
        Action<ImDrawListPtr, Vector2, Vector2>? iconPainter = null,
        Vector4? titleColor = null,
        Vector4? dropCapColor = null)
    {
        var hasIcon = iconTextureId != IntPtr.Zero || iconPainter != null;
        var titleX = hasIcon ? x + IconSize + UiScale.Scaled(12f) : x;

        // Drop cap takes the lead position on the left when requested. Caller
        // supplies the domain/deity color; we strip the first character from
        // the rendered title and shift the rest right so it flows from the
        // cap. Hidden if the title is empty or starts with whitespace.
        var displayTitle = title;
        var dropCapPresent = dropCapColor.HasValue
                             && !string.IsNullOrWhiteSpace(title);
        if (dropCapPresent)
        {
            var letter = char.ToUpperInvariant(title[0]);
            ChromeRenderer.DrawDropCap(drawList, letter, titleX, y, dropCapColor!.Value);
            titleX += ChromeRenderer.DropCapSize + DropCapGap;
            displayTitle = title.Substring(1);
        }

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
            drawList.AddRect(iconMin, iconMax, borderColor, UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(1f));
        }

        // Chapter title uses Cinzel Regular at the nearest baked size; rank
        // tag stays in the default font (small annotation). The serif helper
        // returns the actual rendered width so the rank tag lands flush with
        // the title's right edge regardless of Cinzel vs default metrics.
        // When a drop cap leads the row, the title baseline shifts down so
        // small caps sit on the cap's optical midline.
        var titleBaselineY = dropCapPresent ? y + UiScale.Scaled(12f) : y + UiScale.Scaled(4f);
        var titleWidth = TextRenderer.DrawSerifLabel(drawList, displayTitle, titleX, titleBaselineY,
            FontSizes.PageTitle, titleColor ?? ColorPalette.White);

        if (!string.IsNullOrEmpty(rankTag))
        {
            var rankText = $"[{rankTag}]";
            drawList.AddText(ImGui.GetFont(), FontSizes.SubsectionLabel,
                new Vector2(titleX + titleWidth + UiScale.Scaled(8f), y + UiScale.Scaled(6f)),
                ImGui.ColorConvertFloat4ToU32(rankColor ?? ColorPalette.Gold), rankText);
        }

        if (!string.IsNullOrEmpty(rightTitle))
        {
            // Right-aligned entity name at the same baseline as the title.
            var rightWidth = TextRenderer.MeasureSerifLabel(rightTitle, FontSizes.PageTitle);
            var rightX = x + width - rightWidth;
            TextRenderer.DrawSerifLabel(drawList, rightTitle, rightX, y + UiScale.Scaled(4f),
                FontSizes.PageTitle, ColorPalette.Gold);
        }

        var dividerY = y + (dropCapPresent ? DropCapRowHeight : RowHeight);
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return dividerY + DividerBelowSpacing;
    }
}
