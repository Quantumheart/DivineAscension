using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Right-aligned ledger footer actions: [ Leave ] [ Disband † ]. The dagger
/// is painted as a primitive (ChromeRenderer.DrawDagger) so it renders in
/// fonts without the U+2020 codepoint, and marks Disband as destructive in
/// the manuscript style — no red tint.
/// </summary>
internal static class ReligionInfoActionsRenderer
{
    private const float ButtonHeight = 34f;
    private const float LeaveWidth = 140f;
    private const float DisbandWidth = 170f;
    private const float ButtonGap = 10f;
    private const float DaggerSize = 14f;
    private const float DaggerLabelPadding = 8f;

    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var rightX = x + width;
        var cursor = rightX;

        if (viewModel.IsFounder)
        {
            cursor -= DisbandWidth;
            var disbandLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTION_DISBAND_LEDGER);
            if (ButtonRenderer.DrawButton(drawList,
                    disbandLabel,
                    cursor, y, DisbandWidth, ButtonHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.DisbandOpen());
            }

            // Dagger after the centered label: measure the label, find its
            // right edge inside the button, then paint a dagger primitive at
            // a fixed offset so it tracks the text.
            var labelSize = ImGui.CalcTextSize(disbandLabel);
            var labelRightX = cursor + (DisbandWidth + labelSize.X) / 2f;
            var daggerCx = labelRightX + DaggerLabelPadding;
            // Keep the dagger inside the button bounds even if the label is wide.
            var maxDaggerCx = cursor + DisbandWidth - DaggerSize / 2f - 4f;
            if (daggerCx > maxDaggerCx) daggerCx = maxDaggerCx;
            ChromeRenderer.DrawDagger(drawList,
                daggerCx,
                y + ButtonHeight / 2f,
                DaggerSize,
                ColorPalette.LightText);

            cursor -= ButtonGap;
        }

        cursor -= LeaveWidth;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTION_LEAVE),
                cursor, y, LeaveWidth, ButtonHeight))
        {
            events.Add(new InfoEvent.LeaveClicked());
        }

        return y + ButtonHeight + 6f;
    }
}
