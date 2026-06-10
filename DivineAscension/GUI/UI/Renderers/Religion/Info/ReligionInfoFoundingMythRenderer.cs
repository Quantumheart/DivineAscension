using System.Collections.Generic;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Long-form founding myth / origin story (#362). Founder toggles inline edit
/// via pencil glyph; large textarea only appears in edit mode.
/// </summary>
internal static class ReligionInfoFoundingMythRenderer
{
    private static float HeadingHeight => UiScale.Scaled(22f);
    private static float EditGlyphSize => UiScale.Scaled(20f);
    private static float ButtonHeight => UiScale.Scaled(26f);
    private static float ButtonWidth => UiScale.Scaled(80f);
    private static float ButtonGap => UiScale.Scaled(8f);
    private static float EditBoxHeight => UiScale.Scaled(200f);
    private static float SectionBottomSpacing => UiScale.Scaled(8f);
    // Fixed reservation sized for the 2000-char myth cap (~34 wrapped
    // lines × ~16px line height) so long stories don't shove the
    // sections below up or down on each render.
    private static float ProseBodyHeight => UiScale.Scaled(540f);

    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events,
        float? bodyHeightOverride = null)
    {
        var bodyHeight = bodyHeightOverride ?? ProseBodyHeight;
        var currentY = y;

        var heading = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MYTH_HEADING);
        TextRenderer.DrawLabel(drawList, heading, x, currentY, SubsectionLabel, ColorPalette.Gold);

        if (viewModel.IsFounder && !viewModel.IsEditingFoundingMyth)
        {
            var glyphX = x + width - EditGlyphSize;
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    glyphX, currentY,
                    EditGlyphSize, EditGlyphSize,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditFoundingMythOpen());
            }
            ChromeRenderer.DrawPencil(drawList,
                glyphX + EditGlyphSize / 2f,
                currentY + EditGlyphSize / 2f,
                EditGlyphSize - UiScale.Scaled(8f),
                ColorPalette.LightText);
        }

        currentY += HeadingHeight;

        if (viewModel.IsFounder && viewModel.IsEditingFoundingMyth)
        {
            var newMyth = TextInput.DrawMultiline(drawList, "##religionFoundingMyth",
                viewModel.FoundingMythText, x, currentY, width, EditBoxHeight);
            if (newMyth != viewModel.FoundingMythText)
                events.Add(new InfoEvent.FoundingMythChanged(newMyth));

            currentY += EditBoxHeight + UiScale.Scaled(6f);

            var hasChanges = viewModel.HasFoundingMythChanges();
            var rightX = x + width;
            var cancelX = rightX - ButtonWidth;

            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_CANCEL),
                    cancelX, currentY, ButtonWidth, ButtonHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditFoundingMythCancel());
            }

            if (hasChanges)
            {
                var saveX = cancelX - ButtonGap - ButtonWidth;
                if (ButtonRenderer.DrawButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVE),
                        saveX, currentY, ButtonWidth, ButtonHeight,
                        isPrimary: true, enabled: true))
                {
                    events.Add(new InfoEvent.SaveFoundingMythClicked(viewModel.FoundingMythText));
                }
            }

            currentY += ButtonHeight + SectionBottomSpacing;
        }
        else
        {
            var prose = string.IsNullOrWhiteSpace(viewModel.FoundingMyth)
                ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MYTH_EMPTY)
                : viewModel.FoundingMyth!;
            var proseColor = string.IsNullOrWhiteSpace(viewModel.FoundingMyth)
                ? ColorPalette.Grey
                : ColorPalette.White;
            drawList.PushClipRect(
                new Vector2(x, currentY),
                new Vector2(x + width, currentY + bodyHeight),
                true);
            TextRenderer.DrawInfoText(drawList, prose, x, currentY, width, Secondary, proseColor);
            drawList.PopClipRect();
            currentY += bodyHeight + SectionBottomSpacing;
        }

        return currentY;
    }
}
