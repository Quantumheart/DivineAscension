using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for religion description section
/// Handles both read-only display and editable description for founders
/// </summary>
internal static class ReligionInfoDescriptionRenderer
{
    /// <summary>
    /// Renders the description section
    /// Returns the updated Y position and emits events for description changes
    /// </summary>
    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;

        if (viewModel.IsFounder)
        {
            // Editable description for founder
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DESCRIPTION_EDITABLE),
                x, currentY, SubsectionLabel, ColorPalette.White);
            currentY += 22f;

            const float descHeight = 80f;
            var newDescription = TextInput.DrawMultiline(drawList, "##religionDescription", viewModel.DescriptionText,
                x, currentY, width, descHeight);

            // Emit event if description changed
            if (newDescription != viewModel.DescriptionText)
            {
                events.Add(new InfoEvent.DescriptionChanged(newDescription));
            }

            currentY += descHeight + 5f;

            // Save Description button
            var saveButtonWidth = 150f;
            var saveButtonX = x + width - saveButtonWidth;
            var hasChanges = viewModel.HasDescriptionChanges();

            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_SAVE_DESCRIPTION),
                    saveButtonX, currentY, saveButtonWidth, 32f, false, hasChanges))
            {
                events.Add(new InfoEvent.SaveDescriptionClicked(viewModel.DescriptionText));
            }

            currentY += 40f;
        }
        else
        {
            // Read-only description for members
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DESCRIPTION_LABEL),
                x, currentY, SubsectionLabel, ColorPalette.White);
            currentY += 22f;

            var desc = string.IsNullOrEmpty(viewModel.Description)
                ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DESCRIPTION_EMPTY)
                : viewModel.Description;
            TextRenderer.DrawInfoText(drawList, desc, x, currentY, width);
            currentY += 40f;
        }

        return currentY;
    }
}