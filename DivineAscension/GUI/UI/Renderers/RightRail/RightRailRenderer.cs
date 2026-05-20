using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.RightRail;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Blessing;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.RightRail;

/// <summary>
///     Vertical 340 px column: religion + civilization status (via
///     <see cref="ReligionHeaderRenderer" />), then a scrollable notification
///     feed styled as a stack of letters. Owns the outer panel chrome and
///     delegates status content to the header renderer.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RightRailRenderer
{
    private const float Padding = 8f;
    private const float HeaderBottomGap = 8f;
    private const float FeedHeaderBottomGap = 4f;

    public static IReadOnlyList<RightRailEvent> Draw(UiRect rect, RightRailViewModel vm)
    {
        var events = new List<RightRailEvent>();
        if (rect.W <= 0f || rect.H <= 0f) return events;

        var drawList = ImGui.GetWindowDrawList();

        // Outer panel chrome.
        var bg = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var border = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        var topLeft = new Vector2(rect.X, rect.Y);
        var botRight = new Vector2(rect.Right, rect.Bottom);
        drawList.AddRectFilled(topLeft, botRight, bg, 4f);
        drawList.AddRect(topLeft, botRight, border, 4f, ImDrawFlags.None, 2f);

        // Status block (deity + civ identity).
        var headerVm = WithBounds(vm.Header, rect.X + Padding, rect.Y + Padding,
            rect.W - Padding * 2f);
        var headerHeight = ReligionHeaderRenderer.Draw(headerVm);

        // Notification feed fills whatever's left below the status block.
        var feedTop = rect.Y + Padding + headerHeight + HeaderBottomGap;
        var feedHeight = rect.Bottom - Padding - feedTop;
        if (feedHeight > 0f)
        {
            DrawNotificationFeed(
                new UiRect(rect.X + Padding, feedTop, rect.W - Padding * 2f, feedHeight),
                vm, events);
        }

        return events;
    }

    private static ReligionHeaderViewModel WithBounds(ReligionHeaderViewModel src,
        float x, float y, float width)
    {
        return new ReligionHeaderViewModel(
            src.HasReligion,
            src.HasCivilization,
            src.CurrentCivilizationName,
            src.CivilizationMemberReligions,
            src.CurrentDeity,
            src.CurrentDeityName,
            src.CurrentReligionName,
            src.ReligionMemberCount,
            src.PlayerRoleInReligion,
            src.PlayerFavorProgress,
            src.ReligionPrestigeProgress,
            src.IsCivilizationFounder,
            src.CivilizationIcon,
            src.CivilizationRank,
            x,
            y,
            width);
    }

    private static void DrawNotificationFeed(UiRect rect, RightRailViewModel vm,
        List<RightRailEvent> events)
    {
        var headerHeight = DrawFeedHeader(rect, vm, events);

        var listTop = rect.Y + headerHeight + FeedHeaderBottomGap;
        var listHeight = rect.Bottom - listTop;
        if (listHeight <= 0f) return;

        ImGui.SetCursorScreenPos(new Vector2(rect.X, listTop));
        ImGui.BeginChild("##da-rightrail-feed", new Vector2(rect.W, listHeight), false,
            ImGuiWindowFlags.None);

        var rowWidth = ImGui.GetContentRegionAvail().X;
        var visible = 0;

        // Newest-first.
        for (var i = vm.Notifications.Count - 1; i >= 0; i--)
        {
            var entry = vm.Notifications[i];
            if (vm.ShowUnreadOnly && entry.Read) continue;
            DrawNotificationRow(i, entry, rowWidth, events);
            visible++;
        }

        if (visible == 0)
        {
            var key = vm.ShowUnreadOnly
                ? LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_EMPTY_UNREAD
                : LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_EMPTY;
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Grey);
            ImGui.TextWrapped(LocalizationService.Instance.Get(key));
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();
    }

    /// <summary>
    ///     Draw the feed header (title + filter checkbox + clear button) into
    ///     the top of <paramref name="rect" />. Returns the actual height
    ///     consumed so the caller can position the row list below it.
    /// </summary>
    private static float DrawFeedHeader(UiRect rect, RightRailViewModel vm,
        List<RightRailEvent> events)
    {
        var drawList = ImGui.GetWindowDrawList();
        var unreadLabel = LocalizationService.Instance.Get(LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_UNREAD_ONLY);
        var clearLabel = LocalizationService.Instance.Get(LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_CLEAR);

        // Use real ImGui frame metrics so the controls align cleanly against
        // the right edge regardless of font size or style padding.
        var style = ImGui.GetStyle();
        var frameH = ImGui.GetFrameHeight();
        var checkboxWidth = frameH + style.ItemInnerSpacing.X
                                   + ImGui.CalcTextSize(unreadLabel).X;
        var clearWidth = ImGui.CalcTextSize(clearLabel).X + style.FramePadding.X * 2f;
        var controlsWidth = checkboxWidth + style.ItemSpacing.X + clearWidth;

        var headerHeight = frameH;

        // Left: title text, vertical-centered against the control row height.
        var title = LocalizationService.Instance.Get(LocalizationKeys.RIGHT_RAIL_NOTIFICATIONS_TITLE);
        var titleColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        var fontHeight = ImGui.GetFontSize();
        var titleY = rect.Y + (headerHeight - fontHeight) / 2f;
        drawList.AddText(new Vector2(rect.X, titleY), titleColor, title);

        // Right: checkbox + button via ImGui's auto-layout. Anchor the cursor
        // at right-edge minus the combined control width and let SameLine
        // handle the in-between spacing.
        ImGui.SetCursorScreenPos(new Vector2(rect.Right - controlsWidth, rect.Y));

        var unread = vm.ShowUnreadOnly;
        if (ImGui.Checkbox($"{unreadLabel}##da-rail-unread", ref unread))
        {
            events.Add(new RightRailEvent.SetUnreadOnly(unread));
        }
        ImGui.SameLine();
        if (ImGui.Button($"{clearLabel}##da-rail-clear"))
        {
            events.Add(new RightRailEvent.ClearNotificationHistory());
        }

        return headerHeight;
    }

    private static void DrawNotificationRow(int index, NotificationHistoryEntry entry,
        float width, List<RightRailEvent> events)
    {
        const float iconSize = 20f;
        const float iconRightGap = 8f;
        const float verticalPadding = 4f;
        const float rowSpacing = 4f;

        var fontHeight = ImGui.GetFontSize();
        var textColumnWidth = MathF.Max(0f, width - iconSize - iconRightGap - 4f);

        var titleLine = $"{entry.Timestamp:HH:mm}  {entry.Title}";
        var hasBody = !string.IsNullOrEmpty(entry.Body);
        var rowHeight = verticalPadding * 2f + fontHeight + (hasBody ? fontHeight + 2f : 0f);
        rowHeight = MathF.Max(rowHeight, iconSize + verticalPadding * 2f);

        var rowStart = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        // Invisible click target spans the row. Read entries are not interactive.
        var clicked = ImGui.InvisibleButton($"##da-rail-notif-{index}",
            new Vector2(width, rowHeight));
        var hovered = ImGui.IsItemHovered() && !entry.Read;
        if (clicked && !entry.Read)
        {
            events.Add(new RightRailEvent.MarkNotificationRead(index));
        }
        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        // Background: subtle unread highlight, hover lift, read = transparent.
        var rowEnd = new Vector2(rowStart.X + width, rowStart.Y + rowHeight);
        if (!entry.Read)
        {
            var fill = hovered
                ? ColorPalette.WithAlpha(ColorPalette.Gold, 0.18f)
                : ColorPalette.WithAlpha(ColorPalette.Gold, 0.08f);
            drawList.AddRectFilled(rowStart, rowEnd, ImGui.ColorConvertFloat4ToU32(fill), 3f);
        }

        // Envelope icon, deity-tinted when unread, washed out when read.
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

        // Title row: HH:mm + title.
        var textX = rowStart.X + iconSize + iconRightGap;
        var titleColor = ImGui.ColorConvertFloat4ToU32(entry.Read
            ? ColorPalette.Grey
            : ColorPalette.White);
        drawList.AddText(new Vector2(textX, rowStart.Y + verticalPadding),
            titleColor, titleLine);

        // Body line directly under the title, dimmed. Truncated by clip rect to
        // avoid bleeding into the next row when very long.
        if (hasBody)
        {
            var bodyY = rowStart.Y + verticalPadding + fontHeight + 2f;
            var bodyColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.PushClipRect(new Vector2(textX, bodyY),
                new Vector2(textX + textColumnWidth, bodyY + fontHeight + 2f), true);
            drawList.AddText(new Vector2(textX, bodyY), bodyColor, entry.Body);
            drawList.PopClipRect();
        }

        ImGui.Dummy(new Vector2(0f, rowSpacing));
    }
}
