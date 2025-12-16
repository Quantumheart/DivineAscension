using System;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Models.Religion.Header;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;

namespace PantheonWars.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders the religion/deity header banner at the top of the blessing dialog
///     Shows: Religion name, deity icon/name, favor/prestige ranks
/// </summary>
internal static class ReligionHeaderRenderer
{
    private const string NoReligionJoinOrCreateOneToUnlockBlessings =
        "No Religion - Join or create one to unlock blessings!";

    /// <summary>
    /// Draw a religion header.
    /// </summary>
    public static float Draw(ReligionHeaderViewModel viewModel)
    {
        // Two-column header: fixed height, no extra section below
        const float baseHeaderHeight = 130f;
        var headerHeight = baseHeaderHeight;
        const float padding = 16f;

        var drawList = ImGui.GetWindowDrawList();
        var startPos = new Vector2(viewModel.X, viewModel.Y);
        var endPos = new Vector2(viewModel.X + viewModel.Width, viewModel.Y + headerHeight);

        // Draw header background
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(startPos, endPos, bgColor, 4f); // Rounded corners

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRect(startPos, endPos, borderColor, 4f, ImDrawFlags.None, 2f);

        // Check if player has a religion
        if (!viewModel.HasReligion)
        {
            // Display "No Religion" message
            var noReligionText = NoReligionJoinOrCreateOneToUnlockBlessings;
            var textSize = ImGui.CalcTextSize(noReligionText);
            var textPos = new Vector2(
                viewModel.X + (viewModel.Width - textSize.X) / 2,
                viewModel.Y + (headerHeight - textSize.Y) / 2 - 10f
            );

            var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 16f, textPos, textColor, noReligionText);

            return headerHeight;
        }

        // Layout calculations (support two columns when civilization exists)
        var innerX = viewModel.X + padding;
        var innerWidth = viewModel.Width - padding * 2f;
        // Show two columns if the game reports a civilization OR if we have any civ metadata to show
        var twoColumns = viewModel.HasCivilization
                         || !string.IsNullOrEmpty(viewModel.CurrentCivilizationName)
                         || (viewModel.CivilizationMemberReligions?.Count ?? 0) > 0;
        var columnSpacing = twoColumns ? padding : 0f;
        var colWidth = twoColumns ? (innerWidth - columnSpacing) / 2f : innerWidth;

        // Religion info available - draw detailed header (left column)
        var currentX = innerX; // column 1 start
        var centerY = viewModel.Y + headerHeight / 2;

        // Draw deity icon (with fallback to colored circle)
        const float iconSize = 48f;
        var deityTextureId = DeityIconLoader.GetDeityTextureId(viewModel.CurrentDeity);

        if (deityTextureId != IntPtr.Zero)
        {
            // Render deity icon texture
            var iconPos = new Vector2(currentX, centerY - iconSize / 2);
            var iconMin = iconPos;
            var iconMax = new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize);

