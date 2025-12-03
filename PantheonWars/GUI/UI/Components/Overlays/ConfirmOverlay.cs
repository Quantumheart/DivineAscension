using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Components.Overlays;

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
        string confirmLabel = "Confirm",
        string cancelLabel = "Cancel",
        float dialogWidth = 520f)
    {
        confirmed = false;
        canceled = false;

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
        var msgSize = ImGui.CalcTextSize(message);
        var contentHeight = titleSize.Y + 8f + msgSize.Y + 24f + 36f + 8f; // title + gap + message + gap + buttons + gap
        var dialogHeight = contentHeight + padding * 2f;

        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;
        var dlgStart = new Vector2(dlgX, dlgY);
        var dlgEnd = new Vector2(dlgX + dialogWidth, dlgY + dialogHeight);
        var dlgBg = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.95f);
        var dlgBorder = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
        drawList.AddRectFilled(dlgStart, dlgEnd, dlgBg, 6f);
        drawList.AddRect(dlgStart, dlgEnd, dlgBorder, 6f, ImDrawFlags.None, 1.5f);

        var curX = dlgX + padding;
        var curY = dlgY + padding;

        // Title
        TextRenderer.DrawLabel(drawList, title, curX, curY, 18f, ColorPalette.White);
        curY += titleSize.Y + 8f;

        // Message (single-line assumed)
        drawList.AddText(new Vector2(curX, curY), ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), message);
        curY += msgSize.Y + 24f;

        // Buttons
        var btnW = 120f;
        var btnH = 36f;
        var spacing = 10f;
        var totalButtonsWidth = btnW * 2 + spacing;
        var btnStartX = dlgX + (dialogWidth - totalButtonsWidth) / 2f;
        var btnY = curY;

        if (ButtonRenderer.DrawButton(drawList, confirmLabel, btnStartX, btnY, btnW, btnH, isPrimary: true))
        {
            confirmed = true;
        }

        if (ButtonRenderer.DrawButton(drawList, cancelLabel, btnStartX + btnW + spacing, btnY, btnW, btnH))
        {
            canceled = true;
        }
    }
}
