using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.UI.Renderers.Utilities;
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

        // Title + ornamental divider, with the unlock state as the rank tag.
        var (statusText, statusColor) = selectedState.VisualState switch
        {
            BlessingNodeVisualState.Unlocked => ("UNLOCKED", ColorPalette.Gold),
            BlessingNodeVisualState.Unlockable => ("AVAILABLE", ColorPalette.Green),
            _ => ("LOCKED", ColorPalette.Red),
        };

        currentY = PaneHeaderRenderer.Draw(drawList,
            selectedState.Blessing.Name,
            vm.X + padding, currentY, vm.Width - padding * 2,
            rankTag: statusText, rankColor: statusColor);

        // Category and kind metadata, drawn below the divider as supplementary
        // info (the status badge moved into the header's rank slot above).
        var metaText =
            $"{selectedState.Blessing.Category} | {selectedState.Blessing.Kind} Blessing | Tier {selectedState.Tier}";
        var metaColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(vm.X + padding, currentY), metaColorU32, metaText);
        currentY += 20f;

        return currentY;
    }
}