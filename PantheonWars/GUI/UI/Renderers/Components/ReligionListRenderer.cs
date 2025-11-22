using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a scrollable religion list for religion browser
/// </summary>
[ExcludeFromCodeCoverage]
public static class ReligionListRenderer
{
    /// <summary>
    ///     Draw religion list with scrolling and selection
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Width of list</param>
    /// <param name="height">Height of list</param>
    /// <param name="religions">List of religions to display</param>
    /// <param name="isLoading">Whether the list is loading</param>
    /// <param name="scrollY">Current scroll position (will be modified)</param>
    /// <param name="selectedReligionUID">Currently selected religion UID (will be modified)</param>
    /// <returns>Tuple of (updated scroll Y, updated selected UID, hovered religion)</returns>
    public static (float scrollY, string? selectedUID, ReligionListResponsePacket.ReligionInfo? hoveredReligion) Draw(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        float x,
        float y,
        float width,
        float height,
        List<ReligionListResponsePacket.ReligionInfo> religions,
        bool isLoading,
        float scrollY,
        string? selectedReligionUID)
    {
        const float itemHeight = 80f;
        const float itemSpacing = 8f;
        const float scrollbarWidth = 16f;

        // Draw list background
        var listStart = new Vector2(x, y);
        var listEnd = new Vector2(x + width, y + height);
        var listBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.5f);
        drawList.AddRectFilled(listStart, listEnd, listBgColor, 4f);

        // Loading state
        if (isLoading)
        {
            var loadingText = "Loading religions...";
            var loadingSize = ImGui.CalcTextSize(loadingText);
            var loadingPos = new Vector2(
                x + (width - loadingSize.X) / 2,
                y + (height - loadingSize.Y) / 2
            );
            var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(loadingPos, loadingColor, loadingText);
            return (scrollY, selectedReligionUID, null);
        }

        // No religions state
        if (religions.Count == 0)
        {
            var noReligionText = "No religions found";
            var noReligionSize = ImGui.CalcTextSize(noReligionText);
            var noReligionPos = new Vector2(
                x + (width - noReligionSize.X) / 2,
                y + (height - noReligionSize.Y) / 2
            );
            var noReligionColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(noReligionPos, noReligionColor, noReligionText);
            return (scrollY, selectedReligionUID, null);
        }

        // Calculate scroll limits
        var contentHeight = religions.Count * (itemHeight + itemSpacing);
        var maxScroll = Math.Max(0f, contentHeight - height);

