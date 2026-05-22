using System.Collections.Generic;
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
/// Short one-line motto/creed (#361). Founder toggles inline edit via pencil
/// glyph; recessed single-line input only appears in edit mode.
/// </summary>
internal static class ReligionInfoMottoRenderer
{
    private const float HeadingHeight = 22f;
    private const float EditGlyphSize = 20f;
    private const float ButtonHeight = 26f;
    private const float ButtonWidth = 80f;
    private const float ButtonGap = 8f;
    private const float InputHeight = 28f;
    private const float SectionBottomSpacing = 8f;
    private const int MaxLen = 80;

    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;

        var heading = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MOTTO_HEADING);
        TextRenderer.DrawLabel(drawList, heading, x, currentY, SubsectionLabel, ColorPalette.Gold);

        if (viewModel.IsFounder && !viewModel.IsEditingMotto)
        {
            var glyphX = x + width - EditGlyphSize;
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    glyphX, currentY,
                    EditGlyphSize, EditGlyphSize,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditMottoOpen());
            }
            ChromeRenderer.DrawPencil(drawList,
                glyphX + EditGlyphSize / 2f,
                currentY + EditGlyphSize / 2f,
                EditGlyphSize - 8f,
                ColorPalette.LightText);
        }

        currentY += HeadingHeight;

        if (viewModel.IsFounder && viewModel.IsEditingMotto)
        {
            var newMotto = TextInput.Draw(drawList, "##religionMotto",
                viewModel.MottoText, x, currentY, width, InputHeight,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MOTTO_PLACEHOLDER),
                MaxLen);
            if (newMotto != viewModel.MottoText)
                events.Add(new InfoEvent.MottoChanged(newMotto));

            currentY += InputHeight + 6f;

            var hasChanges = viewModel.HasMottoChanges();
            var rightX = x + width;
            var cancelX = rightX - ButtonWidth;

            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_CANCEL),
                    cancelX, currentY, ButtonWidth, ButtonHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditMottoCancel());
            }

            if (hasChanges)
            {
                var saveX = cancelX - ButtonGap - ButtonWidth;
                if (ButtonRenderer.DrawButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVE),
                        saveX, currentY, ButtonWidth, ButtonHeight,
                        isPrimary: true, enabled: true))
                {
                    events.Add(new InfoEvent.SaveMottoClicked(viewModel.MottoText));
                }
            }

            currentY += ButtonHeight + SectionBottomSpacing;
        }
        else
        {
            var prose = string.IsNullOrWhiteSpace(viewModel.Motto)
                ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MOTTO_EMPTY)
                : $"“{viewModel.Motto}”";
            var proseColor = string.IsNullOrWhiteSpace(viewModel.Motto)
                ? ColorPalette.Grey
                : ColorPalette.White;
            TextRenderer.DrawInfoText(drawList, prose, x, currentY, width, Secondary, proseColor);
            var height = TextRenderer.MeasureWrappedHeight(prose, width);
            currentY += (height > 0 ? height : 20f) + SectionBottomSpacing;
        }

        return currentY;
    }
}
