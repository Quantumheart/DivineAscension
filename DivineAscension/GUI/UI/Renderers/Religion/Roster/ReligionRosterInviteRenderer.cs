using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Roster;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion.Roster;

[ExcludeFromCodeCoverage]
internal static class ReligionRosterInviteRenderer
{
    public const float LabelHeight = 22f;
    public const float InputRowHeight = 40f;
    public const float TotalHeight = LabelHeight + InputRowHeight;

    public static float Draw(
        ReligionRosterViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<RosterEvent> events)
    {
        if (!vm.IsFounder) return y;
        var loc = LocalizationService.Instance;

        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_LABEL),
            x, currentY, FontSizes.SubsectionLabel, ColorPalette.Gold);
        currentY += LabelHeight;

        const float buttonW = 100f;
        const float gap = 10f;
        var inputWidth = width - buttonW - gap;
        var newName = TextInput.Draw(drawList, "##rosterInvite", vm.InvitePlayerName,
            x, currentY, inputWidth, 32f,
            loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_PLACEHOLDER));

        if (newName != vm.InvitePlayerName)
            events.Add(new RosterEvent.InviteNameChanged(newName));

        var canInvite = !string.IsNullOrWhiteSpace(vm.InvitePlayerName);
        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_BUTTON),
                x + inputWidth + gap, currentY, buttonW, 32f, false, canInvite)
            && canInvite)
        {
            events.Add(new RosterEvent.InviteClicked(vm.InvitePlayerName.Trim()));
        }

        currentY += InputRowHeight;
        return currentY;
    }
}
