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
    private const string IconDirectory = "GUI";
    private const float ToggleStripHeight = 28f;
    private const float GroupHeaderHeight = 22f;
    private const float ItemHeight = 28f;
    private const float ItemIndent = 12f;
    private const float ItemIconSize = 18f;
    private const float ItemIconPadding = 6f;
    private const float CollapsedStripIconSize = 32f;
    private const float CollapsedIconPadding = 4f;

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
        var label = vm.IsCollapsed ? ">>" : "<<";
        if (ImGui.Button($"{label}##da-sidebar-toggle", new Vector2(-1f, ToggleStripHeight)))
        {
            events.Add(new SidebarEvent.SidebarToggled());
        }
    }

    private static void DrawGroups(SidebarViewModel vm, List<SidebarEvent> events)
    {
        foreach (var group in vm.Groups)
        {
            DrawGroupHeader(group, events);
            if (group.IsCollapsed) continue;

            foreach (var item in group.Items)
            {
                DrawItem(item, events);
            }
        }
    }

    private static void DrawGroupHeader(SidebarGroupViewModel group, List<SidebarEvent> events)
    {
        var chevron = group.IsCollapsed ? "+" : "-";
        ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Gold);
        if (ImGui.Selectable($"{chevron} {group.Label}##da-sidebar-grp-{group.Key}", false,
                ImGuiSelectableFlags.None, new Vector2(0, GroupHeaderHeight)))
        {
            events.Add(new SidebarEvent.GroupToggled(group.Key));
        }
        ImGui.PopStyleColor();
    }

    private static void DrawItem(SidebarItemViewModel item, List<SidebarEvent> events)
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

        // Icon on the left.
        var iconX = cursor.X + ItemIconPadding;
        var iconY = cursor.Y + (ItemHeight - ItemIconSize) / 2f;
        DrawItemIcon(drawList, item.IconName, iconX, iconY, ItemIconSize, item.IsDisabled);

        // Label after the icon. Vertical center against text height.
        var textColor = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
            ? ColorPalette.Grey
            : ColorPalette.White);
        var fontSize = ImGui.GetFontSize();
        var textX = iconX + ItemIconSize + ItemIconPadding;
        var textY = cursor.Y + (ItemHeight - fontSize) / 2f;
        drawList.AddText(new Vector2(textX, textY), textColor, label);

        ImGui.Unindent(ItemIndent);
    }

    private static void DrawItemIcon(ImDrawListPtr drawList, string iconName, float x, float y,
        float iconSize, bool isDisabled)
    {
        if (string.IsNullOrEmpty(iconName)) return;
        var textureId = GuiIconLoader.GetTextureId(IconDirectory, iconName);
        if (textureId == IntPtr.Zero) return;

        var tint = ImGui.ColorConvertFloat4ToU32(isDisabled
            ? new Vector4(1f, 1f, 1f, 0.5f)
            : new Vector4(1f, 1f, 1f, 1f));
        drawList.AddImage(textureId,
            new Vector2(x, y),
            new Vector2(x + iconSize, y + iconSize),
            Vector2.Zero, Vector2.One, tint);
    }

    private static void DrawCollapsedStrip(SidebarViewModel vm, List<SidebarEvent> events)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(CollapsedStripIconSize, CollapsedStripIconSize);

        foreach (var group in vm.Groups)
        {
            foreach (var item in group.Items)
            {
                var rowOrigin = ImGui.GetCursorScreenPos();

                // Hit-test selectable spans the icon's bounding box; the icon
                // overlay is drawn afterwards via drawList so it paints on top.
                var flags = item.IsDisabled
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None;

                if (ImGui.Selectable($"##da-strip-{item.Id}", item.IsActive, flags, size))
                {
                    if (!item.IsDisabled) events.Add(new SidebarEvent.ItemClicked(item.Id));
                }

                var isHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);

                // Overlay the icon on top of the selectable.
                if (!string.IsNullOrEmpty(item.IconName))
                {
                    var textureId = GuiIconLoader.GetTextureId(IconDirectory, item.IconName);
                    if (textureId != IntPtr.Zero)
                    {
                        var iconMin = new Vector2(rowOrigin.X + CollapsedIconPadding,
                            rowOrigin.Y + CollapsedIconPadding);
                        var iconMax = new Vector2(
                            rowOrigin.X + CollapsedStripIconSize - CollapsedIconPadding,
                            rowOrigin.Y + CollapsedStripIconSize - CollapsedIconPadding);
                        var tint = ImGui.ColorConvertFloat4ToU32(item.IsDisabled
                            ? new Vector4(1f, 1f, 1f, 0.4f)
                            : new Vector4(1f, 1f, 1f, 1f));
                        drawList.AddImage(textureId, iconMin, iconMax,
                            Vector2.Zero, Vector2.One, tint);

                        if (item.IsActive)
                        {
                            var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
                            drawList.AddRect(
                                new Vector2(rowOrigin.X, rowOrigin.Y),
                                new Vector2(rowOrigin.X + CollapsedStripIconSize,
                                    rowOrigin.Y + CollapsedStripIconSize),
                                borderColor, 2f, ImDrawFlags.None, 1.5f);
                        }
                    }
                }

                if (isHovered)
                {
                    using var _ = ChromeRenderer.BeginStyledTooltip();
                    ImGui.TextUnformatted(item.Label);
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
