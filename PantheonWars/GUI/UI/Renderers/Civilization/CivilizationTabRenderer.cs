using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Banners;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationTabRenderer
{
    public static void Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivState;
        var drawList = ImGui.GetWindowDrawList();

        // Sub-tab header
        var tabX = x;
        var tabY = y;
        var tabW = width;
        var tabH = 36f;

        // Simple tab buttons: Browse | My Civilization | Invites | Create
        var tabWidth = 150f;
        var spacing = 6f;
        var prevTab = state.CurrentSubTab;

        DrawTabButton("Browse", 0);
        DrawTabButton("My Civilization", 1);
        DrawTabButton("Invites", 2);
        DrawTabButton("Create", 3);

        void DrawTabButton(string label, int index)
        {
            var tx = tabX + index * (tabWidth + spacing);
            var isActive = state.CurrentSubTab == index;
            var clicked = ButtonRenderer.DrawButton(drawList, label, tx, tabY, tabWidth, tabH,
                isPrimary: isActive, enabled: true,
                customColor: isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f);
            if (clicked)
            {
                state.CurrentSubTab = index;

                // Clear transient action error on tab change
                state.LastActionError = null;

                // Optionally clear context-specific errors when switching into a tab
                switch (index)
                {
                    case 0:
                        // Browse/Details errors
                        if (state.ViewingCivilizationId != null) state.DetailsError = null;
                        else state.BrowseError = null;
                        break;
                    case 1:
                        state.MyCivError = null;
                        break;
                    case 2:
                        state.InvitesError = null;
                        break;
                }
            }
        }

        var contentY = y + tabH + 10f;
        var contentHeight = height - (contentY - y);

        // Error banner (LastActionError has priority)
        var bannerMessage = state.LastActionError;
        bool showRetry = false;
        int effectiveTab = state.CurrentSubTab;

        if (bannerMessage == null)
        {
            switch (state.CurrentSubTab)
            {
                case 0:
                    // If viewing details, prioritize details error
                    bannerMessage = state.ViewingCivilizationId != null ? state.DetailsError : state.BrowseError;
                    showRetry = bannerMessage != null; // allow retry for fetch errors
                    break;
                case 1:
                    bannerMessage = state.MyCivError;
                    showRetry = bannerMessage != null;
                    break;
                case 2:
                    bannerMessage = state.InvitesError;
                    showRetry = bannerMessage != null;
                    break;
            }
        }

        if (bannerMessage != null)
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, x, contentY, width, bannerMessage,
                out var retryClicked, out var dismissClicked, showRetry: showRetry);
            contentY += consumed;
            contentHeight -= consumed;

            if (dismissClicked)
            {
                if (state.LastActionError != null) state.LastActionError = null;
                else
                {
                    switch (effectiveTab)
                    {
                        case 0:
                            if (state.ViewingCivilizationId != null) state.DetailsError = null; else state.BrowseError = null;
                            break;
                        case 1:
                            state.MyCivError = null;
                            break;
                        case 2:
                            state.InvitesError = null;
                            break;
                    }
                }
            }

            if (retryClicked)
            {
                switch (effectiveTab)
                {
                    case 0:
                        if (state.ViewingCivilizationId != null)
                        {
                            manager.RequestCivilizationInfo(state.ViewingCivilizationId);
                        }
                        else
                        {
                            manager.RequestCivilizationList(state.DeityFilter);
                        }
                        break;
                    case 1:
                    case 2:
                        manager.RequestCivilizationInfo("");
                        break;
                }
            }
        }

        switch (state.CurrentSubTab)
        {
            case 0:
                CivilizationBrowseRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 1:
                CivilizationManageRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 2:
                CivilizationInvitesRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 3:
                CivilizationCreateRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
        }
    }
}
