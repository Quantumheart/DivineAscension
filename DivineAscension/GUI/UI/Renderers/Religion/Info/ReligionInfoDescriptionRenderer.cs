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
/// Renders the order's description as a prose block on the ledger page. The
/// founder can toggle inline edit via a pencil glyph; the recessed textarea
/// only appears in edit mode so a single short line doesn't reserve a tall
/// box of dead space.
/// </summary>
internal static class ReligionInfoDescriptionRenderer
{
    private const float HeadingHeight = 22f;
    private const float EditGlyphSize = 20f;
    private const float ButtonHeight = 26f;
    private const float ButtonWidth = 80f;
    private const float ButtonGap = 8f;
    private const float EditBoxHeight = 80f;
    private const float SectionBottomSpacing = 8f;
    // Fixed reservation sized for the 200-char description cap so the
    // section's vertical footprint doesn't shift with text length.
    private const float ProseBodyHeight = 80f;

    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;

        // Heading + (founder-only) edit glyph to the right.
        var heading = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PURPOSE_HEADING);
        TextRenderer.DrawLabel(drawList, heading, x, currentY, SubsectionLabel, ColorPalette.Gold);

        if (viewModel.IsFounder && !viewModel.IsEditingDescription)
        {
            var glyphX = x + width - EditGlyphSize;
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    glyphX, currentY,
                    EditGlyphSize, EditGlyphSize,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditDescriptionOpen());
            }
            ChromeRenderer.DrawPencil(drawList,
                glyphX + EditGlyphSize / 2f,
                currentY + EditGlyphSize / 2f,
                EditGlyphSize - 8f,
                ColorPalette.LightText);
        }

        currentY += HeadingHeight;

        if (viewModel.IsFounder && viewModel.IsEditingDescription)
        {
            var newDescription = TextInput.DrawMultiline(drawList, "##religionDescription",
                viewModel.DescriptionText, x, currentY, width, EditBoxHeight);
            if (newDescription != viewModel.DescriptionText)
                events.Add(new InfoEvent.DescriptionChanged(newDescription));

            currentY += EditBoxHeight + 6f;

            // Save (only when dirty) + Cancel, right-aligned.
            var hasChanges = viewModel.HasDescriptionChanges();
            var rightX = x + width;
            var cancelX = rightX - ButtonWidth;

            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_CANCEL),
                    cancelX, currentY, ButtonWidth, ButtonHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditDescriptionCancel());
            }

            if (hasChanges)
            {
                var saveX = cancelX - ButtonGap - ButtonWidth;
                if (ButtonRenderer.DrawButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVE),
                        saveX, currentY, ButtonWidth, ButtonHeight,
                        isPrimary: true, enabled: true))
                {
                    events.Add(new InfoEvent.SaveDescriptionClicked(viewModel.DescriptionText));
                }
            }

            currentY += ButtonHeight + SectionBottomSpacing;
        }
        else
        {
            var prose = string.IsNullOrWhiteSpace(viewModel.Description)
                ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PURPOSE_EMPTY)
                : viewModel.Description!;
            // Body prose on parchment → iron-gall ink for the order's own
            // purpose, faded sepia for the empty-state placeholder.
            var proseColor = string.IsNullOrWhiteSpace(viewModel.Description)
                ? ColorPalette.Grey
                : ColorPalette.White;
            // Reserve a fixed prose block sized for the cap; clip is a
            // defensive guard against any wrap-estimate drift.
            drawList.PushClipRect(
                new Vector2(x, currentY),
                new Vector2(x + width, currentY + ProseBodyHeight),
                true);
            TextRenderer.DrawInfoText(drawList, prose, x, currentY, width, Secondary, proseColor);
            drawList.PopClipRect();
            currentY += ProseBodyHeight + SectionBottomSpacing;
        }

        return currentY;
    }
}
