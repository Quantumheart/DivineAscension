using System;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders the religion/deity header banner at the top of the blessing dialog
///     Shows: Religion name, deity icon/name, favor/prestige ranks
/// </summary>
internal static class ReligionHeaderRenderer
{
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
            var noReligionText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_NO_RELIGION);
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
        var religionName = viewModel.CurrentReligionName ??
                           LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_RELIGION);
        var deityName = GetDeityDisplayName(viewModel.CurrentDeity);
        var headerText = $"{religionName} - {deityName}";

        var headerTextPos = new Vector2(currentX, viewModel.Y + 12f);
        var headerTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 18f, headerTextPos, headerTextColor, headerText);

        // Member count and role
        var memberInfo = viewModel.ReligionMemberCount > 0
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_MEMBER_COUNT, viewModel.ReligionMemberCount,
                viewModel.ReligionMemberCount == 1 ? "" : "s")
            : LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_NO_MEMBERS);
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
        drawList.AddText(ImGui.GetFont(), 12f, favorLabelPos, labelColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_PLAYER_PROGRESS));

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
        drawList.AddText(ImGui.GetFont(), 12f, prestigeLabelPos, labelColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_RELIGION_PROGRESS));

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
            const float civIconSize = 48f; // Match deity icon size

            // Load and render civilization icon texture (centered vertically like deity icon)
            var civTextureId = CivilizationIconLoader.GetIconTextureId(viewModel.CivilizationIcon ?? "default");
            var civIconPos = new Vector2(civCurrentX, centerY - civIconSize / 2);
            var civIconMin = civIconPos;
            var civIconMax = new Vector2(civIconPos.X + civIconSize, civIconPos.Y + civIconSize);

            // Render icon (white tint = no tint)
            var civTintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(civTextureId, civIconMin, civIconMax, Vector2.Zero, Vector2.One, civTintColorU32);

            // Add subtle border for visual consistency
            var civBorderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1f));
            drawList.AddRect(civIconMin, civIconMax, civBorderColor, 4f, ImDrawFlags.None, 2f);

            civCurrentX += civIconSize + padding;

            // Civilization name (align with religion header position Y+12)
            var civName = viewModel.CurrentCivilizationName ??
                          LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_CIVILIZATION);
            var civNameText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CIVILIZATION, civName);
            var civNamePos = new Vector2(civCurrentX, viewModel.Y + 12f);
            var civNameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.8f, 1f, 1f));
            drawList.AddText(ImGui.GetFont(), 15f, civNamePos, civNameColor, civNameText);

            // Member religions with deity colors (align with religion info position Y+35)
            var civInfoY = viewModel.Y + 35f;
            var memberCount = viewModel.CivilizationMemberReligions?.Count ?? 0;
            var memberText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_RELIGIONS_COUNT,
                memberCount);
            var memberPos = new Vector2(civCurrentX, civInfoY);
            var memberColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 13f, memberPos, memberColor, memberText);

            // Add deity icons below member count (add visual balance)
            if (viewModel.CivilizationMemberReligions?.Count > 0)
            {
                var deityIconsY = viewModel.Y + 54f; // Align with progress bar position
                var deityIconX = civCurrentX;
                const float deityIconSize = 20f; // Slightly larger for better visibility
                const float deityIconSpacing = 6f;

                foreach (var memberReligion in viewModel.CivilizationMemberReligions!)
                    if (Enum.TryParse<DeityDomain>(memberReligion.Domain, out var deityType))
                    {
                        var memberDeityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
                        var deityIconPos = new Vector2(deityIconX, deityIconsY);
                        drawList.AddImage(memberDeityTextureId,
                            deityIconPos,
                            new Vector2(deityIconPos.X + deityIconSize, deityIconPos.Y + deityIconSize),
                            Vector2.Zero, Vector2.One,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
                        deityIconX += deityIconSize + deityIconSpacing;
                    }
            }

            // Founder badge (position below deity icons for visual balance)
            if (viewModel.IsCivilizationFounder)
            {
                var founderText = "*** Founder ***";
                var founderPos = new Vector2(civCurrentX, viewModel.Y + 82f); // Add spacing for visual balance
                var founderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                drawList.AddText(ImGui.GetFont(), 12f, founderPos, founderColor, founderText);
            }
        }

        return headerHeight;
    }

    // Old below-header civilization section removed in favor of two-column layout

    /// <summary>
    ///     Get display name for a deity with title
    /// </summary>
    private static string GetDeityDisplayName(DeityDomain deity)
    {
        return deity.ToLocalizedStringWithTitle();
    }
}