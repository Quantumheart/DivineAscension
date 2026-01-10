using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Activity;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Placeholder renderer for religion activity feed
///     Phase 1: Shows "coming soon" message
///     Future: Display recent activity events (joins, leaves, invites, kicks, bans, prestige milestones)
/// </summary>
internal static class ReligionActivityRenderer
{
    public static ReligionActivityRenderResult Draw(ReligionActivityViewModel viewModel)
    {
        var drawList = ImGui.GetWindowDrawList();
        var currentY = viewModel.Y + viewModel.Height / 3f; // Center vertically

        // Background box for the placeholder message
        var boxPadding = 40f;
        var boxX = viewModel.X + boxPadding;
        var boxWidth = viewModel.Width - boxPadding * 2;
        var boxHeight = 120f;
        var boxY = currentY;

        // Draw styled info box
        drawList.AddRectFilled(
            new Vector2(boxX, boxY),
            new Vector2(boxX + boxWidth, boxY + boxHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.6f),
            8f
        );

        // Border
        drawList.AddRect(
            new Vector2(boxX, boxY),
            new Vector2(boxX + boxWidth, boxY + boxHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f),
            8f,
            ImDrawFlags.None,
            2f
        );

        // Icon (simple placeholder circle with "i")
        var iconRadius = 20f;
        var iconX = boxX + 30f;
        var iconY = boxY + boxHeight / 2f;
        drawList.AddCircleFilled(
            new Vector2(iconX, iconY),
            iconRadius,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f)
        );

        var iconTextPos = new Vector2(iconX - 5f, iconY - 10f);
        drawList.AddText(ImGui.GetFont(), 20f, iconTextPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), "i");

        // Message title
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_TITLE);
        var titleY = boxY + 20f;
        var titlePos = new Vector2(iconX + iconRadius + 20f, titleY);
        drawList.AddText(ImGui.GetFont(), 16f, titlePos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), titleText);

        // Description
        var descText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_DESCRIPTION);
        var descY = titleY + 25f;
        var descPos = new Vector2(iconX + iconRadius + 20f, descY);
        drawList.AddText(ImGui.GetFont(), 13f, descPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), descText);

        // TODO comment
        var todoText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_TODO);
        var todoY = descY + 25f;
        var todoPos = new Vector2(iconX + iconRadius + 20f, todoY);
        drawList.AddText(ImGui.GetFont(), 11f, todoPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.7f), todoText);

        return new ReligionActivityRenderResult();
    }
}