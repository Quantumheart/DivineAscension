using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for religion invite player section (founder only)
/// Handles the invite player input and button
/// </summary>
internal static class ReligionInfoInviteRenderer
{
    /// <summary>
    /// Renders the invite player section for founders
    /// Returns the updated Y position and emits events for invite actions
    /// </summary>
    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        if (!viewModel.IsFounder)
            return y; // Only founders can invite

        var currentY = y;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_INVITE_LABEL),
            x, currentY, 15f, ColorPalette.Gold);
        currentY += 22f;

        var inviteInputWidth = width - 120f;
        var newInvitePlayerName = TextInput.Draw(drawList, "##invitePlayer", viewModel.InvitePlayerName,
            x, currentY, inviteInputWidth, 32f,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_INVITE_PLACEHOLDER));

        // Emit event if invite player name changed
        if (newInvitePlayerName != viewModel.InvitePlayerName)
        {
            events.Add(new InfoEvent.InviteNameChanged(newInvitePlayerName));
        }

        // Invite button
        var inviteButtonX = x + inviteInputWidth + 10f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_INVITE_BUTTON),
                inviteButtonX, currentY, 100f, 32f, false,
                !string.IsNullOrWhiteSpace(viewModel.InvitePlayerName)))
            if (!string.IsNullOrWhiteSpace(viewModel.InvitePlayerName))
            {
                events.Add(new InfoEvent.InviteClicked(viewModel.InvitePlayerName.Trim()));
            }

        currentY += 40f;

        return currentY;
    }
}