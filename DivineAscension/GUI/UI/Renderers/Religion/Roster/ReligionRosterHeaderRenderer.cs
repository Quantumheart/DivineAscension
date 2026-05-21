using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Roster;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Roster;

[ExcludeFromCodeCoverage]
internal static class ReligionRosterHeaderRenderer
{
    public static float Draw(ReligionRosterViewModel vm, ImDrawListPtr drawList, float x, float y, float width)
    {
        var loc = LocalizationService.Instance;
        var title = loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_TITLE, vm.ReligionName);
        var afterHeader = PaneHeaderRenderer.Draw(drawList, title, x, y, width);

        var introKey = vm.MemberCount == 1
            ? LocalizationKeys.UI_RELIGION_ROSTER_INTRO_ONE
            : LocalizationKeys.UI_RELIGION_ROSTER_INTRO;
        var intro = loc.Get(introKey, vm.MemberCount);
        TextRenderer.DrawInfoText(drawList, intro, x, afterHeader, width, Body, ColorPalette.White);
        return afterHeader + 22f;
    }
}
