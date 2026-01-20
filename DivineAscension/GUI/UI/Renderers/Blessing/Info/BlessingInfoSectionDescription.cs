using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

[ExcludeFromCodeCoverage]
internal static class BlessingInfoSectionDescription
{
    public static float Draw(BlessingNodeState selectedState, float x,
        float currentY, float padding, float contentWidth)
    {
        var descriptionColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        BlessingInfoTextUtils.DrawWrappedText(selectedState.Blessing.Description,
            x + padding, currentY, contentWidth, descriptionColorU32, 14f);
        currentY += 40f; // Approximate space for description
        return currentY;
    }
}