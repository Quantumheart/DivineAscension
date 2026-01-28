using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Invites;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

[ExcludeFromCodeCoverage]
internal static class CivilizationInvitesRenderer
{
    public static CivilizationInvitesRendererResult Draw(
        CivilizationInvitesViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<InvitesEvent>();
        var currentY = vm.Y;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_TITLE), vm.X, currentY, SectionHeader,
            ColorPalette.White);
        currentY += 26f;

        // Help text explaining where to send invites
        TextRenderer.DrawInfoText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_DESCRIPTION),
            vm.X, currentY, vm.Width);
        currentY += 32f;

        if (!vm.HasInvites)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_NO_INVITATIONS), vm.X,
                currentY + 8f, vm.Width);
            return new CivilizationInvitesRendererResult(events, vm.Height);
        }

        var listHeight = vm.Height - (currentY - vm.Y);
        var invitesList = vm.Invites.ToList();

        var newScrollY = ScrollableList.Draw(
            drawList,
            vm.X,
            currentY,
            vm.Width,
            listHeight,
            invitesList,
            80f,
            10f,
            vm.ScrollY,
            (invite, cx, cy, cw, ch) => DrawInviteCard(invite, cx, cy, cw, ch, drawList, vm.IsLoading, events),
            loadingText: vm.IsLoading
                ? LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_LOADING)
                : null
        );

        // Emit scroll event if changed
        if (newScrollY != vm.ScrollY)
            events.Add(new InvitesEvent.ScrollChanged(newScrollY));

        return new CivilizationInvitesRendererResult(events, vm.Height);
    }

    private static void DrawInviteCard(
        CivilizationInfoResponsePacket.PendingInvite invite,
        float x,
        float y,
        float width,
        float height,
        ImDrawListPtr drawList,
        bool isLoading,
        List<InvitesEvent> events)
    {
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_CARD_TITLE), x + 12f, y + 8f,
            TableHeader);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x + 14f, y + 30f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_CARD_FROM, invite.ReligionName));
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x + 14f, y + 48f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_CARD_EXPIRES,
                invite.ExpiresAt.ToString("yyyy-MM-dd HH:mm")));

        var enabled = !isLoading;

        // Accept button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_ACCEPT_BUTTON),
                x + width - 180f, y + height - 32f, 80f, 28f, true, enabled))
            events.Add(new InvitesEvent.AcceptInviteClicked(invite.InviteId));

        // Decline button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_DECLINE_BUTTON),
                x + width - 90f, y + height - 32f, 80f, 28f, false, enabled))
            events.Add(new InvitesEvent.DeclineInviteClicked(invite.InviteId));
    }
}