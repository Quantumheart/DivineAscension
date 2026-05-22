using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization.Info;

/// <summary>
/// Right-aligned ledger footer: [ Leave ] [ Disband † ]. Dagger painted as a
/// primitive so it renders in fonts without U+2020.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationInfoActionsRenderer
{
    private const float ButtonHeight = 34f;
    private const float LeaveWidth = 140f;
    private const float DisbandWidth = 170f;
    private const float ButtonGap = 10f;
    private const float DaggerSize = 14f;
    private const float DaggerLabelPadding = 8f;

    public static float Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        bool overlayOpen,
        List<InfoEvent> events)
    {
        var rightX = x + width;
        var cursor = rightX;

        if (vm.IsFounder)
        {
            cursor -= DisbandWidth;
            var disbandLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_LEDGER);
            if (!overlayOpen && ButtonRenderer.DrawButton(drawList,
                    disbandLabel,
                    cursor, y, DisbandWidth, ButtonHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.DisbandOpened());
            }

            var labelSize = ImGui.CalcTextSize(disbandLabel);
            var labelRightX = cursor + (DisbandWidth + labelSize.X) / 2f;
            var daggerCx = labelRightX + DaggerLabelPadding;
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
        if (!overlayOpen && ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LEAVE_LEDGER),
                cursor, y, LeaveWidth, ButtonHeight))
        {
            events.Add(new InfoEvent.LeaveClicked());
        }

        return y + ButtonHeight + 6f;
    }
}
