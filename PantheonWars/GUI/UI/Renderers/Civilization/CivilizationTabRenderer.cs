using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components.Banners;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationTabRenderer
{
    public static void Draw(
        GuiDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivTabState;
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
        if (!manager.HasCivilization()) DrawTabButton("Invites", 2);
        if (!manager.HasCivilization()) DrawTabButton("Create", 3);

        void DrawTabButton(string label, int index)
        {
            var tx = tabX + index * (tabWidth + spacing);
            var isActive = state.CurrentSubTab == (CivilizationSubTab)index;
            var clicked = ButtonRenderer.DrawButton(drawList, label, tx, tabY, tabWidth, tabH,
                isActive, true,
                isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f);
            if (clicked)
            {
                state.CurrentSubTab = (CivilizationSubTab)index;

                // Clear transient action error on tab change
                state.LastActionError = null;

                // Optionally clear context-specific errors when switching into a tab
                switch (index)
                {
                    case 0:
                        // Browse/Details errors
                        if (state.DetailState.ViewingCivilizationId != null) state.DetailState.ErrorMsg = null;
                        else state.BrowseState.ErrorMsg = null;
                        break;
                    case 1:
                        // My Civilization - refresh data to ensure it's current
                        state.InfoState.ErrorMsg = null;
                        manager.CivilizationManager.RequestCivilizationInfo();
                        break;
                    case 2:
                        // Invites - refresh data to ensure it's current
                        state.InviteState.ErrorMsg = null;
                        manager.CivilizationManager.RequestCivilizationInfo();
                        break;
                }
            }
        }

        var contentY = y + tabH + 10f;
        var contentHeight = height - (contentY - y);

        // Error banner (LastActionError has priority)
        var bannerMessage = state.LastActionError;
        var showRetry = false;
        var effectiveTab = (int)state.CurrentSubTab;

        if (bannerMessage == null)
            switch (state.CurrentSubTab)
            {
                case 0:
                    // If viewing details, prioritize details error
                    bannerMessage = state.DetailState.ViewingCivilizationId != null
                        ? state.DetailState.ErrorMsg
                        : state.BrowseState.ErrorMsg;
                    showRetry = bannerMessage != null; // allow retry for fetch errors
                    break;
                case CivilizationSubTab.MyCiv:
                    bannerMessage = state.InfoState.ErrorMsg;
                    showRetry = bannerMessage != null;
                    break;
                case CivilizationSubTab.Invites:
                    bannerMessage = state.InviteState.ErrorMsg;
                    showRetry = bannerMessage != null;
                    break;
            }

        if (bannerMessage != null)
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, x, contentY, width, bannerMessage,
                out var retryClicked, out var dismissClicked, showRetry);
            contentY += consumed;
            contentHeight -= consumed;

            if (dismissClicked)
            {
                if (state.LastActionError != null) state.LastActionError = null;
                else
                    switch (effectiveTab)
                    {
                        case 0:
                            if (state.DetailState.ViewingCivilizationId != null) state.DetailState.ErrorMsg = null;
                            else state.BrowseState.ErrorMsg = null;
                            break;
                        case 1:
                            state.InfoState.ErrorMsg = null;
                            break;
                        case 2:
                            state.InviteState.ErrorMsg = null;
                            break;
                    }
            }

            if (retryClicked)
                switch (effectiveTab)
                {
                    case 0:
                        if (state.DetailState.ViewingCivilizationId != null)
                            manager.CivilizationManager.RequestCivilizationInfo(state.DetailState
                                .ViewingCivilizationId);
                        else
                            manager.CivilizationManager.RequestCivilizationList(state.BrowseState.DeityFilter);
                        break;
                    case 1:
                    case 2:
                        manager.CivilizationManager.RequestCivilizationInfo();
                        break;
                }
        }

        switch (state.CurrentSubTab)
        {
            case CivilizationSubTab.Browse:
                CivilizationBrowseRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.MyCiv:
                CivilizationManageRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.Invites:
                CivilizationInvitesRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case CivilizationSubTab.Create:
                CivilizationCreateRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
        }
    }
}