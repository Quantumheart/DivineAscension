using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization.Info;

/// <summary>
/// "Of the Realm's Purpose" prose block with founder-only inline edit toggle.
/// Recessed textarea only appears in edit mode so a short value doesn't
/// reserve a tall box of dead space.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationInfoDescriptionRenderer
{
    private static float HeadingHeight => UiScale.Scaled(22f);
    private static float EditGlyphSize => UiScale.Scaled(20f);
    private static float ButtonHeight => UiScale.Scaled(26f);
    private static float ButtonWidth => UiScale.Scaled(80f);
    private static float ButtonGap => UiScale.Scaled(8f);
    private static float EditBoxHeight => UiScale.Scaled(80f);
    private static float SectionBottomSpacing => UiScale.Scaled(8f);

    public static float Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;

        var heading = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_PURPOSE_HEADING);
        TextRenderer.DrawLabel(drawList, heading, x, currentY, SubsectionLabel, ColorPalette.Gold);

        if (vm.IsFounder && !vm.IsEditingDescription)
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
                EditGlyphSize - UiScale.Scaled(8f),
                ColorPalette.LightText);
        }

        currentY += HeadingHeight;

        if (vm.IsFounder && vm.IsEditingDescription)
        {
            var newDescription = TextInput.DrawMultiline(drawList, "##civDescription",
                vm.DescriptionText, x, currentY, width, EditBoxHeight);
            if (newDescription != vm.DescriptionText)
                events.Add(new InfoEvent.DescriptionChanged(newDescription));

            currentY += EditBoxHeight + UiScale.Scaled(6f);

            var rightX = x + width;
            var cancelX = rightX - ButtonWidth;
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CANCEL),
                    cancelX, currentY, ButtonWidth, ButtonHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditDescriptionCancel());
            }

            if (vm.HasDescriptionChanges)
            {
                var saveX = cancelX - ButtonGap - ButtonWidth;
                if (ButtonRenderer.DrawButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_SAVE),
                        saveX, currentY, ButtonWidth, ButtonHeight,
                        isPrimary: true, enabled: true))
                {
                    events.Add(new InfoEvent.SaveDescriptionClicked());
                }
            }

            currentY += ButtonHeight + SectionBottomSpacing;
        }
        else
        {
            var prose = vm.HasDescription
                ? vm.Description
                : LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_PURPOSE_EMPTY);
            var proseColor = vm.HasDescription ? ColorPalette.White : ColorPalette.Grey;
            TextRenderer.DrawInfoText(drawList, prose, x, currentY, width, Secondary, proseColor);
            var height = TextRenderer.MeasureWrappedHeight(prose, width);
            currentY += (height > 0 ? height : Secondary + LinePadding) + SectionBottomSpacing;
        }

        return currentY;
    }
}
