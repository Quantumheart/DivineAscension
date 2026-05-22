using System;
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
            var hasMotto = !string.IsNullOrWhiteSpace(viewModel.Motto);
            var prose = hasMotto
                ? viewModel.Motto!
                : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MOTTO_EMPTY);
            var proseColor = hasMotto ? ColorPalette.White : ColorPalette.Grey;

            if (hasMotto)
            {
                // Paint primitive curly-quote glyphs flanking the motto since
                // the font lacks U+201C/U+201D coverage. Motto is single-line
                // (80-char cap), so the close quote hugs the text end.
                const float glyphSize = 14f;
                const float glyphGap = 6f;
                var textIndent = glyphSize + glyphGap;
                var openCx = x + glyphSize / 2f;
                var glyphCy = currentY + glyphSize / 2f;
                ChromeRenderer.DrawQuoteMark(drawList, openCx, glyphCy, glyphSize, closing: false,
                    colorOverride: ColorPalette.Grey);

                var textX = x + textIndent;
                var textWidth = ImGui.CalcTextSize(prose).X;
                TextRenderer.DrawInfoText(drawList, prose, textX, currentY,
                    width - textIndent * 2f, Secondary, proseColor);

                var closeCx = textX + textWidth + glyphGap + glyphSize / 2f;
                // Clamp to the section's right edge in case a long motto wraps.
                closeCx = MathF.Min(closeCx, x + width - glyphSize / 2f);
                ChromeRenderer.DrawQuoteMark(drawList, closeCx, glyphCy, glyphSize, closing: true,
                    colorOverride: ColorPalette.Grey);

                var textHeight = TextRenderer.MeasureWrappedHeight(prose, width - textIndent * 2f);
                currentY += (textHeight > 0 ? textHeight : 20f) + SectionBottomSpacing;
            }
            else
            {
                TextRenderer.DrawInfoText(drawList, prose, x, currentY, width, Secondary, proseColor);
                var textHeight = TextRenderer.MeasureWrappedHeight(prose, width);
                currentY += (textHeight > 0 ? textHeight : 20f) + SectionBottomSpacing;
            }
        }

        return currentY;
    }
}
