using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Models.Religion.Info;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Religion.Info;

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
        TextRenderer.DrawLabel(drawList, "Deity:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.Deity);

        // Member count
        TextRenderer.DrawLabel(drawList, "Members:", rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.MemberCount.ToString());

        currentY += 22f;

        // Founder
        TextRenderer.DrawLabel(drawList, "Founder:", leftCol, currentY, 13f, ColorPalette.Grey);
        var founderName = viewModel.GetFounderDisplayName();
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), founderName);

        // Prestige
        TextRenderer.DrawLabel(drawList, "Prestige:", rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            $"{viewModel.Prestige} (Rank {viewModel.PrestigeRank})");

        currentY += 28f;

        return currentY;
    }
}
