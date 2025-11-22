using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a scrollable member list for religion management
/// </summary>
[ExcludeFromCodeCoverage]
public static class MemberListRenderer
{
    /// <summary>
    ///     Draw member list with scrolling and kick functionality
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Width of list</param>
    /// <param name="height">Height of list</param>
    /// <param name="members">List of members to display</param>
    /// <param name="scrollY">Current scroll position (will be modified)</param>
    /// <param name="onKickMember">Callback when kick button is clicked (memberUID)</param>
    /// <param name="onBanMember">Callback when ban button is clicked (memberUID)</param>
    /// <returns>Updated scroll position</returns>
    public static float Draw(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        float x,
        float y,
        float width,
        float height,
        List<PlayerReligionInfoResponsePacket.MemberInfo> members,
        float scrollY,
        Action<string> onKickMember,
        Action<string>? onBanMember = null)
    {
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
            return scrollY;
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
                scrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
            }
        }

        // Clip to bounds
        drawList.PushClipRect(listStart, listEnd, true);

        // Draw members
        var itemY = y - scrollY;
        var currentPlayerUID = api.World.Player.PlayerUID;

        foreach (var member in members)
        {
            // Skip if not visible
            if (itemY + itemHeight < y || itemY > y + height)
            {
                itemY += itemHeight + itemSpacing;
                continue;
            }

            DrawMemberItem(drawList, api, member, x, itemY, width - scrollbarWidth - 4f, itemHeight,
                currentPlayerUID, onKickMember, onBanMember);
            itemY += itemHeight + itemSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > height)
        {
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);
        }

        return scrollY;
    }

    /// <summary>
    ///     Draw single member item
    /// </summary>
    private static void DrawMemberItem(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        PlayerReligionInfoResponsePacket.MemberInfo member,
        float x,
        float y,
        float width,
        float height,
        string currentPlayerUID,
        Action<string> onKickMember,
        Action<string>? onBanMember)
    {
        const float padding = 8f;
        const float buttonWidth = 50f;
        const float buttonSpacing = 5f;

        // Draw background
        var itemStart = new Vector2(x, y);
        var itemEnd = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.8f);
        drawList.AddRectFilled(itemStart, itemEnd, bgColor, 3f);

        // Player name
        var nameText = member.PlayerName + (member.IsFounder ? " [Founder]" : "");
        var namePos = new Vector2(x + padding, y + (height - 14f) / 2);
        var nameColor = ImGui.ColorConvertFloat4ToU32(member.IsFounder ? ColorPalette.Gold : ColorPalette.White);
        drawList.AddText(namePos, nameColor, nameText);
        // Calculate button area width (kick + ban buttons if both callbacks provided)
        var hasBanButton = onBanMember != null;

        // Action buttons (only if not founder and not self)
        if (!member.IsFounder && member.PlayerUID != currentPlayerUID)
        {
            var buttonY = y + (height - 22f) / 2;

            // Ban button (leftmost if both buttons present)
            if (hasBanButton)
            {
                var banButtonX = x + width - (buttonWidth * 2 + buttonSpacing + padding);
                if (ButtonRenderer.DrawSmallButton(drawList, "Ban", banButtonX, buttonY, buttonWidth, 22f))
                {
                    api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    onBanMember.Invoke(member.PlayerUID);
                }
            }

            // Kick button (rightmost)
            var kickButtonX = x + width - buttonWidth - padding;
            if (ButtonRenderer.DrawSmallButton(drawList, "Kick", kickButtonX, buttonY, buttonWidth, 22f))
            {
                api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                onKickMember.Invoke(member.PlayerUID);
            }
        }
    }
}
