using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for religion info header and stats grid
/// Displays: Religion name, deity, member count, founder, prestige
/// </summary>
internal static class ReligionInfoHeaderRenderer
{
    /// <summary>
    /// Renders the header section with religion name and info grid
    /// Returns the updated Y position after rendering
    /// </summary>
    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width)
    {
        var currentY = y;

        // === RELIGION HEADER ===
        TextRenderer.DrawLabel(drawList, viewModel.ReligionName, x, currentY, 20f, ColorPalette.Gold);
        currentY += 32f;

        // Info grid
        var leftCol = x;
        var rightCol = x + width / 2f;

        // Deity
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
            leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.Deity);

        // Member count
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
            rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.MemberCount.ToString());

        currentY += 22f;

        // Founder
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_FOUNDER_LABEL),
            leftCol, currentY, 13f, ColorPalette.Grey);
        var founderName = viewModel.GetFounderDisplayName();
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), founderName);

        // Prestige
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_LABEL),
            rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_VALUE,
                viewModel.Prestige, viewModel.PrestigeRank));

        currentY += 28f;

        return currentY;
    }
}