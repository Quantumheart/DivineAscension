using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

[ExcludeFromCodeCoverage]
internal static class BlessingInfoSectionHeader
{
    public static float Draw(BlessingNodeState selectedState, BlessingInfoViewModel vm, float currentY, float padding)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Title
        var titleColor = selectedState.IsUnlocked ? ColorPalette.Gold : ColorPalette.White;
        var titleColorU32 = ImGui.ColorConvertFloat4ToU32(titleColor);
        drawList.AddText(ImGui.GetFont(), PageTitle, new Vector2(vm.X + padding, currentY), titleColorU32,
            selectedState.Blessing.Name);
        currentY += 28f;

        // Status badge
        var statusText = selectedState.VisualState switch
        {
            BlessingNodeVisualState.Unlocked => "[UNLOCKED]",
            BlessingNodeVisualState.Unlockable => "[AVAILABLE]",
            _ => "[LOCKED]"
        };

        var statusColor = selectedState.VisualState switch
        {
            BlessingNodeVisualState.Unlocked => ColorPalette.Gold,
            BlessingNodeVisualState.Unlockable => ColorPalette.Green,
            _ => ColorPalette.Red
        };

        var statusColorU32 = ImGui.ColorConvertFloat4ToU32(statusColor);
        drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(vm.X + padding, currentY), statusColorU32, statusText);
        currentY += 20f;

        // Category and kind
        var metaText =
            $"{selectedState.Blessing.Category} | {selectedState.Blessing.Kind} Blessing | Tier {selectedState.Tier}";
        var metaColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(vm.X + padding, currentY), metaColorU32, metaText);
        currentY += 20f;

        // Separator line
        var lineY = currentY;
        var lineStart = new Vector2(vm.X + padding, lineY);
        var lineEnd = new Vector2(vm.X + vm.Width - padding, lineY);
        var lineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f);
        drawList.AddLine(lineStart, lineEnd, lineColor, 1f);
        currentY += 8f;

        return currentY;
    }
}