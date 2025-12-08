using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Models.Blessing.Tree;
using PantheonWars.Models;

namespace PantheonWars.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders the split-panel blessing tree view
///     Left panel: Player blessings (50% width)
///     Right panel: Religion blessings (50% width)
///     Both panels are independently scrollable
/// </summary>
internal static class BlessingTreeRenderer
{
    // Color constants
    private static readonly Vector4 ColorDivider = new(0.573f, 0.502f, 0.416f, 1.0f); // #92806a
    private static readonly Vector4 ColorLabel = new(0.996f, 0.682f, 0.204f, 1.0f); // #feae34 gold

    /// <summary>
    ///     Draw the split-panel blessing tree using EDA: returns events instead of mutating state.
    /// </summary>
    public static BlessingTreeRendererResult Draw(BlessingTreeViewModel vm)
    {
        const float labelHeight = 30f;
        const float dividerWidth = 2f;

        // Calculate panel dimensions
        var panelWidth = (vm.Width - dividerWidth) / 2f;
        var treeAreaHeight = vm.Height - labelHeight;

        var drawList = ImGui.GetWindowDrawList();

        // === LEFT PANEL: Player Blessings ===
        var leftX = vm.X;
        var leftY = vm.Y;

        // Draw label
        DrawPanelLabel(drawList, "Player Blessings", leftX, leftY, panelWidth);

        // Draw tree area (use local variables for scroll offsets)
        var treeY = leftY + labelHeight;
        var leftPanel = DrawTreePanel(drawList,
            leftX, treeY, panelWidth, treeAreaHeight,
            vm.PlayerBlessingStates,
            vm.DeltaTime,
            vm.PlayerTreeScroll.X,
            vm.PlayerTreeScroll.Y,
            vm.SelectedBlessingId
        );

        // === CENTER DIVIDER ===
        var dividerX = vm.X + panelWidth;
        var dividerTop = new Vector2(dividerX, vm.Y);
        var dividerBottom = new Vector2(dividerX, vm.Y + vm.Height);
        var dividerColor = ImGui.ColorConvertFloat4ToU32(ColorDivider);
        drawList.AddLine(dividerTop, dividerBottom, dividerColor, dividerWidth);

        // === RIGHT PANEL: Religion Blessings ===
        var rightX = dividerX + dividerWidth;
        var rightY = vm.Y;

        // Draw label
        DrawPanelLabel(drawList, "Religion Blessings", rightX, rightY, panelWidth);

        // Draw tree area (use local variables for scroll offsets)
        var rightPanel = DrawTreePanel(drawList,
            rightX, treeY, panelWidth, treeAreaHeight,
            vm.ReligionBlessingStates,
            vm.DeltaTime,
            vm.ReligionTreeScroll.X,
            vm.ReligionTreeScroll.Y,
            vm.SelectedBlessingId
        );

        // Build events
        var events = new List<BlessingTreeEvent>(4);

        // Hover
        var hovering = leftPanel.HoveringBlessingId ?? rightPanel.HoveringBlessingId;
        events.Add(new BlessingTreeEvent.BlessingHovered(hovering));

        // Selection
        var clicked = leftPanel.ClickedBlessingId ?? rightPanel.ClickedBlessingId;
        if (!string.IsNullOrEmpty(clicked))
            events.Add(new BlessingTreeEvent.BlessingSelected(clicked!));

        // Scroll changes
        if (!NearlyEqual(vm.PlayerTreeScroll.X, leftPanel.ScrollX) ||
            !NearlyEqual(vm.PlayerTreeScroll.Y, leftPanel.ScrollY))
            events.Add(new BlessingTreeEvent.PlayerTreeScrollChanged(leftPanel.ScrollX, leftPanel.ScrollY));
        if (!NearlyEqual(vm.ReligionTreeScroll.X, rightPanel.ScrollX) ||
            !NearlyEqual(vm.ReligionTreeScroll.Y, rightPanel.ScrollY))
            events.Add(new BlessingTreeEvent.ReligionTreeScrollChanged(rightPanel.ScrollX, rightPanel.ScrollY));

        return new BlessingTreeRendererResult(events, vm.Height);
    }

    /// <summary>
    ///     Draw panel label header
    /// </summary>
    private static void DrawPanelLabel(ImDrawListPtr drawList, string label, float x, float y, float width)
    {
        const float labelHeight = 30f;

        // Draw background
        var bgStart = new Vector2(x, y);
        var bgEnd = new Vector2(x + width, y + labelHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.12f, 0.09f, 0.8f));
        drawList.AddRectFilled(bgStart, bgEnd, bgColor);

