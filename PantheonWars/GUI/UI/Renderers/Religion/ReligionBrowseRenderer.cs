using System.Collections.Generic;
using ImGuiNET;
using PantheonWars.GUI.Events.Religion;
using PantheonWars.GUI.Models.Religion.Browse;
using PantheonWars.GUI.Models.Religion.List;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Renderers.Components;

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
    public static ReligionBrowseRenderResult Draw(
        ReligionBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
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
            events.Add(new BrowseEvent.DeityFilterChanged(newFilter));
        }

        currentY += tabHeight + 8f;

        // === RELIGION LIST ===
        var listHeight = height - (currentY - y) - 50f; // Reserve space for bottom buttons
        var listVm = new ReligionListViewModel(
            religions: viewModel.Religions,
            isLoading: viewModel.IsLoading,
            scrollY: viewModel.ScrollY,
            selectedReligionUID: viewModel.SelectedReligionUID,
            x: x,
            y: currentY,
            width: width,
            height: listHeight);

        var listResult = ReligionListRenderer.Draw(listVm, drawList);

        // Translate list events → browse events
        var updatedSelected = viewModel.SelectedReligionUID;
        var updatedScroll = viewModel.ScrollY;

        foreach (var le in listResult.Events)
        {
            switch (le)
            {
                case ListEvent.ScrollChanged sc:
                    updatedScroll = sc.NewScrollY;
                    events.Add(new BrowseEvent.ScrollChanged(updatedScroll));
                    break;
                case ListEvent.ItemClicked ic:
                    updatedSelected = ic.ReligionUID;
                    updatedScroll = ic.NewScrollY;
                    events.Add(new BrowseEvent.Selected(updatedSelected, updatedScroll));
                    break;
            }
        }

        var hoveredReligion = listResult.HoveredReligion;

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
                events.Add(new BrowseEvent.CreateClicked());
            }

            // Join Religion
            var joinButtonX = buttonsStartX + buttonWidth + buttonSpacing;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX,
                    buttonY, buttonWidth, buttonHeight, false, canJoin))
            {
                if (canJoin && updatedSelected != null)
                {
                    events.Add(new BrowseEvent.JoinClicked(updatedSelected));
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
                    events.Add(new BrowseEvent.JoinClicked(updatedSelected));
                }
            }
        }

        return new ReligionBrowseRenderResult(events, hoveredReligion, height);
    }
}