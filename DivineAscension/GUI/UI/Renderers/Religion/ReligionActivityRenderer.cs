using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Activity;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for religion activity feed showing member contributions
/// </summary>
internal static class ReligionActivityRenderer
{
    private const float ENTRY_HEIGHT = 80f;
    private const float ENTRY_PADDING = 12f;
    private const float ICON_SIZE = 48f;

    public static ReligionActivityRenderResult Draw(ReligionActivityViewModel viewModel)
    {
        var events = new List<ActivityEvent>();
        var drawList = ImGui.GetWindowDrawList();

        // Loading state
        if (viewModel.IsLoading)
        {
            DrawLoadingState(viewModel, drawList);
            return new ReligionActivityRenderResult(events, viewModel.Height);
        }

        // Error state
        if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
        {
            DrawErrorState(viewModel, drawList);
            return new ReligionActivityRenderResult(events, viewModel.Height);
        }

        // Empty state
        if (viewModel.Entries.Count == 0)
        {
            DrawEmptyState(viewModel, drawList);
            return new ReligionActivityRenderResult(events, viewModel.Height);
        }

        // Header with refresh button
        var headerHeight = DrawHeader(viewModel, drawList, out var refreshClicked);
        if (refreshClicked)
        {
            events.Add(new ActivityEvent.RefreshRequested());
        }

        // Activity feed with scrolling
        var contentY = viewModel.Y + headerHeight + 20f;
        var contentHeight = viewModel.Height - headerHeight - 20f;

        DrawActivityFeed(viewModel, contentY, contentHeight, events);

        return new ReligionActivityRenderResult(events, viewModel.Height);
    }

    private static float DrawHeader(ReligionActivityViewModel viewModel,
        ImDrawListPtr drawList, out bool refreshClicked)
    {
        refreshClicked = false;
        var headerY = viewModel.Y + 10f;

        // Title
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_TITLE);
        drawList.AddText(ImGui.GetFont(), 18f,
            new Vector2(viewModel.X + 20f, headerY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), titleText);

