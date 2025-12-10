using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Civilization;
using PantheonWars.GUI.Models.Civilization.Browse;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationBrowseRenderer
{
    public static CivilizationBrowseRenderResult Draw(
        CivilizationBrowseViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var currentY = vm.Y + 8f;

        // Filter label
        TextRenderer.DrawLabel(drawList, "Filter by deity:", vm.X, currentY);

        var selectedIndex = vm.GetCurrentFilterIndex();

        var dropdownX = vm.X + 120f;
        var dropdownY = currentY - 6f;
        var dropdownW = 200f;
        var dropdownH = 30f;

        // Draw dropdown button
        if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, dropdownW, dropdownH,
                vm.DeityFilters[selectedIndex], vm.IsDeityDropDownOpen))
            events.Add(new BrowseEvent.DeityDropDownToggled(!vm.IsDeityDropDownOpen));

        // Refresh button
        if (ButtonRenderer.DrawButton(drawList, "Refresh", dropdownX + dropdownW + 12f, dropdownY, 100f, dropdownH,
                false, !vm.IsLoading))
            events.Add(new BrowseEvent.RefreshClicked());

        currentY += 40f;

        // Scrollable list of civilizations
        var civilizationsList = vm.Civilizations.ToList();
        var listHeight = vm.Height - (currentY - vm.Y);

        var newScrollY = ScrollableList.Draw(
            drawList,
            vm.X,
            currentY,
            vm.Width,
            listHeight,
            civilizationsList,
            90f,
            8f,
            vm.ScrollY,
            (civ, cx, cy, cw, ch) => DrawCivilizationCard(civ, cx, cy, cw, ch, drawList, events),
            "No civilizations found.",
            vm.IsLoading ? "Loading civilizations..." : null
        );

        // Emit scroll event if changed
        if (newScrollY != vm.ScrollY)
            events.Add(new BrowseEvent.ScrollChanged(newScrollY));

        // Draw dropdown menu AFTER the list so it appears on top (z-ordering)
        if (vm.IsDeityDropDownOpen)
        {
            // Draw menu visual
            Dropdown.DrawMenuVisual(drawList, dropdownX, dropdownY, dropdownW, dropdownH,
                vm.DeityFilters, selectedIndex, 34f);

            // Handle menu interaction
            var (newIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(dropdownX, dropdownY,
                dropdownW, dropdownH,
                vm.DeityFilters, selectedIndex, 34f);

            if (shouldClose)
            {
                events.Add(new BrowseEvent.DeityDropDownToggled(false));

                // Update filter if selection changed
                if (newIndex != selectedIndex)
                {
                    var newFilter = newIndex == 0 ? string.Empty : vm.DeityFilters[newIndex];
                    events.Add(new BrowseEvent.DeityFilterChanged(newFilter));
                }
            }
        }

        return new CivilizationBrowseRenderResult(events, vm.Height);
    }

    private static void DrawCivilizationCard(
        CivilizationListResponsePacket.CivilizationInfo civ,
        float x,
        float y,
        float width,
        float height,
        ImDrawListPtr drawList,
        List<BrowseEvent> events)
    {
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
            events.Add(new BrowseEvent.ViewDetailedsClicked(civ.CivId));
    }
}
