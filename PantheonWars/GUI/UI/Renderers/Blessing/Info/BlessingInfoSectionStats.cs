using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Models.Blessing.Info;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models;

namespace PantheonWars.GUI.UI.Renderers.Blessing.Info;

internal static class BlessingInfoSectionStats
{
    public static float Draw(BlessingNodeState selectedState, BlessingInfoViewModel vm, float currentY, float padding)
    {
        if (currentY >= vm.Y + vm.Height - 60f || selectedState.Blessing.StatModifiers.Count == 0)
            return currentY;

        var drawList = ImGui.GetWindowDrawList();

        currentY += 8f;
        var statsTitleColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 16f, new Vector2(vm.X + padding, currentY), statsTitleColorU32,
            "Effects:");
        currentY += 22f;

        foreach (var stat in selectedState.Blessing.StatModifiers)
        {
            var statText = BlessingInfoTextUtils.FormatStatModifier(stat.Key, stat.Value);
            var statColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Green);
            drawList.AddText(ImGui.GetFont(), 14f, new Vector2(vm.X + padding + 8, currentY), statColorU32,
                statText);
            currentY += 18f;

            if (currentY > vm.Y + vm.Height - 20f) break;
        }

        return currentY;
    }
}