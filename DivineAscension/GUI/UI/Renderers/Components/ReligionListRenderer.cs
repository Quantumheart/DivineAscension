using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.List;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a scrollable religion list for religion browser
/// </summary>
[ExcludeFromCodeCoverage]
public static class ReligionListRenderer
{
    /// <summary>
    /// Pure renderer for a scrollable religion list. Emits events instead of mutating state.
    /// </summary>
    public static ReligionListRenderResult Draw(
        ReligionListViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ListEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var religions = new List<ReligionListResponsePacket.ReligionInfo>(viewModel.Religions);
        var isLoading = viewModel.IsLoading;
        var scrollY = viewModel.ScrollY;
        var selectedReligionUID = viewModel.SelectedReligionUID;

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
            var loadingText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_LOADING);
            var loadingSize = ImGui.CalcTextSize(loadingText);
            var loadingPos = new Vector2(
                x + (width - loadingSize.X) / 2,
                y + (height - loadingSize.Y) / 2
            );
            var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(loadingPos, loadingColor, loadingText);
            return new ReligionListRenderResult(events, null, height);
        }

        // No religions state
        if (religions.Count == 0)
        {
            var noReligionText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_NO_RELIGIONS);
            var noReligionSize = ImGui.CalcTextSize(noReligionText);
            var noReligionPos = new Vector2(
                x + (width - noReligionSize.X) / 2,
                y + (height - noReligionSize.Y) / 2
            );
            var noReligionColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(noReligionPos, noReligionColor, noReligionText);
            return new ReligionListRenderResult(events, null, height);
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
                var newScroll = Math.Clamp(scrollY - wheel * 40f, 0f, maxScroll);
                if (Math.Abs(newScroll - scrollY) > 0.01f)
                {
                    scrollY = newScroll;
                    events.Add(new ListEvent.ScrollChanged(scrollY));
                }
            }
        }

        // Clip to list bounds
        drawList.PushClipRect(listStart, listEnd, true);

        // Draw religion items and track hovered item
        ReligionListResponsePacket.ReligionInfo? hoveredReligion = null;
        var itemY = y - scrollY;
        for (var i = 0; i < religions.Count; i++)
        {
            var religion = religions[i];

            // Skip if not visible
            if (itemY + itemHeight < y || itemY > y + height)
            {
                itemY += itemHeight + itemSpacing;
                continue;
            }

            var (clickedUID, isHovered) = DrawReligionItem(drawList, religion, x, itemY, width - scrollbarWidth,
                itemHeight, selectedReligionUID);
            if (clickedUID != null)
            {
                selectedReligionUID = clickedUID;
                events.Add(new ListEvent.ItemClicked(clickedUID, scrollY));
            }

            if (isHovered) hoveredReligion = religion;
            itemY += itemHeight + itemSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);

        return new ReligionListRenderResult(events, hoveredReligion, height);
    }

    /// <summary>
    ///     Draw a single religion item
    /// </summary>
    /// <returns>Tuple of (Religion UID if clicked, whether item is hovered)</returns>
    private static (string? clickedUID, bool isHovered) DrawReligionItem(
        ImDrawListPtr drawList,
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
        }

        // Draw deity icon (with fallback to colored circle)
        const float iconSize = 48f;
        var deityType = DomainHelper.ParseDeityType(religion.Domain);
        var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);

        if (deityTextureId != IntPtr.Zero)
        {
            // Render deity icon texture
            var iconPos = new Vector2(x + padding, y + (height - iconSize) / 2);
            var iconMin = iconPos;
            var iconMax = new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize);

            // Draw icon with full color (no tint)
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(deityTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Add subtle border around icon for visual cohesion
            var deityColor = DomainHelper.GetDeityColor(religion.Domain);
            var iconBorderColor = ImGui.ColorConvertFloat4ToU32(deityColor * 0.8f);
            drawList.AddRect(iconMin, iconMax, iconBorderColor, 4f, ImDrawFlags.None, 2f);
        }
        else
        {
            // Fallback: Use placeholder colored circle if texture not available
            var iconCenter = new Vector2(x + padding + iconSize / 2, y + height / 2);
            var deityColor = DomainHelper.GetDeityColor(religion.Domain);
            var iconColorU32 = ImGui.ColorConvertFloat4ToU32(deityColor);
            drawList.AddCircleFilled(iconCenter, iconSize / 2, iconColorU32, 16);
        }

        // Draw religion name
        var namePos = new Vector2(x + padding * 2 + iconSize, y + padding);
        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 16f, namePos, nameColor, religion.ReligionName);

        // Draw deity name - show custom name if available, otherwise domain with title
        var deityText = !string.IsNullOrWhiteSpace(religion.DeityName)
            ? $"{religion.DeityName} ({religion.Domain})"
            : $"{religion.Domain} - {DomainHelper.GetDeityTitle(religion.Domain)}";
        var deityPos = new Vector2(x + padding * 2 + iconSize, y + padding + 22f);
        var deityColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), 13f, deityPos, deityColorU32, deityText);

        // Draw member count and prestige
        var statusText = religion.IsPublic
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PUBLIC)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PRIVATE);
        var infoText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_INFO_SHORT,
            religion.MemberCount, religion.PrestigeRank, statusText);
        var infoPos = new Vector2(x + padding * 2 + iconSize, y + padding + 42f);
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

        // Deity - show custom name if available
        var deityLine = !string.IsNullOrWhiteSpace(religion.DeityName)
            ? $"{religion.DeityName} - {DomainHelper.GetDeityTitle(religion.Domain)}"
            : $"{religion.Domain} - {DomainHelper.GetDeityTitle(religion.Domain)}";
        lines.Add(deityLine);

        // Separator
        lines.Add(""); // Empty line for spacing

        // Member count
        lines.Add(
            $"{LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_MEMBERS_LABEL)} {religion.MemberCount}");

        // Prestige
        lines.Add(
            $"{LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_PRESTIGE_LABEL)} {religion.PrestigeRank} ({religion.Prestige})");

        // Public/Private status
        var status = religion.IsPublic
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PUBLIC)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PRIVATE);
        lines.Add($"{LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_STATUS_LABEL)} {status}");

        // Description (if available)
        if (!string.IsNullOrEmpty(religion.Description))
        {
            lines.Add(""); // Empty line for spacing
            lines.Add(LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_DESCRIPTION_LABEL));

            // Wrap description text
            var wrappedDesc = WrapText(religion.Description, tooltipMaxWidth - tooltipPadding * 2, 13f);
            lines.AddRange(wrappedDesc);
        }

        // Calculate tooltip dimensions
        var lineHeight = 16f;
        var tooltipHeight = tooltipPadding * 2 + lines.Count * lineHeight;
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
        for (var i = 0; i < lines.Count; i++)
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
            else if (i == 1)
            {
                // Deity subtitle
                textColor = ColorPalette.White;
                fontSize = 13f;
            }
            else if (line.StartsWith(
                         LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_DESCRIPTION_LABEL)) ||
                     line.StartsWith(
                         LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_MEMBERS_LABEL)) ||
                     line.StartsWith(
                         LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_PRESTIGE_LABEL)) ||
                     line.StartsWith(LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_LIST_STATUS_LABEL)))
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