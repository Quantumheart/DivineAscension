using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

[ExcludeFromCodeCoverage]
internal static class CivilizationInfoRenderer
{
    public static CivilizationInfoRendererResult Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        IReadOnlyList<CivilizationInfoResponsePacket.PendingInvite>? pendingInvites,
        string? founderReligionUID,
        DateTime createdDate)
    {
        var events = new List<InfoEvent>();
        var currentY = vm.Y;

        var overlayOpen = vm.ShowDisbandConfirm || vm.IsKickConfirmOpen;

        // Loading state for My Civilization tab
        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LOADING),
                vm.X, currentY + 8f, vm.Width);
            return new CivilizationInfoRendererResult(events, vm.Height);
        }

        if (!vm.HasCivilization)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_NOT_IN_CIV),
                vm.X, currentY + 8f, vm.Width);
            return new CivilizationInfoRendererResult(events, vm.Height);
        }

        // Header with icon
        const float iconSize = 32f;
        var iconTextureId = CivilizationIconLoader.GetIconTextureId(vm.Icon);

        if (iconTextureId != IntPtr.Zero)
        {
            // Draw icon
            var iconMin = new Vector2(vm.X, currentY);
            var iconMax = new Vector2(vm.X + iconSize, currentY + iconSize);
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Draw icon border
            var iconBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
            drawList.AddRect(iconMin, iconMax, iconBorderColor, 4f, ImDrawFlags.None, 1f);
        }

        // Draw civilization name next to icon
        TextRenderer.DrawLabel(drawList, vm.CivName, vm.X + iconSize + 12f, currentY + 4f, 18f, ColorPalette.White);
        currentY += Math.Max(iconSize + 4f, 32f);

        // Info grid
        var leftCol = vm.X;
        var rightCol = vm.X + vm.Width / 2f;

        // Founded date
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_FOUNDED),
            leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), createdDate.ToString("yyyy-MM-dd"));

        // Member count
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_MEMBERS),
            rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), $"{vm.MemberReligions?.Count ?? 0}/4");

        currentY += 20f;

        // Civilization founder (player name)
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_FOUNDER),
            leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.FounderName);

        currentY += 20f;

        // Founding religion
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_FOUNDING_RELIGION),
            leftCol, currentY, 13f, ColorPalette.Grey);
        var founderReligionName =
            vm.MemberReligions?.FirstOrDefault(m => m.ReligionId == founderReligionUID)?.ReligionName ??
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_UNKNOWN_RELIGION);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), founderReligionName);

        currentY += 28f;

        // Description section
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DESCRIPTION_LABEL),
            vm.X, currentY, 14f, ColorPalette.Grey);
        currentY += 22f;

        if (vm.IsFounder)
        {
            // Editable description for founder
            var newDescription = TextInput.DrawMultiline(drawList, "##civDescription", vm.DescriptionText,
                vm.X, currentY,
                vm.Width * 0.7f, 80f, 200);

            if (newDescription != vm.DescriptionText)
                events.Add(new InfoEvent.DescriptionChanged(newDescription));

            currentY += 90f;

            // Save description button (only visible when there are changes)
            if (vm.HasDescriptionChanges)
            {
                if (ButtonRenderer.DrawButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_SAVE_DESCRIPTION_BUTTON),
                        vm.X, currentY, 160f, 30f, true, !overlayOpen))
                    events.Add(new InfoEvent.SaveDescriptionClicked());

                currentY += 40f;
            }
        }
        else if (!string.IsNullOrEmpty(vm.Description))
        {
            // Read-only description for non-founders
            TextRenderer.DrawInfoText(drawList, vm.Description, vm.X, currentY, vm.Width * 0.9f);
            currentY += 60f;
        }

        currentY += 10f;

        // Members section title
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_MEMBER_RELIGIONS),
            vm.X, currentY, 14f, ColorPalette.Grey);
        currentY += 22f;

        // Members list
        var membersList = vm.MemberReligions?.ToList() ?? new List<CivilizationInfoResponsePacket.MemberReligion>();
        var membersListHeight = vm.Height * 0.45f;

        var newMemberScrollY = ScrollableList.Draw(
            drawList,
            vm.X,
            currentY,
            vm.Width,
            membersListHeight,
            membersList,
            64f,
            6f,
            vm.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawMemberCard(member, cx, cy, cw, ch, drawList, vm, founderReligionUID, events)
        );

        // Emit scroll event if changed
        if (newMemberScrollY != vm.MemberScrollY)
            events.Add(new InfoEvent.MemberScrollChanged(newMemberScrollY));

        currentY += membersListHeight + 10f;

        // Invite input and pending invites (only for founder)
        if (vm.IsFounder)
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITE_LABEL),
                vm.X, currentY);
            currentY += 22f;

            // Text input
            var newInviteReligionName = TextInput.Draw(
                drawList,
                "##inviteReligion",
                vm.InviteReligionName,
                vm.X,
                currentY,
                vm.Width * 0.6f,
                30f,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITE_PLACEHOLDER),
                64
            );

            if (newInviteReligionName != vm.InviteReligionName)
                events.Add(new InfoEvent.InviteReligionNameChanged(newInviteReligionName));

            // Send invite button
            var inviteButtonX = vm.X + vm.Width * 0.6f + 10f;
            var canInvite = !overlayOpen && vm.CanInvite && !vm.IsLoading;
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITE_BUTTON),
                    inviteButtonX, currentY - 2f, 140f, 32f, true, canInvite))
                events.Add(new InfoEvent.InviteReligionClicked(vm.InviteReligionName));

            currentY += 40f;

            // Pending invites list (may be loading)
            if (vm.IsLoading)
            {
                TextRenderer.DrawInfoText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITATIONS_LOADING),
                    vm.X, currentY + 8f, vm.Width);
                currentY += 30f;
            }
            else if (pendingInvites != null && pendingInvites.Count > 0)
            {
                TextRenderer.DrawLabel(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_PENDING_INVITATIONS),
                    vm.X, currentY, 14f, ColorPalette.Grey);
                currentY += 22f;

                // Simple list of pending invites
                foreach (var invite in pendingInvites)
                {
                    DrawInviteRow(invite, vm.X, currentY, vm.Width, 26f, drawList);
                    currentY += 30f;
                }
            }
        }

        // Footer actions
        currentY += 10f;
        var leaveEnabled = !overlayOpen;
        if (leaveEnabled && ButtonRenderer.DrawActionButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LEAVE_BUTTON),
                vm.X, currentY, 180f, 34f))
            events.Add(new InfoEvent.LeaveClicked());

        if (vm.IsFounder)
        {
            var editIconEnabled = !overlayOpen;
            if (editIconEnabled && ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_EDIT_ICON_BUTTON),
                    vm.X + 190f, currentY, 120f, 34f))
                events.Add(new InfoEvent.EditIconClicked());

            var disbandEnabled = !overlayOpen;
            if (disbandEnabled && ButtonRenderer.DrawActionButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_BUTTON),
                    vm.X + 320f, currentY, 200f, 34f, true))
                events.Add(new InfoEvent.DisbandOpened());
        }

        // Draw confirmation overlays (modal)
        currentY += 40f;

        // Disband confirmation
        if (vm.ShowDisbandConfirm)
        {
            ConfirmOverlay.Draw(
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_CONFIRM_TITLE),
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_CONFIRM_MESSAGE),
                out var confirmed,
                out var canceled,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_CONFIRM_BUTTON));

            if (confirmed) events.Add(new InfoEvent.DisbandConfirmed());
            if (canceled) events.Add(new InfoEvent.DisbandCancel());
        }

        // Kick confirmation
        if (vm.IsKickConfirmOpen)
        {
            var targetId = vm.KickConfirmReligionId!;
            var targetName = vm.MemberReligions?.FirstOrDefault(m => m.ReligionId == targetId)?.ReligionName ??
                             LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_UNKNOWN_RELIGION);

            ConfirmOverlay.Draw(
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_CONFIRM_TITLE),
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_CONFIRM_MESSAGE,
                    targetName),
                out var confirmed,
                out var canceled,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_CONFIRM_BUTTON));

            if (confirmed) events.Add(new InfoEvent.KickConfirm(targetName));
            if (canceled) events.Add(new InfoEvent.KickCancel());
        }

        return new CivilizationInfoRendererResult(events, currentY - vm.Y);
    }

    private static void DrawMemberCard(
        CivilizationInfoResponsePacket.MemberReligion member,
        float x,
        float y,
        float width,
        float height,
        ImDrawListPtr drawList,
        CivilizationInfoViewModel vm,
        string? founderReligionUID,
        List<InfoEvent> events)
    {
        // Background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        // Deity icon
        const float deityIconSize = 16f;
        if (Enum.TryParse<DeityDomain>(member.Domain, out var deityType))
        {
            var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
            var iconX = x + 12f - deityIconSize / 2f;
            var iconY = y + height / 2f - deityIconSize / 2f;
            drawList.AddImage(deityTextureId,
                new Vector2(iconX, iconY),
                new Vector2(iconX + deityIconSize, iconY + deityIconSize),
                Vector2.Zero, Vector2.One,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
        }

        // Texts
        TextRenderer.DrawLabel(drawList, member.ReligionName, x + 32f, y + 10f, 16f);
        var deityLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_RELIGION_CARD_DEITY);
        var membersLabel =
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_RELIGION_CARD_MEMBERS);
        var subText = $"{deityLabel} {member.Domain}  |  {membersLabel} {member.MemberCount}";
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 32f, y + 34f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), subText);

        // Kick button for founder only and not the civilization founder religion
        var isCivFounderReligion = founderReligionUID == member.ReligionId;
        if (vm.IsFounder && !isCivFounderReligion)
        {
            var kickEnabled = !vm.IsLoading && !vm.IsKickConfirmOpen && !vm.ShowDisbandConfirm;
            if (kickEnabled && ButtonRenderer.DrawSmallButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_BUTTON),
                    x + width - 80f, y + (height - 26f) / 2f, 70f, 26f, ColorPalette.Red * 0.7f))
                events.Add(new InfoEvent.KickOpen(member.ReligionId, member.ReligionName));
        }
    }

    private static void DrawInviteRow(
        CivilizationInfoResponsePacket.PendingInvite invite,
        float x,
        float y,
        float width,
        float height,
        ImDrawListPtr drawList)
    {
        // Row background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.6f), 3f);
        // Texts
        var expiresLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITE_EXPIRES);
        var text = $"{invite.ReligionName} ({expiresLabel} {invite.ExpiresAt:yyyy-MM-dd})";
        drawList.AddText(new Vector2(x + 8f, y + 6f), ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }
}