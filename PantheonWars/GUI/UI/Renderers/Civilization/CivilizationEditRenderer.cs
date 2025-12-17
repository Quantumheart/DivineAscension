using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Civilization;
using PantheonWars.GUI.Models.Civilization.Edit;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

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
        TextRenderer.DrawLabel(drawList, "Edit Civilization Icon", contentX, currentY, 18f, ColorPalette.White);
        currentY += 40f;

        // Civilization name
        TextRenderer.DrawLabel(drawList, $"Civilization: {vm.CivilizationName}", contentX, currentY, 14f,
            ColorPalette.Grey);
        currentY += 30f;

        // Current icon preview
        TextRenderer.DrawLabel(drawList, "Current Icon:", contentX, currentY, 14f, ColorPalette.Grey);
        currentY += 25f;

        var previewHeight = IconPicker.DrawPreview(
            drawList,
            vm.CurrentIcon,
            "Icon",
            contentX,
            currentY
        );
        currentY += previewHeight + 20f;

        // Icon selection
        TextRenderer.DrawLabel(drawList, "Select New Icon:", contentX, currentY, 14f, ColorPalette.White);
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
        if (ButtonRenderer.DrawButton(drawList, "Update Icon", contentX, buttonY, 150f, 36f, true))
            events.Add(new EditEvent.SubmitClicked());

        if (ButtonRenderer.DrawButton(drawList, "Cancel", contentX + 160f, buttonY, 100f, 36f))
            events.Add(new EditEvent.CancelClicked());

        return new CivilizationEditRenderResult(events);
    }
}

public readonly record struct CivilizationEditRenderResult(IReadOnlyList<EditEvent> Events);