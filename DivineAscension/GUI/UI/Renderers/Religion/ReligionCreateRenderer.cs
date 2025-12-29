using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Create;
using DivineAscension.GUI.UI.Components;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

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
        var events = new List<CreateEvent>();
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
            events.Add(new CreateEvent.NameChanged(newReligionName));
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

        // Deity Selection (tab-based approach with icons and tooltips)
        TextRenderer.DrawLabel(drawList, "Deity:", formX, currentY);
        currentY += 25f;

        var currentDeityIndex = viewModel.GetCurrentDeityIndex();

        // Prepare deity icon names for tabs (lowercase for icon loader)
        var deityIconNames = viewModel.AvailableDeities
            .Select(d => d.ToLower())
            .ToArray();

        // Draw deity selection as tabs with icons and hover tracking
        var (newDeityIndex, hoveredIndex) = TabControl.DrawWithHover(
            drawList,
            formX,
            currentY,
            fieldWidth,
            32f,
            viewModel.AvailableDeities,
            currentDeityIndex,
            4f,
            "deities",  // Icon directory
            deityIconNames);

        // Emit event if deity changed
        if (newDeityIndex != currentDeityIndex)
        {
            var newDeity = viewModel.AvailableDeities[newDeityIndex];
            events.Add(new CreateEvent.DeityChanged(newDeity));
        }

        // Track hovered deity for tooltip rendering
        string? hoveredDeityName = null;
        if (hoveredIndex >= 0 && hoveredIndex < viewModel.AvailableDeities.Length)
        {
            hoveredDeityName = viewModel.AvailableDeities[hoveredIndex];
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
            events.Add(new CreateEvent.IsPublicChanged(newIsPublic));
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
            events.Add(new CreateEvent.SubmitClicked());
        }

        currentY += buttonHeight;

        // Render deity tooltip if hovering over a deity tab
        if (!string.IsNullOrEmpty(hoveredDeityName))
        {
            var mousePos = ImGui.GetMousePos();
            DeityTooltipRenderer.Draw(
                hoveredDeityName,
                mousePos.X,
                mousePos.Y,
                viewModel.Width,
                viewModel.Height);
        }

        return new ReligionCreateRenderResult(events, currentY - viewModel.Y);
    }
}