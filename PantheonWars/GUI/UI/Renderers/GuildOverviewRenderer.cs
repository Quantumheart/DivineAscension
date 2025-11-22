using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Overview tab renderer for guild information
///     Displays guild description, statistics, and leave button
/// </summary>
[ExcludeFromCodeCoverage]
internal static class GuildOverviewRenderer
{
    /// <summary>
    ///     Draw the guild overview tab
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="height">Available height</param>
    /// <param name="state">Religion management state with guild info</param>
    /// <param name="onLeaveGuild">Callback when leave button clicked</param>
    public static void Draw(
        ICoreClientAPI api,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        ReligionManagementState state,
        Action onLeaveGuild)
    {
        const float padding = 20f;
        const float sectionSpacing = 24f;

        var currentY = y + padding;

        if (state.ReligionInfo == null)
        {
            // Loading state
            var loadingText = "Loading guild information...";
            var loadingSize = ImGui.CalcTextSize(loadingText);
            var loadingPos = new Vector2(x + (width - loadingSize.X) / 2, y + height / 2 - loadingSize.Y / 2);
            var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 16f, loadingPos, loadingColor, loadingText);
            return;
        }

        // === WELCOME SECTION ===
        var welcomeText = $"Welcome to {state.ReligionInfo.ReligionName}";
        var welcomeSize = ImGui.CalcTextSize(welcomeText);
        var welcomePos = new Vector2(x + padding, currentY);
        var welcomeColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 22f, welcomePos, welcomeColor, welcomeText);

        currentY += welcomeSize.Y + sectionSpacing;

        // === GUILD STATISTICS ===
        var statsLabelPos = new Vector2(x + padding, currentY);
        var statsLabelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), 18f, statsLabelPos, statsLabelColor, "Guild Information");

        currentY += ImGui.CalcTextSize("Guild Information").Y + 12f;

        // Member count
        var memberCountText = $"Members: {state.ReligionInfo.Members.Count}";
        var memberCountPos = new Vector2(x + padding * 2, currentY);
        var memberCountColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 16f, memberCountPos, memberCountColor, memberCountText);

        currentY += ImGui.CalcTextSize(memberCountText).Y + 8f;

        // Visibility
        var visibilityText = $"Visibility: {(state.ReligionInfo.IsPublic ? "Public" : "Private")}";
        var visibilityPos = new Vector2(x + padding * 2, currentY);
        var visibilityColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 16f, visibilityPos, visibilityColor, visibilityText);

        currentY += ImGui.CalcTextSize(visibilityText).Y + sectionSpacing;

        // === GUILD DESCRIPTION ===
        var descLabelPos = new Vector2(x + padding, currentY);
        var descLabelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), 18f, descLabelPos, descLabelColor, "Description");

        currentY += ImGui.CalcTextSize("Description").Y + 12f;

        // Description panel
        var descPanelPadding = 16f;
        var descPanelHeight = 150f;
        var descPanelStart = new Vector2(x + padding, currentY);
        var descPanelEnd = new Vector2(x + width - padding, currentY + descPanelHeight);
        var descPanelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(descPanelStart, descPanelEnd, descPanelColor, 4f);

        // Description border
        var descBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown);
        drawList.AddRect(descPanelStart, descPanelEnd, descBorderColor, 4f, ImDrawFlags.None, 1f);

        // Description text
        var description = !string.IsNullOrWhiteSpace(state.ReligionInfo.Description)
            ? state.ReligionInfo.Description
            : "No description provided.";

        var descTextPos = new Vector2(descPanelStart.X + descPanelPadding, descPanelStart.Y + descPanelPadding);
        var descTextColor = ImGui.ColorConvertFloat4ToU32(
            !string.IsNullOrWhiteSpace(state.ReligionInfo.Description) ? ColorPalette.White : ColorPalette.Grey);

        // Word wrap description
        var descWidth = (descPanelEnd.X - descPanelStart.X) - descPanelPadding * 2;
        DrawWrappedText(drawList, description, descTextPos.X, descTextPos.Y, descWidth, descTextColor, 16f);

        currentY += descPanelHeight + sectionSpacing * 2;

        // === LEAVE GUILD BUTTON ===
        const float buttonWidth = 200f;
        const float buttonHeight = 40f;
        var leaveButtonX = x + (width - buttonWidth) / 2;
        var leaveButtonY = y + height - padding - buttonHeight;

        if (ButtonRenderer.DrawButton(drawList, "Leave Guild", leaveButtonX, leaveButtonY, buttonWidth, buttonHeight, isPrimary: false, enabled: true, customColor: ColorPalette.Red))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onLeaveGuild.Invoke();
        }

        // Helper text
        var helperText = "Note: Leaders cannot leave until they transfer leadership or disband the guild.";
        if (state.ReligionInfo.IsFounder)
        {
            var helperSize = ImGui.CalcTextSize(helperText);
            var helperPos = new Vector2(x + (width - helperSize.X) / 2, leaveButtonY - 30f);
            var helperColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Yellow);
            drawList.AddText(ImGui.GetFont(), 14f, helperPos, helperColor, helperText);
        }
    }

    /// <summary>
    ///     Draw text with word wrapping
    /// </summary>
    private static void DrawWrappedText(ImDrawListPtr drawList, string text, float x, float y, float maxWidth, uint color, float fontSize)
    {
        var font = ImGui.GetFont();
        var words = text.Split(' ');
        var currentX = x;
        var currentY = y;
        var spaceWidth = ImGui.CalcTextSize(" ").X;
        var lineHeight = fontSize + 4f;

        foreach (var word in words)
        {
            // Skip empty words (from multiple consecutive spaces)
            if (string.IsNullOrEmpty(word)) continue;

            var wordSize = ImGui.CalcTextSize(word);

            // Check if word fits on current line
            if (currentX + wordSize.X > x + maxWidth && currentX > x)
            {
                // Move to next line
                currentX = x;
                currentY += lineHeight;
            }

            // Draw word
            drawList.AddText(font, fontSize, new Vector2(currentX, currentY), color, word);
            currentX += wordSize.X + spaceWidth;
        }
    }
}
