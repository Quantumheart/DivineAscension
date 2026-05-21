using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Tree;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders a single blessing tree panel. The renderer is kind-agnostic — III.ii Blessings
///     hosts it for the personal tree and I.iii Vows of the Order hosts it for the communal
///     tree. Each host passes the appropriate states dictionary, scroll state, and balance
///     header text. The renderer emits the kind-neutral
///     <see cref="TreeEvent.ScrollChanged"/> event when the panel scrolls; hosts translate
///     that into <see cref="TreeEvent.PlayerTreeScrollChanged"/> or
///     <see cref="TreeEvent.ReligionTreeScrollChanged"/> before forwarding to the state
///     manager so scroll position persists per page.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingTreeRenderer
{
    private static readonly Vector4 ColorDivider = new(0.573f, 0.502f, 0.416f, 1.0f); // #92806a
    private static readonly Vector4 ColorLabel = new(0.996f, 0.682f, 0.204f, 1.0f); // #feae34 gold

    public static BlessingTreeRendererResult Draw(BlessingTreeViewModel vm)
    {
        const float labelHeight = 30f;
        var treeAreaHeight = vm.ShowBalanceHeader ? vm.Height - labelHeight : vm.Height;

        var drawList = ImGui.GetWindowDrawList();

        if (vm.ShowBalanceHeader)
        {
            DrawPanelLabel(drawList, vm.PanelLabel, vm.X, vm.Y, vm.Width, vm.BalanceText);
        }

        var panel = DrawTreePanel(drawList,
            vm.X, vm.ShowBalanceHeader ? vm.Y + labelHeight : vm.Y, vm.Width, treeAreaHeight,
            vm.BlessingStates,
            vm.DeltaTime,
            vm.TreeScroll.X,
            vm.TreeScroll.Y,
            vm.SelectedBlessingId,
            vm.PanelId
        );

        var events = new List<TreeEvent>(3)
        {
            new TreeEvent.Hovered(panel.HoveringBlessingId)
        };

        if (!string.IsNullOrEmpty(panel.ClickedBlessingId))
            events.Add(new TreeEvent.Selected(panel.ClickedBlessingId!));

        if (!NearlyEqual(vm.TreeScroll.X, panel.ScrollX) ||
            !NearlyEqual(vm.TreeScroll.Y, panel.ScrollY))
        {
            events.Add(new TreeEvent.ScrollChanged(panel.ScrollX, panel.ScrollY));
        }

        return new BlessingTreeRendererResult(events, vm.Height);
    }

    private static void DrawPanelLabel(ImDrawListPtr drawList, string label, float x, float y, float width,
        string? currencyText = null)
    {
        const float labelHeight = 30f;
        const float padding = 8f;

        var bgStart = new Vector2(x, y);
        var bgEnd = new Vector2(x + width, y + labelHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.12f, 0.09f, 0.8f));
        drawList.AddRectFilled(bgStart, bgEnd, bgColor);

        var textColor = ImGui.ColorConvertFloat4ToU32(ColorLabel);

        if (string.IsNullOrEmpty(currencyText))
        {
            var textSize = ImGui.CalcTextSize(label);
            var textPos = new Vector2(
                x + (width - textSize.X) / 2,
                y + (labelHeight - textSize.Y) / 2
            );
            drawList.AddText(ImGui.GetFont(), TableHeader, textPos, textColor, label);
        }
        else
        {
            var labelSize = ImGui.CalcTextSize(label);
            var labelPos = new Vector2(
                x + padding,
                y + (labelHeight - labelSize.Y) / 2
            );
            drawList.AddText(ImGui.GetFont(), TableHeader, labelPos, textColor, label);

            var currencySize = ImGui.CalcTextSize(currencyText);
            var currencyPos = new Vector2(
                x + width - currencySize.X - padding,
                y + (labelHeight - currencySize.Y) / 2
            );
            drawList.AddText(ImGui.GetFont(), TableHeader, currencyPos, textColor, currencyText);
        }
    }

    private static (float ScrollX, float ScrollY, string? HoveringBlessingId, string? ClickedBlessingId) DrawTreePanel(
        ImDrawListPtr drawList,
        float x, float y, float width, float height,
        IReadOnlyDictionary<string, BlessingNodeState> blessingStates,
        float deltaTime,
        float prevScrollX, float prevScrollY,
        string? selectedBlessingId,
        string panelId)
    {
        const float padding = 16f;

        if (blessingStates.Count == 0)
        {
            var emptyText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_TREE_NO_BLESSINGS);
            var textSize = ImGui.CalcTextSize(emptyText);
            var textPos = new Vector2(
                x + (width - textSize.X) / 2,
                y + (height - textSize.Y) / 2
            );
            var textColor = ImGui.ColorConvertFloat4ToU32(ColorDivider);
            drawList.AddText(textPos, textColor, emptyText);
            return (prevScrollX, prevScrollY, null, null);
        }

        if (blessingStates.Values.First().PositionX == 0 && blessingStates.Values.First().PositionY == 0)
            BlessingTreeLayout.CalculateLayout(blessingStates.ToDictionary(k => k.Key, v => v.Value),
                width - padding * 2);

        var dict = blessingStates.ToDictionary(k => k.Key, v => v.Value);
        var totalHeight = BlessingTreeLayout.GetTotalHeight(dict);
        var totalWidth = BlessingTreeLayout.GetTotalWidth(dict);

        ImGui.SetCursorScreenPos(new Vector2(x, y));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding, padding));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.08f, 0.06f, 0.5f));

        ImGui.BeginChild(panelId, new Vector2(width, height), true,
            ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

        ImGui.Dummy(new Vector2(totalWidth + padding * 2, totalHeight + padding * 2));

        var mousePos = ImGui.GetMousePos();
        var childWindowPos = ImGui.GetWindowPos();

        var scrollX = ImGui.GetScrollX();
        var scrollY = ImGui.GetScrollY();

        var drawOffsetX = childWindowPos.X + padding - scrollX;
        var drawOffsetY = childWindowPos.Y + padding - scrollY;

        foreach (var state in blessingStates.Values)
            if (state.Blessing.PrerequisiteBlessings is { Count: > 0 })
                foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
                    if (blessingStates.TryGetValue(prereqId, out var prereqState))
                        BlessingNodeRenderer.DrawConnectionLine(
                            prereqState, state,
                            drawOffsetX, drawOffsetY
                        );

        string? hoveringBlessingId = null;
        string? clickedBlessingId = null;
        foreach (var state in blessingStates.Values)
        {
            var isSelected = selectedBlessingId == state.Blessing.BlessingId;
            var isHovering = BlessingNodeRenderer.DrawNode(
                state,
                drawOffsetX, drawOffsetY,
                mousePos.X, mousePos.Y,
                deltaTime,
                isSelected
            );

            if (isHovering)
            {
                hoveringBlessingId = state.Blessing.BlessingId;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    clickedBlessingId = state.Blessing.BlessingId;
                }
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        return (scrollX, scrollY, hoveringBlessingId, clickedBlessingId);
    }

    private static bool NearlyEqual(float a, float b)
    {
        return Math.Abs(a - b) < 0.5f;
    }
}
