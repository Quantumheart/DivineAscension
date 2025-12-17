using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for religion action buttons
/// Handles Leave Religion and Disband Religion buttons
/// </summary>
internal static class ReligionInfoActionsRenderer
{
    /// <summary>
    /// Renders the action buttons (Leave, Disband)
    /// Returns the updated Y position and emits events for button clicks
    /// </summary>
    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        List<InfoEvent> events)
    {
        var currentY = y;

        // Leave Religion button (always available)
        if (ButtonRenderer.DrawButton(drawList, "Leave Religion", x, currentY, 160f, 34f))
        {
            events.Add(new InfoEvent.LeaveClicked());
        }

        // Disband Religion button (founder only)
        if (viewModel.IsFounder)
            if (ButtonRenderer.DrawButton(drawList, "Disband Religion", x + 170f, currentY, 180f, 34f, false, true,
                    ColorPalette.Red * 0.7f))
            {
                events.Add(new InfoEvent.DisbandOpen());
            }

        currentY += 40f;

        return currentY;
    }
}