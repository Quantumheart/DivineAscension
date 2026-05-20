using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Table;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a table view for religion browsing with fixed header and scrollable rows.
///     Width + height derive from the supplied <c>ReligionTableViewModel</c>; column
///     widths divide the available horizontal space evenly (minus the scrollbar).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionTableRenderer
{
    private const int ColumnCount = 5;
    private const float MinColumnWidth = 120f;
    private const float HeaderHeight = 27f;
    private const float RowHeight = 80f;
    private const float RowSpacing = 8f;
    private const float ScrollbarWidth = 16f;
    private const float IconSize = 48f;

    /// <summary>
    ///     Pure renderer for religion table. Emits events instead of mutating state.
    /// </summary>
    public static ReligionTableRenderResult Draw(
        ReligionTableViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ListEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var religions = new List<ReligionListResponsePacket.ReligionInfo>(viewModel.Religions);
        var isLoading = viewModel.IsLoading;
        var scrollY = viewModel.ScrollY;
        var selectedReligionUID = viewModel.SelectedReligionUID;

        // Responsive table sizing — width and height come from the view model
        // (which inherits the content rect handed in by MainLayoutCoordinator).
        var tableWidth = MathF.Max(viewModel.Width, MinColumnWidth * ColumnCount);
        var tableHeight = MathF.Max(viewModel.Height, HeaderHeight + RowHeight);
        var columnWidth = MathF.Max(MinColumnWidth, (tableWidth - ScrollbarWidth) / ColumnCount);

        // Draw table background container
        DrawTableBackground(drawList, x, y, tableWidth, tableHeight);

        // Loading state
        if (isLoading)
        {
            DrawLoadingState(drawList, x, y, tableWidth, tableHeight);
            return new ReligionTableRenderResult(events, tableHeight);
        }

        // Draw fixed header row
        DrawTableHeader(drawList, x, y, columnWidth);

        // No religions state
        if (religions.Count == 0)
        {
            DrawEmptyState(drawList, x, y + HeaderHeight, tableWidth, tableHeight - HeaderHeight);
            return new ReligionTableRenderResult(events, tableHeight);
        }

        // Calculate scroll limits
        var contentHeight = religions.Count * (RowHeight + RowSpacing);
        var visibleHeight = tableHeight - HeaderHeight;
        var maxScroll = Math.Max(0f, contentHeight - visibleHeight);

        // Handle mouse wheel scrolling
        var mousePos = ImGui.GetMousePos();
        var isMouseOver = mousePos.X >= x && mousePos.X <= x + tableWidth &&
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
        var rowEnd = new Vector2(x + tableWidth, y + tableHeight);
        drawList.PushClipRect(rowStart, rowEnd, true);

        // Draw visible rows with culling optimization
        var rowY = y + HeaderHeight - scrollY;
        for (var i = 0; i < religions.Count; i++)
        {
            var religion = religions[i];

            // Skip if not visible
            if (rowY + RowHeight < y + HeaderHeight || rowY > y + tableHeight)
            {
                rowY += RowHeight + RowSpacing;
                continue;
            }

            var clickedUID = DrawTableRow(drawList, religion, x, rowY, tableWidth, columnWidth, selectedReligionUID);
            if (clickedUID != null)
            {
                selectedReligionUID = clickedUID;
                events.Add(new ListEvent.ItemClicked(clickedUID, scrollY));
            }

            rowY += RowHeight + RowSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > visibleHeight)
        {
            Scrollbar.Draw(drawList, x + tableWidth - ScrollbarWidth, y + HeaderHeight,
                ScrollbarWidth, visibleHeight, scrollY, maxScroll);
        }

        return new ReligionTableRenderResult(events, tableHeight);
    }

    private static void DrawTableBackground(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var start = new Vector2(x, y);
        var end = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground);
        drawList.AddRectFilled(start, end, bgColor, 4f);
    }

    private static void DrawTableHeader(ImDrawListPtr drawList, float x, float y, float columnWidth)
    {
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        const float fontSize = TableHeader;
        const float padding = 12f;

        var columns = new[]
        {
            LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_NAME),
            LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_DOMAIN),
            LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_PRESTIGE),
            LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_MEMBERS),
            LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_PUBLIC)
        };

        for (var i = 0; i < columns.Length; i++)
        {
            var colX = x + i * columnWidth;

            if (i == 0)
            {
                // Name column: offset header to align with text area after icon
                var textAreaX = colX + padding + IconSize + padding;
                var textAreaWidth = columnWidth - IconSize - padding * 3;
                DrawCenteredText(drawList, columns[i], textAreaX, y + 8f, textAreaWidth, headerColor, fontSize);
            }
            else
            {
                DrawCenteredText(drawList, columns[i], colX, y + 8f, columnWidth, headerColor, fontSize);
            }
        }
    }

    private static string? DrawTableRow(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float x,
        float y,
        float tableWidth,
        float columnWidth,
        string? selectedReligionUID)
    {
        var rowStart = new Vector2(x, y);
        var rowEnd = new Vector2(x + tableWidth, y + RowHeight);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + tableWidth &&
                         mousePos.Y >= y && mousePos.Y <= y + RowHeight;
        var isSelected = selectedReligionUID == religion.ReligionUID;

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

        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(rowStart, rowEnd, bgColorU32, 4f);

        var borderColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.DarkBrown);
        drawList.AddRect(rowStart, rowEnd, borderColor, 4f, ImDrawFlags.None, 2f);

        string? clickedUID = null;
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            clickedUID = religion.ReligionUID;
        }

        DrawNameColumn(drawList, religion, x, y, columnWidth);
        DrawDeityColumn(drawList, religion, x + columnWidth, y, columnWidth);
        DrawPrestigeColumn(drawList, religion, x + columnWidth * 2, y, columnWidth);
        DrawMembersColumn(drawList, religion, x + columnWidth * 3, y, columnWidth);
        DrawPublicColumn(drawList, religion, x + columnWidth * 4, y, columnWidth);

        return clickedUID;
    }

    private static void DrawNameColumn(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float colX,
        float rowY,
        float columnWidth)
    {
        const float padding = 12f;

        var iconX = colX + padding;
        var iconY = rowY + (RowHeight - IconSize) / 2f;
        DrawDeityIcon(drawList, religion.Domain, iconX, iconY);

        var textX = iconX + IconSize + padding;
        var textWidth = columnWidth - IconSize - padding * 3;
        var nameText = religion.ReligionName;
        var textSize = ImGui.CalcTextSize(nameText);
        var textPosX = textX + (textWidth - textSize.X) / 2f;
        var textPosY = rowY + (RowHeight - textSize.Y) / 2f;

        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textPosX, textPosY), nameColor, nameText);
    }

    private static void DrawDeityColumn(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        DrawCenteredText(drawList, religion.DeityName, colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawPrestigeColumn(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        DrawCenteredText(drawList, religion.PrestigeRank, colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawMembersColumn(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        var memberText = religion.MemberCount.ToString();
        DrawCenteredText(drawList, memberText, colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawPublicColumn(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        var publicText = religion.IsPublic
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PUBLIC)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PRIVATE);
        DrawCenteredText(drawList, publicText, colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawDeityIcon(ImDrawListPtr drawList, string deityName, float x, float y)
    {
        var deityType = DomainHelper.ParseDeityType(deityName);
        var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);

        var iconMin = new Vector2(x, y);
        var iconMax = new Vector2(x + IconSize, y + IconSize);

        if (deityTextureId != IntPtr.Zero)
        {
            var tintColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(deityTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColor);
        }
        else
        {
            var deityColor = DomainHelper.GetDeityColor(deityName);
            var iconCenter = new Vector2(x + IconSize / 2f, y + IconSize / 2f);
            var iconColorU32 = ImGui.ColorConvertFloat4ToU32(deityColor);
            drawList.AddCircleFilled(iconCenter, IconSize / 2f, iconColorU32, 16);
        }

        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRect(iconMin, iconMax, borderColor, 4f, ImDrawFlags.None, 2f);
    }

    private static void DrawCenteredText(
        ImDrawListPtr drawList,
        string text,
        float colX,
        float colY,
        float colWidth,
        uint color,
        float fontSize)
    {
        var defaultFontSize = ImGui.GetFont().FontSize;
        var baseTextSize = ImGui.CalcTextSize(text);
        var scale = fontSize / defaultFontSize;
        var scaledWidth = baseTextSize.X * scale;
        var textX = colX + (colWidth - scaledWidth) / 2f;
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(textX, colY), color, text);
    }

    private static void DrawLoadingState(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var loadingText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_LOADING);
        var loadingSize = ImGui.CalcTextSize(loadingText);
        var loadingPos = new Vector2(
            x + (width - loadingSize.X) / 2f,
            y + (height - loadingSize.Y) / 2f
        );
        var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(loadingPos, loadingColor, loadingText);
    }

    private static void DrawEmptyState(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var emptyText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_NO_RELIGIONS);
        var emptySize = ImGui.CalcTextSize(emptyText);
        var emptyPos = new Vector2(
            x + (width - emptySize.X) / 2f,
            y + (height - emptySize.Y) / 2f
        );
        var emptyColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(emptyPos, emptyColor, emptyText);
    }
}
