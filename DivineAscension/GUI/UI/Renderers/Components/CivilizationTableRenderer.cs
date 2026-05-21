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
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a table view for civilization browsing with variable-height rows.
///     Width derives from the view model and is split proportionally between
///     Name (30%), Religions (15%) and Description (55%) of the available inner
///     space (table width minus scrollbar).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationTableRenderer
{
    private const float NameWeight = 0.30f;
    private const float ReligionsWeight = 0.15f;
    private const float DescriptionWeight = 0.55f;
    private const float MinNameColumnWidth = 160f;
    private const float MinReligionsColumnWidth = 80f;
    private const float MinDescriptionColumnWidth = 240f;
    private const float HeaderHeight = 27f;
    private const float MinRowHeight = 80f;
    private const float RowPaddingVertical = 12f;
    private const float RowSpacing = 8f;
    private const float ScrollbarWidth = 16f;
    private const float CivIconSize = 48f;
    private const float DeityIconSize = 12f;
    private const float DeityIconSpacing = 4f;
    private const float DescriptionPaddingHorizontal = 12f;
    private const float NamePadding = 12f;

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

        // Responsive sizing — split available width between columns by weight,
        // each clamped to a usable minimum.
        var minTableWidth = MinNameColumnWidth + MinReligionsColumnWidth + MinDescriptionColumnWidth + ScrollbarWidth;
        var tableWidth = MathF.Max(viewModel.Width, minTableWidth);
        var inner = tableWidth - ScrollbarWidth;
        var nameColumnWidth = MathF.Max(MinNameColumnWidth, inner * NameWeight);
        var religionsColumnWidth = MathF.Max(MinReligionsColumnWidth, inner * ReligionsWeight);
        var descriptionColumnWidth = MathF.Max(MinDescriptionColumnWidth,
            inner - nameColumnWidth - religionsColumnWidth);

        DrawTableBackground(drawList, x, y, tableWidth, tableHeight);

        if (isLoading)
        {
            DrawLoadingState(drawList, x, y, tableWidth, tableHeight);
            return new CivilizationTableRenderResult(events, tableHeight);
        }

        DrawTableHeader(drawList, x, y, tableWidth, nameColumnWidth, religionsColumnWidth, descriptionColumnWidth);

        if (civilizations.Count == 0)
        {
            DrawEmptyState(drawList, x, y + HeaderHeight, tableWidth, tableHeight - HeaderHeight);
            return new CivilizationTableRenderResult(events, tableHeight);
        }

        // Pre-calculate all row heights (needed for scroll calculation)
        var rowHeights =
            CalculateAllRowHeights(civilizations, descriptionColumnWidth - DescriptionPaddingHorizontal * 2f);

        var contentHeight = 0f;
        for (var i = 0; i < rowHeights.Count; i++)
        {
            contentHeight += rowHeights[i];
            if (i < rowHeights.Count - 1)
                contentHeight += RowSpacing;
        }

        var visibleHeight = tableHeight - HeaderHeight;
        var maxScroll = Math.Max(0f, contentHeight - visibleHeight);

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

        var rowStart = new Vector2(x, y + HeaderHeight);
        var rowEnd = new Vector2(x + tableWidth, y + tableHeight);
        drawList.PushClipRect(rowStart, rowEnd, true);

        var currentY = y + HeaderHeight - scrollY;
        for (var i = 0; i < civilizations.Count; i++)
        {
            var civ = civilizations[i];
            var rowHeight = rowHeights[i];

            if (currentY + rowHeight < y + HeaderHeight || currentY > y + tableHeight)
            {
                currentY += rowHeight + RowSpacing;
                continue;
            }

            var clickedCivId = DrawTableRow(drawList, civ, x, currentY, rowHeight,
                tableWidth, nameColumnWidth, religionsColumnWidth, descriptionColumnWidth, selectedCivId);
            if (clickedCivId != null)
            {
                selectedCivId = clickedCivId;
                events.Add(new ListEvent.ItemClicked(clickedCivId, scrollY));
            }

            currentY += rowHeight + RowSpacing;
        }

        drawList.PopClipRect();

        if (contentHeight > visibleHeight)
        {
            Scrollbar.Draw(drawList, x + tableWidth - ScrollbarWidth, y + HeaderHeight,
                ScrollbarWidth, visibleHeight, scrollY, maxScroll);
        }

        return new CivilizationTableRenderResult(events, tableHeight);
    }

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

    private static float CalculateRowHeight(string description, float descriptionWidth)
    {
        if (string.IsNullOrEmpty(description))
            return MinRowHeight;

        var wrappedSize = ImGui.CalcTextSize(description, descriptionWidth);
        var textHeight = wrappedSize.Y;

        var calculatedHeight = textHeight + (RowPaddingVertical * 2f);
        return Math.Max(MinRowHeight, calculatedHeight);
    }

    private static void DrawTableBackground(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var start = new Vector2(x, y);
        var end = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground);
        drawList.AddRectFilled(start, end, bgColor, 4f);
    }

    private static void DrawTableHeader(ImDrawListPtr drawList, float x, float y,
        float tableWidth, float nameColumnWidth, float religionsColumnWidth, float descriptionColumnWidth)
    {
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        const float fontSize = TableHeader;

        // Column 1: "Name" — center over the text area after the icon (matches religion table)
        var nameTextAreaX = x + NamePadding + CivIconSize + NamePadding;
        var nameTextAreaWidth = nameColumnWidth - CivIconSize - NamePadding * 3;
        DrawCenteredText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_HEADER_NAME),
            nameTextAreaX, y + 8f, nameTextAreaWidth, headerColor, fontSize);

        // Column 2: "Religions" — centered
        DrawCenteredText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_HEADER_RELIGIONS),
            x + nameColumnWidth, y + 8f, religionsColumnWidth, headerColor, fontSize);

        // Column 3: "Description" — centered like other table headers
        DrawCenteredText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_HEADER_DESCRIPTION),
            x + nameColumnWidth + religionsColumnWidth, y + 8f, descriptionColumnWidth, headerColor, fontSize);
    }

    private static string? DrawTableRow(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float x,
        float y,
        float rowHeight,
        float tableWidth,
        float nameColumnWidth,
        float religionsColumnWidth,
        float descriptionColumnWidth,
        string? selectedCivId)
    {
        var rowStart = new Vector2(x, y);
        var rowEnd = new Vector2(x + tableWidth, y + rowHeight);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + tableWidth &&
                         mousePos.Y >= y && mousePos.Y <= y + rowHeight;
        var isSelected = selectedCivId == civ.CivId;

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

        string? clickedCivId = null;
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            clickedCivId = civ.CivId;
        }

        DrawNameColumn(drawList, civ, x, y, rowHeight, nameColumnWidth);
        DrawReligionsColumn(drawList, civ, x + nameColumnWidth, y, religionsColumnWidth);
        DrawDescriptionColumn(drawList, civ, x + nameColumnWidth + religionsColumnWidth, y, descriptionColumnWidth);

        return clickedCivId;
    }

    private static void DrawNameColumn(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float colX,
        float rowY,
        float rowHeight,
        float columnWidth)
    {
        var iconX = colX + NamePadding;
        var iconY = rowY + (rowHeight - CivIconSize) / 2f;
        DrawCivIcon(drawList, civ.Icon, iconX, iconY);

        var textX = iconX + CivIconSize + NamePadding;
        var textWidth = columnWidth - CivIconSize - NamePadding * 3;
        var textSize = ImGui.CalcTextSize(civ.Name);
        var scale = Body / ImGui.GetFont().FontSize;
        var scaledWidth = textSize.X * scale;
        var textPosX = textX + (textWidth - scaledWidth) / 2f;
        var textPosY = rowY + (rowHeight - Body) / 2f;

        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textPosX, textPosY), nameColor, civ.Name);
    }

    private static void DrawReligionsColumn(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float colX,
        float rowY,
        float columnWidth)
    {
        if (civ.MemberDeities == null || civ.MemberDeities.Count == 0)
            return;

        var iconX = colX + 8f;
        var iconY = rowY + RowPaddingVertical;

        var currentX = iconX;
        var currentY = iconY;
        const float iconTotalWidth = DeityIconSize + DeityIconSpacing;

        foreach (var deityName in civ.MemberDeities)
        {
            // Wrap to next row when we'd overflow the column.
            if (currentX + DeityIconSize > colX + columnWidth - 8f)
            {
                currentX = iconX;
                currentY += DeityIconSize + 4f;
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

    private static void DrawDescriptionColumn(
        ImDrawListPtr drawList,
        CivilizationListResponsePacket.CivilizationInfo civ,
        float colX,
        float rowY,
        float columnWidth)
    {
        if (string.IsNullOrEmpty(civ.Description))
            return;

        var textX = colX + DescriptionPaddingHorizontal;
        var textY = rowY + RowPaddingVertical;
        var textWidth = columnWidth - DescriptionPaddingHorizontal * 2f;

        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        ImGui.PushTextWrapPos(textX + textWidth);
        drawList.AddText(ImGui.GetFont(), Secondary,
            new Vector2(textX, textY),
            textColor,
            civ.Description);
        ImGui.PopTextWrapPos();
    }

    private static void DrawCivIcon(ImDrawListPtr drawList, string iconName, float x, float y)
    {
        var iconTextureId = CivilizationIconLoader.GetIconTextureId(iconName);
        var iconMin = new Vector2(x, y);
        var iconMax = new Vector2(x + CivIconSize, y + CivIconSize);

        if (iconTextureId != IntPtr.Zero)
        {
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);
        }
        else
        {
            var center = new Vector2(x + CivIconSize / 2f, y + CivIconSize / 2f);
            var fallbackColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown);
            drawList.AddCircleFilled(center, CivIconSize / 2f, fallbackColor, 16);
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
        var loadingText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_LOADING);
        var textSize = ImGui.CalcTextSize(loadingText);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2f,
            y + (height - textSize.Y) / 2f);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(textPos, textColor, loadingText);
    }

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
