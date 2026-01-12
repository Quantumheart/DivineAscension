using System.Collections.Generic;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for religion info header and stats grid
/// Displays: Religion name, deity, member count, founder, prestige
/// </summary>
internal static class ReligionInfoHeaderRenderer
{
    /// <summary>
    /// Renders the header section with religion name and info grid
    /// Returns the updated Y position after rendering
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

        // === RELIGION HEADER ===
        TextRenderer.DrawLabel(drawList, viewModel.ReligionName, x, currentY, 20f, ColorPalette.Gold);
        currentY += 32f;

        // Info grid
        var leftCol = x;
        var rightCol = x + width / 2f;

        // Deity - display custom name if available, otherwise just domain
        // If founder and editing, show input field; otherwise show text with edit button
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
            leftCol, currentY, 13f, ColorPalette.Grey);

        if (viewModel.IsEditingDeityName)
        {
            // Edit mode - show input field with save/cancel buttons
            currentY = DrawDeityNameEditMode(viewModel, drawList, leftCol, currentY, width, events);
        }
        else
        {
            // Display mode - show deity name with edit button for founders
            var deityDisplay = !string.IsNullOrWhiteSpace(viewModel.DeityName)
                ? $"{viewModel.DeityName} ({viewModel.Deity})"
                : viewModel.Deity;

            drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), deityDisplay);

            // Show edit button for founders
            if (viewModel.IsFounder)
            {
                var textWidth = ImGui.CalcTextSize(deityDisplay).X;
                const float buttonWidth = 40f;
                const float buttonHeight = 18f;
                const float buttonPadding = 8f;
                var buttonX = leftCol + 80f + textWidth + buttonPadding;

                if (ButtonRenderer.DrawButton(drawList, "Edit", buttonX, currentY - 2f, buttonWidth, buttonHeight,
                        isPrimary: false, enabled: true))
                {
                    events.Add(new InfoEvent.EditDeityNameOpen());
                }
            }

            // Member count (same row as deity in display mode)
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
                rightCol, currentY, 13f, ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.MemberCount.ToString());

            currentY += 22f;
        }

        // Founder
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_FOUNDER_LABEL),
            leftCol, currentY, 13f, ColorPalette.Grey);
        var founderName = viewModel.GetFounderDisplayName();
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), founderName);

        // Prestige
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_LABEL),
            rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_VALUE,
                viewModel.Prestige, viewModel.PrestigeRank));

        currentY += 28f;

        return currentY;
    }

    /// <summary>
    /// Renders the deity name edit mode UI
    /// </summary>
    private static float DrawDeityNameEditMode(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;
        const float inputHeight = 28f;
        const float inputWidth = 300f;
        const float buttonWidth = 60f;
        const float buttonHeight = 24f;
        const float buttonGap = 8f;

        // Input field
        var newValue = TextInput.Draw(
            drawList,
            "##editDeityName",
            viewModel.EditDeityNameValue,
            x + 80f,
            currentY,
            inputWidth,
            inputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_PLACEHOLDER),
            48);

        if (newValue != viewModel.EditDeityNameValue)
        {
            events.Add(new InfoEvent.EditDeityNameChanged(newValue));
        }

        currentY += inputHeight + 4f;

        // Error message
        if (!string.IsNullOrEmpty(viewModel.DeityNameError))
        {
            TextRenderer.DrawErrorText(drawList, viewModel.DeityNameError, x + 80f, currentY);
            currentY += 20f;
        }

        // Save/Cancel buttons
        var buttonX = x + 80f;

        // Save button
        var canSave = !viewModel.IsSavingDeityName &&
                      !string.IsNullOrWhiteSpace(viewModel.EditDeityNameValue) &&
                      viewModel.EditDeityNameValue.Length >= 2 &&
                      viewModel.EditDeityNameValue.Length <= 48;

        if (ButtonRenderer.DrawButton(
                drawList,
                viewModel.IsSavingDeityName
                    ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVING)
                    : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVE),
                buttonX,
                currentY,
                buttonWidth,
                buttonHeight,
                isPrimary: true,
                enabled: canSave))
        {
            events.Add(new InfoEvent.EditDeityNameSave(viewModel.EditDeityNameValue));
        }

        buttonX += buttonWidth + buttonGap;

        // Cancel button
        if (ButtonRenderer.DrawButton(
                drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_CANCEL),
                buttonX,
                currentY,
                buttonWidth,
                buttonHeight,
                isPrimary: false,
                enabled: !viewModel.IsSavingDeityName))
        {
            events.Add(new InfoEvent.EditDeityNameCancel());
        }

        currentY += buttonHeight + 8f;

        // Member count (show on separate row in edit mode)
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
            x, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.MemberCount.ToString());

        currentY += 22f;

        return currentY;
    }
}