using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Confirmation overlay for leaving a religion
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This overlay has been migrated to ConfirmOverlay in the BlessingDialog tab system with inline confirmation in ReligionMyReligionRenderer. Use BlessingDialog's Religion tab instead.")]
internal static class LeaveReligionConfirmOverlay
{
    /// <summary>
    ///     Draw the leave religion confirmation overlay
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Parent window width</param>
    /// <param name="windowHeight">Parent window height</param>
    /// <param name="religionName">Name of the religion being left</param>
    /// <param name="onCancel">Callback when cancel clicked</param>
    /// <param name="onConfirm">Callback when confirm clicked</param>
    /// <returns>True if overlay should remain open</returns>
    public static bool Draw(
        ICoreClientAPI api,
        int windowWidth,
        int windowHeight,
        string religionName,
        Action onCancel,
        Action onConfirm)
    {
        const float overlayWidth = 450f;
        const float overlayHeight = 280f; // Increased from 250f to prevent button/text overlap
        const float padding = 20f;

        var windowPos = ImGui.GetWindowPos();
        var overlayX = windowPos.X + (windowWidth - overlayWidth) / 2;
        var overlayY = windowPos.Y + (windowHeight - overlayHeight) / 2;

        var drawList = ImGui.GetForegroundDrawList();

        // Draw semi-transparent background
        var bgStart = windowPos;
        var bgEnd = new Vector2(windowPos.X + windowWidth, windowPos.Y + windowHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f));
        drawList.AddRectFilled(bgStart, bgEnd, bgColor);

        // Draw main panel
        var panelStart = new Vector2(overlayX, overlayY);
        var panelEnd = new Vector2(overlayX + overlayWidth, overlayY + overlayHeight);
        var panelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(panelStart, panelEnd, panelColor, 8f);

        // Draw border (red to indicate warning)
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddRect(panelStart, panelEnd, borderColor, 8f, ImDrawFlags.None, 3f);

        var currentY = overlayY + padding;

        // === HEADER ===
        var headerText = "Leave Religion?";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(overlayX + (overlayWidth - headerSize.X) / 2, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(ImGui.GetFont(), 20f, headerPos, headerColor, headerText);

        currentY += headerSize.Y + padding * 1.5f;

        // === WARNING MESSAGE ===
        var warningLines = new[]
        {
            $"Are you sure you want to leave '{religionName}'?",
            "",
            "You will lose:",
            "  - Access to all deity blessings",
            "  - Your rank and position in the religion",
            "  - Connection with fellow members",
            "",
            "This action cannot be easily undone."
        };

        foreach (var line in warningLines)
        {
            if (string.IsNullOrEmpty(line))
            {
                currentY += 8f;
                continue;
            }

            var lineSize = ImGui.CalcTextSize(line);
            var linePos = new Vector2(overlayX + padding, currentY);
            var lineColor = line.StartsWith("  -")
                ? ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey)
                : ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
            drawList.AddText(ImGui.GetFont(), 13f, linePos, lineColor, line);
            currentY += lineSize.Y + 4f;
        }

        // === BUTTONS ===
        const float buttonWidth = 120f;
        const float buttonHeight = 36f;
        const float buttonSpacing = 20f;
        var buttonY = overlayY + overlayHeight - padding - buttonHeight;
        var buttonsStartX = overlayX + (overlayWidth - buttonWidth * 2 - buttonSpacing) / 2;

        // Cancel button
        var cancelButtonX = buttonsStartX;
        if (ButtonRenderer.DrawButton(drawList, "Cancel", cancelButtonX, buttonY, buttonWidth, buttonHeight, true))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onCancel.Invoke();
            return false;
        }

        // Leave button (warning colored)
        var leaveButtonX = buttonsStartX + buttonWidth + buttonSpacing;
        if (ButtonRenderer.DrawButton(drawList, "Leave", leaveButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: false, enabled: true, customColor: ColorPalette.Red))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onConfirm.Invoke();
            return false;
        }

        return true; // Keep overlay open
    }
}
