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
    ///     Draw the religion header banner (current)
    /// </summary>
    /// <param name="manager">Blessing dialog state manager</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <returns>Height used by this renderer</returns>
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width)
    {
        // Two-column header: fixed height, no extra section below
        const float baseHeaderHeight = 130f;
        float headerHeight = baseHeaderHeight;
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

            // Previously, a "Browse Religions" button was rendered here. It has been removed.

            return headerHeight;
        }

        // Layout calculations (support two columns when civilization exists)
        var innerX = x + padding;
        var innerY = y + 0f;
        var innerWidth = width - padding * 2f;
        // Show two columns if the game reports a civilization OR if we have any civ metadata to show
        var twoColumns = manager.HasCivilization()
                          || !string.IsNullOrEmpty(manager.CurrentCivilizationName)
                          || (manager.CivilizationMemberReligions?.Count ?? 0) > 0;
        var columnSpacing = twoColumns ? padding : 0f;
        var colWidth = twoColumns ? (innerWidth - columnSpacing) / 2f : innerWidth;

        // Religion info available - draw detailed header (left column)
        var currentX = innerX; // column 1 start
        var centerY = y + headerHeight / 2;

        // Draw deity icon (with fallback to colored circle)
        const float iconSize = 48f;
        var deityTextureId = DeityIconLoader.GetDeityTextureId(manager.CurrentDeity);

        if (deityTextureId != IntPtr.Zero)
        {
            // Render deity icon texture
            var iconPos = new Vector2(currentX, centerY - iconSize / 2);
            var iconMin = iconPos;
            var iconMax = new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize);

            // Draw icon with deity color tint for visual cohesion
            var tintColor = DeityHelper.GetDeityColor(manager.CurrentDeity);
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)); // Full white = no tint

            drawList.AddImage(deityTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Optional: Add subtle border around icon
            var iconBorderColor = ImGui.ColorConvertFloat4ToU32(tintColor * 0.8f);
            drawList.AddRect(iconMin, iconMax, iconBorderColor, 4f, ImDrawFlags.None, 2f);
        }
        else
        {
            // Fallback: Use placeholder colored circle if texture not available
            var iconCenter = new Vector2(currentX + iconSize / 2, centerY);
            var iconColor = ImGui.ColorConvertFloat4ToU32(DeityHelper.GetDeityColor(manager.CurrentDeity));
            drawList.AddCircleFilled(iconCenter, iconSize / 2, iconColor, 16);
        }

        currentX += iconSize + padding;

        // Religion name and deity
        var religionName = manager.CurrentReligionName ?? "Unknown Religion";
        var deityName = GetDeityDisplayName(manager.CurrentDeity);
        var headerText = $"{religionName} - {deityName}";

        var headerTextPos = new Vector2(currentX, y + 12f);
        var headerTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 18f, headerTextPos, headerTextColor, headerText);

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

        // Progress bars
        currentX = innerX + iconSize + padding;
        var progressY = y + 54f;
        // Keep bars readable but not oversized: clamp width and reduce height
        var progressBarWidth = MathF.Min(380f, MathF.Max(160f, colWidth - 140f));
        const float progressBarHeight = 14f;
        const float progressBarSpacing = 22f;

        // Player Favor Progress
        var favorProgress = manager.GetPlayerFavorProgress();
        var favorLabel = favorProgress.IsMaxRank
            ? $"{RankRequirements.GetFavorRankName(favorProgress.CurrentRank)} (MAX)"
            : $"{RankRequirements.GetFavorRankName(favorProgress.CurrentRank)} ({favorProgress.CurrentFavor}/{favorProgress.RequiredFavor})";

        // Label
        var favorLabelPos = new Vector2(currentX, progressY);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), 12f, favorLabelPos, labelColor, "Player Progress:");

        // Progress bar
        ProgressBarRenderer.DrawProgressBar(
            drawList,
            currentX + 110f, progressY - 2f, progressBarWidth, progressBarHeight,
            favorProgress.ProgressPercentage,
            ColorPalette.Gold,
            ColorPalette.DarkBrown,
            favorLabel,
            showGlow: favorProgress.ProgressPercentage > 0.8f
        );

        progressY += progressBarSpacing;

        // Religion Prestige Progress
        var prestigeProgress = manager.GetReligionPrestigeProgress();
        var prestigeLabel = prestigeProgress.IsMaxRank
            ? $"{RankRequirements.GetPrestigeRankName(prestigeProgress.CurrentRank)} (MAX)"
            : $"{RankRequirements.GetPrestigeRankName(prestigeProgress.CurrentRank)} ({prestigeProgress.CurrentPrestige}/{prestigeProgress.RequiredPrestige})";

        // Label
        var prestigeLabelPos = new Vector2(currentX, progressY);
        drawList.AddText(ImGui.GetFont(), 12f, prestigeLabelPos, labelColor, "Religion Progress:");

        // Progress bar (purple color)
        ProgressBarRenderer.DrawProgressBar(
            drawList,
            currentX + 110f, progressY - 2f, progressBarWidth, progressBarHeight,
            prestigeProgress.ProgressPercentage,
            new Vector4(0.48f, 0.41f, 0.93f, 1f), // Purple
            ColorPalette.DarkBrown,
            prestigeLabel,
            showGlow: prestigeProgress.ProgressPercentage > 0.8f
        );
        
        // === CIVILIZATION COLUMN (right side when available) ===
        if (twoColumns)
        {
            var col2X = innerX + colWidth + columnSpacing; // right column start

            // Vertical separator between columns
            var separatorX = col2X - columnSpacing / 2f;
            var sepColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.3f);
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(separatorX, y + 8f),
                new Vector2(separatorX, y + headerHeight - 8f),
                sepColor,
                1f);

            // Civilization icon/badge
            float civCurrentX = col2X;
            float civCurrentY = y + 12f; // small top padding
            const float civIconSize = 32f;
            var civIconCenter = new Vector2(civCurrentX + civIconSize / 2f, civCurrentY + civIconSize / 2f);
            var civOuter = ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.6f, 0.8f, 1f));
            var civInner = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.3f, 0.4f, 1f));
            drawList.AddCircleFilled(civIconCenter, civIconSize / 2f, civOuter, 16);
            drawList.AddCircleFilled(civIconCenter, civIconSize / 2f - 4f, civInner, 16);

            civCurrentX += civIconSize + 10f;

            // Civilization name
            var civName = manager.CurrentCivilizationName ?? "Unknown Civilization";
            var civNameText = $"Civilization: {civName}";
            var civNamePos = new Vector2(civCurrentX, civCurrentY);
            var civNameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.8f, 1f, 1f));
            drawList.AddText(ImGui.GetFont(), 15f, civNamePos, civNameColor, civNameText);

            civCurrentY += 22f;

            // Member religions with deity colors
            var memberCount = manager.CivilizationMemberReligions.Count;
            var memberText = $"{memberCount}/4 Religions: ";
            var memberPos = new Vector2(civCurrentX, civCurrentY);
            var memberColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 12f, memberPos, memberColor, memberText);

            var textSize = ImGui.CalcTextSize(memberText);
            var deityIconX = civCurrentX + textSize.X + 4f;
            const float deityIconSize = 16f;
            const float deityIconSpacing = 4f;
            foreach (var memberReligion in manager.CivilizationMemberReligions)
            {
                if (Enum.TryParse<DeityType>(memberReligion.Deity, out var deityType))
                {
                    var deityColor = DeityHelper.GetDeityColor(deityType);
                    var deityColorU32 = ImGui.ColorConvertFloat4ToU32(deityColor);
                    var deityIconPos = new Vector2(deityIconX, civCurrentY);
                    drawList.AddRectFilled(
                        deityIconPos,
                        new Vector2(deityIconPos.X + deityIconSize, deityIconPos.Y + deityIconSize),
                        deityColorU32,
                        2f
                    );
                    var deityBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
                    drawList.AddRect(
                        deityIconPos,
                        new Vector2(deityIconPos.X + deityIconSize, deityIconPos.Y + deityIconSize),
                        deityBorderColor,
                        2f,
                        ImDrawFlags.None,
                        1f
                    );
                    deityIconX += deityIconSize + deityIconSpacing;
                }
            }

            // Founder badge
            if (manager.IsCivilizationFounder)
            {
                var founderText = "=== Founder ===";
                var founderPos = new Vector2(deityIconX + 8f, civCurrentY);
                var founderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                drawList.AddText(ImGui.GetFont(), 11f, founderPos, founderColor, founderText);
            }
        }

        return headerHeight;
    }

    // Old below-header civilization section removed in favor of two-column layout

    /// <summary>
    ///     Get display name for a deity
    /// </summary>
    private static string GetDeityDisplayName(DeityType deity)
    {
        return deity switch
        {
            DeityType.Khoras => "Khoras - God of War",
            DeityType.Lysa => "Lysa - Goddess of the Hunt",
            DeityType.Aethra => "Aethra - Goddess of Light",
            DeityType.Gaia => "Gaia - Goddess of Earth",
            _ => "Unknown Deity"
        };
    }

    /// <summary>
    ///     Get favor rank name from rank number
    /// </summary>
    private static string GetFavorRankName(int rank)
    {
        return rank switch
        {
            0 => "Initiate",
            1 => "Devoted",
            2 => "Zealot",
            3 => "Champion",
            4 => "Exalted",
            _ => $"Rank {rank}"
        };
    }

    /// <summary>
    ///     Get prestige rank name from rank number
    /// </summary>
    private static string GetPrestigeRankName(int rank)
    {
        return rank switch
        {
            0 => "Fledgling",
            1 => "Established",
            2 => "Renowned",
            3 => "Legendary",
            4 => "Mythic",
            _ => $"Rank {rank}"
        };
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