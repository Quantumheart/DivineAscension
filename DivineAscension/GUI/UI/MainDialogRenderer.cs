using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Blessing;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI;

/// <summary>
///     Central coordinator that orchestrates all blessing UI renderers
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MainDialogRenderer
{
    private static string[] GetMainTabNames() =>
    [
        LocalizationService.Instance.Get(LocalizationKeys.UI_TAB_RELIGION),
        LocalizationService.Instance.Get(LocalizationKeys.UI_TAB_BLESSINGS),
        LocalizationService.Instance.Get(LocalizationKeys.UI_TAB_CIVILIZATION)
    ];

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
            manager.ReligionStateManager.CurrentReligionDomain, manager.ReligionStateManager.CurrentDeityName,
            manager.ReligionStateManager.CurrentReligionName,
            manager.ReligionStateManager.ReligionMemberCount, manager.ReligionStateManager.PlayerRoleInReligion,
            manager.ReligionStateManager.GetPlayerFavorProgress(),
            manager.ReligionStateManager.GetReligionPrestigeProgress(), manager.IsCivilizationFounder,
            manager.CivilizationManager.CivilizationIcon, windowPos.X + x,
            windowPos.Y + y, width);
        var headerHeight = ReligionHeaderRenderer.Draw(
            religionHeaderViewModel
        );
        y += headerHeight + 8f;

        // === 2. MAIN TABS ===
        var drawList = ImGui.GetWindowDrawList();

        // Top-right close (X) button
        const float closeSize = 24f;
        var closeX = windowPos.X + windowWidth - padding - closeSize;
        var closeY = windowPos.Y + padding;
        if (ButtonRenderer.DrawCloseButton(drawList, closeX, closeY, closeSize)) state.RequestClose = true;

        // Define icon names for main tabs
        var mainTabIcons = new[] { "temple", "meditation", "castle" };

        var newMainTab = TabControl.Draw(
            drawList,
            windowPos.X + x,
            windowPos.Y + y,
            width,
            tabHeight,
            GetMainTabNames(),
            (int)state.CurrentMainTab,
            4f, // tabSpacing (default)
            "gui", // iconDirectory
            mainTabIcons // iconNames
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