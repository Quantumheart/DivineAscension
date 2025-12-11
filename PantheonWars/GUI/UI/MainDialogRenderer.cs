using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using PantheonWars.GUI.Models.Religion.Header;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Renderers.Blessing;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.UI;

/// <summary>
///     Central coordinator that orchestrates all blessing UI renderers
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MainDialogRenderer
{
    private static readonly string[] MainTabNames =
        [nameof(MainDialogTab.Religion), nameof(MainDialogTab.Blessings), nameof(MainDialogTab.Civilization)];

    /// <summary>
    ///     Draw the complete blessing UI
    /// </summary>
    /// <param name="manager">Blessing dialog state manager</param>
    /// <param name="state">Dialog state</param>
    /// <param name="windowWidth">Total window width</param>
    /// <param name="windowHeight">Total window height</param>
    /// <param name="deltaTime">Time elapsed since last frame (for animations)</param>
    public static void Draw(
        GuiDialogManager manager,
        GuiDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime)
    {
        const float padding = 16f;
        const float tabHeight = 36f;

        // Get window position for screen-space drawing
        var windowPos = ImGui.GetWindowPos();

        var x = padding;
        var y = padding;
        var width = windowWidth - padding * 2;

        // === 1. RELIGION HEADER (Top Banner, always visible) ===
        // Top-level religion action buttons have been removed; only pass civilization callback
        ReligionHeaderViewModel religionHeaderViewModel = new(manager.HasReligion(), manager.HasCivilization(),
            manager.CivilizationManager.CurrentCivilizationName,
            manager.CivilizationManager.CivilizationMemberReligions ??
            new List<CivilizationInfoResponsePacket.MemberReligion>(),
            manager.ReligionStateManager.CurrentDeity, manager.ReligionStateManager.CurrentReligionName,
            manager.ReligionStateManager.ReligionMemberCount, manager.ReligionStateManager.PlayerRoleInReligion,
            manager.ReligionStateManager.GetPlayerFavorProgress(),
            manager.ReligionStateManager.GetReligionPrestigeProgress(), manager.IsCivilizationFounder, windowPos.X + x,
            windowPos.Y + y, width);
        var headerHeight = ReligionHeaderRenderer.Draw(
            religionHeaderViewModel
        );
        y += headerHeight + 8f;

        // === 2. MAIN TABS ===
        var drawList = ImGui.GetWindowDrawList();
        var newMainTab = TabControl.Draw(
            drawList,
            windowPos.X + x,
            windowPos.Y + y,
            width,
            tabHeight,
            MainTabNames,
            (int)state.CurrentMainTab
        );

        if (newMainTab != (int)state.CurrentMainTab)
        {
            state.CurrentMainTab = (MainDialogTab)newMainTab;

            if (newMainTab == 0) // Religion tab
            {
                // Request both browse and my religion data
                manager.ReligionStateManager.State.BrowseState.IsBrowseLoading = true;
                manager.ReligionStateManager.RequestReligionList(manager.ReligionStateManager.State.BrowseState
                    .DeityFilter);


                // Request player religion info (includes invitations if player has no religion)
                if (manager.HasReligion())
                {
                    manager.ReligionStateManager.State.InfoState.Loading = true;
                }
                else
                {
                    manager.ReligionStateManager.State.InvitesState.Loading = true;
                }

                manager.ReligionStateManager.RequestPlayerReligionInfo();
            }
            else if (newMainTab == 2) // Civilization tab
            {
                manager.CivilizationManager.RequestCivilizationList(manager.CivTabState.BrowseState.DeityFilter);
                manager.CivilizationManager.RequestCivilizationInfo();
            }
        }

        y += tabHeight + 8f;

        // === 3. TAB CONTENT ===
        var contentHeight = windowHeight - y - padding;

        switch (state.CurrentMainTab)
        {
            case MainDialogTab.Religion: // Manage Religion
                manager.ReligionStateManager.DrawReligionTab(windowPos.X + x, windowPos.Y + y, width, contentHeight);
                break;
            case MainDialogTab.Blessings: // Blessings
                manager.BlessingStateManager.DrawBlessingsTab(
                    windowPos.X + x,
                    windowPos.Y + y,
                    width,
                    contentHeight,
                    windowWidth,
                    windowHeight,
                    deltaTime);
                break;
            case MainDialogTab.Civilization: // Civilization
                manager.CivilizationManager.DrawCivilizationTab(windowPos.X + x, windowPos.Y + y, width, contentHeight);
                break;
        }
    }
}