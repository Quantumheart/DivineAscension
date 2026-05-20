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

        // Dialog box dimensions — height sized to fit the icon picker grid
        // plus the surrounding labels + preview + buttons. The previous fixed
        // 450f wasn't tall enough for the current icon count, so the button
        // row drew on top of the bottom grid rows.
        const float dialogWidth = 500f;
        const int pickerColumns = 4;
        const float pickerIconSize = 40f;
        const float pickerSpacing = 8f;
        const float previewHeightEstimate = 14f + 8f + 48f + 8f + 14f; // label+gap+icon+gap+name
        var availableIconsForMeasure = CivilizationIconLoader.GetAvailableIcons();
        var pickerRows = (int)System.Math.Ceiling(availableIconsForMeasure.Count / (double)pickerColumns);
        var pickerHeightEstimate = pickerRows * pickerIconSize + System.Math.Max(0, pickerRows - 1) * pickerSpacing;

        // Section heights match the currentY increments below:
        //   20 top pad
        // + 40 title row
        // + 30 civ-name row
        // + 25 'Current Icon:' label
        // + previewHeight + 20 gap
        // + 25 'Select New Icon:' label
        // + pickerHeight + 20 gap
        // + 60 button strip
        // + 20 bottom pad
        var dialogHeight = 20f + 40f + 30f + 25f + previewHeightEstimate + 20f
                           + 25f + pickerHeightEstimate + 20f + 60f + 20f;
        // Clamp to the available overlay so dialogs never escape the dialog.
        dialogHeight = System.MathF.Min(dialogHeight, System.MathF.Max(300f, vm.Height - 40f));

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