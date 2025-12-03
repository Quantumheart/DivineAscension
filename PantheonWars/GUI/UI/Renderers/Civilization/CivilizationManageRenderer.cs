using System;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationManageRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        var civ = state.MyCivilization;
        if (civ == null)
        {
            TextRenderer.DrawInfoText(drawList, "You are not currently in a civilization.", x, currentY + 8f, width);
            return height;
        }

        // Header
        TextRenderer.DrawLabel(drawList, civ.Name, x, currentY + 4f, 18f, ColorPalette.White);
        currentY += 28f;

        // Members section title
        TextRenderer.DrawLabel(drawList, "Member Religions", x, currentY, 14f, ColorPalette.Grey);
        currentY += 22f;

        // Members list
        state.MemberScrollY = ScrollableList.Draw(
            drawList,
            x,
            currentY,
            width,
            height * 0.45f,
            civ.MemberReligions,
            64f,
            6f,
            state.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawMemberCard(member, cx, cy, cw, ch, manager, api)
        );

        currentY += height * 0.45f + 10f;

        // Invite input and pending invites (only for founder)
        var isFounder = manager.IsCivilizationFounder;
        if (isFounder)
        {
            TextRenderer.DrawLabel(drawList, "Invite Religion by Name:", x, currentY, 14f);
            currentY += 22f;

            // Text input
            state.InviteReligionName = PantheonWars.GUI.UI.Components.Inputs.TextInput.Draw(
                drawList,
                "##inviteReligion",
                state.InviteReligionName,
                x,
                currentY,
                width * 0.6f,
                30f,
                placeholder: "Enter religion name...",
                maxLength: 64
            );

            // Send invite
            var inviteButtonX = x + (width * 0.6f) + 10f;
            if (ButtonRenderer.DrawButton(drawList, "Send Invite", inviteButtonX, currentY - 2f, 140f, 32f, true,
                    enabled: !string.IsNullOrWhiteSpace(state.InviteReligionName)))
            {
                manager.RequestCivilizationAction("invite", civ.CivId, state.InviteReligionName);
                state.InviteReligionName = string.Empty;
            }

            currentY += 40f;

            if (civ.PendingInvites.Count > 0)
            {
                TextRenderer.DrawLabel(drawList, "Pending Invitations", x, currentY, 14f, ColorPalette.Grey);
                currentY += 22f;

                // Simple list of pending invites
                foreach (var invite in civ.PendingInvites)
                {
                    DrawInviteRow(invite, x, currentY, width, 26f);
                    currentY += 30f;
                }
            }
        }

        // Footer actions
        currentY += 10f;
        if (ButtonRenderer.DrawActionButton(drawList, "Leave Civilization", x, currentY, 180f, 34f, false))
        {
            manager.RequestCivilizationAction("leave");
        }

        if (isFounder)
        {
            if (ButtonRenderer.DrawActionButton(drawList, "Disband Civilization", x + 190f, currentY, 200f, 34f, true))
            {
                manager.RequestCivilizationAction("disband", civ.CivId);
            }
        }

        return currentY + 40f - y;
    }

    private static void DrawMemberCard(
        CivilizationInfoResponsePacket.MemberReligion member,
        float x,
        float y,
        float width,
        float height,
        BlessingDialogManager manager,
        ICoreClientAPI api)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        // Deity chip
        var deityColor = DeityHelper.GetDeityColor(member.Deity);
        drawList.AddCircleFilled(new Vector2(x + 12f, y + height / 2f), 8f, ImGui.ColorConvertFloat4ToU32(deityColor));

        // Texts
        TextRenderer.DrawLabel(drawList, member.ReligionName, x + 32f, y + 10f, 14f);
        var subText = $"Deity: {member.Deity}  •  Members: {member.MemberCount}";
        drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x + 32f, y + 34f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), subText);

        // Kick button for founder only and not the civilization founder religion
        var isCivFounderReligion = manager.CivilizationFounderReligionUID == member.ReligionId;
        if (manager.IsCivilizationFounder && !isCivFounderReligion)
        {
            if (ButtonRenderer.DrawSmallButton(drawList, "Kick", x + width - 80f, y + (height - 26f) / 2f, 70f, 26f,
                    ColorPalette.Red * 0.7f))
            {
                manager.RequestCivilizationAction("kick", manager.CurrentCivilizationId ?? string.Empty, member.ReligionName);
            }
        }
    }

    private static void DrawInviteRow(CivilizationInfoResponsePacket.PendingInvite invite, float x, float y, float width, float height)
    {
        var drawList = ImGui.GetWindowDrawList();
        // Row background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.6f), 3f);
        // Texts
        var text = $"{invite.ReligionName} (expires {invite.ExpiresAt:yyyy-MM-dd})";
        drawList.AddText(new Vector2(x + 8f, y + 6f), ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }
}
