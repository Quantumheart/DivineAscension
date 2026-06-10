using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.PlayerInfo;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.PlayerInfo;

/// <summary>
///     Scrollable notification list with section header + filter + clear
///     controls. Extracted verbatim from the retired right-rail renderer;
///     the only changes are the section-label styling (Gold + SubsectionLabel
///     to match other content-page sections) and that container width is now
///     content-pane width instead of the fixed 340 px rail.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class NotificationFeedRenderer
{
    private static float HeaderHeight => UiScale.Scaled(27f);
    private static float InnerPadding => UiScale.Scaled(12f);

    public static void Draw(
        float x, float y, float width, float height,
        IReadOnlyList<NotificationHistoryEntry> notifications,
        bool showUnreadOnly,
        List<PlayerInfoEvent> events)
    {
        // Panel backdrop — matches the Religion / Civilization table style so
        // the feed reads as the same kind of container as a browse table.
        var drawList = ImGui.GetWindowDrawList();
        var bg = ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground);
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            bg, UiScale.Scaled(4f));

        DrawHeader(x + InnerPadding, y + UiScale.Scaled(4f), width - InnerPadding * 2f, showUnreadOnly, events);

        var listTop = y + HeaderHeight;
        var listHeight = y + height - listTop - InnerPadding;
        if (listHeight <= 0f) return;

        var visibleCount = 0;
        for (var i = 0; i < notifications.Count; i++)
            if (!showUnreadOnly || !notifications[i].Read) visibleCount++;

        if (visibleCount == 0)
        {
            var key = showUnreadOnly
                ? LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_EMPTY_UNREAD
                : LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_EMPTY;
            DrawCenteredEmptyState(drawList,
                LocalizationService.Instance.Get(key),
                x, listTop, width, listHeight);
            return;
        }

        ImGui.SetCursorScreenPos(new Vector2(x + InnerPadding, listTop));
        ImGui.BeginChild("##da-playerinfo-feed",
            new Vector2(width - InnerPadding * 2f, listHeight), false,
            ImGuiWindowFlags.None);

        var rowWidth = ImGui.GetContentRegionAvail().X;
        for (var i = notifications.Count - 1; i >= 0; i--)
        {
            var entry = notifications[i];
            if (showUnreadOnly && entry.Read) continue;
            DrawNotificationRow(i, entry, rowWidth, events);
        }

        ImGui.EndChild();
    }

    private static void DrawCenteredEmptyState(ImDrawListPtr drawList, string text,
        float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawHeader(float x, float y, float width, bool showUnreadOnly,
        List<PlayerInfoEvent> events)
    {
        var drawList = ImGui.GetWindowDrawList();
        var title = LocalizationService.Instance.Get(LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_TITLE);
        var unreadLabel = LocalizationService.Instance.Get(LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_UNREAD_ONLY);
        var clearLabel = LocalizationService.Instance.Get(LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_CLEAR);

        // Section label — Gold + SubsectionLabel, same style as the other
        // content-page sub-section headings (Religion Info / Description).
        TextRenderer.DrawLabel(drawList, title, x, y, SubsectionLabel, ColorPalette.Gold);

        // Right-aligned controls. Anchor to the right edge using ImGui frame
        // metrics so checkbox + button line up regardless of font.
        var style = ImGui.GetStyle();
        var frameH = ImGui.GetFrameHeight();
        var checkboxWidth = frameH + style.ItemInnerSpacing.X
                                   + ImGui.CalcTextSize(unreadLabel).X;
        var clearWidth = ImGui.CalcTextSize(clearLabel).X + style.FramePadding.X * 2f;
        var controlsWidth = checkboxWidth + style.ItemSpacing.X + clearWidth;

        ImGui.SetCursorScreenPos(new Vector2(x + width - controlsWidth, y));

        var unread = showUnreadOnly;
        if (ImGui.Checkbox($"{unreadLabel}##da-playerinfo-unread", ref unread))
        {
            events.Add(new PlayerInfoEvent.SetUnreadOnly(unread));
        }
        ImGui.SameLine();
        if (ImGui.Button($"{clearLabel}##da-playerinfo-clear"))
        {
            events.Add(new PlayerInfoEvent.ClearNotificationHistory());
        }
    }

    private static void DrawNotificationRow(int index, NotificationHistoryEntry entry,
        float width, List<PlayerInfoEvent> events)
    {
        var iconSize = UiScale.Scaled(20f);
        var iconRightGap = UiScale.Scaled(8f);
        var verticalPadding = UiScale.Scaled(4f);
        var rowSpacing = UiScale.Scaled(4f);

        var fontHeight = ImGui.GetFontSize();
        var textColumnWidth = MathF.Max(0f, width - iconSize - iconRightGap - UiScale.Scaled(4f));

        var titleLine = $"{entry.Timestamp:HH:mm}  {entry.Title}";
        var hasBody = !string.IsNullOrEmpty(entry.Body);
        var rowHeight = verticalPadding * 2f + fontHeight + (hasBody ? fontHeight + UiScale.Scaled(2f) : 0f);
        rowHeight = MathF.Max(rowHeight, iconSize + verticalPadding * 2f);

        var rowStart = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        var clicked = ImGui.InvisibleButton($"##da-playerinfo-notif-{index}",
            new Vector2(width, rowHeight));
        var hovered = ImGui.IsItemHovered() && !entry.Read;
        if (clicked && !entry.Read)
        {
            events.Add(new PlayerInfoEvent.MarkNotificationRead(index));
        }
        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        var rowEnd = new Vector2(rowStart.X + width, rowStart.Y + rowHeight);
        if (!entry.Read)
        {
            var fill = hovered
                ? ColorPalette.WithAlpha(ColorPalette.Gold, 0.18f)
                : ColorPalette.WithAlpha(ColorPalette.Gold, 0.08f);
            drawList.AddRectFilled(rowStart, rowEnd, ImGui.ColorConvertFloat4ToU32(fill), UiScale.Scaled(3f));
        }

        var iconY = rowStart.Y + verticalPadding;
        var textureId = GuiIconLoader.GetTextureId("gui", "invites");
        if (textureId != IntPtr.Zero)
        {
            var tint = entry.Read
                ? new Vector4(0.55f, 0.55f, 0.55f, 0.55f)
                : DomainHelper.GetDeityColor(entry.Deity);
            drawList.AddImage(textureId,
                new Vector2(rowStart.X, iconY),
                new Vector2(rowStart.X + iconSize, iconY + iconSize),
                Vector2.Zero, Vector2.One,
                ImGui.ColorConvertFloat4ToU32(tint));
        }

        var textX = rowStart.X + iconSize + iconRightGap;
        var titleColor = ImGui.ColorConvertFloat4ToU32(entry.Read
            ? ColorPalette.Grey
            : ColorPalette.White);
        drawList.AddText(new Vector2(textX, rowStart.Y + verticalPadding),
            titleColor, titleLine);

        if (hasBody)
        {
            var bodyY = rowStart.Y + verticalPadding + fontHeight + UiScale.Scaled(2f);
            var bodyColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.PushClipRect(new Vector2(textX, bodyY),
                new Vector2(textX + textColumnWidth, bodyY + fontHeight + UiScale.Scaled(2f)), true);
            drawList.AddText(new Vector2(textX, bodyY), bodyColor, entry.Body);
            drawList.PopClipRect();
        }

        ImGui.Dummy(new Vector2(0f, rowSpacing));
    }
}
