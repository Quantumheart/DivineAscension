using System;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationBrowseRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivState;

        // If viewing a specific civilization's details, show detail view instead
        if (state.ViewingCivilizationId != null)
            return CivilizationDetailViewRenderer.Draw(manager, api, x, y, width, height);

        var drawList = ImGui.GetWindowDrawList();
        var currentY = y + 8f;

        // Filter label
        TextRenderer.DrawLabel(drawList, "Filter by deity:", x, currentY);

        // Deity filter dropdown (uses DeityHelper names plus All)
        var deityNames = DeityHelper.DeityNames;
        var deities = new string[deityNames.Length + 1];
        deities[0] = "All";
        Array.Copy(deityNames, 0, deities, 1, deityNames.Length);

        var selectedIndex = 0;
        if (!string.IsNullOrEmpty(state.DeityFilter))
            for (var i = 1; i < deities.Length; i++)
                if (string.Equals(deities[i], state.DeityFilter, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }

        var dropdownX = x + 120f;
        var dropdownY = currentY - 6f;
        var dropdownW = 200f;
        var dropdownH = 30f;

        // Draw dropdown button
        if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, dropdownW, dropdownH, deities[selectedIndex],
                state.IsDeityFilterOpen))
            // Toggle dropdown open/close
            state.IsDeityFilterOpen = !state.IsDeityFilterOpen;

        // Refresh button
        if (ButtonRenderer.DrawButton(drawList, "Refresh", dropdownX + dropdownW + 12f, dropdownY, 100f, dropdownH,
                false, !state.IsBrowseLoading))
            manager.RequestCivilizationList(state.DeityFilter);

        currentY += 40f;

        // Scrollable list of civilizations
        state.BrowseScrollY = ScrollableList.Draw(
            drawList,
            x,
            currentY,
            width,
            height - (currentY - y),
            state.AllCivilizations,
            90f,
            8f,
            state.BrowseScrollY,
            (civ, cx, cy, cw, ch) => DrawCivilizationCard(civ, cx, cy, cw, ch, manager, api),
            "No civilizations found.",
            state.IsBrowseLoading ? "Loading civilizations..." : null
        );

        // Draw dropdown menu AFTER the list so it appears on top (z-ordering)
        if (state.IsDeityFilterOpen)
        {
            // Draw menu visual
            Dropdown.DrawMenuVisual(drawList, dropdownX, dropdownY, dropdownW, dropdownH, deities, selectedIndex, 34f);

            // Handle menu interaction
            var (newIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(
                drawList, api, dropdownX, dropdownY, dropdownW, dropdownH, deities, selectedIndex, 34f);

            if (shouldClose)
            {
                state.IsDeityFilterOpen = false;

                // Update filter if selection changed
                if (newIndex != selectedIndex)
                {
                    state.DeityFilter = newIndex == 0 ? string.Empty : deities[newIndex];
                    manager.RequestCivilizationList(state.DeityFilter);
                }
            }
        }

        return height;
    }

    private static void DrawCivilizationCard(
        CivilizationListResponsePacket.CivilizationInfo civ,
        float x,
        float y,
        float width,
        float height,
        BlessingDialogManager manager,
        ICoreClientAPI api)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Card background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        // Civ name
        TextRenderer.DrawLabel(drawList, civ.Name, x + 12f, y + 8f, 16f, ColorPalette.White);

        // Members and diversity line
        var membersText = $"Members: {civ.MemberCount}";
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 12f, y + 32f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), membersText);

        // Deity indicators for member deities
        var iconX = x + 12f;
        var iconY = y + 52f;
        foreach (var deityName in civ.MemberDeities)
        {
            var color = DeityHelper.GetDeityColor(deityName);
            drawList.AddCircleFilled(new Vector2(iconX, iconY), 6f, ImGui.ColorConvertFloat4ToU32(color));
            iconX += 16f;
        }

        // View details button
        if (ButtonRenderer.DrawButton(drawList, "View Details", x + width - 130f, y + height - 36f, 120f, 28f, true))
        {
            // Set viewing state and request details
            manager.CivState.ViewingCivilizationId = civ.CivId;
            manager.RequestCivilizationInfo(civ.CivId);
        }
    }
}