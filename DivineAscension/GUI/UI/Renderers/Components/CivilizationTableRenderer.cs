using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Table;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a table view for civilization browsing with variable-height rows.
///     Rows expand based on wrapped description text.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationTableRenderer
{
    // Table dimensions
    private const float TableWidth = 1368f;
    private const float NameColumnWidth = 400f;
    private const float ReligionsColumnWidth = 200f;
    private const float DescriptionColumnWidth = 768f;
    private const float HeaderHeight = 27f;
    private const float MinRowHeight = 80f;
    private const float RowPaddingVertical = 12f;
    private const float RowSpacing = 8f;
    private const float ScrollbarWidth = 16f;
    private const float CivIconSize = 28f;
    private const float DeityIconSize = 12f;
    private const float DeityIconSpacing = 4f;
    private const float DescriptionFontSize = 12f;
    private const float NameFontSize = 14f;
    private const float HeaderFontSize = 16f;
    private const float DescriptionPaddingHorizontal = 12f;

    /// <summary>
    ///     Pure renderer for civilization table. Emits events instead of mutating state.
    /// </summary>
    public static CivilizationTableRenderResult Draw(
        CivilizationTableViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ListEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var civilizations = viewModel.Civilizations;
        var isLoading = viewModel.IsLoading;
        var scrollY = viewModel.ScrollY;
        var selectedCivId = viewModel.SelectedCivId;
        var tableHeight = viewModel.Height;

        // Draw table background container
        DrawTableBackground(drawList, x, y, TableWidth, tableHeight);

        // Loading state
        if (isLoading)
        {
            DrawLoadingState(drawList, x, y, TableWidth, tableHeight);
            return new CivilizationTableRenderResult(events, tableHeight);
        }

        // Draw fixed header row
        DrawTableHeader(drawList, x, y);

        // No civilizations state
        if (civilizations.Count == 0)
        {
            DrawEmptyState(drawList, x, y + HeaderHeight, TableWidth, tableHeight - HeaderHeight);
            return new CivilizationTableRenderResult(events, tableHeight);
        }

        // Pre-calculate all row heights (needed for scroll calculation)
        var rowHeights =
            CalculateAllRowHeights(civilizations, DescriptionColumnWidth - DescriptionPaddingHorizontal * 2f);

        // Calculate content height (sum of all row heights + spacing)
        var contentHeight = 0f;
        for (var i = 0; i < rowHeights.Count; i++)
        {
            contentHeight += rowHeights[i];
            if (i < rowHeights.Count - 1)
                contentHeight += RowSpacing;
        }

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

        // Draw visible rows with position-based culling
        var currentY = y + HeaderHeight - scrollY;
        for (var i = 0; i < civilizations.Count; i++)
        {
            var civ = civilizations[i];
            var rowHeight = rowHeights[i];

            // Culling: skip if row completely outside visible area
            if (currentY + rowHeight < y + HeaderHeight || currentY > y + tableHeight)
            {
                currentY += rowHeight + RowSpacing;
                continue;
            }

            var clickedCivId = DrawTableRow(drawList, civ, x, currentY, rowHeight, selectedCivId);
            if (clickedCivId != null)
            {
                selectedCivId = clickedCivId;
                events.Add(new ListEvent.ItemClicked(clickedCivId, scrollY));
            }

            currentY += rowHeight + RowSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > visibleHeight)
        {
            Scrollbar.Draw(drawList, x + TableWidth - ScrollbarWidth, y + HeaderHeight,
                ScrollbarWidth, visibleHeight, scrollY, maxScroll);
        }

        return new CivilizationTableRenderResult(events, tableHeight);
    }

    /// <summary>
    ///     Pre-calculate heights for all rows based on wrapped description text
    /// </summary>
    private static List<float> CalculateAllRowHeights(
        IReadOnlyList<CivilizationListResponsePacket.CivilizationInfo> civilizations,
        float descriptionWidth)
    {
        var heights = new List<float>(civilizations.Count);
        foreach (var civ in civilizations)
        {
            heights.Add(CalculateRowHeight(civ.Description, descriptionWidth));
        }

        return heights;
    }

    /// <summary>
    ///     Calculate height for a single row based on wrapped description text
    /// </summary>
    private static float CalculateRowHeight(string description, float descriptionWidth)
    {
        if (string.IsNullOrEmpty(description))
            return MinRowHeight;

        // Calculate wrapped text height using ImGui
        var wrappedSize = ImGui.CalcTextSize(description, descriptionWidth);
        var textHeight = wrappedSize.Y;

        // Row height = text height + padding, but at least MinRowHeight
        var calculatedHeight = textHeight + (RowPaddingVertical * 2f);
        return Math.Max(MinRowHeight, calculatedHeight);
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

        // Header background
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + TableWidth, y + HeaderHeight),
            headerColor);

        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);

        // Column 1: "Name" (aligned with name area after icon)
        var col1X = x;
        var nameHeaderX = col1X + 12f + CivIconSize + 8f; // After icon space
        drawList.AddText(ImGui.GetFont(), HeaderFontSize,
            new Vector2(nameHeaderX, y + (HeaderHeight - HeaderFontSize) / 2f),
            textColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_HEADER_NAME));

        // Column 2: "Religions" (centered)
        var col2X = x + NameColumnWidth;
        var religionsHeader =
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_HEADER_RELIGIONS);
        var religionsSize = ImGui.CalcTextSize(religionsHeader);
        var religionsCenterX = col2X + (ReligionsColumnWidth - religionsSize.X) / 2f;
        drawList.AddText(ImGui.GetFont(), HeaderFontSize,
            new Vector2(religionsCenterX, y + (HeaderHeight - HeaderFontSize) / 2f),
            textColor,
            religionsHeader);

        // Column 3: "Description" (left-aligned)
        var col3X = x + NameColumnWidth + ReligionsColumnWidth;
        drawList.AddText(ImGui.GetFont(), HeaderFontSize,
            new Vector2(col3X + DescriptionPaddingHorizontal, y + (HeaderHeight - HeaderFontSize) / 2f),
            textColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_HEADER_DESCRIPTION));
    }

    /// <summary>
    ///     Draw a single table row with 3 columns and variable height
    /// </summary>
    /// <returns>Civilization ID if row was clicked, null otherwise</returns>
    private static string? DrawTableRow(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float x,
        float y,
        float rowHeight,
        string? selectedCivId)
    {
        var rowStart = new Vector2(x, y);
        var rowEnd = new Vector2(x + TableWidth, y + rowHeight);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + TableWidth &&
                         mousePos.Y >= y && mousePos.Y <= y + rowHeight;
        var isSelected = selectedCivId == civ.CivId;

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

        // Draw row border
        var borderColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.DarkBrown);
        drawList.AddRect(rowStart, rowEnd, borderColor, 4f, ImDrawFlags.None, 2f);

        // Handle click
        string? clickedCivId = null;
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            clickedCivId = civ.CivId;
        }

        // Column 1: Name (icon + text)
        DrawNameColumn(drawList, civ, x, y, rowHeight);

        // Column 2: Religions (deity icons)
        DrawReligionsColumn(drawList, civ, x + NameColumnWidth, y, rowHeight);

        // Column 3: Description (wrapped text)
        DrawDescriptionColumn(drawList, civ, x + NameColumnWidth + ReligionsColumnWidth, y, rowHeight);

        return clickedCivId;
    }

    /// <summary>
    ///     Draw Name column: Civilization icon (28x28) + name
    /// </summary>
    private static void DrawNameColumn(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float colX,
        float rowY,
        float rowHeight)
    {
        const float padding = 12f;

        // Draw civilization icon (vertically centered)
        var iconX = colX + padding;
        var iconY = rowY + (rowHeight - CivIconSize) / 2f;
        DrawCivIcon(drawList, civ.Icon, iconX, iconY);

        // Draw civilization name (left-aligned after icon)
        var textX = iconX + CivIconSize + 8f;
        var textY = rowY + (rowHeight - NameFontSize) / 2f;
        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), NameFontSize,
            new Vector2(textX, textY),
            nameColor,
            civ.Name);
    }

    /// <summary>
    ///     Draw Religions column: Deity icons showing diversity
    /// </summary>
    private static void DrawReligionsColumn(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float colX,
        float rowY,
        float rowHeight)
    {
        if (civ.MemberDeities == null || civ.MemberDeities.Count == 0)
            return;

        // Start position (with padding, vertically centered for first row)
        var iconX = colX + 8f;
        var iconY = rowY + RowPaddingVertical;

        // Draw deity icons, wrapping if needed
        var currentX = iconX;
        var currentY = iconY;
        const float iconTotalWidth = DeityIconSize + DeityIconSpacing;

        foreach (var deityName in civ.MemberDeities)
        {
            // Check if we need to wrap to next row
            if (currentX + DeityIconSize > colX + ReligionsColumnWidth - 8f)
            {
                currentX = iconX;
                currentY += DeityIconSize + 4f; // Move to next row
            }

            if (Enum.TryParse<DeityDomain>(deityName, out var deityType))
            {
                var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
                drawList.AddImage(
                    deityTextureId,
                    new Vector2(currentX, currentY),
                    new Vector2(currentX + DeityIconSize, currentY + DeityIconSize),
                    Vector2.Zero,
                    Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            }

            currentX += iconTotalWidth;
        }
    }

    /// <summary>
    ///     Draw Description column: Wrapped text
    /// </summary>
    private static void DrawDescriptionColumn(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float colX,
        float rowY,
        float rowHeight)
    {
        if (string.IsNullOrEmpty(civ.Description))
            return;

        var textX = colX + DescriptionPaddingHorizontal;
        var textY = rowY + RowPaddingVertical;
        var textWidth = DescriptionColumnWidth - DescriptionPaddingHorizontal * 2f;

        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        // Push text wrap width for multi-line rendering
        ImGui.PushTextWrapPos(textX + textWidth);
        drawList.AddText(ImGui.GetFont(), DescriptionFontSize,
            new Vector2(textX, textY),
            textColor,
            civ.Description);
        ImGui.PopTextWrapPos();
    }

    /// <summary>
    ///     Draw civilization icon with border
    /// </summary>
    private static void DrawCivIcon(ImDrawListPtr drawList, string iconName, float x, float y)
    {
        var iconTextureId = CivilizationIconLoader.GetIconTextureId(iconName);

        if (iconTextureId != IntPtr.Zero)
        {
            var iconMin = new Vector2(x, y);
            var iconMax = new Vector2(x + CivIconSize, y + CivIconSize);
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Icon border
            var iconBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
            drawList.AddRect(iconMin, iconMax, iconBorderColor, 3f, ImDrawFlags.None, 1f);
        }
        else
        {
            // Fallback: draw colored circle
            var center = new Vector2(x + CivIconSize / 2f, y + CivIconSize / 2f);
            var radius = CivIconSize / 2f;
            var fallbackColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown);
            drawList.AddCircleFilled(center, radius, fallbackColor);
            drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown), 0, 2f);
        }
    }

    /// <summary>
    ///     Draw loading state message
    /// </summary>
    private static void DrawLoadingState(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var loadingText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_LOADING);
        var textSize = ImGui.CalcTextSize(loadingText);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2f,
            y + (height - textSize.Y) / 2f);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(textPos, textColor, loadingText);
    }

    /// <summary>
    ///     Draw empty state message (no civilizations)
    /// </summary>
    private static void DrawEmptyState(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var emptyText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_NO_CIVS);
        var textSize = ImGui.CalcTextSize(emptyText);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2f,
            y + (height - textSize.Y) / 2f);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(textPos, textColor, emptyText);
    }
}