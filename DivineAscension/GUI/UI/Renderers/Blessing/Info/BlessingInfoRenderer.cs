using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Blessing;
using PantheonWars.GUI.Models.Blessing.Info;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Blessing.Info;

/// <summary>
///     Renders the selected blessing details panel at the bottom of the dialog
///     Shows: Name, description, requirements, stats, effects
/// </summary>
internal static class BlessingInfoRenderer
{
    private const string SelectABlessingToViewDetails = "Select a blessing to view details";

    /// <summary>
    ///     Draw the blessing info panel
    /// </summary>
    /// <param name="vm">View model for rendering</param>
    /// <returns>Render result with events and height used</returns>
    public static BlessingInfoRenderResult Draw(BlessingInfoViewModel vm)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Section: Background
        BlessingInfoSectionBackground.Draw(vm);

        // Selected blessing state
        var selectedState = vm.SelectedBlessingState;

        if (selectedState == null)
        {
            // No blessing selected - show prompt
            var promptText = SelectABlessingToViewDetails;
            var textSize = ImGui.CalcTextSize(promptText);
            var textPos = new Vector2(
                vm.X + (vm.Width - textSize.X) / 2,
                vm.Y + (vm.Height - textSize.Y) / 2
            );
            var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(textPos, textColor, promptText);

            return BlessingInfoRenderResult.Empty(vm.Height);
        }

        // Blessing selected - draw detailed info
        const float padding = 16f;
        var currentY = vm.Y + padding;
        var contentWidth = vm.Width - padding * 2;

        // Section: Header/meta
        currentY = BlessingInfoSectionHeader.Draw(selectedState, vm, currentY, padding);

        // Description (word-wrapped)
        currentY = BlessingInfoSectionDescription.Draw(selectedState, vm.X, currentY, padding, contentWidth);

        // Requirements section (check if space available)
        currentY = BlessingInfoSectionRequirements.Draw(selectedState, vm, currentY, padding, contentWidth);

        // Stats section (if space available)
        currentY = BlessingInfoSectionStats.Draw(selectedState, vm, currentY, padding);

        return new BlessingInfoRenderResult(new List<InfoEvent>(0), vm.Height);
    }
}