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
using static DivineAscension.GUI.UI.Utilities.FontSizes;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders a table view for holy site browsing with fixed header and scrollable rows.
///     Width derives from the view model; columns split the available width evenly.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class HolySiteTableRenderer
{
    private const int ColumnCount = 5;
    private const float MinColumnWidth = 120f;
    private const float HeaderHeight = 27f;
    private const float RowHeight = 80f;
    private const float RowSpacing = 8f;
    private const float ScrollbarWidth = 16f;
    private const float IconSize = 48f;

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

        // Responsive sizing — width comes from the view model (= the content
        // rect handed in by MainLayoutCoordinator).
        var tableWidth = MathF.Max(viewModel.Width, MinColumnWidth * ColumnCount);
        var columnWidth = MathF.Max(MinColumnWidth, (tableWidth - ScrollbarWidth) / ColumnCount);

        DrawTableBackground(drawList, x, y, tableWidth, tableHeight);

        if (isLoading)
        {
            DrawLoadingState(drawList, x, y, tableWidth, tableHeight);
            return new HolySiteTableRenderResult(events, tableHeight);
        }

        DrawTableHeader(drawList, x, y, columnWidth);

        if (sites.Count == 0)
        {
            DrawEmptyState(drawList, x, y + HeaderHeight, tableWidth, tableHeight - HeaderHeight);
            return new HolySiteTableRenderResult(events, tableHeight);
        }

        var contentHeight = sites.Count * (RowHeight + RowSpacing);
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

        var rowY = y + HeaderHeight - scrollY;
        for (var i = 0; i < sites.Count; i++)
        {
            var site = sites[i];

            if (rowY + RowHeight < y + HeaderHeight || rowY > y + tableHeight)
            {
                rowY += RowHeight + RowSpacing;
                continue;
            }

            var clickedUID = DrawTableRow(drawList, site, x, rowY, tableWidth, columnWidth, selectedSiteUID);
            if (clickedUID != null)
            {
                selectedSiteUID = clickedUID;
                events.Add(new ListEvent.ItemClicked(clickedUID, scrollY));
            }

            rowY += RowHeight + RowSpacing;
        }

        drawList.PopClipRect();

        if (contentHeight > visibleHeight)
        {
            Scrollbar.Draw(drawList, x + tableWidth - ScrollbarWidth, y + HeaderHeight,
                ScrollbarWidth, visibleHeight, scrollY, maxScroll);
        }

        return new HolySiteTableRenderResult(events, tableHeight);
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
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_NAME),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_TIER),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_RITUALS),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_PRAYER),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_TABLE_DESCRIPTION)
        };

        for (var i = 0; i < columns.Length; i++)
        {
            var colX = x + i * columnWidth;

            if (i == 0)
            {
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
        HolySiteResponsePacket.HolySiteInfo site,
        float x,
        float y,
        float tableWidth,
        float columnWidth,
        string? selectedSiteUID)
    {
        var rowStart = new Vector2(x, y);
        var rowEnd = new Vector2(x + tableWidth, y + RowHeight);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + tableWidth &&
                         mousePos.Y >= y && mousePos.Y <= y + RowHeight;
        var isSelected = selectedSiteUID == site.SiteUID;

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
            clickedUID = site.SiteUID;
        }

        DrawNameColumn(drawList, site, x, y, columnWidth);
        DrawTierColumn(drawList, site, x + columnWidth, y, columnWidth);
        DrawRitualsColumn(drawList, site, x + columnWidth * 2, y, columnWidth);
        DrawPrayerColumn(drawList, site, x + columnWidth * 3, y, columnWidth);
        DrawDescriptionColumn(drawList, site, x + columnWidth * 4, y, columnWidth);

        return clickedUID;
    }

    private static void DrawNameColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY,
        float columnWidth)
    {
        const float padding = 12f;

        var iconX = colX + padding;
        var iconY = rowY + (RowHeight - IconSize) / 2f;
        DrawDeityIcon(drawList, site.Domain, iconX, iconY);

        var textX = iconX + IconSize + padding;
        var textWidth = columnWidth - IconSize - padding * 3;
        var nameText = site.SiteName;
        var textSize = ImGui.CalcTextSize(nameText);
        var textPosX = textX + (textWidth - textSize.X) / 2f;
        var textPosY = rowY + (RowHeight - textSize.Y) / 2f;

        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textPosX, textPosY), nameColor, nameText);
    }

    private static void DrawTierColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        DrawCenteredText(drawList, site.Tier.ToString(), colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawRitualsColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        DrawCenteredText(drawList, site.RitualsCompleted.ToString(), colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawPrayerColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Body;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        var multiplierText = $"{site.PrayerMultiplier:F2}x";
        DrawCenteredText(drawList, multiplierText, colX, centerY, columnWidth, textColor, fontSize);
    }

    private static void DrawDescriptionColumn(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        float colX,
        float rowY,
        float columnWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        const float fontSize = Secondary;
        const float padding = 8f;
        var centerY = rowY + (RowHeight - fontSize) / 2f;

        var description = site.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = "-";
        }
        else
        {
            var maxWidth = columnWidth - padding * 2;
            var textSize = ImGui.CalcTextSize(description);
            var scale = fontSize / ImGui.GetFont().FontSize;
            var scaledWidth = textSize.X * scale;

            if (scaledWidth > maxWidth)
            {
                while (description.Length > 0)
                {
                    description = description.Substring(0, description.Length - 1);
                    var truncated = description + "...";
                    textSize = ImGui.CalcTextSize(truncated);
                    scaledWidth = textSize.X * scale;
                    if (scaledWidth <= maxWidth)
                    {
                        description = truncated;
                        break;
                    }
                }
            }
        }

        DrawCenteredText(drawList, description, colX, centerY, columnWidth, textColor, fontSize);
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
        var loadingText = LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_BROWSE_LOADING);
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
        var emptyText = LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_BROWSE_NO_SITES);
        var emptySize = ImGui.CalcTextSize(emptyText);
        var emptyPos = new Vector2(
            x + (width - emptySize.X) / 2f,
            y + (height - emptySize.Y) / 2f
        );
        var emptyColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(emptyPos, emptyColor, emptyText);
    }
}
