using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components.Banners;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Pure renderer for the Civilization tab header and error banner.
///     Accepts a view model and emits UI events. Does not mutate state or perform side effects.
/// </summary>
internal static class CivilizationTabRenderer
{
    public static CivilizationTabRendererResult Draw(
        CivilizationTabViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<SubTabEvent>();

        // Sub-tab header
        var tabX = vm.X;
        var tabY = vm.Y;
        const float tabH = 36f;

        // Tab buttons: Conditionally render based on religion/civilization membership (Issue #71: 130px width, 4px spacing)
        const float tabWidth = 130f;
        const float spacing = 4f;

        // Track dynamic X position for visible tabs (avoids gaps when tabs are hidden)
        float currentX = tabX;

        void DrawTabButton(string label, CivilizationSubTab tab, string directory = "", string iconName = "")
        {
            var isActive = vm.CurrentSubTab == tab;
            var clicked = ButtonRenderer.DrawButton(drawList, label, currentX, tabY, tabWidth, tabH,
                isActive, true,
                isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f, directory, iconName);
            if (clicked && tab != vm.CurrentSubTab)
                events.Add(new SubTabEvent.TabChanged(tab));
            currentX += tabWidth + spacing; // Advance position for next visible tab
        }

        // Always show Browse (neutral for all states)
        DrawTabButton(LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_BROWSE),
            CivilizationSubTab.Browse, "GUI", "browse");

        // Conditional tabs based on religion/civilization membership
        if (vm.ShowInfoTab)
            DrawTabButton(LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_INFO),
                CivilizationSubTab.Info, "GUI", "info");

        if (vm.ShowInvitesTab)
            DrawTabButton(LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_INVITES),
                CivilizationSubTab.Invites, "GUI", "invites");

        if (vm.ShowCreateTab)
            DrawTabButton(LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_CREATE),
                CivilizationSubTab.Create, "GUI", "create");

        if (vm.ShowDiplomacyTab)
            DrawTabButton(LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_DIPLOMACY),
                CivilizationSubTab.Diplomacy, "GUI", "diplomacy");

        var contentY = vm.Y + tabH + 10f;
        var renderedHeight = tabH + 10f;

        // Error banner (LastActionError has priority)
        var bannerMessage = vm.LastActionError;
        var showRetry = false;
        var effectiveTab = vm.CurrentSubTab;

        if (bannerMessage == null)
            switch (vm.CurrentSubTab)
            {
                case CivilizationSubTab.Browse:
                    // If viewing details, prioritize details error
                    bannerMessage = vm.IsViewingDetails ? null : vm.BrowseError;
                    showRetry = bannerMessage != null; // allow retry for fetch errors
                    break;
                case CivilizationSubTab.Info:
                    bannerMessage = vm.InfoError;
                    showRetry = bannerMessage != null;
                    break;
                case CivilizationSubTab.Invites:
                    bannerMessage = vm.InvitesError;
                    showRetry = bannerMessage != null;
                    break;
            }

        if (bannerMessage != null)
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, vm.X, contentY, vm.Width, bannerMessage,
                out var retryClicked, out var dismissClicked, showRetry);
            renderedHeight += consumed;

            if (dismissClicked)
            {
                if (vm.LastActionError != null)
                    events.Add(new SubTabEvent.DismissActionError());
                else
                    events.Add(new SubTabEvent.DismissContextError(effectiveTab));
            }

            if (retryClicked)
                events.Add(new SubTabEvent.RetryRequested(effectiveTab));
        }

        return new CivilizationTabRendererResult(events, renderedHeight);
    }
}