using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Overlays;

/// <summary>
///     Generic modal confirm overlay with title, message and Confirm/Cancel buttons.
///     Draws a dim background and a centered dialog. Returns user action via out parameters.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ConfirmOverlay
{
    public static void Draw(
        string title,
        string message,
        out bool confirmed,
        out bool canceled,
        string? confirmLabel = null,
        string? cancelLabel = null,
        float dialogWidth = 520f)
    {
        confirmed = false;
        canceled = false;

        // Use localized defaults if not provided
        confirmLabel ??= LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CONFIRM);
        cancelLabel ??= LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL);

        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        // 1) Backdrop
        var backdropStart = winPos;
        var backdropEnd = new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y);
        var backdropColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(backdropStart, backdropEnd, backdropColor);

        // 2) Dialog box
        var padding = 16f;
        var titleSize = ImGui.CalcTextSize(title);

        // Compute an adaptive dialog width when caller passes a non-positive width or when the default looks too wide.
        // We consider: title width, message unwrapped width, and total buttons width, then clamp to sensible bounds and window size.
        var btnW = 120f;
        var btnH = 36f;
        var btnSpacing = 10f;
        var totalButtonsWidth = btnW * 2f + btnSpacing;

        var unwrappedMsgWidth = ImGui.CalcTextSize(message).X;
        var minWidth = 420f;
        var maxWidth = Math.Min(640f, winSize.X - 80f); // keep nice margins from window edges

        var effectiveDialogWidth = dialogWidth;
        if (dialogWidth <= 0f || dialogWidth == 520f) // treat default as auto-size candidate
        {
            var contentTarget = Math.Max(titleSize.X, unwrappedMsgWidth);
            effectiveDialogWidth = Math.Clamp(contentTarget + padding * 2f, minWidth, maxWidth);

            // Ensure buttons fit comfortably
            var minForButtons = totalButtonsWidth + padding * 2f;
            if (effectiveDialogWidth < minForButtons)
                effectiveDialogWidth = Math.Clamp(minForButtons, minWidth, maxWidth);
        }

        // Now that we have the final width, compute wrapping and height
        var messageWidth = effectiveDialogWidth - padding * 2f;
        var wrappedMsgHeight = TextRenderer.MeasureWrappedHeight(message, messageWidth, 13f);

        // Vertical rhythm: title + small gap + message + smaller gap + buttons, consistent padding top/bottom
        var contentHeight = titleSize.Y + 8f + wrappedMsgHeight + 16f + btnH;
        var dialogHeight = contentHeight + padding * 2f;

        var dlgX = winPos.X + (winSize.X - effectiveDialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;
        var dlgStart = new Vector2(dlgX, dlgY);
        var dlgEnd = new Vector2(dlgX + effectiveDialogWidth, dlgY + dialogHeight);
        var dlgBg = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.95f);
        var dlgBorder = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
        drawList.AddRectFilled(dlgStart, dlgEnd, dlgBg, 6f);
        drawList.AddRect(dlgStart, dlgEnd, dlgBorder, 6f, ImDrawFlags.None, 1.5f);

        var curX = dlgX + padding;
        var curY = dlgY + padding;

        // Title
        TextRenderer.DrawLabel(drawList, title, curX, curY, 18f, ColorPalette.White);
        curY += titleSize.Y + 8f;

        // Message (word-wrapped)
        TextRenderer.DrawInfoText(drawList, message, curX, curY, messageWidth, 13f);
        curY += wrappedMsgHeight;

        // Buttons
        var btnStartX = dlgX + (effectiveDialogWidth - totalButtonsWidth) / 2f;
        // Anchor buttons to the bottom padding so vertical spacing looks consistent regardless of message height
        var btnY = dlgEnd.Y - padding - btnH;

        if (ButtonRenderer.DrawButton(drawList, confirmLabel, btnStartX, btnY, btnW, btnH, true)) confirmed = true;

        if (ButtonRenderer.DrawButton(drawList, cancelLabel, btnStartX + btnW + btnSpacing, btnY, btnW, btnH))
            canceled = true;
    }
}