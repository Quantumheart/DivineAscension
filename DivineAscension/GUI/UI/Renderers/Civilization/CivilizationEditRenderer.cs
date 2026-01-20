using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Edit;
using DivineAscension.GUI.UI.Components;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

[ExcludeFromCodeCoverage]
internal static class CivilizationEditRenderer
{
    public static CivilizationEditRenderResult Draw(
        CivilizationEditViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<EditEvent>();

        // Semi-transparent dark overlay
        var overlayColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.7f));
        drawList.AddRectFilled(
            new Vector2(vm.X, vm.Y),
            new Vector2(vm.X + vm.Width, vm.Y + vm.Height),
            overlayColor
        );

        // Dialog box dimensions
        var dialogWidth = 500f;
        var dialogHeight = 450f;
        var dialogX = vm.X + (vm.Width - dialogWidth) / 2;
        var dialogY = vm.Y + (vm.Height - dialogHeight) / 2;

        // Dialog background
        var dialogStart = new Vector2(dialogX, dialogY);
        var dialogEnd = new Vector2(dialogX + dialogWidth, dialogY + dialogHeight);
        var dialogBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(dialogStart, dialogEnd, dialogBgColor, 8f);

        // Dialog border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddRect(dialogStart, dialogEnd, borderColor, 8f, ImDrawFlags.None, 2f);

        var currentY = dialogY + 20f;
        var contentX = dialogX + 20f;
        var contentWidth = dialogWidth - 40f;

        // Title
        TextRenderer.DrawLabel(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_EDIT_TITLE),
            contentX, currentY, 18f, ColorPalette.White);
        currentY += 40f;

        // Civilization name
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_EDIT_CIV_LABEL, vm.CivilizationName),
            contentX, currentY, 14f,
            ColorPalette.Grey);
        currentY += 30f;

        // Current icon preview
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_EDIT_CURRENT_ICON), contentX, currentY,
            14f, ColorPalette.Grey);
        currentY += 25f;

        var previewHeight = IconPicker.DrawPreview(
            drawList,
            vm.CurrentIcon,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_EDIT_ICON_LABEL),
            contentX,
            currentY
        );
        currentY += previewHeight + 20f;

        // Icon selection
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_EDIT_SELECT_ICON), contentX, currentY,
            14f, ColorPalette.White);
        currentY += 25f;

        var availableIcons = CivilizationIconLoader.GetAvailableIcons();
        var (clickedIcon, pickerHeight) = IconPicker.Draw(
            drawList,
            availableIcons,
            vm.EditingIcon,
            contentX,
            currentY,
            contentWidth // spacing
        );

        if (clickedIcon != null)
            events.Add(new EditEvent.IconSelected(clickedIcon));

        currentY += pickerHeight + 20f;

        // Buttons
        var buttonY = dialogY + dialogHeight - 60f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_EDIT_UPDATE_BUTTON), contentX,
                buttonY, 150f, 36f, true))
            events.Add(new EditEvent.SubmitClicked());

        if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                contentX + 160f, buttonY, 100f, 36f))
            events.Add(new EditEvent.CancelClicked());

        return new CivilizationEditRenderResult(events);
    }
}

public readonly record struct CivilizationEditRenderResult(IReadOnlyList<EditEvent> Events);