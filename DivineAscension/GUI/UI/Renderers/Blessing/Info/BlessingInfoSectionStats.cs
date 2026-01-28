using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

[ExcludeFromCodeCoverage]
internal static class BlessingInfoSectionStats
{
    public static float Draw(BlessingNodeState selectedState, BlessingInfoViewModel vm, float currentY, float padding)
    {
        if (currentY >= vm.Y + vm.Height - 60f || selectedState.Blessing.StatModifiers.Count == 0)
            return currentY;

        var drawList = ImGui.GetWindowDrawList();

        currentY += 8f;
        var statsTitleColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), TableHeader, new Vector2(vm.X + padding, currentY), statsTitleColorU32,
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_EFFECTS));
        currentY += 22f;

        foreach (var stat in selectedState.Blessing.StatModifiers)
        {
            var statText = BlessingInfoTextUtils.FormatStatModifier(stat.Key, stat.Value);
            var statColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Green);
            drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(vm.X + padding + 8, currentY), statColorU32,
                statText);
            currentY += 18f;

            if (currentY > vm.Y + vm.Height - 20f) break;
        }

        return currentY;
    }
}