using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Layout;
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
    private const float ItemHeight = 28f;
    private const float ItemIndent = 12f;
    private const float CollapsedStripIconSize = 24f;

    public static IReadOnlyList<SidebarEvent> Draw(UiRect rect, SidebarViewModel vm)
    {
        var events = new List<SidebarEvent>();
        if (rect.W <= 0f || rect.H <= 0f) return events;

        // Anchor a child window to the passed rect so widget cursor coords align.
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
        // Indent everything inside a group.
        ImGui.Indent(ItemIndent);

        var label = item.Badge > 0
            ? $"{item.Label}  ({item.Badge})"
            : item.Label;

        if (item.IsDisabled)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Grey);
            ImGui.Selectable($"{label}##da-sidebar-item-{item.Id}", false,
                ImGuiSelectableFlags.Disabled, new Vector2(0, ItemHeight));
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)
                && !string.IsNullOrEmpty(item.DisabledTooltipKey))
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(LocalizationService.Instance.Get(item.DisabledTooltipKey!));
                ImGui.EndTooltip();
            }
        }
        else
        {
            if (item.IsActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPalette.Darken(ColorPalette.Gold, 0.6f));
            }

            if (ImGui.Selectable($"{label}##da-sidebar-item-{item.Id}", item.IsActive,
                    ImGuiSelectableFlags.None, new Vector2(0, ItemHeight)))
            {
                events.Add(new SidebarEvent.ItemClicked(item.Id));
            }

            if (item.IsActive)
            {
                ImGui.PopStyleColor();
            }
        }

        ImGui.Unindent(ItemIndent);
    }

    private static void DrawCollapsedStrip(SidebarViewModel vm, List<SidebarEvent> events)
    {
        // 40px strip: one square per enabled item, icon-only with tooltip on hover.
        foreach (var group in vm.Groups)
        {
            foreach (var item in group.Items)
            {
                var size = new Vector2(CollapsedStripIconSize, CollapsedStripIconSize);
                if (ImGui.Selectable($"##da-strip-{item.Id}", item.IsActive,
                        item.IsDisabled
                            ? ImGuiSelectableFlags.Disabled
                            : ImGuiSelectableFlags.None,
                        size))
                {
                    if (!item.IsDisabled) events.Add(new SidebarEvent.ItemClicked(item.Id));
                }

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(item.Label);
                    if (item.IsDisabled && !string.IsNullOrEmpty(item.DisabledTooltipKey))
                    {
                        ImGui.Separator();
                        ImGui.TextUnformatted(LocalizationService.Instance.Get(item.DisabledTooltipKey!));
                    }
                    ImGui.EndTooltip();
                }
            }
        }
    }
}
