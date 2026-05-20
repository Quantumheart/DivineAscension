using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Sidebar;

/// <summary>
///     Draws the sidebar nav. Pure renderer — accepts a view model and a
///     <see cref="UiRect" />, returns the events the layout coordinator should apply.
///     No state mutation, no manager touching.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class SidebarRenderer
{
    private const float ToggleStripHeight = 28f;
    private const float GroupHeaderHeight = 22f;
    private const float ItemHeight = 24f;
    private const float ItemIndent = 12f;
    private const float ChapterChevronSize = 9f;
    private const float ChapterChevronPadding = 6f;
    private const float ItemBulletHalfSize = 3f;
    private const float ItemBulletPadding = 6f;
    private const float ItemVerseGap = 8f;
    private const float CollapsedStripIconSize = 32f;

    private static readonly string[] LowerRomanVerse =
    {
        "i.", "ii.", "iii.", "iv.", "v.", "vi.", "vii.", "viii.", "ix.", "x."
    };

    private static readonly string[] UpperRomanChapter =
    {
        "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"
    };

    private static readonly string[] LowerRomanVerseBare =
    {
        "i", "ii", "iii", "iv", "v", "vi", "vii", "viii", "ix", "x"
    };

    public static IReadOnlyList<SidebarEvent> Draw(UiRect rect, SidebarViewModel vm)
    {
        var events = new List<SidebarEvent>();
        if (rect.W <= 0f || rect.H <= 0f) return events;

        ImGui.SetCursorScreenPos(new Vector2(rect.X, rect.Y));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ColorPalette.TableBackground);
        ImGui.BeginChild("##da-sidebar", new Vector2(rect.W, rect.H), false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        DrawHideToggle(vm, events);

        if (vm.IsCollapsed)
        {
            DrawCollapsedStrip(vm, events);
        }
        else
        {
            DrawGroups(vm, events);
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
        return events;
    }

    private static void DrawHideToggle(SidebarViewModel vm, List<SidebarEvent> events)
    {
        var cursor = ImGui.GetCursorScreenPos();
        var availWidth = ImGui.GetContentRegionAvail().X;
        if (availWidth <= 0f) availWidth = ToggleStripHeight;

        if (ImGui.Button("##da-sidebar-toggle", new Vector2(-1f, ToggleStripHeight)))
        {
            events.Add(new SidebarEvent.SidebarToggled());
        }

        // Double-chevron painted over the button face. Collapsed → points
        // right (»), expanded → points left («). Primitive triangles keep
        // it consistent with the rest of the codex chrome.
        var drawList = ImGui.GetWindowDrawList();
        var direction = vm.IsCollapsed
            ? ChromeRenderer.ChevronDirection.Right
            : ChromeRenderer.ChevronDirection.Left;
        const float chevronSize = 9f;
        const float chevronStride = 7f;
        var cy = cursor.Y + ToggleStripHeight / 2f;
        var cx = cursor.X + availWidth / 2f;
        ChromeRenderer.DrawChevron(drawList, cx - chevronStride / 2f, cy, chevronSize, direction);
        ChromeRenderer.DrawChevron(drawList, cx + chevronStride / 2f, cy, chevronSize, direction);
    }

    private static void DrawGroups(SidebarViewModel vm, List<SidebarEvent> events)
    {
        foreach (var group in vm.Groups)
        {
            DrawGroupHeader(group, events);
            if (group.IsCollapsed) continue;

            for (var i = 0; i < group.Items.Count; i++)
            {
                DrawItem(group.Items[i], i, events);
            }
        }
    }

    private static void DrawGroupHeader(SidebarGroupViewModel group, List<SidebarEvent> events)
    {
        var cursor = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        // Empty-label Selectable owns the click area; chevron + label painted
        // on top so the chevron stays a primitive (font-coverage independent).
        if (ImGui.Selectable($"##da-sidebar-grp-{group.Key}", false,
                ImGuiSelectableFlags.None, new Vector2(0, GroupHeaderHeight)))
        {
            events.Add(new SidebarEvent.GroupToggled(group.Key));
        }

        var chevronX = cursor.X + ChapterChevronPadding + ChapterChevronSize / 2f;
        var chevronY = cursor.Y + GroupHeaderHeight / 2f;
        var chapterDirection = group.IsCollapsed
            ? ChromeRenderer.ChevronDirection.Right
            : ChromeRenderer.ChevronDirection.Down;
        ChromeRenderer.DrawChevron(drawList, chevronX, chevronY, ChapterChevronSize,
            chapterDirection, ColorPalette.Gold);

        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        var fontSize = ImGui.GetFontSize();
        var textX = chevronX + ChapterChevronSize / 2f + ChapterChevronPadding;
        var textY = cursor.Y + (GroupHeaderHeight - fontSize) / 2f;
        drawList.AddText(new Vector2(textX, textY), textColor, group.Label);
    }

    private static void DrawItem(SidebarItemViewModel item, int verseIndex, List<SidebarEvent> events)
    {
        ImGui.Indent(ItemIndent);

        var label = item.Badge > 0
            ? $"{item.Label}  ({item.Badge})"
            : item.Label;

        var cursor = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        // Empty-label Selectable owns hit detection + active/hover background.
        // Icon and text are painted on top via drawList so the icon doesn't
        // collide with ImGui's text positioning.
        var clicked = false;
        if (item.IsDisabled)
        {
            ImGui.Selectable($"##da-sidebar-item-{item.Id}", false,
                ImGuiSelectableFlags.Disabled, new Vector2(0, ItemHeight));

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)
                && !string.IsNullOrEmpty(item.DisabledTooltipKey))
            {
                using var _ = ChromeRenderer.BeginStyledTooltip();
                ImGui.TextUnformatted(LocalizationService.Instance.Get(item.DisabledTooltipKey!));
            }
        }
        else
        {
            if (item.IsActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPalette.Darken(ColorPalette.Gold, 0.6f));
            }

            clicked = ImGui.Selectable($"##da-sidebar-item-{item.Id}", item.IsActive,
                ImGuiSelectableFlags.None, new Vector2(0, ItemHeight));

            if (item.IsActive)
            {
                ImGui.PopStyleColor();
            }
        }

        if (clicked) events.Add(new SidebarEvent.ItemClicked(item.Id));

        // Diamond bullet (`✦` stand-in) leads each chapter verse.
        var bulletCx = cursor.X + ItemBulletPadding + ItemBulletHalfSize;
        var bulletCy = cursor.Y + ItemHeight / 2f;
        var bulletColor = item.IsDisabled
            ? ColorPalette.Gold * 0.4f
            : ColorPalette.Gold;
        ChromeRenderer.DrawDiamond(drawList, bulletCx, bulletCy, ItemBulletHalfSize, bulletColor);

        // Lowercase Roman verse numeral (i. ii. iii. ...), then label.
        var fontSize = ImGui.GetFontSize();
        var textY = cursor.Y + (ItemHeight - fontSize) / 2f;
        var verseColor = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
            ? ColorPalette.Gold * 0.4f
            : ColorPalette.Gold * 0.75f);
        var textColor = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
            ? ColorPalette.Grey
            : ColorPalette.White);

        var verse = verseIndex >= 0 && verseIndex < LowerRomanVerse.Length
            ? LowerRomanVerse[verseIndex]
            : string.Empty;
        var verseX = bulletCx + ItemBulletHalfSize + ItemBulletPadding;
        var verseWidth = 0f;
        if (verse.Length > 0)
        {
            drawList.AddText(new Vector2(verseX, textY), verseColor, verse);
            verseWidth = ImGui.CalcTextSize(verse).X;
        }

        var textX = verseX + verseWidth + (verseWidth > 0f ? ItemVerseGap : 0f);
        drawList.AddText(new Vector2(textX, textY), textColor, label);

        ImGui.Unindent(ItemIndent);
    }

    private static void DrawCollapsedStrip(SidebarViewModel vm, List<SidebarEvent> events)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(CollapsedStripIconSize, CollapsedStripIconSize);

        for (var g = 0; g < vm.Groups.Count; g++)
        {
            var group = vm.Groups[g];
            var chapter = g >= 0 && g < UpperRomanChapter.Length
                ? UpperRomanChapter[g]
                : (g + 1).ToString();

            // Slim chapter divider above every group except the first — keeps
            // the chapter boundaries visible when only verse numerals show.
            if (g > 0)
            {
                var dividerOrigin = ImGui.GetCursorScreenPos();
                var dividerWidth = ImGui.GetContentRegionAvail().X;
                if (dividerWidth > 0f)
                {
                    ChromeRenderer.DrawDivider(drawList, dividerOrigin.X,
                        dividerOrigin.Y + 4f, dividerWidth);
                }
                ImGui.Dummy(new Vector2(0f, 12f));
            }

            for (var i = 0; i < group.Items.Count; i++)
            {
                var item = group.Items[i];
                var verse = i >= 0 && i < LowerRomanVerseBare.Length
                    ? LowerRomanVerseBare[i]
                    : (i + 1).ToString();
                var stamp = verse;
                var tooltipStamp = $"{chapter}.{verse}";

                var rowOrigin = ImGui.GetCursorScreenPos();

                var flags = item.IsDisabled
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None;

                if (ImGui.Selectable($"##da-strip-{item.Id}", item.IsActive, flags, size))
                {
                    if (!item.IsDisabled) events.Add(new SidebarEvent.ItemClicked(item.Id));
                }

                var isHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);

                // Chapter.verse stamp centered in the cell. Mirrors the
                // expanded ledger's contents-page numbering so the collapsed
                // rail still reads as the same book.
                var stampColor = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
                    ? ColorPalette.Gold * 0.4f
                    : ColorPalette.Gold);
                var stampSize = ImGui.CalcTextSize(stamp);
                var stampPos = new Vector2(
                    rowOrigin.X + (CollapsedStripIconSize - stampSize.X) / 2f,
                    rowOrigin.Y + (CollapsedStripIconSize - stampSize.Y) / 2f);
                drawList.AddText(stampPos, stampColor, stamp);

                if (item.IsActive)
                {
                    var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                    drawList.AddRect(
                        new Vector2(rowOrigin.X, rowOrigin.Y),
                        new Vector2(rowOrigin.X + CollapsedStripIconSize,
                            rowOrigin.Y + CollapsedStripIconSize),
                        borderColor, 2f, ImDrawFlags.None, 1.5f);
                }

                if (item.Badge > 0)
                {
                    // Small diamond in the top-right corner signals unread
                    // letters / pending work — matches the chapter bullet style.
                    ChromeRenderer.DrawDiamond(drawList,
                        rowOrigin.X + CollapsedStripIconSize - 5f,
                        rowOrigin.Y + 5f,
                        3f, ColorPalette.Gold);
                }

                if (isHovered)
                {
                    using var _ = ChromeRenderer.BeginStyledTooltip();
                    ImGui.TextUnformatted($"{tooltipStamp}  {item.Label}");
                    if (item.Badge > 0)
                    {
                        ImGui.TextUnformatted($"({item.Badge})");
                    }
                    if (item.IsDisabled && !string.IsNullOrEmpty(item.DisabledTooltipKey))
                    {
                        ImGui.Separator();
                        ImGui.TextUnformatted(LocalizationService.Instance.Get(item.DisabledTooltipKey!));
                    }
                }
            }
        }
    }
}
