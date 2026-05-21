using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

/// <summary>
///     Renders the selected blessing details panel at the bottom of the dialog
///     Shows: Name, description, requirements, stats, effects
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingInfoRenderer
{
    /// <summary>
    ///     Draw the blessing info panel
    /// </summary>
    /// <param name="vm">View model for rendering</param>
    /// <returns>Render result with events and height used</returns>
    public static BlessingInfoRenderResult Draw(BlessingInfoViewModel vm)
    {
        var drawList = ImGui.GetWindowDrawList();
        var events = new List<InfoEvent>(1);

        BlessingInfoSectionBackground.Draw(vm);

        var selectedState = vm.SelectedBlessingState;

        if (selectedState == null)
        {
            var promptText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_SELECT_TO_VIEW);
            var textSize = ImGui.CalcTextSize(promptText);
            var textPos = new Vector2(
                vm.X + (vm.Width - textSize.X) / 2,
                vm.Y + (vm.Height - textSize.Y) / 2
            );
            var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(textPos, textColor, promptText);

            return BlessingInfoRenderResult.Empty(vm.Height);
        }

        const float padding = 16f;
        var currentY = vm.Y + padding;
        var contentWidth = vm.Width - padding * 2;

        currentY = BlessingInfoSectionHeader.Draw(selectedState, vm, currentY, padding);

        currentY = BlessingInfoSectionDescription.Draw(selectedState, vm.X, currentY, padding, contentWidth,
            vm.IsDescriptionExpanded, out var toggleEvent);
        if (toggleEvent != null) events.Add(toggleEvent);

        currentY = BlessingInfoSectionRequirements.Draw(selectedState, vm, currentY, padding, contentWidth);

        currentY = BlessingInfoSectionStats.Draw(selectedState, vm, currentY, padding);

        return new BlessingInfoRenderResult(events, vm.Height);
    }
}
