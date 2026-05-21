using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

[ExcludeFromCodeCoverage]
internal static class BlessingInfoSectionDescription
{
    /// <summary>
    ///     Lines of description text shown before the "Read more ▾" affordance kicks in.
    ///     Three keeps the info pane scannable when the blessing has a long codex entry.
    /// </summary>
    private const int PreviewLineLimit = 3;

    /// <summary>
    ///     Draw the description block. Returns the Y coordinate immediately below the block
    ///     (caller advances <c>currentY</c> from there). When <paramref name="toggleEvent"/>
    ///     is non-null on return the user clicked the Read more / Read less hit area this
    ///     frame and the host should forward it through <see cref="BlessingInfoRenderResult"/>.
    /// </summary>
    public static float Draw(BlessingNodeState selectedState, float x,
        float currentY, float padding, float contentWidth, bool isExpanded,
        out InfoEvent.DescriptionExpansionToggled? toggleEvent)
    {
        toggleEvent = null;

        var drawList = ImGui.GetWindowDrawList();
        var descriptionText = selectedState.Blessing.Description ?? string.Empty;
        var descriptionColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText);
        var textX = x + padding;

        var fullHeight = TextRenderer.MeasureWrappedHeight(descriptionText, contentWidth, SubsectionLabel);
        var lineHeight = SubsectionLabel + 6f; // mirrors TextRenderer.DrawInfoText spacing
        var fullLineCount = lineHeight > 0f
            ? (int)System.MathF.Round(fullHeight / lineHeight)
            : 0;

        var needsToggle = fullLineCount > PreviewLineLimit;

        if (!needsToggle || isExpanded)
        {
            BlessingInfoTextUtils.DrawWrappedText(descriptionText,
                textX, currentY, contentWidth, descriptionColorU32, SubsectionLabel);
            currentY += fullHeight;
        }
        else
        {
            var preview = TruncateToLines(descriptionText, contentWidth, SubsectionLabel, PreviewLineLimit);
            BlessingInfoTextUtils.DrawWrappedText(preview,
                textX, currentY, contentWidth, descriptionColorU32, SubsectionLabel);
            currentY += PreviewLineLimit * lineHeight;
        }

        if (needsToggle)
        {
            var labelKey = isExpanded
                ? LocalizationKeys.UI_BLESSING_READ_LESS
                : LocalizationKeys.UI_BLESSING_READ_MORE;
            var labelText = LocalizationService.Instance.Get(labelKey);
            if (DrawToggleAffordance(drawList, labelText, textX, currentY + 4f, isExpanded))
            {
                toggleEvent = new InfoEvent.DescriptionExpansionToggled();
            }
            currentY += lineHeight + 6f;
        }
        else
        {
            currentY += 8f;
        }

        return currentY + 8f;
    }

    private static bool DrawToggleAffordance(ImDrawListPtr drawList, string label,
        float x, float y, bool isExpanded)
    {
        var textSize = ImGui.CalcTextSize(label);
        var width = textSize.X + 18f; // room for the chevron
        var height = textSize.Y + 4f;
        var min = new Vector2(x, y);
        var max = new Vector2(x + width, y + height);

        var mouse = ImGui.GetMousePos();
        var hover = mouse.X >= min.X && mouse.X <= max.X && mouse.Y >= min.Y && mouse.Y <= max.Y;

        var textColor = ImGui.ColorConvertFloat4ToU32(hover ? ColorPalette.Gold : ColorPalette.Gold * 0.85f);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x, y + 2f), textColor, label);

        var chevronCx = x + textSize.X + 8f;
        var chevronCy = y + height * 0.5f;
        ChromeRenderer.DrawChevron(drawList, chevronCx, chevronCy, 8f,
            isExpanded ? ChromeRenderer.ChevronDirection.Up : ChromeRenderer.ChevronDirection.Down,
            hover ? ColorPalette.Gold : ColorPalette.Gold * 0.85f);

        if (hover)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                return true;
        }

        return false;
    }

    private static string TruncateToLines(string text, float width, float fontSize, int maxLines)
    {
        if (string.IsNullOrEmpty(text) || maxLines <= 0) return string.Empty;

        var words = text.Split(' ');
        var currentLine = string.Empty;
        var output = new System.Text.StringBuilder();
        var lines = 0;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            var testSize = ImGui.CalcTextSize(testLine);

            if (testSize.X > width && !string.IsNullOrEmpty(currentLine))
            {
                output.Append(currentLine).Append(' ');
                lines++;
                if (lines >= maxLines) return output.ToString().TrimEnd() + "…";
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine)) output.Append(currentLine);
        return output.ToString();
    }
}
