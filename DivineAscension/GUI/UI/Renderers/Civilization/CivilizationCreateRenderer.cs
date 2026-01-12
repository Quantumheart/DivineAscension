using System.Collections.Generic;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Create;
using DivineAscension.GUI.UI.Components;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

internal static class CivilizationCreateRenderer
{
    public static CivilizationCreateRenderResult Draw(
        CivilizationCreateViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<CreateEvent>();
        var currentY = vm.Y;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_TITLE), vm.X, currentY, 18f,
            ColorPalette.White);
        currentY += 32f;

        // Requirements
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_REQUIREMENTS), vm.X, currentY, 14f,
            ColorPalette.Grey);
        currentY += 22f;

        var requirements = new[]
        {
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_REQ_FOUNDER),
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_REQ_NOT_IN_CIV),
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_REQ_NAME_LENGTH)
        };

        foreach (var req in requirements)
        {
            // bullet
            drawList.AddCircleFilled(new Vector2(vm.X + 8f, currentY + 7f), 2f,
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));
            drawList.AddText(ImGui.GetFont(), 14f, new Vector2(vm.X + 16f, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), req);
            currentY += 18f;
        }

        currentY += 16f;

        // Civilization name input
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_NAME_LABEL), vm.X, currentY);
        currentY += 20f;

        var newName = TextInput.Draw(drawList, "##createCivName", vm.CivilizationName,
            vm.X, currentY,
            vm.Width * 0.7f, 30f,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_NAME_PLACEHOLDER), 32);

        if (newName != vm.CivilizationName)
            events.Add(new CreateEvent.NameChanged(newName));

        currentY += 40f;

        // Validation feedback
        if (!string.IsNullOrWhiteSpace(vm.CivilizationName))
        {
            if (vm.CivilizationName.Length < 3)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_NAME_ERROR_TOO_SHORT),
                    vm.X, currentY);
                currentY += 25f;
            }
            else if (vm.CivilizationName.Length > 32)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_NAME_ERROR_TOO_LONG),
                    vm.X, currentY);
                currentY += 25f;
            }
            else if (vm.HasProfanity)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_NAME_ERROR_PROFANITY,
                        vm.ProfanityMatchedWord ?? ""), vm.X, currentY);
                currentY += 25f;
            }
        }

        // Icon selection
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_ICON_LABEL), vm.X, currentY);
        currentY += 20f;

        var availableIcons = CivilizationIconLoader.GetAvailableIcons();
        var (clickedIcon, pickerHeight) = IconPicker.Draw(
            drawList,
            availableIcons,
            vm.SelectedIcon,
            vm.X,
            currentY,
            vm.Width * 0.7f // spacing
        );

        if (clickedIcon != null)
            events.Add(new CreateEvent.IconSelected(clickedIcon));

        currentY += pickerHeight + 20f;

        // Create button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_BUTTON), vm.X, currentY, 200f,
                36f, isPrimary: true, enabled: vm.CanCreate))
            events.Add(new CreateEvent.SubmitClicked());

        // Clear button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_CLEAR_BUTTON), vm.X + 210f,
                currentY, 80f, 36f))
            events.Add(new CreateEvent.ClearClicked());

        currentY += 50f;

        // Info text
        TextRenderer.DrawInfoText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_INFO_TEXT),
            vm.X, currentY, vm.Width);
        currentY += 40f;

        return new CivilizationCreateRenderResult(events, currentY - vm.Y);
    }
}