            // Draw icon with deity color tint for visual cohesion
            var tintColor = DeityHelper.GetDeityColor(viewModel.CurrentDeity);
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
            var iconColor = ImGui.ColorConvertFloat4ToU32(DeityHelper.GetDeityColor(viewModel.CurrentDeity));
            drawList.AddCircleFilled(iconCenter, iconSize / 2, iconColor, 16);
        }

        currentX += iconSize + padding;

        // Religion name and deity
        var religionName = viewModel.CurrentReligionName ?? "Unknown Religion";
        var deityName = GetDeityDisplayName(viewModel.CurrentDeity);
        var headerText = $"{religionName} - {deityName}";

        var headerTextPos = new Vector2(currentX, viewModel.Y + 12f);
        var headerTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 18f, headerTextPos, headerTextColor, headerText);

        // Member count and role
        var memberInfo = viewModel.ReligionMemberCount > 0
            ? $"{viewModel.ReligionMemberCount} member{(viewModel.ReligionMemberCount == 1 ? "" : "s")}"
            : "No members";
        var roleInfo = !string.IsNullOrEmpty(viewModel.PlayerRoleInReligion)
            ? $" | {viewModel.PlayerRoleInReligion}"
            : "";
        var infoText = $"{memberInfo}{roleInfo}";
        var infoTextPos = new Vector2(currentX, viewModel.Y + 35f);
        var infoTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, infoTextPos, infoTextColor, infoText);

        // Progress bars
        currentX = innerX + iconSize + padding;
        var progressY = viewModel.Y + 54f;
        // Keep bars readable but not oversized: clamp width and reduce height
        var progressBarWidth = MathF.Min(380f, MathF.Max(160f, colWidth - 140f));
        const float progressBarHeight = 14f;
        const float progressBarSpacing = 22f;

        // Player Favor Progress
        var favorProgress = viewModel.PlayerFavorProgress;
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
            favorProgress.ProgressPercentage > 0.8f
        );

        progressY += progressBarSpacing;

        // Religion Prestige Progress
        var prestigeProgress = viewModel.ReligionPrestigeProgress;
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
            prestigeProgress.ProgressPercentage > 0.8f
        );

        // === CIVILIZATION COLUMN (right side when available) ===
        if (twoColumns)
        {
            var col2X = innerX + colWidth + columnSpacing; // right column start

            // Vertical separator between columns
            var separatorX = col2X - columnSpacing / 2f;
            var sepColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.3f);
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(separatorX, viewModel.Y + 8f),
                new Vector2(separatorX, viewModel.Y + headerHeight - 8f),
                sepColor,
                1f);

            // Civilization icon/badge
            var civCurrentX = col2X;
            var civCurrentY = viewModel.Y + 12f; // small top padding
            const float civIconSize = 32f;

            // Load and render civilization icon texture
            var civTextureId = CivilizationIconLoader.GetIconTextureId(viewModel.CivilizationIcon ?? "default");
            var iconPos = new Vector2(civCurrentX, civCurrentY);
            var iconMin = iconPos;
            var iconMax = new Vector2(iconPos.X + civIconSize, iconPos.Y + civIconSize);

            // Render icon (white tint = no tint)
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(civTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Add subtle border for visual consistency
            var civBorderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1f));
            drawList.AddRect(iconMin, iconMax, civBorderColor, 4f, ImDrawFlags.None, 2f);

            civCurrentX += civIconSize + 10f;

            // Civilization name
            var civName = viewModel.CurrentCivilizationName ?? "Unknown Civilization";
            var civNameText = $"Civilization: {civName}";
            var civNamePos = new Vector2(civCurrentX, civCurrentY);
            var civNameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.8f, 1f, 1f));
            drawList.AddText(ImGui.GetFont(), 15f, civNamePos, civNameColor, civNameText);

            civCurrentY += 22f;

            // Member religions with deity colors
            var memberCount = viewModel.CivilizationMemberReligions?.Count;
            var memberText = $"{memberCount}/4 Religions: ";
            var memberPos = new Vector2(civCurrentX, civCurrentY);
            var memberColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 12f, memberPos, memberColor, memberText);

            var textSize = ImGui.CalcTextSize(memberText);
            var deityIconX = civCurrentX + textSize.X + 4f;
            const float deityIconSize = 16f;
            const float deityIconSpacing = 4f;
            foreach (var memberReligion in viewModel.CivilizationMemberReligions!)
                if (Enum.TryParse<DeityType>(memberReligion.Deity, out var deityType))
                {
                    var memberDeityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
                    var deityIconPos = new Vector2(deityIconX, civCurrentY);
                    drawList.AddImage(memberDeityTextureId,
                        deityIconPos,
                        new Vector2(deityIconPos.X + deityIconSize, deityIconPos.Y + deityIconSize),
                        Vector2.Zero, Vector2.One,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
                    deityIconX += deityIconSize + deityIconSpacing;
                }

            // Founder badge
            if (viewModel.IsCivilizationFounder)
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
}