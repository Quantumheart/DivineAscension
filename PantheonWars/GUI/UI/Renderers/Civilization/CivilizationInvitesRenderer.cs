using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationInvitesRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        TextRenderer.DrawLabel(drawList, "Your Civilization Invitations", x, currentY, 18f, ColorPalette.White);
        currentY += 26f;

        // Help text explaining where to send invites
        TextRenderer.DrawInfoText(drawList, "This tab shows invitations you've received. To send invitations, go to the \"My Civilization\" tab (founders only).", x, currentY, width);
        currentY += 32f;

        if (state.MyInvites.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList, "No pending invitations.", x, currentY + 8f, width);
            return height;
        }

        state.InvitesScrollY = ScrollableList.Draw(
            drawList,
            x,
            currentY,
            width,
            height - (currentY - y),
            state.MyInvites,
            80f,
            10f,
            state.InvitesScrollY,
            (invite, cx, cy, cw, ch) => DrawInviteCard(invite, cx, cy, cw, ch, manager, api),
            loadingText: state.IsInvitesLoading ? "Loading invitations..." : null
        );

        return height;
    }

    private static void DrawInviteCard(
        CivilizationInfoResponsePacket.PendingInvite invite,
        float x,
        float y,
        float width,
        float height,
        BlessingDialogManager manager,
        ICoreClientAPI api)
    {
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        TextRenderer.DrawLabel(drawList, "Invitation to Civilization", x + 12f, y + 8f, 16f);
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 14f, y + 30f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"From: {invite.ReligionName}");
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 14f, y + 48f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"Expires: {invite.ExpiresAt:yyyy-MM-dd HH:mm}");

        var enabled = !manager.CivState.IsInvitesLoading;
        if (ButtonRenderer.DrawButton(drawList, "Accept", x + width - 180f, y + height - 32f, 80f, 28f, true,
                enabled: enabled))
        {
            manager.RequestCivilizationAction("accept", "", invite.InviteId);
        }

        if (ButtonRenderer.DrawButton(drawList, "Decline", x + width - 90f, y + height - 32f, 80f, 28f,
                isPrimary: false, enabled: enabled))
        {
            api.ShowChatMessage("Decline functionality coming soon!");
        }
    }
}
