using System;
using System.Collections.Generic;
using ImGuiNET;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Models.Religion.Browse;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Renderers.Components;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for browsing and joining religions
///     Migrates functionality from ReligionBrowserOverlay
/// </summary>
internal static class ReligionBrowseRenderer
{
    /// <summary>
    /// Pure renderer: builds visuals from the view model and emits UI events. No state or side effects.
    /// </summary>
    public static RenderResult Draw(
        ReligionBrowseViewModel viewModel,
        ImDrawListPtr drawList,
        ICoreClientAPI api)
    {
        var events = new List<ReligionBrowseEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var currentY = y;

        // === DEITY FILTER TABS ===
        const float tabHeight = 32f;

        var currentSelectedIndex = viewModel.GetCurrentFilterIndex();
        if (currentSelectedIndex == -1) currentSelectedIndex = 0; // Default to "All"

        var newSelectedIndex = TabControl.Draw(
            drawList,
            x,
            currentY,
            width,
            tabHeight,
            viewModel.DeityFilters,
            currentSelectedIndex);

        if (newSelectedIndex != currentSelectedIndex)
        {
            var newFilter = viewModel.DeityFilters[newSelectedIndex];
            events.Add(new ReligionBrowseEvent.DeityFilterChanged(newFilter));
        }

        currentY += tabHeight + 8f;

        // === RELIGION LIST ===
        var listHeight = height - (currentY - y) - 50f; // Reserve space for bottom buttons
        var listResult = ReligionListRenderer.Draw(
            drawList, api, x, currentY, width, listHeight,
            [..viewModel.Religions],
            viewModel.IsLoading,
            viewModel.ScrollY,
            viewModel.SelectedReligionUID);

        // Emit selection and/or scroll events as needed
        var updatedScroll = listResult.scrollY;
        var updatedSelected = listResult.selectedUID;
        var hoveredReligion = listResult.hoveredReligion;

        if (Math.Abs(updatedScroll - viewModel.ScrollY) > 0.1)
        {
            events.Add(new ReligionBrowseEvent.ScrollChanged(updatedScroll));
        }

        if (updatedSelected != viewModel.SelectedReligionUID)
        {
            events.Add(new ReligionBrowseEvent.ReligionSelected(updatedSelected, updatedScroll));
        }

        currentY += listHeight + 10f;

        // === ACTION BUTTONS ===
        const float buttonWidth = 180f;
        const float buttonHeight = 36f;
        const float buttonSpacing = 12f;
        var buttonY = currentY;
        var canJoin = viewModel.CanJoinReligion;
        var userHasReligion = viewModel.UserHasReligion;

        if (!userHasReligion)
        {
            var totalButtonWidth = buttonWidth * 2 + buttonSpacing;
            var buttonsStartX = x + (width - totalButtonWidth) / 2;

            // Create Religion
            var createButtonX = buttonsStartX;
            if (ButtonRenderer.DrawButton(drawList, "Create Religion", createButtonX, buttonY, buttonWidth,
                    buttonHeight, true))
            {
                events.Add(new ReligionBrowseEvent.CreateReligionClicked());
            }

            // Join Religion
            var joinButtonX = buttonsStartX + buttonWidth + buttonSpacing;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX,
                    buttonY, buttonWidth, buttonHeight, false, canJoin))
            {
                if (canJoin && updatedSelected != null)
                {
                    events.Add(new ReligionBrowseEvent.JoinReligionClicked(updatedSelected));
                }
            }
        }
        else
        {
            // Only Join button (centered)
            var joinButtonX = x + (width - buttonWidth) / 2;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX,
                    buttonY, buttonWidth, buttonHeight, false, canJoin))
            {
                if (canJoin && updatedSelected != null)
                {
                    events.Add(new ReligionBrowseEvent.JoinReligionClicked(updatedSelected));
                }
            }
        }

        return new RenderResult(events, hoveredReligion, height);
    }
}