        // Refresh button
        var buttonWidth = 100f;
        var buttonHeight = 30f;
        var buttonX = viewModel.X + viewModel.Width - buttonWidth - 20f;
        var refreshText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_REFRESH);

        if (ButtonRenderer.DrawButton(
                drawList,
                refreshText,
                buttonX,
                headerY,
                buttonWidth,
                buttonHeight,
                isPrimary: false))
        {
            refreshClicked = true;
        }

        return 50f; // Header height
    }

    private static void DrawActivityFeed(ReligionActivityViewModel viewModel,
        float contentY, float contentHeight, List<ActivityEvent> events)
    {
        const float scrollbarWidth = 12f;
        const float scrollbarPadding = 4f;

        // Calculate total content height
        var totalContentHeight = viewModel.Entries.Count * (ENTRY_HEIGHT + ENTRY_PADDING);
        var maxScroll = Math.Max(0f, totalContentHeight - contentHeight);

        // Scrollable region (without built-in scrollbar)
        var scrollRegionStart = new Vector2(viewModel.X, contentY);
        var scrollRegionSize = new Vector2(viewModel.Width - scrollbarWidth - scrollbarPadding, contentHeight);

        ImGui.SetCursorScreenPos(scrollRegionStart);
        ImGui.BeginChild("ActivityScroll", scrollRegionSize, false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        // Get child window drawlist and position
        var childDrawList = ImGui.GetWindowDrawList();
        var childPos = ImGui.GetCursorScreenPos();

        // Draw entries with manual scroll offset
        var currentY = -viewModel.ScrollY;

        foreach (var entry in viewModel.Entries)
        {
            var entryY = childPos.Y + currentY;

            // Only draw if visible (culling for performance)
            if (entryY + ENTRY_HEIGHT >= contentY && entryY <= contentY + contentHeight)
            {
                DrawActivityEntry(entry, childPos.X + 10f, entryY,
                    viewModel.Width - scrollbarWidth - scrollbarPadding - 20f, childDrawList);
            }

            currentY += ENTRY_HEIGHT + ENTRY_PADDING;
        }

        ImGui.EndChild();

        // Draw custom scrollbar
        if (maxScroll > 0)
        {
            var scrollbarX = viewModel.X + viewModel.Width - scrollbarWidth;
            var drawList = ImGui.GetWindowDrawList();

            Scrollbar.Draw(
                drawList,
                scrollbarX,
                contentY,
                scrollbarWidth,
                contentHeight,
                viewModel.ScrollY,
                maxScroll
            );

            // Handle mouse wheel scrolling
            var mousePos = ImGui.GetMousePos();
            var newScrollY = Scrollbar.HandleMouseWheel(
                viewModel.ScrollY,
                maxScroll,
                mousePos.X,
                mousePos.Y,
                viewModel.X,
                contentY,
                viewModel.Width,
                contentHeight
            );

            // Handle scrollbar dragging
            newScrollY = Scrollbar.HandleDragging(
                newScrollY,
                maxScroll,
                scrollbarX,
                contentY,
                scrollbarWidth,
                contentHeight
            );

            // Track scroll changes
            if (Math.Abs(newScrollY - viewModel.ScrollY) > 0.01f)
            {
                events.Add(new ActivityEvent.ScrollChanged(newScrollY));
            }
        }
    }

    private static void DrawActivityEntry(ActivityLogResponsePacket.ActivityEntry entry,
        float x, float y, float width,
        ImDrawListPtr drawList)
    {
        var entryPos = new Vector2(x, y);

        // Background
        drawList.AddRectFilled(entryPos,
            new Vector2(x + width, y + ENTRY_HEIGHT),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.4f), 8f);

        // Border
        drawList.AddRect(entryPos,
            new Vector2(x + width, y + ENTRY_HEIGHT),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.3f), 8f,
            ImDrawFlags.None, 1f);

        // Deity icon (left) - position center so left edge is at x + 15f
        var iconCenterX = x + 15f + (ICON_SIZE / 2f);
        var iconCenterY = y + (ENTRY_HEIGHT / 2f);
        DrawDeityIcon(entry.DeityDomain, iconCenterX, iconCenterY, drawList);

        // Text content (right of icon)
        var textX = x + 15f + ICON_SIZE + 15f;
        var textY = y + 12f;

        // Player name
        drawList.AddText(ImGui.GetFont(), 14f,
            new Vector2(textX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            entry.PlayerName);

        // Action description
        var actionText = FormatActionText(entry.ActionType, entry.FavorAmount, entry.PrestigeAmount);
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(textX, textY + 20f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            actionText);

        // Timestamp (bottom right)
        var timestamp = new DateTime(entry.TimestampTicks);
        var timeText = FormatTimestamp(timestamp);
        var timeTextSize = ImGui.CalcTextSize(timeText);
        drawList.AddText(ImGui.GetFont(), 11f,
            new Vector2(x + width - timeTextSize.X - 15f, y + ENTRY_HEIGHT - 25f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.7f),
            timeText);
    }

    private static void DrawDeityIcon(string domain, float x, float y, ImDrawListPtr drawList)
    {
        // Parse domain string to enum
        var deityType = DomainHelper.ParseDeityType(domain);
        var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);

        // Calculate icon bounds (centered on x, y)
        var iconMin = new Vector2(x - ICON_SIZE / 2f, y - ICON_SIZE / 2f);
        var iconMax = new Vector2(x + ICON_SIZE / 2f, y + ICON_SIZE / 2f);

        if (deityTextureId != IntPtr.Zero)
        {
            // Draw deity icon texture (no tint - full color)
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(deityTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Add subtle border around icon for visual cohesion
            var deityColor = DomainHelper.GetDeityColor(domain);
            var iconBorderColor = ImGui.ColorConvertFloat4ToU32(deityColor * 0.8f);
            drawList.AddRect(iconMin, iconMax, iconBorderColor, 4f, ImDrawFlags.None, 2f);
        }
        else
        {
            // Fallback: Use placeholder colored circle if texture not available
            var deityColor = DomainHelper.GetDeityColor(domain);
            drawList.AddCircleFilled(new Vector2(x, y), ICON_SIZE / 2f,
                ImGui.ColorConvertFloat4ToU32(deityColor));

            // Domain initial letter
            var letter = domain.Length > 0 ? domain[0].ToString() : "?";
            var textSize = ImGui.CalcTextSize(letter);
            drawList.AddText(ImGui.GetFont(), 20f,
                new Vector2(x - textSize.X / 2f, y - textSize.Y / 2f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), letter);
        }
    }

    private static string FormatActionText(string actionType, int favor, int prestige)
    {
        // Clean up the action text with regex
        // 1. Replace hyphens and underscores with spaces
        var cleaned = Regex.Replace(actionType, @"[-_]", " ");

        // 2. Remove common suffixes (adult, male, female, etc.) - keep it concise
        cleaned = Regex.Replace(cleaned, @"\s+(adult|male|female|baby|young)(?=\s|$)", "", RegexOptions.IgnoreCase);

        // 3. Capitalize first letter of each word
        cleaned = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower());

        // 4. Limit length to keep UI clean (show first 3-4 words)
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 4)
        {
            cleaned = string.Join(" ", words.Take(4)) + "...";
        }

        return $"{cleaned} +{favor} favor, +{prestige} prestige";
    }

    private static string FormatTimestamp(DateTime timestamp)
    {
        var elapsed = DateTime.UtcNow - timestamp;

        if (elapsed.TotalMinutes < 1) return "Just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";

        return timestamp.ToString("MMM dd");
    }

    private static void DrawLoadingState(ReligionActivityViewModel viewModel, ImDrawListPtr drawList)
    {
        var text = "Loading activity...";
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            viewModel.Y + viewModel.Height / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawEmptyState(ReligionActivityViewModel viewModel, ImDrawListPtr drawList)
    {
        var text = "No recent activity";
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            viewModel.Y + viewModel.Height / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawErrorState(ReligionActivityViewModel viewModel, ImDrawListPtr drawList)
    {
        var text = $"Error: {viewModel.ErrorMessage}";
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            viewModel.Y + viewModel.Height / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.3f, 1f)), text);
    }
}