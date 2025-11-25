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
    /// <param name="manager">Blessing dialog state manager</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="onChangeReligionClicked">Callback when Change Religion button clicked</param>
    /// <param name="onManageReligionClicked">Callback when Manage Religion button clicked (if leader)</param>
    /// <param name="onLeaveReligionClicked">Callback when Leave Religion button clicked</param>
    /// <param name="onManageCivilizationClicked">Callback when Manage Civilization button clicked (if civ founder)</param>
    /// <returns>Height used by this renderer</returns>
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width,
        Action? onChangeReligionClicked = null,
        Action? onManageReligionClicked = null,
        Action? onLeaveReligionClicked = null,
        Action? onManageCivilizationClicked = null)
    {
        // Dynamic height: base + civilization section if in civ
        const float baseHeaderHeight = 130f;
        const float civSectionHeight = 70f; // Reduced to match DrawCivilizationSection
        float headerHeight = baseHeaderHeight + (manager.HasCivilization() ? civSectionHeight : 0f);
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
        currentX = x + iconSize + padding * 2;
        var progressY = y + 54f;
        const float progressBarWidth = 300f;
        const float progressBarHeight = 20f;
        const float progressBarSpacing = 28f;

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

        // === CIVILIZATION SECTION ===
        if (manager.HasCivilization())
        {
            DrawCivilizationSection(manager, api, x, y + baseHeaderHeight, width, onManageCivilizationClicked);
        }

        return headerHeight;
    }

    /// <summary>
    ///     Draw civilization section below religion header
    /// </summary>
    private static void DrawCivilizationSection(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width,
        Action? onManageCivilizationClicked)
    {
        const float sectionHeight = 70f; // Reduced from 80f
        const float padding = 12f; // Reduced from 16f
        const float topPadding = 8f; // Smaller top padding

        var drawList = ImGui.GetWindowDrawList();
        var startPos = new Vector2(x, y);
        var endPos = new Vector2(x + width, y + sectionHeight);

        // Draw separator line above civ section with smaller offset
        var separatorColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.3f);
        drawList.AddLine(new Vector2(x + padding, y + 2f), new Vector2(x + width - padding, y + 2f), separatorColor, 1f);

        var currentX = x + padding;
        var currentY = y + topPadding;

        // Civilization icon/badge (simple shield icon or colored circle)
        const float iconSize = 32f;
        var iconCenter = new Vector2(currentX + iconSize / 2, currentY + iconSize / 2);
        var iconColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.6f, 0.8f, 1f)); // Cyan-blue for civ
        drawList.AddCircleFilled(iconCenter, iconSize / 2, iconColor, 16);

        // Add inner circle for badge effect
        var innerColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.3f, 0.4f, 1f));
        drawList.AddCircleFilled(iconCenter, iconSize / 2 - 4f, innerColor, 16);

        currentX += iconSize + padding;

        // Civilization name
        var civName = manager.CurrentCivilizationName ?? "Unknown Civilization";
        var civNameText = $"Civilization: {civName}";
        var civNamePos = new Vector2(currentX, currentY);
        var civNameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.8f, 1f, 1f)); // Light cyan
        drawList.AddText(ImGui.GetFont(), 15f, civNamePos, civNameColor, civNameText); // Slightly smaller font

        currentY += 20f; // Reduced spacing

        // Member religions with deity colors
        var memberCount = manager.CivilizationMemberReligions.Count;
        var memberText = $"{memberCount}/4 Religions: ";
        var memberPos = new Vector2(currentX, currentY);
        var memberColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 12f, memberPos, memberColor, memberText); // Smaller font

        // Draw deity color indicators for each member religion
        var textSize = ImGui.CalcTextSize(memberText);
        var deityIconX = currentX + textSize.X + 4f;
        const float deityIconSize = 16f;
        const float deityIconSpacing = 4f;

        foreach (var memberReligion in manager.CivilizationMemberReligions)
        {
            // Parse deity type from string
            if (Enum.TryParse<DeityType>(memberReligion.Deity, out var deityType))
            {
                var deityColor = DeityHelper.GetDeityColor(deityType);
                var deityColorU32 = ImGui.ColorConvertFloat4ToU32(deityColor);
                var deityIconPos = new Vector2(deityIconX, currentY);

                // Draw small colored square for deity
                drawList.AddRectFilled(
                    deityIconPos,
                    new Vector2(deityIconPos.X + deityIconSize, deityIconPos.Y + deityIconSize),
                    deityColorU32,
                    2f
                );

                // Add border
                var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
                drawList.AddRect(
                    deityIconPos,
                    new Vector2(deityIconPos.X + deityIconSize, deityIconPos.Y + deityIconSize),
                    borderColor,
                    2f,
                    ImDrawFlags.None,
                    1f
                );

                deityIconX += deityIconSize + deityIconSpacing;
            }
        }

        // Founder badge (if player is founder) - on same line as member count
        if (manager.IsCivilizationFounder)
        {
            var founderText = "=== Founder ===";
            var founderX = deityIconX + (deityIconSize + deityIconSpacing) * memberCount + 8f;
            var founderPos = new Vector2(founderX, currentY);
            var founderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddText(ImGui.GetFont(), 11f, founderPos, founderColor, founderText);
        }

        // "Manage Civilization" button (only if founder)
        if (onManageCivilizationClicked != null && manager.IsCivilizationFounder)
        {
            const float buttonWidth = 160f;
            const float buttonHeight = 28f;
            var buttonX = x + width - padding - buttonWidth;
            var buttonY = y + (sectionHeight - buttonHeight) / 2;

            if (DrawButton("Manage Civilization", buttonX, buttonY, buttonWidth, buttonHeight,
                new Vector4(0.3f, 0.5f, 0.7f, 1.0f)))
            {
                onManageCivilizationClicked.Invoke();
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
            }
        }
    }

    /// <summary>
    ///     Get display name for a deity
    /// </summary>
    private static string GetDeityDisplayName(DeityType deity)
    {
        return deity switch
        {
            DeityType.Khoras => "Khoras - God of War",
            DeityType.Lysa => "Lysa - Goddess of the Hunt",
            DeityType.Morthen => "Morthen - God of Death",
            DeityType.Aethra => "Aethra - Goddess of Light",
            DeityType.Umbros => "Umbros - God of Shadows",
            DeityType.Tharos => "Tharos - God of Storms",
            DeityType.Gaia => "Gaia - Goddess of Earth",
            DeityType.Vex => "Vex - God of Madness",
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