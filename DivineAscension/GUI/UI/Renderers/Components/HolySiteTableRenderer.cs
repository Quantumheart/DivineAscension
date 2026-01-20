using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.HolySite.Table;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a table view for holy site browsing with fixed header and scrollable rows.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class HolySiteTableRenderer
{
    // Table dimensions
    private const float TableWidth = 1368f;
    private const float ColumnWidth = 273.6f;
    private const float HeaderHeight = 27f;
    private const float RowHeight = 80f;
    private const float RowSpacing = 8f;
    private const float ScrollbarWidth = 16f;
    private const float IconSize = 48f;

    /// <summary>
    ///     Pure renderer for holy site table. Emits events instead of mutating state.
    /// </summary>
    public static HolySiteTableRenderResult Draw(
        HolySiteTableViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ListEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var sites = new List<HolySiteResponsePacket.HolySiteInfo>(viewModel.Sites);
        var isLoading = viewModel.IsLoading;
        var scrollY = viewModel.ScrollY;
        var selectedSiteUID = viewModel.SelectedSiteUID;
        var tableHeight = viewModel.Height;

        // Draw table background container
        DrawTableBackground(drawList, x, y, TableWidth, tableHeight);

        // Loading state
        if (isLoading)
        {
            DrawLoadingState(drawList, x, y, tableHeight);
            return new HolySiteTableRenderResult(events, tableHeight);
        }

        // Draw fixed header row
        DrawTableHeader(drawList, x, y);

        // No sites state
        if (sites.Count == 0)
        {
            DrawEmptyState(drawList, x, y + HeaderHeight, tableHeight);
            return new HolySiteTableRenderResult(events, tableHeight);
        }

        // Calculate scroll limits
        var contentHeight = sites.Count * (RowHeight + RowSpacing);
        var visibleHeight = tableHeight - HeaderHeight;
        var maxScroll = Math.Max(0f, contentHeight - visibleHeight);

        // Handle mouse wheel scrolling
        var mousePos = ImGui.GetMousePos();
        var isMouseOver = mousePos.X >= x && mousePos.X <= x + TableWidth &&
                          mousePos.Y >= y + HeaderHeight && mousePos.Y <= y + tableHeight;
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

        // Set clipping region for rows (below header)
        var rowStart = new Vector2(x, y + HeaderHeight);
        var rowEnd = new Vector2(x + TableWidth, y + tableHeight);
        drawList.PushClipRect(rowStart, rowEnd, true);

        // Draw visible rows with culling optimization
        var rowY = y + HeaderHeight - scrollY;
        for (var i = 0; i < sites.Count; i++)
        {
            var site = sites[i];

            // Skip if not visible
            if (rowY + RowHeight < y + HeaderHeight || rowY > y + tableHeight)
            {
                rowY += RowHeight + RowSpacing;
                continue;
            }

            var clickedUID = DrawTableRow(drawList, site, x, rowY, selectedSiteUID);
            if (clickedUID != null)
            {
                selectedSiteUID = clickedUID;
                events.Add(new ListEvent.ItemClicked(clickedUID, scrollY));
            }

            rowY += RowHeight + RowSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > visibleHeight)
        {
            Scrollbar.Draw(drawList, x + TableWidth - ScrollbarWidth, y + HeaderHeight,
                ScrollbarWidth, visibleHeight, scrollY, maxScroll);
        }

        return new HolySiteTableRenderResult(events, tableHeight);
    }

    /// <summary>
    ///     Draw the table background container
    /// </summary>
    private static void DrawTableBackground(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var start = new Vector2(x, y);
        var end = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground);
        drawList.AddRectFilled(start, end, bgColor, 4f);
    }

    /// <summary>
    ///     Draw the fixed table header with column labels
    /// </summary>
    private static void DrawTableHeader(ImDrawListPtr drawList, float x, float y)
    {
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        const float fontSize = 16f;
        const float padding = 12f;

        // Column labels
        var columns = new[]
        {
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_NAME),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_TIER),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_VOLUME),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_PRAYER)
        };

        for (var i = 0; i < columns.Length; i++)
        {
            var colX = x + i * ColumnWidth;

            if (i == 0)
            {
                // Name column: offset header to align with text area after icon
                var textAreaX = colX + padding + IconSize + padding;
                var textAreaWidth = ColumnWidth - IconSize - padding * 3;
                DrawCenteredText(drawList, columns[i], textAreaX, y + 8f, textAreaWidth, headerColor, fontSize);
            }
            else
            {
                DrawCenteredText(drawList, columns[i], colX, y + 8f, ColumnWidth, headerColor, fontSize);
            }
        }
    }

    /// <summary>
    ///     Draw a single table row with 5 columns
    /// </summary>
    /// <returns>Site UID if row was clicked, null otherwise</returns>
    private static string? DrawTableRow(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float x,
        float y,
        string? selectedSiteUID)
    {
        var rowStart = new Vector2(x, y);
        var rowEnd = new Vector2(x + TableWidth, y + RowHeight);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + TableWidth &&
                         mousePos.Y >= y && mousePos.Y <= y + RowHeight;
        var isSelected = selectedSiteUID == site.SiteUID;

        // Determine background color based on state
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
            bgColor = ColorPalette.Background;
        }

        // Draw row background
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(rowStart, rowEnd, bgColorU32, 4f);

        // Draw row border (2px solid)
        var borderColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.DarkBrown);
        drawList.AddRect(rowStart, rowEnd, borderColor, 4f, ImDrawFlags.None, 2f);

        // Handle click
        string? clickedUID = null;
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            clickedUID = site.SiteUID;
        }

        // Column 1: Name (deity icon + site name)
        DrawNameColumn(drawList, site, x, y);

        // Column 2: Tier
        DrawTierColumn(drawList, site, x + ColumnWidth, y);

        // Column 3: Volume
        DrawVolumeColumn(drawList, site, x + ColumnWidth * 2, y);

        // Column 4: Prayer Multiplier
        DrawPrayerColumn(drawList, site, x + ColumnWidth * 3, y);

        return clickedUID;
    }

    /// <summary>
    ///     Draw Name column: Deity icon (48x48) on left + site name centered
    /// </summary>
    private static void DrawNameColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY)
    {
        const float padding = 12f;

        // Draw deity icon
        var iconX = colX + padding;
        var iconY = rowY + (RowHeight - IconSize) / 2f;
        DrawDeityIcon(drawList, site.Domain, iconX, iconY);

        // Draw site name (centered in remaining space)
        var textX = iconX + IconSize + padding;
        var textWidth = ColumnWidth - IconSize - padding * 3;
        var nameText = site.SiteName;
        var textSize = ImGui.CalcTextSize(nameText);
        var textPosX = textX + (textWidth - textSize.X) / 2f;
        var textPosY = rowY + (RowHeight - textSize.Y) / 2f;

        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(textPosX, textPosY), nameColor, nameText);
    }

    /// <summary>
    ///     Draw Tier column: Tier number centered
    /// </summary>
    private static void DrawTierColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = 13f;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        DrawCenteredText(drawList, site.Tier.ToString(), colX, centerY, ColumnWidth, textColor, fontSize);
    }

    /// <summary>
    ///     Draw Volume column: Volume number centered
    /// </summary>
    private static void DrawVolumeColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = 13f;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        DrawCenteredText(drawList, site.Volume.ToString(), colX, centerY, ColumnWidth, textColor, fontSize);
    }

    /// <summary>
    ///     Draw Prayer column: Prayer multiplier centered
    /// </summary>
    private static void DrawPrayerColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = 13f;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        var multiplierText = $"{site.PrayerMultiplier:F2}x";
        DrawCenteredText(drawList, multiplierText, colX, centerY, ColumnWidth, textColor, fontSize);
    }

    /// <summary>
    ///     Draw deity icon with border (48x48px)
    /// </summary>
    private static void DrawDeityIcon(ImDrawListPtr drawList, string deityName, float x, float y)
    {
        var deityType = DomainHelper.ParseDeityType(deityName);
        var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);

        var iconMin = new Vector2(x, y);
        var iconMax = new Vector2(x + IconSize, y + IconSize);

        if (deityTextureId != IntPtr.Zero)
        {
            // Draw icon texture
            var tintColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(deityTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColor);
        }
        else
        {
            // Fallback: Colored circle
            var deityColor = DomainHelper.GetDeityColor(deityName);
            var iconCenter = new Vector2(x + IconSize / 2f, y + IconSize / 2f);
            var iconColorU32 = ImGui.ColorConvertFloat4ToU32(deityColor);
            drawList.AddCircleFilled(iconCenter, IconSize / 2f, iconColorU32, 16);
        }

        // Draw border (2px solid)
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRect(iconMin, iconMax, borderColor, 4f, ImDrawFlags.None, 2f);
    }

    /// <summary>
    ///     Helper: Draw center-aligned text in a column
    /// </summary>
    private static void DrawCenteredText(
        ImDrawListPtr drawList,
        string text,
        float colX,
        float colY,
        float colWidth,
        uint color,
        float fontSize)
    {
        // Calculate text size scaled to match the actual render font size
        var defaultFontSize = ImGui.GetFont().FontSize;
        var baseTextSize = ImGui.CalcTextSize(text);
        var scale = fontSize / defaultFontSize;
        var scaledWidth = baseTextSize.X * scale;
        var textX = colX + (colWidth - scaledWidth) / 2f;
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(textX, colY), color, text);
    }

    /// <summary>
    ///     Draw loading state
    /// </summary>
    private static void DrawLoadingState(ImDrawListPtr drawList, float x, float y, float tableHeight)
    {
        var loadingText = LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_BROWSE_LOADING);
        var loadingSize = ImGui.CalcTextSize(loadingText);
        var loadingPos = new Vector2(
            x + (TableWidth - loadingSize.X) / 2f,
            y + (tableHeight - loadingSize.Y) / 2f
        );
        var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(loadingPos, loadingColor, loadingText);
    }

    /// <summary>
    ///     Draw empty state (no holy sites)
    /// </summary>
    private static void DrawEmptyState(ImDrawListPtr drawList, float x, float y, float tableHeight)
    {
        var emptyText = LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_BROWSE_NO_SITES);
        var emptySize = ImGui.CalcTextSize(emptyText);
        var emptyPos = new Vector2(
            x + (TableWidth - emptySize.X) / 2f,
            y + (tableHeight - HeaderHeight - emptySize.Y) / 2f
        );
        var emptyColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(emptyPos, emptyColor, emptyText);
    }
}
