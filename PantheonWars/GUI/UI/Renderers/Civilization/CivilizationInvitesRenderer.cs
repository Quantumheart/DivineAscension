using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Civilization;
using PantheonWars.GUI.Models.Civilization.Invites;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationInvitesRenderer
{
    public static CivilizationInvitesRendererResult Draw(
        CivilizationInvitesViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<InvitesEvent>();
        var currentY = vm.Y;

        TextRenderer.DrawLabel(drawList, "Your Civilization Invitations", vm.X, currentY, 18f, ColorPalette.White);
        currentY += 26f;

        // Help text explaining where to send invites
        TextRenderer.DrawInfoText(drawList,
            "This tab shows invitations you've received. To send invitations, go to the \"Info\" tab (founders only).",
            vm.X, currentY, vm.Width);
        currentY += 32f;

        if (!vm.HasInvites)
        {
            TextRenderer.DrawInfoText(drawList, "No pending invitations.", vm.X, currentY + 8f, vm.Width);
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
            loadingText: vm.IsLoading ? "Loading invitations..." : null
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

        TextRenderer.DrawLabel(drawList, "Invitation to Civilization", x + 12f, y + 8f, 16f);
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 14f, y + 30f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"From: {invite.ReligionName}");
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 14f, y + 48f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"Expires: {invite.ExpiresAt:yyyy-MM-dd HH:mm}");

        var enabled = !isLoading;

        // Accept button
        if (ButtonRenderer.DrawButton(drawList, "Accept", x + width - 180f, y + height - 32f, 80f, 28f, true, enabled))
            events.Add(new InvitesEvent.AcceptInviteClicked(invite.InviteId));

        // Decline button
        if (ButtonRenderer.DrawButton(drawList, "Decline", x + width - 90f, y + height - 32f, 80f, 28f, false, enabled))
            events.Add(new InvitesEvent.AcceptInviteDeclined(invite.InviteId));
    }
}