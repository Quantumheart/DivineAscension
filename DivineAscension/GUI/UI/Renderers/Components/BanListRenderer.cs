using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using ImGuiNET;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a scrollable banned players list for religion management
/// </summary>
[ExcludeFromCodeCoverage]
public static class BanListRenderer
{
    /// <summary>
    ///     Draw banned players list with scrolling and unban functionality
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Width of list</param>
    /// <param name="height">Height of list</param>
    /// <param name="bannedPlayers">List of banned players to display</param>
    /// <param name="scrollY">Current scroll position (will be modified)</param>
    /// <param name="onUnbanPlayer">Callback when unban button is clicked (playerUID)</param>
    /// <returns>Updated scroll position</returns>
    public static float Draw(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        float x,
        float y,
        float width,
        float height,
        List<PlayerReligionInfoResponsePacket.BanInfo> bannedPlayers,
        float scrollY,
        Action<string>? onUnbanPlayer = null)
    {
        const float itemHeight = 40f; // Taller to fit two lines of text
        const float itemSpacing = 4f;
        const float scrollbarWidth = 16f;

        // Draw background
        var listStart = new Vector2(x, y);
        var listEnd = new Vector2(x + width, y + height);
        var listBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.5f);
        drawList.AddRectFilled(listStart, listEnd, listBgColor, 4f);

        if (bannedPlayers.Count == 0)
        {
            var noPlayersText = "No banned players";
            var noPlayersSize = ImGui.CalcTextSize(noPlayersText);
            var noPlayersPos = new Vector2(x + (width - noPlayersSize.X) / 2, y + (height - noPlayersSize.Y) / 2);
            var noPlayersColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(noPlayersPos, noPlayersColor, noPlayersText);
            return scrollY;
        }

        // Calculate scroll
        var contentHeight = bannedPlayers.Count * (itemHeight + itemSpacing);
        var maxScroll = Math.Max(0f, contentHeight - height);

        // Handle mouse wheel
        var mousePos = ImGui.GetMousePos();
        var isMouseOver = mousePos.X >= x && mousePos.X <= x + width &&
                          mousePos.Y >= y && mousePos.Y <= y + height;
        if (isMouseOver)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0) scrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
        }

        // Clip to bounds
        drawList.PushClipRect(listStart, listEnd, true);

        // Draw banned players
        var itemY = y - scrollY;

        foreach (var bannedPlayer in bannedPlayers)
        {
            // Skip if not visible
            if (itemY + itemHeight < y || itemY > y + height)
            {
                itemY += itemHeight + itemSpacing;
                continue;
            }

            DrawBannedPlayerItem(drawList, api, bannedPlayer, x, itemY, width - scrollbarWidth - 4f, itemHeight,
                onUnbanPlayer);
            itemY += itemHeight + itemSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);

        return scrollY;
    }

    /// <summary>
    ///     Draw single banned player item
    /// </summary>
    private static void DrawBannedPlayerItem(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        PlayerReligionInfoResponsePacket.BanInfo bannedPlayer,
        float x,
        float y,
        float width,
        float height,
        Action<string>? onUnbanPlayer)
    {
        const float padding = 8f;
        const float buttonWidth = 60f;

        // Draw background
        var itemStart = new Vector2(x, y);
        var itemEnd = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.8f);
        drawList.AddRectFilled(itemStart, itemEnd, bgColor, 3f);

        // Player name and reason (first line)
        var nameText = $"{bannedPlayer.PlayerName} - {bannedPlayer.Reason}";
        var namePos = new Vector2(x + padding, y + padding);
        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(namePos, nameColor, nameText);

        // Ban details (second line)
        var expiryText = bannedPlayer.IsPermanent ? "Never" : bannedPlayer.ExpiresAt;
        var detailsText = $"Banned: {bannedPlayer.BannedAt} | Expires: {expiryText}";
        var detailsPos = new Vector2(x + padding + 10f, y + padding + 16f);
        var detailsColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.9f);

        // Use smaller font for details if available
        drawList.AddText(detailsPos, detailsColor, detailsText);

        // Unban button (only if callback provided)
        if (onUnbanPlayer != null)
        {
            var buttonY = y + (height - 22f) / 2;
            var unbanButtonX = x + width - buttonWidth - padding;

            if (ButtonRenderer.DrawSmallButton(drawList, "Unban", unbanButtonX, buttonY, buttonWidth, 22f))
            {
                onUnbanPlayer.Invoke(bannedPlayer.PlayerUID);
            }
        }
    }
}