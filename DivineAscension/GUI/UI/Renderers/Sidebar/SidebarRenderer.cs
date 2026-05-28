using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using DivineAscension.Services.UI;
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
    private const int ChapterFontSize = 18;
    private const int VerseFontSize = 13;
    private const float ItemIndent = 12f;
    private const float ChapterChevronSize = 9f;
    private const float ChapterChevronPadding = 6f;
    private const float ItemBulletHalfSize = 3f;
    private const float ItemBulletActiveHalfSize = 4f;
    private const float ItemBulletPadding = 6f;
    private const float ItemVerseGap = 8f;
    private const float ActiveRibbonWidth = 2.5f;
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
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorPalette.Gold * 0.18f);
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

        // Codex spine: hairline gold rule along the right edge separates the
        // sidebar from the content pane, like the gutter of a bound book.
        var spineDrawList = ImGui.GetWindowDrawList();
        var spineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.35f);
        spineDrawList.AddLine(
            new Vector2(rect.X + rect.W - 0.5f, rect.Y),
            new Vector2(rect.X + rect.W - 0.5f, rect.Y + rect.H),
            spineColor, 1f);

        ImGui.EndChild();
        ImGui.PopStyleColor(2);
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

        // Single chevron painted over the button face. Collapsed → points
        // right (>), expanded → points left (<). Primitive triangle keeps
        // it consistent with the rest of the codex chrome.
        var drawList = ImGui.GetWindowDrawList();
        var direction = vm.IsCollapsed
            ? ChromeRenderer.ChevronDirection.Right
            : ChromeRenderer.ChevronDirection.Left;
        const float chevronSize = 9f;
        var cy = cursor.Y + ToggleStripHeight / 2f;
        var cx = cursor.X + availWidth / 2f;
        ChromeRenderer.DrawChevron(drawList, cx, cy, chevronSize, direction);

        // Hairline rule under the toggle strip separates the chrome from the
        // first chapter heading.
        var ruleY = cursor.Y + ToggleStripHeight - 1f;
        var ruleColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.35f);
        drawList.AddLine(new Vector2(cursor.X, ruleY),
            new Vector2(cursor.X + availWidth, ruleY), ruleColor, 1f);
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

        // Chapter row uses Cinzel Regular at 18 (matches the default 18px so
        // the chevron + label line stays vertically centred). Item labels
        // below intentionally stay in the default font for legibility.
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        var cinzelChapter = CinzelFontSystem.GetRegular(ChapterFontSize);
        var labelFont = cinzelChapter ?? ImGui.GetFont();
        var labelSize = cinzelChapter.HasValue ? ChapterFontSize : ImGui.GetFontSize();
        var textX = chevronX + ChapterChevronSize / 2f + ChapterChevronPadding;
        var textY = cursor.Y + (GroupHeaderHeight - labelSize) / 2f;
        drawList.AddText(labelFont, labelSize, new Vector2(textX, textY), textColor, group.Label);

        // Chapter rule: ornament divider beneath the heading anchors the
        // chapter visually and breaks the three groups apart on the rail.
        var ruleWidth = ImGui.GetContentRegionAvail().X;
        var ruleY = cursor.Y + GroupHeaderHeight - 3f;
        if (ruleWidth > 0f)
        {
            ChromeRenderer.DrawDivider(drawList, cursor.X, ruleY, ruleWidth);
        }
    }

    private static void DrawItem(SidebarItemViewModel item, int verseIndex, List<SidebarEvent> events)
    {
        ImGui.Indent(ItemIndent);

        var cursor = ImGui.GetCursorScreenPos();
        var rowWidth = ImGui.GetContentRegionAvail().X;
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
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPalette.Gold * 0.22f);
            }

            clicked = ImGui.Selectable($"##da-sidebar-item-{item.Id}", item.IsActive,
                ImGuiSelectableFlags.None, new Vector2(0, ItemHeight));

            if (item.IsActive)
            {
                ImGui.PopStyleColor();
            }
        }

        if (clicked) events.Add(new SidebarEvent.ItemClicked(item.Id));

        // Active rows get a gold ribbon down the left margin — the reader's
        // bookmark for "you are here."
        if (item.IsActive)
        {
            var ribbonColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddRectFilled(
                new Vector2(cursor.X, cursor.Y),
                new Vector2(cursor.X + ActiveRibbonWidth, cursor.Y + ItemHeight),
                ribbonColor);
        }

        // Diamond bullet (`✦` stand-in) leads each chapter verse. Active row
        // bullet sits a touch larger so the active state still reads on the
        // bullet column alone.
        var bulletCx = cursor.X + ItemBulletPadding + ItemBulletHalfSize;
        var bulletCy = cursor.Y + ItemHeight / 2f;
        var bulletColor = item.IsDisabled
            ? ColorPalette.Gold * 0.4f
            : ColorPalette.Gold;
        var bulletHalf = item.IsActive ? ItemBulletActiveHalfSize : ItemBulletHalfSize;
        ChromeRenderer.DrawDiamond(drawList, bulletCx, bulletCy, bulletHalf, bulletColor);

        // Lowercase Roman verse numeral (i. ii. iii. ...) in Cinzel so the
        // numeral carries the same manuscript voice as the chapter header.
        var verseColor = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
            ? ColorPalette.Gold * 0.4f
            : ColorPalette.Gold * 0.8f);

        // Active rows render the label in gold; inactive rows keep white for
        // contrast against the page.
        var textColor = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
            ? ColorPalette.Grey
            : item.IsActive ? ColorPalette.Gold : ColorPalette.White);

        var verse = verseIndex >= 0 && verseIndex < LowerRomanVerse.Length
            ? LowerRomanVerse[verseIndex]
            : string.Empty;
        var cinzelVerse = CinzelFontSystem.GetRegular(VerseFontSize);
        var verseFont = cinzelVerse ?? ImGui.GetFont();
        var verseFontSize = cinzelVerse.HasValue ? VerseFontSize : ImGui.GetFontSize();
        var verseX = bulletCx + ItemBulletHalfSize + ItemBulletPadding;
        var verseY = cursor.Y + (ItemHeight - verseFontSize) / 2f;
        var verseWidth = 0f;
        if (verse.Length > 0)
        {
            drawList.AddText(verseFont, verseFontSize, new Vector2(verseX, verseY),
                verseColor, verse);
            verseWidth = ImGui.CalcTextSize(verse).X
                         * (verseFontSize / ImGui.GetFontSize());
        }

        // Body label stays in the default font for legibility.
        var labelFontSize = ImGui.GetFontSize();
        var textY = cursor.Y + (ItemHeight - labelFontSize) / 2f;
        var textX = verseX + verseWidth + (verseWidth > 0f ? ItemVerseGap : 0f);
        drawList.AddText(new Vector2(textX, textY), textColor, item.Label);

        // Badge: right-aligned parchment chip with gold border. Inline
        // "(N)" suffix gets lost; a chip on the right edge reads at a glance.
        if (item.Badge > 0)
        {
            var badgeText = item.Badge.ToString();
            var badgeTextSize = ImGui.CalcTextSize(badgeText);
            const float badgePadX = 5f;
            const float badgePadY = 1f;
            const float badgeRightMargin = 8f;
            var badgeW = badgeTextSize.X + badgePadX * 2f;
            var badgeH = badgeTextSize.Y + badgePadY * 2f;
            var badgeX = cursor.X + rowWidth - badgeRightMargin - badgeW;
            var badgeY = cursor.Y + (ItemHeight - badgeH) / 2f;
            var badgeBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.15f);
            var badgeBorderColor = ImGui.ColorConvertFloat4ToU32(
                item.IsDisabled ? ColorPalette.Gold * 0.5f : ColorPalette.Gold);
            var badgeTextColor = ImGui.ColorConvertFloat4ToU32(
                item.IsDisabled ? ColorPalette.Grey : ColorPalette.Gold);
            var bMin = new Vector2(badgeX, badgeY);
            var bMax = new Vector2(badgeX + badgeW, badgeY + badgeH);
            drawList.AddRectFilled(bMin, bMax, badgeBgColor, 2f);
            drawList.AddRect(bMin, bMax, badgeBorderColor, 2f, ImDrawFlags.None, 1f);
            drawList.AddText(new Vector2(badgeX + badgePadX, badgeY + badgePadY),
                badgeTextColor, badgeText);
        }

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
                var cinzelStamp = CinzelFontSystem.GetRegular(VerseFontSize);
                var stampFont = cinzelStamp ?? ImGui.GetFont();
                var stampFontSize = cinzelStamp.HasValue ? VerseFontSize : ImGui.GetFontSize();
                var stampRawSize = ImGui.CalcTextSize(stamp);
                var stampScale = stampFontSize / ImGui.GetFontSize();
                var stampSize = new Vector2(stampRawSize.X * stampScale,
                    stampRawSize.Y * stampScale);
                var stampPos = new Vector2(
                    rowOrigin.X + (CollapsedStripIconSize - stampSize.X) / 2f,
                    rowOrigin.Y + (CollapsedStripIconSize - stampSize.Y) / 2f);
                drawList.AddText(stampFont, stampFontSize, stampPos, stampColor, stamp);

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
