using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Banners;

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
        drawList.AddRectFilled(bgStart, bgEnd, bgColor, UiScale.Scaled(6f));
        drawList.AddRect(bgStart, bgEnd, borderColor, UiScale.Scaled(6f), ImDrawFlags.None, UiScale.Scaled(1.5f));

        // Icon (exclamation) circle
        var iconCenter = new Vector2(x + UiScale.Scaled(16f), y + height / 2f);
        drawList.AddCircleFilled(iconCenter, UiScale.Scaled(10f), ImGui.ColorConvertFloat4ToU32(ColorPalette.Red * 0.8f));
        var exPos = new Vector2(iconCenter.X - UiScale.Scaled(3.5f), iconCenter.Y - UiScale.Scaled(7f));
        drawList.AddText(exPos, ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText), "!");

        // Message text
        var textX = x + UiScale.Scaled(36f);
        var textY = y + (height - ImGui.CalcTextSize(message).Y) / 2f;
        drawList.AddText(new Vector2(textX, textY), ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText), message);

        // Action buttons on the right
        var btnW = UiScale.Scaled(86f);
        var btnH = UiScale.Scaled(28f);
        var rightPadding = UiScale.Scaled(10f);
        var btnY = y + (height - btnH) / 2f;
        var curX = x + width - rightPadding - btnW;

        // Dismiss button
        if (ButtonRenderer.DrawSmallButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_DISMISS), curX, btnY, btnW, btnH,
                ColorPalette.DarkBrown))
            dismissClicked = true;
        curX -= btnW + UiScale.Scaled(8f);

        if (showRetry)
            if (ButtonRenderer.DrawSmallButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_RETRY), curX, btnY, btnW, btnH,
                    ColorPalette.Gold * 0.8f))
                retryClicked = true;

        return height + UiScale.Scaled(8f); // include small spacing after banner
    }
}