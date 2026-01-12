using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Browse;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

internal static class CivilizationBrowseRenderer
{
    public static CivilizationBrowseRenderResult Draw(
        CivilizationBrowseViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var currentY = vm.Y + 8f;

        // Filter label
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_FILTER), vm.X, currentY);

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
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_REFRESH),
                dropdownX + dropdownW + 12f, dropdownY, 100f, dropdownH,
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
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_NO_CIVS),
            vm.IsLoading ? LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_LOADING) : null
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

        // Civilization icon
        const float iconSize = 28f;
        var iconTextureId = CivilizationIconLoader.GetIconTextureId(civ.Icon);

        if (iconTextureId != IntPtr.Zero)
        {
            var iconMin = new Vector2(x + 12f, y + 8f);
            var iconMax = new Vector2(x + 12f + iconSize, y + 8f + iconSize);
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);

            // Icon border
            var iconBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
            drawList.AddRect(iconMin, iconMax, iconBorderColor, 3f, ImDrawFlags.None, 1f);
        }

        // Civ name (positioned next to icon)
        TextRenderer.DrawLabel(drawList, civ.Name, x + 12f + iconSize + 8f, y + 10f, 16f, ColorPalette.White);

        // Members and diversity line
        var membersText =
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_MEMBERS_LABEL, civ.MemberCount);
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + 12f, y + 32f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), membersText);

        // Deity icons for member deities
        const float deityIconSize = 12f;
        var iconX = x + 12f;
        var iconY = y + 52f - deityIconSize / 2f;
        foreach (var deityName in civ.MemberDeities)
        {
            if (Enum.TryParse<DeityDomain>(deityName, out var deityType))
            {
                var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
                drawList.AddImage(deityTextureId,
                    new Vector2(iconX, iconY),
                    new Vector2(iconX + deityIconSize, iconY + deityIconSize),
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            }

            iconX += 16f;
        }

        // View details button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_VIEW_DETAILS),
                x + width - 130f, y + height - 36f, 120f, 28f, true))
            events.Add(new BrowseEvent.ViewDetailedsClicked(civ.CivId));
    }
}