        // Handle mouse wheel scrolling
        var mousePos = ImGui.GetMousePos();
        var isMouseOver = mousePos.X >= x && mousePos.X <= x + width &&
                          mousePos.Y >= y && mousePos.Y <= y + height;
        if (isMouseOver)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                scrollY = Math.Clamp(scrollY - wheel * 40f, 0f, maxScroll);
            }
        }

        // Clip to list bounds
        drawList.PushClipRect(listStart, listEnd, true);

        // Draw religion items and track hovered item
        ReligionListResponsePacket.ReligionInfo? hoveredReligion = null;
        var itemY = y - scrollY;
        for (int i = 0; i < religions.Count; i++)
        {
            var religion = religions[i];

            // Skip if not visible
            if (itemY + itemHeight < y || itemY > y + height)
            {
                itemY += itemHeight + itemSpacing;
                continue;
            }

            var (clickedUID, isHovered) = DrawReligionItem(drawList, api, religion, x, itemY, width - scrollbarWidth, itemHeight, selectedReligionUID);
            if (clickedUID != null)
            {
                selectedReligionUID = clickedUID;
            }
            if (isHovered)
            {
                hoveredReligion = religion;
            }
            itemY += itemHeight + itemSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > height)
        {
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);
        }

        return (scrollY, selectedReligionUID, hoveredReligion);
    }

    /// <summary>
    ///     Draw a single religion item
    /// </summary>
    /// <returns>Tuple of (Religion UID if clicked, whether item is hovered)</returns>
    private static (string? clickedUID, bool isHovered) DrawReligionItem(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        ReligionListResponsePacket.ReligionInfo religion,
        float x,
        float y,
        float width,
        float height,
        string? currentSelectedUID)
    {
        const float padding = 12f;

        var itemStart = new Vector2(x, y);
        var itemEnd = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + width &&
                        mousePos.Y >= y && mousePos.Y <= y + height;
        var isSelected = currentSelectedUID == religion.ReligionUID;

        // Determine background color
        Vector4 bgColor;
        if (isSelected)
        {
            bgColor = ColorPalette.Gold * 0.3f;
        }
        else if (isHovering)
        {
            bgColor = ColorPalette.LightBrown * 0.7f;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            bgColor = ColorPalette.DarkBrown;
        }

        // Draw background
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(itemStart, itemEnd, bgColorU32, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.Grey * 0.5f);
        drawList.AddRect(itemStart, itemEnd, borderColor, 4f, ImDrawFlags.None, isSelected ? 2f : 1f);

        // Handle click
        string? clickedUID = null;
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            clickedUID = religion.ReligionUID;
            api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
        }

        // Note: Deity icons removed as part of guild conversion - no longer displayed

        // Draw religion name
        var namePos = new Vector2(x + padding, y + padding);
        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 16f, namePos, nameColor, religion.ReligionName);

        // Draw member count and visibility
        var infoText = $"{religion.MemberCount} members | {(religion.IsPublic ? "Public" : "Private")}";
        var infoPos = new Vector2(x + padding, y + padding + 24f);
        var infoColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 12f, infoPos, infoColor, infoText);

        return (clickedUID, isHovering);
    }

    /// <summary>
    ///     Draw tooltip for a religion
    /// </summary>
    public static void DrawTooltip(
        ReligionListResponsePacket.ReligionInfo religion,
        float mouseX,
        float mouseY,
        float windowWidth,
        float windowHeight)
    {
        const float tooltipMaxWidth = 300f;
        const float tooltipPadding = 12f;
        const float lineSpacing = 6f;

        var drawList = ImGui.GetForegroundDrawList();

        // Build tooltip content
        var lines = new List<string>();

        // Religion name (title)
        lines.Add(religion.ReligionName);

        // Separator
        lines.Add(""); // Empty line for spacing

        // Member count
        lines.Add($"Members: {religion.MemberCount}");

        // Public/Private status
        lines.Add($"Status: {(religion.IsPublic ? "Public" : "Private")}");

        // Description (if available)
        if (!string.IsNullOrEmpty(religion.Description))
        {
            lines.Add(""); // Empty line for spacing
            lines.Add("Description:");

            // Wrap description text
            var wrappedDesc = WrapText(religion.Description, tooltipMaxWidth - tooltipPadding * 2, 13f);
            lines.AddRange(wrappedDesc);
        }

        // Calculate tooltip dimensions
        var lineHeight = 16f;
        var tooltipHeight = tooltipPadding * 2 + (lines.Count * lineHeight);
        var tooltipWidth = tooltipMaxWidth;

        // Get window position for screen-space positioning
        var windowPos = ImGui.GetWindowPos();

        // Position tooltip (offset from mouse, check screen edges)
        var offsetX = 16f;
        var offsetY = 16f;

        var tooltipX = mouseX + offsetX;
        var tooltipY = mouseY + offsetY;

        // Check right edge
        if (tooltipX - windowPos.X + tooltipWidth > windowWidth)
            tooltipX = mouseX - tooltipWidth - offsetX;

        // Check bottom edge
        if (tooltipY - windowPos.Y + tooltipHeight > windowHeight)
            tooltipY = mouseY - tooltipHeight - offsetY;

        // Ensure doesn't go off left edge
        if (tooltipX < windowPos.X)
            tooltipX = windowPos.X + 4f;

        // Ensure doesn't go off top edge
        if (tooltipY < windowPos.Y)
            tooltipY = windowPos.Y + 4f;

        // Draw tooltip background
        var bgStart = new Vector2(tooltipX, tooltipY);
        var bgEnd = new Vector2(tooltipX + tooltipWidth, tooltipY + tooltipHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(bgStart, bgEnd, bgColor, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
        drawList.AddRect(bgStart, bgEnd, borderColor, 4f, ImDrawFlags.None, 2f);

        // Draw content
        var currentY = tooltipY + tooltipPadding;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (string.IsNullOrEmpty(line))
            {
                // Empty line - just add spacing
                currentY += lineSpacing;
                continue;
            }

            Vector4 textColor;
            float fontSize;

            if (i == 0)
            {
                // Title (religion name)
                textColor = ColorPalette.Gold;
                fontSize = 16f;
            }
            else if (line.StartsWith("Description:") || line.StartsWith("Members:") || line.StartsWith("Status:"))
            {
                // Section headers
                textColor = ColorPalette.Grey;
                fontSize = 12f;
            }
            else
            {
                // Regular text
                textColor = ColorPalette.White;
                fontSize = 13f;
            }

            var textPos = new Vector2(tooltipX + tooltipPadding, currentY);
            var textColorU32 = ImGui.ColorConvertFloat4ToU32(textColor);
            drawList.AddText(ImGui.GetFont(), fontSize, textPos, textColorU32, line);

            currentY += lineHeight;
        }
    }

    /// <summary>
    ///     Wrap text to fit within max width
    /// </summary>
    private static List<string> WrapText(string text, float maxWidth, float fontSize)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) return result;

        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = ImGui.CalcTextSize(testLine);
            var scaledWidth = testSize.X * (fontSize / ImGui.GetFontSize());

            if (scaledWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            result.Add(currentLine);

        return result;
    }
}
