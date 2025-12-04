using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using PantheonWars.Models;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Renders the split-panel blessing tree view
///     Left panel: Player blessings (50% width)
///     Right panel: Religion blessings (50% width)
///     Both panels are independently scrollable
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingTreeRenderer
{
    // Color constants
    private static readonly Vector4 ColorDivider = new(0.573f, 0.502f, 0.416f, 1.0f); // #92806a
    private static readonly Vector4 ColorLabel = new(0.996f, 0.682f, 0.204f, 1.0f); // #feae34 gold

    /// <summary>
    ///     Draw the split-panel blessing tree
    /// </summary>
    /// <param name="manager">Blessing dialog state manager</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Total available width</param>
    /// <param name="height">Total available height</param>
    /// <param name="deltaTime">Time elapsed since last frame (for animations)</param>
    /// <param name="hoveringBlessingId">Output: ID of currently hovered blessing</param>
    /// <returns>Height used by this renderer</returns>
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height,
        float deltaTime,
        ref string? hoveringBlessingId)
    {
        const float labelHeight = 30f;
        const float dividerWidth = 2f;

        // Calculate panel dimensions
        var panelWidth = (width - dividerWidth) / 2f;
        var treeAreaHeight = height - labelHeight;

        var drawList = ImGui.GetWindowDrawList();

        // === LEFT PANEL: Player Blessings ===
        var leftX = x;
        var leftY = y;

        // Draw label
        DrawPanelLabel(drawList, "Player Blessings", leftX, leftY, panelWidth);

        // Draw tree area (use local variables for scroll offsets)
        var treeY = leftY + labelHeight;
        var playerScrollX = manager.PlayerTreeScrollX;
        var playerScrollY = manager.PlayerTreeScrollY;
        DrawTreePanel(
            manager, api, drawList,
            leftX, treeY, panelWidth, treeAreaHeight,
            manager.PlayerBlessingStates,
            deltaTime,
            ref playerScrollX,
            ref playerScrollY,
            ref hoveringBlessingId
        );
        manager.PlayerTreeScrollX = playerScrollX;
        manager.PlayerTreeScrollY = playerScrollY;

        // === CENTER DIVIDER ===
        var dividerX = x + panelWidth;
        var dividerTop = new Vector2(dividerX, y);
        var dividerBottom = new Vector2(dividerX, y + height);
        var dividerColor = ImGui.ColorConvertFloat4ToU32(ColorDivider);
        drawList.AddLine(dividerTop, dividerBottom, dividerColor, dividerWidth);

        // === RIGHT PANEL: Religion Blessings ===
        var rightX = dividerX + dividerWidth;
        var rightY = y;

        // Draw label
        DrawPanelLabel(drawList, "Religion Blessings", rightX, rightY, panelWidth);

        // Draw tree area (use local variables for scroll offsets)
        var religionScrollX = manager.ReligionTreeScrollX;
        var religionScrollY = manager.ReligionTreeScrollY;
        DrawTreePanel(
            manager, api, drawList,
            rightX, treeY, panelWidth, treeAreaHeight,
            manager.ReligionBlessingStates,
            deltaTime,
            ref religionScrollX,
            ref religionScrollY,
            ref hoveringBlessingId
        );
        manager.ReligionTreeScrollX = religionScrollX;
        manager.ReligionTreeScrollY = religionScrollY;

        return height;
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
    private static void DrawTreePanel(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        ImDrawListPtr drawList,
        float x, float y, float width, float height,
        Dictionary<string, BlessingNodeState> blessingStates,
        float deltaTime,
        ref float scrollX, ref float scrollY,
        ref string? hoveringBlessingId)
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
            return;
        }

        // Calculate layout if not already done
        if (blessingStates.Values.First().PositionX == 0 && blessingStates.Values.First().PositionY == 0)
            BlessingTreeLayout.CalculateLayout(blessingStates, width - padding * 2);

        // Get total tree dimensions
        var totalHeight = BlessingTreeLayout.GetTotalHeight(blessingStates);
        var totalWidth = BlessingTreeLayout.GetTotalWidth(blessingStates);

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
        scrollX = ImGui.GetScrollX();
        scrollY = ImGui.GetScrollY();

        // Calculate drawing offset (accounts for scroll)
        var drawOffsetX = childWindowPos.X + padding - scrollX;
        var drawOffsetY = childWindowPos.Y + padding - scrollY;

        // Draw connection lines first (behind nodes)
        foreach (var state in blessingStates.Values)
            if (state.Blessing.PrerequisiteBlessings != null && state.Blessing.PrerequisiteBlessings.Count > 0)
                foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
                    if (blessingStates.TryGetValue(prereqId, out var prereqState))
                        BlessingNodeRenderer.DrawConnectionLine(
                            prereqState, state,
                            drawOffsetX, drawOffsetY
                        );

        // Draw blessing nodes
        foreach (var state in blessingStates.Values)
        {
            var isSelected = manager.SelectedBlessingId == state.Blessing.BlessingId;
            var isHovering = BlessingNodeRenderer.DrawNode(
                state, api,
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
                    api.Logger.Notification($"[PantheonWars] Clicked blessing: {state.Blessing.Name}");
                    manager.SelectBlessing(state.Blessing.BlessingId);
                    api.Logger.Notification($"[PantheonWars] Selected blessing ID: {manager.SelectedBlessingId}");

                    // Play click sound
                    api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                }
            }
        }

        // Set child window content size for scrolling
        // We need to use Dummy to actually reserve the space since we're using DrawList
        ImGui.Dummy(new Vector2(totalWidth + padding * 2, totalHeight + padding * 2));

        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
    }
}