using System;
using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for browsing and joining religions
///     Migrates functionality from ReligionBrowserOverlay
/// </summary>
internal static class ReligionBrowseRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.ReligionStateManager.State;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        // === DEITY FILTER TABS ===
        var deityFilters = new[] { "All", "Khoras", "Lysa", "Aethra", "Gaia" };
        const float tabHeight = 32f;

        // Find current selected index
        var currentSelectedIndex = Array.IndexOf(deityFilters, state.DeityFilter == "" ? "All" : state.DeityFilter);
        if (currentSelectedIndex == -1) currentSelectedIndex = 0; // Default to "All"

        // Draw tabs using TabControl component
        var newSelectedIndex = TabControl.Draw(
            drawList,
            x,
            currentY,
            width,
            tabHeight,
            deityFilters,
            currentSelectedIndex);

        // Handle selection change
        if (newSelectedIndex != currentSelectedIndex)
        {
            var newFilter = deityFilters[newSelectedIndex];
            state.DeityFilter = newFilter == "All" ? "" : newFilter;
            state.SelectedReligionUID = null;
            state.BrowseScrollY = 0f;

            // Request refresh with new filter
            manager.ReligionStateManager.RequestReligionList(state.DeityFilter);

            api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
        }

        currentY += tabHeight + 8f;

        // === RELIGION LIST ===
        var listHeight = height - (currentY - y) - 50f; // Reserve space for bottom buttons
        ReligionListResponsePacket.ReligionInfo? hoveredReligion;
        (state.BrowseScrollY, state.SelectedReligionUID, hoveredReligion) = ReligionListRenderer.Draw(
            drawList, api, x, currentY, width, listHeight,
            state.AllReligions, state.IsBrowseLoading, state.BrowseScrollY, state.SelectedReligionUID);

        currentY += listHeight + 10f;

        // === ACTION BUTTONS ===
        const float buttonWidth = 180f;
        const float buttonHeight = 36f;
        const float buttonSpacing = 12f;
        var buttonY = currentY;
        var canJoin = !string.IsNullOrEmpty(state.SelectedReligionUID);
        var userHasReligion = manager.HasReligion();

        // Only show Create button if user doesn't have a religion
        if (!userHasReligion)
        {
            // Show both Create and Join buttons
            var totalButtonWidth = buttonWidth * 2 + buttonSpacing;
            var buttonsStartX = x + (width - totalButtonWidth) / 2;

            // Create Religion button
            var createButtonX = buttonsStartX;
            if (ButtonRenderer.DrawButton(drawList, "Create Religion", createButtonX, buttonY, buttonWidth,
                    buttonHeight, true))
            {
                api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                // Switch to Create tab
                state.CurrentSubTab = ReligionSubTab.Create;
            }

            // Join Religion button
            var joinButtonX = buttonsStartX + buttonWidth + buttonSpacing;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX,
                    buttonY, buttonWidth, buttonHeight, false, canJoin))
            {
                if (canJoin)
                {
                    api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    manager.ReligionStateManager.RequestReligionAction("join", state.SelectedReligionUID!);
                }
                else
                {
                    api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                        api.World.Player.Entity, null, false, 8f, 0.3f);
                }
            }
        }
        else
        {
            // User has religion - only show centered Join button (for switching religions)
            var joinButtonX = x + (width - buttonWidth) / 2;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX,
                    buttonY, buttonWidth, buttonHeight, false, canJoin))
            {
                if (canJoin)
                {
                    api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    manager.ReligionStateManager.RequestReligionAction("join", state.SelectedReligionUID!);
                }
                else
                {
                    api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                        api.World.Player.Entity, null, false, 8f, 0.3f);
                }
            }
        }

        // === TOOLTIP ===
        // Draw tooltip last so it appears on top of everything
        if (hoveredReligion != null)
        {
            var mousePos = ImGui.GetMousePos();
            ReligionListRenderer.DrawTooltip(hoveredReligion, mousePos.X, mousePos.Y, width, height);
        }

        return height;
    }
}