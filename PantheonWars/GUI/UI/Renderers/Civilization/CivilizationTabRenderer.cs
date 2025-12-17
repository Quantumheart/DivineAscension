using System.Collections.Generic;
using ImGuiNET;
using PantheonWars.GUI.Events.Civilization;
using PantheonWars.GUI.Models.Civilization.Tab;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components.Banners;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

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

        // Simple tab buttons: Browse | My Civilization | Invites | Create
        const float tabWidth = 150f;
        const float spacing = 6f;

        void DrawTabButton(string label, CivilizationSubTab tab, string directory = "", string iconName = "")
        {
            var tx = tabX + (int)tab * (tabWidth + spacing);
            var isActive = vm.CurrentSubTab == tab;
            var clicked = ButtonRenderer.DrawButton(drawList, label, tx, tabY, tabWidth, tabH,
                isActive, true,
                isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f, directory, iconName);
            if (clicked && tab != vm.CurrentSubTab)
                events.Add(new SubTabEvent.TabChanged(tab));
        }

        DrawTabButton("Browse", CivilizationSubTab.Browse, "GUI", "browse");
        DrawTabButton("Info", CivilizationSubTab.Info, "GUI", "info");
        DrawTabButton("Invites", CivilizationSubTab.Invites, "GUI", "invites");
        DrawTabButton("Create", CivilizationSubTab.Create, "GUI", "create");

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