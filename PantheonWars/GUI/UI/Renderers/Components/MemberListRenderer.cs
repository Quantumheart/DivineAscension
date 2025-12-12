using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Religion;
using PantheonWars.GUI.Models.Religion.Member;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;

namespace PantheonWars.GUI.UI.Renderers.Components;

/// <summary>
/// Pure EDA renderer for the members list. Emits events instead of mutating state.
/// </summary>
public static class MemberListRenderer
{
    /// <summary>
    ///     Draw a member list with scrolling and moderation actions. Pure: ViewModel + DrawList → RenderResult
    /// </summary>
    public static MemberListRenderResult Draw(
        MemberListViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<MemberListEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var members = new List<PlayerReligionInfoResponsePacket.MemberInfo>(viewModel.Members);
        var scrollY = viewModel.ScrollY;
        var currentPlayerUid = viewModel.CurrentPlayerUID;
        var canModerate = viewModel.Role;

        const float itemHeight = 30f;
        const float itemSpacing = 4f;
        const float scrollbarWidth = 16f;

        // Draw background
        var listStart = new Vector2(x, y);
        var listEnd = new Vector2(x + width, y + height);
        var listBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.5f);
        drawList.AddRectFilled(listStart, listEnd, listBgColor, 4f);

        if (members.Count == 0)
        {
            var noMembersText = "No members";
            var noMembersSize = ImGui.CalcTextSize(noMembersText);
            var noMembersPos = new Vector2(x + (width - noMembersSize.X) / 2, y + (height - noMembersSize.Y) / 2);
            var noMembersColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(noMembersPos, noMembersColor, noMembersText);
            return new MemberListRenderResult(events, height);
        }

        // Calculate scroll
        var contentHeight = members.Count * (itemHeight + itemSpacing);
        var maxScroll = Math.Max(0f, contentHeight - height);

        // Handle mouse wheel
        var mousePos = ImGui.GetMousePos();
        var isMouseOver = mousePos.X >= x && mousePos.X <= x + width &&
                          mousePos.Y >= y && mousePos.Y <= y + height;
        if (isMouseOver)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScroll = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScroll - scrollY) > 0.001f)
                {
                    scrollY = newScroll;
                    events.Add(new MemberListEvent.ScrollChanged(scrollY));
                }
            }
        }

        // Clip to bounds
        drawList.PushClipRect(listStart, listEnd, true);

        // Draw members
        var itemY = y - scrollY;

        foreach (var member in members)
        {
            // Skip if not visible
            if (itemY + itemHeight < y || itemY > y + height)
            {
                itemY += itemHeight + itemSpacing;
                continue;
            }

            DrawMemberItem(drawList, member, x, itemY, width - scrollbarWidth - 4f, itemHeight,
                currentPlayerUid, canModerate, events);
            itemY += itemHeight + itemSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);

        return new MemberListRenderResult(events, height);
    }

    /// <summary>
    ///     Draw single member item
    /// </summary>
    private static void DrawMemberItem(
        ImDrawListPtr drawList,
        PlayerReligionInfoResponsePacket.MemberInfo member,
        float x,
        float y,
        float width,
        float height,
        string currentPlayerUid,
        bool canModerate,
        List<MemberListEvent> events)
    {
        const float padding = 8f;
        const float buttonWidth = 50f;
        const float buttonSpacing = 5f;

        // Draw background
        var itemStart = new Vector2(x, y);
        var itemEnd = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.8f);
        drawList.AddRectFilled(itemStart, itemEnd, bgColor, 3f);

        // Player name with role
        var nameText = $"{member.PlayerName} [{member.RoleName}]";
        var namePos = new Vector2(x + padding, y + (height - 14f) / 2);
        var nameColor = ImGui.ColorConvertFloat4ToU32(member.IsFounder ? ColorPalette.Gold : ColorPalette.White);
        drawList.AddText(namePos, nameColor, nameText);

        // Favor rank
        var rankText = $"{member.FavorRank} ({member.Favor})";
        var rankSize = ImGui.CalcTextSize(rankText);

        // Calculate button area width (kick + ban buttons if both callbacks provided)
        var hasBanButton = canModerate; // founders can ban
        var buttonAreaWidth = hasBanButton ? buttonWidth * 2 + buttonSpacing + padding : buttonWidth + padding;

        var rankPos = new Vector2(x + width - buttonAreaWidth - 10f - rankSize.X, y + (height - 14f) / 2);
        var rankColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(rankPos, rankColor, rankText);

        // Action buttons (only if not founder and not self)
        if (canModerate && !member.IsFounder && member.PlayerUID != currentPlayerUid)
        {
            var buttonY = y + (height - 22f) / 2;

            // Ban button (leftmost if both buttons present)
            if (hasBanButton)
            {
                var banButtonX = x + width - (buttonWidth * 2 + buttonSpacing + padding);
                if (ButtonRenderer.DrawSmallButton(drawList, "Ban", banButtonX, buttonY, buttonWidth, 22f))
                {
                    events.Add(new MemberListEvent.BanClicked(member.PlayerUID));
                }
            }

            // Kick button (rightmost)
            var kickButtonX = x + width - buttonWidth - padding;
            if (ButtonRenderer.DrawSmallButton(drawList, "Kick", kickButtonX, buttonY, buttonWidth, 22f))
            {
                events.Add(new MemberListEvent.KickClicked(member.PlayerUID));
            }
        }
    }
}