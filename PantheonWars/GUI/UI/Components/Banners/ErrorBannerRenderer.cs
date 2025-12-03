using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.GUI.UI.Components.Buttons;

namespace PantheonWars.GUI.UI.Components.Banners;

/// <summary>
///     Simple reusable error/warning banner with optional Retry and Dismiss actions.
///     Draw near top of content area; returns consumed height.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ErrorBannerRenderer
{
    /// <summary>
    ///     Draw an error banner.
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="message">Error message to display</param>
    /// <param name="retryClicked">Out: true if Retry was clicked</param>
    /// <param name="dismissClicked">Out: true if Dismiss was clicked</param>
    /// <param name="showRetry">Whether to show a Retry button</param>
    /// <param name="height">Banner height (default 44f)</param>
    /// <returns>Consumed height</returns>
    public static float Draw(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        string message,
        out bool retryClicked,
        out bool dismissClicked,
        bool showRetry = true,
        float height = 44f)
    {
        retryClicked = false;
        dismissClicked = false;

        var bgStart = new Vector2(x, y);
        var bgEnd = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red * 0.35f);
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red * 0.8f);
        drawList.AddRectFilled(bgStart, bgEnd, bgColor, 6f);
        drawList.AddRect(bgStart, bgEnd, borderColor, 6f, ImDrawFlags.None, 1.5f);

        // Icon (exclamation) circle
        var iconCenter = new Vector2(x + 16f, y + height / 2f);
        drawList.AddCircleFilled(iconCenter, 10f, ImGui.ColorConvertFloat4ToU32(ColorPalette.Red * 0.8f));
        var exPos = new Vector2(iconCenter.X - 3.5f, iconCenter.Y - 7f);
        drawList.AddText(exPos, ImGui.ColorConvertFloat4ToU32(ColorPalette.White), "!");

        // Message text
        var textX = x + 36f;
        var textY = y + (height - ImGui.CalcTextSize(message).Y) / 2f;
        drawList.AddText(new Vector2(textX, textY), ImGui.ColorConvertFloat4ToU32(ColorPalette.White), message);

        // Action buttons on the right
        var btnW = 86f;
        var btnH = 28f;
        var rightPadding = 10f;
        var btnY = y + (height - btnH) / 2f;
        var curX = x + width - rightPadding - btnW;

        // Dismiss button
        if (ButtonRenderer.DrawSmallButton(drawList, "Dismiss", curX, btnY, btnW, btnH, ColorPalette.DarkBrown))
        {
            dismissClicked = true;
        }
        curX -= (btnW + 8f);

        if (showRetry)
        {
            if (ButtonRenderer.DrawSmallButton(drawList, "Retry", curX, btnY, btnW, btnH, ColorPalette.Gold * 0.8f))
            {
                retryClicked = true;
            }
        }

        return height + 8f; // include small spacing after banner
    }
}
