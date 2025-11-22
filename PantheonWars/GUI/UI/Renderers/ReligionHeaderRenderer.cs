using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Renders the religion/deity header banner at the top of the blessing dialog
///     Shows: Religion name, deity icon/name, favor/prestige ranks
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionHeaderRenderer
{
    /// <summary>
    ///     Draw the religion header banner
    /// </summary>
    /// <param name="manager">Guild dialog state manager</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="onChangeReligionClicked">Callback when Change Religion button clicked</param>
    /// <param name="onManageReligionClicked">Callback when Manage Religion button clicked (if leader)</param>
    /// <param name="onLeaveReligionClicked">Callback when Leave Religion button clicked</param>
    /// <returns>Height used by this renderer</returns>
    public static float Draw(
        GuildDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width,
        Action? onChangeReligionClicked = null,
        Action? onManageReligionClicked = null,
        Action? onLeaveReligionClicked = null)
    {
        const float headerHeight = 130f; // Increased to accommodate progress bars
        const float padding = 16f;

        var drawList = ImGui.GetWindowDrawList();
        var startPos = new Vector2(x, y);
        var endPos = new Vector2(x + width, y + headerHeight);

        // Draw header background
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(startPos, endPos, bgColor, 4f); // Rounded corners

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRect(startPos, endPos, borderColor, 4f, ImDrawFlags.None, 2f);

        // Check if player has a religion
        if (!manager.HasReligion())
        {
            // Display "No Religion" message
            var noReligionText = "No Religion - Join or create one to unlock blessings!";
            var textSize = ImGui.CalcTextSize(noReligionText);
            var textPos = new Vector2(
                x + (width - textSize.X) / 2,
                y + (headerHeight - textSize.Y) / 2 - 10f
            );

            var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 16f, textPos, textColor, noReligionText);

            // Add "Browse Religions" button
            if (onChangeReligionClicked != null)
            {
                const float browseButtonWidth = 160f;
                const float browseButtonHeight = 32f;
                var browseButtonX = x + (width - browseButtonWidth) / 2;
                var browseButtonY = y + (headerHeight - browseButtonHeight) / 2 + 20f;

                if (DrawButton("Browse Religions", browseButtonX, browseButtonY, browseButtonWidth, browseButtonHeight))
                {
                    onChangeReligionClicked.Invoke();
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                }
            }

            return headerHeight;
        }

        // Religion info available - draw detailed header
        var currentX = x + padding;

        // Religion name
        var religionName = manager.CurrentReligionName ?? "Unknown Religion";

        var headerTextPos = new Vector2(currentX, y + 12f);
        var headerTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 18f, headerTextPos, headerTextColor, religionName);

        // Member count and role
        var memberInfo = manager.ReligionMemberCount > 0
            ? $"{manager.ReligionMemberCount} member{(manager.ReligionMemberCount == 1 ? "" : "s")}"
            : "No members";
        var roleInfo = !string.IsNullOrEmpty(manager.PlayerRoleInReligion)
            ? $" | {manager.PlayerRoleInReligion}"
            : "";
        var infoText = $"{memberInfo}{roleInfo}";
        var infoTextPos = new Vector2(currentX, y + 35f);
        var infoTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, infoTextPos, infoTextColor, infoText);

        // Note: Progress bars removed as part of guild conversion (no more favor/prestige system)

        // Right-side buttons
        const float buttonWidth = 140f;
        const float buttonHeight = 28f;
        const float buttonSpacing = 8f;
        var buttonY = y + (headerHeight - buttonHeight) / 2;
        var buttonX = x + width - padding - buttonWidth;

        // "Leave Religion" button (always visible when in a religion)
        if (onLeaveReligionClicked != null)
        {
            if (DrawButton("Leave Religion", buttonX, buttonY, buttonWidth, buttonHeight, new Vector4(0.6f, 0.2f, 0.2f, 1.0f)))
            {
                onLeaveReligionClicked.Invoke();
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
            }

            buttonX -= buttonWidth + buttonSpacing;
        }

        // "Change Religion" button
        if (onChangeReligionClicked != null)
        {
            if (DrawButton("Change Religion", buttonX, buttonY, buttonWidth, buttonHeight))
            {
                onChangeReligionClicked.Invoke();
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
            }

            buttonX -= buttonWidth + buttonSpacing;
        }

        // "Manage Religion" button (only if leader)
        if (onManageReligionClicked != null && manager.PlayerRoleInReligion == "Leader")
        {
            if (DrawButton("Manage Religion", buttonX, buttonY, buttonWidth, buttonHeight))
            {
                onManageReligionClicked.Invoke();
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
            }
        }

        return headerHeight;
    }

    /// <summary>
    ///     Draw a simple button
    /// </summary>
    /// <param name="baseColor">Optional base color override (defaults to ColorPalette.DarkBrown)</param>
    /// <returns>True if clicked</returns>
    private static bool DrawButton(string text, float x, float y, float width, float height, Vector4? baseColor = null)
    {
        var drawList = ImGui.GetWindowDrawList();
        var buttonStart = new Vector2(x, y);
        var buttonEnd = new Vector2(x + width, y + height);

        var isHovering = IsMouseInRect(x, y, width, height);
        var actualBaseColor = baseColor ?? ColorPalette.DarkBrown;

        // Determine button color based on state
        Vector4 currentColor;
        if (isHovering && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            // Pressed state
            currentColor = actualBaseColor * 0.8f;
        }
        else if (isHovering)
        {
            // Hover state
            currentColor = actualBaseColor * 1.3f;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            // Normal state
            currentColor = actualBaseColor;
        }

        // Draw button background
        var bgColor = ImGui.ColorConvertFloat4ToU32(currentColor);
        drawList.AddRectFilled(buttonStart, buttonEnd, bgColor, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f);
        drawList.AddRect(buttonStart, buttonEnd, borderColor, 4f, ImDrawFlags.None, 1.5f);

        // Draw button text (centered)
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (height - textSize.Y) / 2
        );
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(textPos, textColor, text);

        // Handle click
        return isHovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }

    /// <summary>
    ///     Check if mouse is within a rectangle
    /// </summary>
    private static bool IsMouseInRect(float x, float y, float width, float height)
    {
        var mousePos = ImGui.GetMousePos();
        return mousePos.X >= x && mousePos.X <= x + width &&
               mousePos.Y >= y && mousePos.Y <= y + height;
    }
}