using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Models.Blessing.Info;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Blessing.Info;

internal static class BlessingInfoSectionBackground
{
    public static void Draw(BlessingInfoViewModel vm)
    {
        var drawList = ImGui.GetWindowDrawList();
        var startPos = new Vector2(vm.X, vm.Y);
        var endPos = new Vector2(vm.X + vm.Width, vm.Y + vm.Height);

        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(startPos, endPos, bgColor, 4f);

        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRect(startPos, endPos, borderColor, 4f, ImDrawFlags.None, 2f);
    }
}