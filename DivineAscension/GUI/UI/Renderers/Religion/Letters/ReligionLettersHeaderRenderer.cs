using DivineAscension.Constants;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Letters;

/// <summary>
/// Letters chapter header: prose intro line below the title strip. Title strip
/// itself is composed by the orchestrator so it can share scroll state with
/// ChapterStripRenderer.
/// </summary>
internal static class ReligionLettersHeaderRenderer
{
    public const float IntroLineHeight = 18f;
    public const float IntroBottomSpacing = 10f;

    public static float Draw(ImDrawListPtr drawList, float x, float y, float width)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, x, y, width, Body, ColorPalette.White);
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, width, Body);
        return y + (introHeight > 0 ? introHeight : IntroLineHeight) + IntroBottomSpacing;
    }
}
