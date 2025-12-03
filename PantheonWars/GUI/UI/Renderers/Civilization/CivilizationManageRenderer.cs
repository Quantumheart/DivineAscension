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

        // Loading state for My Civilization tab
        if (state.IsMyCivLoading)
        {
            TextRenderer.DrawInfoText(drawList, "Loading civilization data...", x, currentY + 8f, width);
            return height;
        }

        var civ = state.MyCivilization;
        if (civ == null)
        {
            TextRenderer.DrawInfoText(drawList, "You are not currently in a civilization.", x, currentY + 8f, width);
            return height;
        }

        // Header
        TextRenderer.DrawLabel(drawList, civ.Name, x, currentY + 4f, 18f, ColorPalette.White);
        currentY += 28f;

        // Info grid
        var leftCol = x;
        var rightCol = x + width / 2f;

        // Founded date
        TextRenderer.DrawLabel(drawList, "Founded:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), civ.CreatedDate.ToString("yyyy-MM-dd"));

        // Member count
        TextRenderer.DrawLabel(drawList, "Members:", rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), $"{civ.MemberReligions.Count}/4");

        currentY += 20f;

        // Civilization founder (player name)
        TextRenderer.DrawLabel(drawList, "Founder:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), civ.FounderName);

        currentY += 20f;

        // Founding religion
        TextRenderer.DrawLabel(drawList, "Founding Religion:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), civ.FounderReligionName);

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
            var canInvite = !string.IsNullOrWhiteSpace(state.InviteReligionName) && !state.IsInvitesLoading && !state.IsMyCivLoading;
            if (ButtonRenderer.DrawButton(drawList, "Send Invite", inviteButtonX, currentY - 2f, 140f, 32f, true,
                    enabled: canInvite))
            {
                manager.RequestCivilizationAction("invite", civ.CivId, state.InviteReligionName);
                state.InviteReligionName = string.Empty;
            }

            currentY += 40f;

            // Pending invites list (may be loading)
            if (state.IsInvitesLoading)
            {
                TextRenderer.DrawInfoText(drawList, "Loading invitations...", x, currentY + 8f, width);
                currentY += 30f;
            }
            else if (civ.PendingInvites.Count > 0)
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
        TextRenderer.DrawLabel(drawList, member.ReligionName, x + 32f, y + 10f, 16f);
        var subText = $"Deity: {member.Deity}  |  Members: {member.MemberCount}";
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 32f, y + 34f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), subText);

        // Kick button for founder only and not the civilization founder religion
        var isCivFounderReligion = manager.CivilizationFounderReligionUID == member.ReligionId;
        if (manager.IsCivilizationFounder && !isCivFounderReligion)
        {
            var kickEnabled = !manager.CivState.IsMyCivLoading; // disabled while tab loading
            if (ButtonRenderer.DrawSmallButton(drawList, "Kick", x + width - 80f, y + (height - 26f) / 2f, 70f, 26f,
                    ColorPalette.Red * 0.7f) && kickEnabled)
            {
                // Use ReligionId (not ReligionName) as the server expects an ID
                manager.RequestCivilizationAction("kick", manager.CurrentCivilizationId ?? string.Empty, member.ReligionId);
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
