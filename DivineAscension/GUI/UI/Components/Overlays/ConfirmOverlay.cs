using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using FontSizes = DivineAscension.GUI.UI.Utilities.FontSizes;
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

        // Flag this as a modal so the dialog chrome stops reacting to click-through this frame.
        ModalInputGuard.MarkOpen();

        // Use localized defaults if not provided
        confirmLabel ??= LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CONFIRM);
        cancelLabel ??= LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL);

        // Draw on the viewport foreground list so the backdrop and dialog paint above
        // everything — including tree nodes drawn inside ImGui child windows, whose draw
        // lists otherwise composite over the parent window's list.
        var drawList = ImGui.GetForegroundDrawList();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        // 1) Backdrop — palette §4 modal dim.
        var backdropStart = winPos;
        var backdropEnd = new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y);
        var backdropColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay);
        drawList.AddRectFilled(backdropStart, backdropEnd, backdropColor);

        // 2) Dialog box
        var padding = 16f;
        var titleSize = ImGui.CalcTextSize(title);

        // Compute an adaptive dialog width when caller passes a non-positive width or when the default looks too wide.
        // We consider: title width, message unwrapped width, and total buttons width, then clamp to sensible bounds and window size.
        // Button widths size to the longer of their labels so "Yes, Declare War"
        // and friends don't crowd the text against the border.
        const float btnMinWidth = 120f;
        const float btnHorizontalPadding = 28f;
        var confirmTextWidth = ImGui.CalcTextSize(confirmLabel).X;
        var cancelTextWidth = ImGui.CalcTextSize(cancelLabel).X;
        var confirmBtnW = MathF.Max(btnMinWidth, confirmTextWidth + btnHorizontalPadding);
        var cancelBtnW = MathF.Max(btnMinWidth, cancelTextWidth + btnHorizontalPadding);
        var btnH = 36f;
        var btnSpacing = 10f;
        var totalButtonsWidth = confirmBtnW + cancelBtnW + btnSpacing;

        var unwrappedMsgWidth = ImGui.CalcTextSize(message).X;
        var minWidth = 420f;
        var maxWidth = Math.Min(640f, winSize.X - 80f); // keep nice margins from window edges

        // Wrap long messages at a comfortable reading measure instead of letting the unwrapped
        // single-line width balloon the box to maxWidth (which left dead space on the right).
        // Short messages still hug their own width; title and buttons remain hard floors.
        const float preferredMessageWidth = 380f;

        var effectiveDialogWidth = dialogWidth;
        if (dialogWidth <= 0f || dialogWidth == 520f) // treat default as auto-size candidate
        {
            var contentTarget = Math.Max(titleSize.X, Math.Min(unwrappedMsgWidth, preferredMessageWidth));
            effectiveDialogWidth = Math.Clamp(contentTarget + padding * 2f, minWidth, maxWidth);

            // Ensure buttons fit comfortably
            var minForButtons = totalButtonsWidth + padding * 2f;
            if (effectiveDialogWidth < minForButtons)
                effectiveDialogWidth = Math.Clamp(minForButtons, minWidth, maxWidth);
        }

        // Now that we have the final width, compute wrapping and height
        var messageWidth = effectiveDialogWidth - padding * 2f;
        var wrappedMsgHeight = TextRenderer.MeasureWrappedHeight(message, messageWidth, 13f);

        // Vertical rhythm: title + divider + message + gap + buttons.
        const float dividerBandHeight = 6f + 16f;
        var contentHeight = FontSizes.PageTitle + dividerBandHeight + wrappedMsgHeight + 16f + btnH;
        var dialogHeight = contentHeight + padding * 2f;

        var dlgX = winPos.X + (winSize.X - effectiveDialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;
        var dlgStart = new Vector2(dlgX, dlgY);
        var dlgEnd = new Vector2(dlgX + effectiveDialogWidth, dlgY + dialogHeight);
        // Parchment mini-page with a faded-ink border, matching the role-edit
        // dialog (`ReligionRolesBrowseRenderer.DrawEditDialog`) so all dialog
        // overlays read as a smaller page laid atop the dimmed main page.
        var dlgBg = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        var dlgBorder = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
        drawList.AddRectFilled(dlgStart, dlgEnd, dlgBg, 6f);
        drawList.AddRect(dlgStart, dlgEnd, dlgBorder, 6f, ImDrawFlags.None, 1.5f);

        var curX = dlgX + padding;
        var curY = dlgY + padding;

        // Title — gold rubric on parchment, matching the role-edit dialog.
        TextRenderer.DrawLabel(drawList, title, curX, curY, FontSizes.PageTitle, ColorPalette.Gold);
        curY += FontSizes.PageTitle + 6f;
        ChromeRenderer.DrawDivider(drawList, curX, curY, messageWidth);
        curY += 16f;

        // Message — ink on parchment (palette §5).
        TextRenderer.DrawInfoText(drawList, message, curX, curY, messageWidth, 13f, ColorPalette.White);
        curY += wrappedMsgHeight;

        // Buttons
        var btnStartX = dlgX + (effectiveDialogWidth - totalButtonsWidth) / 2f;
        // Anchor buttons to the bottom padding so vertical spacing looks consistent regardless of message height
        var btnY = dlgEnd.Y - padding - btnH;

        if (ButtonRenderer.DrawButton(drawList, confirmLabel, btnStartX, btnY, confirmBtnW, btnH, true))
            confirmed = true;

        if (ButtonRenderer.DrawButton(drawList, cancelLabel,
                btnStartX + confirmBtnW + btnSpacing, btnY, cancelBtnW, btnH))
            canceled = true;

        // Esc cancels the modal. GuiDialog suppresses its own close-on-Esc while a modal
        // is blocking (see ProcessRender), so the key dismisses the overlay, not the dialog.
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            canceled = true;
    }
}