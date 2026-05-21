using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Activity;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Activity;

/// <summary>
/// Annals chapter header: prose intro line. The title strip + refresh glyph
/// live in the orchestrator so the strip can share scroll state with
/// ChapterStripRenderer.
/// </summary>
internal static class ReligionActivityHeaderRenderer
{
    public const float IntroLineHeight = 18f;
    public const float IntroBottomSpacing = 10f;

    public static float Draw(
        ReligionActivityViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, x, y, width, Body, ColorPalette.White);
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, width, Body);
        return y + (introHeight > 0 ? introHeight : IntroLineHeight) + IntroBottomSpacing;
    }
}
