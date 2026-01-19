using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.HolySites;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renderer for the Holy Sites sub-tab in the Civilization tab.
///     Displays holy sites grouped by member religion.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationHolySitesRenderer
{
    // Layout constants
    private const float TopPadding = 10f;
    private const float HeaderHeight = 40f;
    private const float RefreshButtonWidth = 100f;
    private const float RefreshButtonHeight = 30f;
    private const float SectionSpacing = 15f;
    private const float ReligionHeaderHeight = 48f;
    private const float SiteItemHeight = 60f;
    private const float SiteItemPadding = 10f;
    private const float IndentSize = 20f;

    /// <summary>
    ///     Pure renderer: builds visuals from view model and emits UI events.
    ///     No state or side effects.
    /// </summary>
    public static CivilizationHolySitesRenderResult Draw(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<HolySitesEvent>();
        var currentY = viewModel.Y + TopPadding;

        // === HEADER WITH REFRESH BUTTON ===
        currentY += DrawHeader(viewModel, drawList, currentY, events);

        // === LOADING STATE ===
        if (viewModel.IsLoading)
        {
            DrawLoadingState(viewModel, drawList, currentY);
            return new CivilizationHolySitesRenderResult(events, viewModel.Height);
        }

        // === ERROR STATE ===
        if (!string.IsNullOrEmpty(viewModel.ErrorMsg))
        {
            DrawErrorState(viewModel, drawList, currentY);
            return new CivilizationHolySitesRenderResult(events, viewModel.Height);
        }

        // === EMPTY STATE ===
        if (viewModel.SitesByReligion.Count == 0)
        {
            DrawEmptyState(viewModel, drawList, currentY);
            return new CivilizationHolySitesRenderResult(events, viewModel.Height);
        }

        // === HOLY SITES GROUPED BY RELIGION ===
        var contentHeight = viewModel.Height - (currentY - viewModel.Y);
        DrawSitesGroupedByReligion(viewModel, drawList, currentY, contentHeight, events);

        return new CivilizationHolySitesRenderResult(events, viewModel.Height);
    }

    private static float DrawHeader(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float y,
        List<HolySitesEvent> events)
    {
        // Title
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_TITLE);
        drawList.AddText(ImGui.GetFont(), 18f,
            new Vector2(viewModel.X + 20f, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            titleText);

        // Refresh button (right-aligned)
        var buttonX = viewModel.X + viewModel.Width - RefreshButtonWidth - 20f;
        var buttonY = y - 5f;

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_REFRESH),
                buttonX, buttonY, RefreshButtonWidth, RefreshButtonHeight,
                false, !viewModel.IsLoading))
        {
            events.Add(new HolySitesEvent.RefreshClicked());
        }

        return HeaderHeight;
    }

    private static void DrawLoadingState(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float contentStartY)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_LOADING);
        var textSize = ImGui.CalcTextSize(text);
        var contentHeight = viewModel.Height - (contentStartY - viewModel.Y);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            contentStartY + contentHeight / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawEmptyState(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float contentStartY)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_EMPTY);
        var textSize = ImGui.CalcTextSize(text);
        var contentHeight = viewModel.Height - (contentStartY - viewModel.Y);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            contentStartY + contentHeight / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawErrorState(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float contentStartY)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_ERROR)
            .Replace("{0}", viewModel.ErrorMsg ?? "Unknown error");
        var textSize = ImGui.CalcTextSize(text);
        var contentHeight = viewModel.Height - (contentStartY - viewModel.Y);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            contentStartY + contentHeight / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.3f, 1f)), text);
    }

    private static void DrawSitesGroupedByReligion(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float startY,
        float availableHeight,
        List<HolySitesEvent> events)
    {
        var currentY = startY;

        // Sort religions by name for consistent display
        var sortedReligions = viewModel.SitesByReligion
            .OrderBy(kvp => viewModel.ReligionNames.GetValueOrDefault(kvp.Key, "Unknown"))
            .ToList();

        foreach (var (religionUID, sites) in sortedReligions)
        {
            var religionName = viewModel.ReligionNames.GetValueOrDefault(religionUID, "Unknown Religion");
            var isExpanded = viewModel.ExpandedReligions.Contains(religionUID);

            // Draw religion header (collapsible)
            var headerClicked = DrawReligionHeader(viewModel, drawList, currentY, religionUID, religionName, sites.Count, isExpanded);
            if (headerClicked)
            {
                events.Add(new HolySitesEvent.ReligionToggled(religionUID));
            }

            currentY += ReligionHeaderHeight;

            // Draw sites if expanded
            if (isExpanded)
            {
                foreach (var site in sites)
                {
                    var siteClicked = DrawSiteItem(viewModel, drawList, currentY, site);
                    if (siteClicked)
                    {
                        events.Add(new HolySitesEvent.SiteSelected(site.SiteUID));
                    }

                    currentY += SiteItemHeight;
                }
            }

            currentY += SectionSpacing;

            // Check if we've exceeded available height
            if (currentY - startY > availableHeight)
            {
                break;
            }
        }
    }

    private static bool DrawReligionHeader(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float y,
        string religionUID,
        string religionName,
        int siteCount,
        bool isExpanded) // Used by caller to determine whether to render child sites
    {
        var x = viewModel.X + 20f;
        var width = viewModel.Width - 40f;
        var headerRect = new Vector2(x, y);
        var headerRectEnd = new Vector2(x + width, y + ReligionHeaderHeight);

        // Background - use warm brown tone matching the rest of the UI
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.DarkBrown, 0.9f));
        drawList.AddRectFilled(headerRect, headerRectEnd, bgColor, 4f);

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
        drawList.AddRect(headerRect, headerRectEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        // Deity icon
        const float deityIconSize = 32f;
        var deityIconX = x + 12f;
        var deityIconY = y + (ReligionHeaderHeight - deityIconSize) / 2f;

        var domain = viewModel.ReligionDomains.GetValueOrDefault(religionUID, "");
        if (!string.IsNullOrEmpty(domain))
        {
            var deityType = DomainHelper.ParseDeityType(domain);
            var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);

            if (deityTextureId != IntPtr.Zero)
            {
                // Draw deity icon
                var iconMin = new Vector2(deityIconX, deityIconY);
                var iconMax = new Vector2(deityIconX + deityIconSize, deityIconY + deityIconSize);
                var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
                drawList.AddImage(deityTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

                // Add subtle border around icon
                var deityColor = DomainHelper.GetDeityColor(domain);
                var iconBorderColor = ImGui.ColorConvertFloat4ToU32(deityColor * 0.8f);
                drawList.AddRect(iconMin, iconMax, iconBorderColor, 2f, ImDrawFlags.None, 1f);
            }
            else
            {
                // Fallback: colored circle
                var iconCenter = new Vector2(deityIconX + deityIconSize / 2, deityIconY + deityIconSize / 2);
                var deityColor = DomainHelper.GetDeityColor(domain);
                var iconColorU32 = ImGui.ColorConvertFloat4ToU32(deityColor);
                drawList.AddCircleFilled(iconCenter, deityIconSize / 2, iconColorU32, 12);
            }
        }

        // Religion name - adjusted X position to account for deity icon
        var nameX = deityIconX + deityIconSize + 12f;
        var nameY = y + (ReligionHeaderHeight - ImGui.CalcTextSize(religionName).Y) / 2f;
        drawList.AddText(ImGui.GetFont(), 18f,
            new Vector2(nameX, nameY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            religionName);

        // Site count
        var countText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_SITE_COUNT)
            .Replace("{0}", siteCount.ToString());
        var countSize = ImGui.CalcTextSize(countText);
        var countX = x + width - countSize.X - 10f;
        var countY = y + (ReligionHeaderHeight - countSize.Y) / 2f;
        drawList.AddText(ImGui.GetFont(), 14f,
            new Vector2(countX, countY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            countText);

        // Check if clicked
        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= headerRect.X && mousePos.X <= headerRectEnd.X &&
                        mousePos.Y >= headerRect.Y && mousePos.Y <= headerRectEnd.Y;
        var isClicked = isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        // Hover effect - use light brown matching the rest of the UI
        if (isHovered)
        {
            var hoverColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.LightBrown, 0.4f));
            drawList.AddRectFilled(headerRect, headerRectEnd, hoverColor, 4f);
        }

        return isClicked;
    }

    private static bool DrawSiteItem(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float y,
        HolySiteResponsePacket.HolySiteInfo site)
    {
        var x = viewModel.X + 20f + IndentSize;
        var width = viewModel.Width - 40f - IndentSize;
        var itemRect = new Vector2(x, y);
        var itemRectEnd = new Vector2(x + width, y + SiteItemHeight);

        // Background - use darker brown tone matching table rows
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.TableBackground, 0.95f));
        drawList.AddRectFilled(itemRect, itemRectEnd, bgColor, 3f);

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.BorderColor, 0.5f));
        drawList.AddRect(itemRect, itemRectEnd, borderColor, 3f, ImDrawFlags.None, 1f);

        var currentX = x + SiteItemPadding;
        var currentY = y + SiteItemPadding;

        // Site name (larger font)
        drawList.AddText(ImGui.GetFont(), 15f,
            new Vector2(currentX, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            site.SiteName);

        currentY += 20f;

        // Tier badge
        var tierText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_TIER)
            .Replace("{0}", site.Tier.ToString());
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(currentX, currentY),
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.84f, 0f, 1f)), // Gold color
            tierText);

        // Volume
        var volumeX = currentX + 100f;
        var volumeText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_VOLUME)
            .Replace("{0}", site.Volume.ToString("N0"));
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(volumeX, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            volumeText);

        // Multipliers
        var multiplierX = currentX + 250f;
        var multiplierText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_MULTIPLIERS)
            .Replace("{0}", site.TerritoryMultiplier.ToString("F1"))
            .Replace("{1}", site.PrayerMultiplier.ToString("F1"));
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(multiplierX, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            multiplierText);

        // Check if clicked
        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= itemRect.X && mousePos.X <= itemRectEnd.X &&
                        mousePos.Y >= itemRect.Y && mousePos.Y <= itemRectEnd.Y;
        var isClicked = isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        // Hover effect - use light brown matching the rest of the UI
        if (isHovered)
        {
            var hoverColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.LightBrown, 0.3f));
            drawList.AddRectFilled(itemRect, itemRectEnd, hoverColor, 3f);
        }

        return isClicked;
    }
}
