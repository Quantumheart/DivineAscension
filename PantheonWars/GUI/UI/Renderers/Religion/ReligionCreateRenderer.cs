using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Models.Religion.Create;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for creating a new religion
/// Takes an immutable view model, returns events representing user interactions
/// Migrates functionality from CreateReligionOverlay
/// </summary>
internal static class ReligionCreateRenderer
{
    /// <summary>
    /// Renders the religion creation form
    /// Pure function: ViewModel + DrawList → RenderResult
    /// </summary>
    public static ReligionCreateRenderResult Draw(
        ReligionCreateViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ReligionCreateEvent>();
        var currentY = viewModel.Y;

        // Center the form
        const float formWidth = 500f;
        var formX = viewModel.X + (viewModel.Width - formWidth) / 2;
        const float padding = 20f;

        // === HEADER ===
        var headerText = "Create New Religion";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(formX, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 20f, headerPos, headerColor, headerText);
        currentY += headerSize.Y + padding;

        // === FORM FIELDS ===
        const float fieldWidth = formWidth;

        // Religion Name
        TextRenderer.DrawLabel(drawList, "Religion Name:", formX, currentY);
        currentY += 25f;

        var newReligionName = TextInput.Draw(
            drawList,
            "##createReligionName",
            viewModel.ReligionName,
            formX,
            currentY,
            fieldWidth,
            32f,
            "Enter religion name...",
            32);

        // Emit event if name changed
        if (newReligionName != viewModel.ReligionName)
        {
            events.Add(new ReligionCreateEvent.NameChanged(newReligionName));
        }

        currentY += 40f;

        // Validation feedback
        if (!string.IsNullOrWhiteSpace(viewModel.ReligionName))
        {
            if (viewModel.ReligionName.Length < 3)
            {
                TextRenderer.DrawErrorText(drawList, "Religion name must be at least 3 characters", formX, currentY);
                currentY += 25f;
            }
            else if (viewModel.ReligionName.Length > 32)
            {
                TextRenderer.DrawErrorText(drawList, "Religion name must be less than 32 characters", formX, currentY);
                currentY += 25f;
            }
        }

        // Deity Selection (tab-based approach)
        TextRenderer.DrawLabel(drawList, "Deity:", formX, currentY);
        currentY += 25f;

        var currentDeityIndex = viewModel.GetCurrentDeityIndex();

        // Draw deity selection as tabs
        var newDeityIndex = TabControl.Draw(
            drawList,
            formX,
            currentY,
            fieldWidth,
            32f,
            viewModel.AvailableDeities,
            currentDeityIndex);

        // Emit event if deity changed
        if (newDeityIndex != currentDeityIndex)
        {
            var newDeity = viewModel.AvailableDeities[newDeityIndex];
            events.Add(new ReligionCreateEvent.DeityChanged(newDeity));
        }

        currentY += 40f;

        // Public/Private Toggle
        var newIsPublic = CheckboxRenderer.DrawCheckbox(
            drawList,
            "Public (anyone can join)",
            formX,
            currentY,
            viewModel.IsPublic);

        // Emit event if public/private changed
        if (newIsPublic != viewModel.IsPublic)
        {
            events.Add(new ReligionCreateEvent.IsPublicChanged(newIsPublic));
        }

        currentY += 35f;

        // Info text
        TextRenderer.DrawInfoText(drawList, viewModel.InfoText, formX, currentY, fieldWidth);
        currentY += 50f;

        // Error message
        if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
        {
            TextRenderer.DrawErrorText(drawList, viewModel.ErrorMessage, formX, currentY);
            currentY += 30f;
        }

        // === CREATE BUTTON (centered) ===
        const float buttonWidth = 160f;
        const float buttonHeight = 36f;
        var createButtonX = formX + (formWidth - buttonWidth) / 2;

        // Draw a Create button
        if (ButtonRenderer.DrawButton(
                drawList,
                "Create Religion",
                createButtonX,
                currentY,
                buttonWidth,
                buttonHeight,
                isPrimary: true,
                enabled: viewModel.CanCreate))
        {
            events.Add(new ReligionCreateEvent.SubmitClicked());
        }

        currentY += buttonHeight;

        return new ReligionCreateRenderResult(events, currentY - viewModel.Y);
    }
}