        // Draw text (centered)
        var textSize = ImGui.CalcTextSize(label);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (labelHeight - textSize.Y) / 2
        );
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorLabel);
        drawList.AddText(ImGui.GetFont(), 16f, textPos, textColor, label);
    }

    /// <summary>
    ///     Draw a single tree panel with scrolling
    /// </summary>
    private static (float ScrollX, float ScrollY, string? HoveringBlessingId, string? ClickedBlessingId) DrawTreePanel(
        ImDrawListPtr drawList,
        float x, float y, float width, float height,
        IReadOnlyDictionary<string, BlessingNodeState> blessingStates,
        float deltaTime,
        float prevScrollX, float prevScrollY,
        string? selectedBlessingId)
    {
        const float padding = 16f;

        // If no blessings, show placeholder
        if (blessingStates.Count == 0)
        {
            var emptyText = "No blessings available";
            var textSize = ImGui.CalcTextSize(emptyText);
            var textPos = new Vector2(
                x + (width - textSize.X) / 2,
                y + (height - textSize.Y) / 2
            );
            var textColor = ImGui.ColorConvertFloat4ToU32(ColorDivider);
            drawList.AddText(textPos, textColor, emptyText);
            return (prevScrollX, prevScrollY, null, null);
        }

        // Calculate layout if not already done
        if (blessingStates.Values.First().PositionX == 0 && blessingStates.Values.First().PositionY == 0)
            BlessingTreeLayout.CalculateLayout(blessingStates.ToDictionary(k => k.Key, v => v.Value),
                width - padding * 2);

        // Get total tree dimensions
        var dict = blessingStates.ToDictionary(k => k.Key, v => v.Value);
        var totalHeight = BlessingTreeLayout.GetTotalHeight(dict);
        var totalWidth = BlessingTreeLayout.GetTotalWidth(dict);

        // Create scrollable area using ImGui child window
        var childId = blessingStates.GetHashCode().ToString(); // Unique ID per panel
        ImGui.SetCursorScreenPos(new Vector2(x, y));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding, padding));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.08f, 0.06f, 0.5f));

        // Enable scrolling with mouse wheel - border=true enables scrollbar
        ImGui.BeginChild(childId, new Vector2(width, height), true,
            ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

        // Get mouse position (in screen space)
        var mousePos = ImGui.GetMousePos();
        var childWindowPos = ImGui.GetWindowPos(); // Position of child window in screen space

        // Get scroll position
        var scrollX = ImGui.GetScrollX();
        var scrollY = ImGui.GetScrollY();

        // Calculate drawing offset (accounts for scroll)
        var drawOffsetX = childWindowPos.X + padding - scrollX;
        var drawOffsetY = childWindowPos.Y + padding - scrollY;

        // Draw connection lines first (behind nodes)
        foreach (var state in blessingStates.Values)
            if (state.Blessing.PrerequisiteBlessings is { Count: > 0 })
                foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
                    if (blessingStates.TryGetValue(prereqId, out var prereqState))
                        BlessingNodeRenderer.DrawConnectionLine(
                            prereqState, state,
                            drawOffsetX, drawOffsetY
                        );

        // Draw blessing nodes
        string? hoveringBlessingId = null;
        string? clickedBlessingId = null;
        foreach (var state in blessingStates.Values)
        {
            var isSelected = selectedBlessingId == state.Blessing.BlessingId;
            var isHovering = BlessingNodeRenderer.DrawNode(
                state,
                drawOffsetX, drawOffsetY,
                mousePos.X, mousePos.Y, // Pass screen-space mouse coordinates
                deltaTime,
                isSelected
            );

            // Handle hover
            if (isHovering)
            {
                hoveringBlessingId = state.Blessing.BlessingId;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                // Handle click
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    clickedBlessingId = state.Blessing.BlessingId;
                }
            }
        }

        // Set child window content size for scrolling
        // We need to use Dummy to actually reserve the space since we're using DrawList
        ImGui.Dummy(new Vector2(totalWidth + padding * 2, totalHeight + padding * 2));

        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        return (scrollX, scrollY, hoveringBlessingId, clickedBlessingId);
    }

    private static bool NearlyEqual(float a, float b)
    {
        return Math.Abs(a - b) < 0.5f; // small threshold to avoid spamming events
    }
}