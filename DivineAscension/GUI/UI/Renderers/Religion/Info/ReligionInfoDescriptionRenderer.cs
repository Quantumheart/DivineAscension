using System.Collections.Generic;
using ImGuiNET;
using PantheonWars.GUI.Events.Religion;
using PantheonWars.GUI.Models.Religion.Info;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Religion.Info;

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
            TextRenderer.DrawLabel(drawList, "Description (editable):", x, currentY, 14f, ColorPalette.White);
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

            if (ButtonRenderer.DrawButton(drawList, "Save Description", saveButtonX, currentY, saveButtonWidth, 32f,
                    false, hasChanges))
            {
                events.Add(new InfoEvent.SaveDescriptionClicked(viewModel.DescriptionText));
            }

            currentY += 40f;
        }
        else
        {
            // Read-only description for members
            TextRenderer.DrawLabel(drawList, "Description:", x, currentY, 14f, ColorPalette.White);
            currentY += 22f;

            var desc = string.IsNullOrEmpty(viewModel.Description) ? "[No description set]" : viewModel.Description;
            TextRenderer.DrawInfoText(drawList, desc, x, currentY, width);
            currentY += 40f;
        }

        return currentY;
    }
}