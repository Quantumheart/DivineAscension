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
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y + 8f;

        // Filter label
        TextRenderer.DrawLabel(drawList, "Filter by deity:", x, currentY, 14f);

        // Deity filter dropdown (uses DeityHelper names plus All)
        var deityNames = DeityHelper.DeityNames;
        var deities = new string[deityNames.Length + 1];
        deities[0] = "All";
        Array.Copy(deityNames, 0, deities, 1, deityNames.Length);

        var selectedIndex = 0;
        if (!string.IsNullOrEmpty(state.DeityFilter))
        {
            for (int i = 1; i < deities.Length; i++)
            {
                if (string.Equals(deities[i], state.DeityFilter, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        var dropdownX = x + 120f;
        var dropdownY = currentY - 6f;
        var dropdownW = 200f;
        var dropdownH = 30f;

        // Button and interactive menu
        if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, dropdownW, dropdownH, deities[selectedIndex], false))
        {
            // Open/close controlled internally by Dropdown for the one-shot menu draw below
        }

        // Show menu and handle selection in-place
        var (newIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(
            drawList, api, dropdownX, dropdownY, dropdownW, dropdownH, deities, selectedIndex, 34f);

        if (clickConsumed)
        {
            // Consume mouse
        }

        if (shouldClose && newIndex != selectedIndex)
        {
            state.DeityFilter = newIndex == 0 ? string.Empty : deities[newIndex];
            manager.RequestCivilizationList(state.DeityFilter);
        }

        // Refresh button
        if (ButtonRenderer.DrawButton(drawList, "Refresh", dropdownX + dropdownW + 12f, dropdownY, 100f, dropdownH))
        {
            manager.RequestCivilizationList(state.DeityFilter);
        }

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
            emptyText: "No civilizations found."
        );

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
        TextRenderer.DrawLabel(drawList, civ.Name, x + 12f, y + 8f, 15f, ColorPalette.White);

        // Members and diversity line
        var membersText = $"Members: {civ.MemberCount}";
        drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x + 12f, y + 32f),
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
            manager.RequestCivilizationInfo(civ.CivId);
            api.ShowChatMessage($"Requesting details for {civ.Name}...");
        }
    }
}
