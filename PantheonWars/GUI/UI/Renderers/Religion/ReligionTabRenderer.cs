using ImGuiNET;
using PantheonWars.GUI.State.Religion;
using PantheonWars.GUI.UI.Components.Banners;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     The main coordinator for the Religion tab in BlessingDialog
///     Renders sub-tabs and routes to appropriate sub-renderers
/// </summary>
internal static class ReligionTabRenderer
{
    public static void Draw(
        GuiDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.ReligionStateManager.State;
        var drawList = ImGui.GetWindowDrawList();

        // Sub-tab header
        var tabX = x;
        var tabY = y;
        var tabW = width;
        var tabH = 36f;

        // Tab buttons: Browse | My Religion | Activity | Invites | Create
        var tabWidth = 130f;
        var spacing = 6f;
        var prevTab = state.CurrentSubTab;

        DrawTabButton(nameof(SubTab.Browse), (int)SubTab.Browse);
        DrawTabButton(nameof(SubTab.Info), (int)SubTab.Info);
        DrawTabButton(nameof(SubTab.Activity), (int)SubTab.Activity);
        if (!manager.HasReligion())
        {
            DrawTabButton(nameof(SubTab.Invites), (int)SubTab.Invites);
            DrawTabButton(nameof(SubTab.Create), (int)SubTab.Create);
        }

        void DrawTabButton(string label, int tabIndex)
        {
            var tx = tabX + tabIndex * (tabWidth + spacing);
            var isActive = state.CurrentSubTab == (SubTab)tabIndex;
            var clicked = ButtonRenderer.DrawButton(drawList, label, tx, tabY, tabWidth, tabH,
                isActive, true,
                isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f);
            if (clicked)
            {
                state.CurrentSubTab = (SubTab)tabIndex;

                // Clear transient action error on tab change
                state.ErrorState.LastActionError = null;

                // Clear context-specific errors and request data when switching into a tab
                switch ((SubTab) tabIndex)
                {
                    case SubTab.Browse:
                        // Browse
                        state.ErrorState.BrowseError = null;
                        break;
                    case SubTab.Info:
                        // My Religion
                        state.ErrorState.InfoError = null;
                        break;
                    case SubTab.Activity:
                        state.ErrorState.ActivityError = null;
                        break;
                    case SubTab.Invites:
                        // Invites - request player religion info to get invitations
                        state.InvitesState.InvitesError = null;
                        state.InvitesState.IsInvitesLoading = true;
                        manager.ReligionStateManager.RequestPlayerReligionInfo();
                        break;
                    case SubTab.Create:
                        // Create
                        state.ErrorState.CreateError = null;
                        break;
                }
            }
        }

        var contentY = y + tabH + 10f;
        var contentHeight = height - (contentY - y);

        // Error banner (LastActionError has priority)
        var bannerMessage = state.ErrorState.LastActionError;
        var showRetry = false;
        var effectiveTab = (int)state.CurrentSubTab;

        if (bannerMessage == null)
            switch (state.CurrentSubTab)
            {
                case SubTab.Browse:
                    bannerMessage = state.ErrorState.BrowseError;
                    showRetry = bannerMessage != null;
                    break;
                case SubTab.Info:
                    bannerMessage = state.ErrorState.InfoError;
                    showRetry = bannerMessage != null;
                    break;
                case SubTab.Create:
                    bannerMessage = state.ErrorState.CreateError;
                    showRetry = false; // Don't show retry for create errors
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
                if (state.ErrorState.LastActionError != null) state.ErrorState.LastActionError = null;
                else
                    switch (effectiveTab)
                    {
                        case 0:
                            state.ErrorState.BrowseError = null;
                            break;
                        case 1:
                            state.ErrorState.InfoError = null;
                            break;
                        case 4:
                            state.ErrorState.CreateError = null;
                            break;
                    }
            }

            if (retryClicked)
                switch (effectiveTab)
                {
                    case 0:
                        manager.ReligionStateManager.RequestReligionList(state.BrowseState.DeityFilter);
                        break;
                    case 1:
                        manager.ReligionStateManager.RequestPlayerReligionInfo();
                        break;
                }
        }

        // Route to appropriate sub-renderer based on current sub-tab
        switch (state.CurrentSubTab)
        {
            case SubTab.Browse:
                manager.ReligionStateManager.DrawReligionBrowse(x, contentY, width, contentHeight);
                break;
            case SubTab.Info:
                manager.ReligionStateManager.DrawReligionInfo(x, contentY, width, contentHeight);
                break;
            case SubTab.Activity:
                ReligionActivityRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case SubTab.Invites:
                manager.ReligionStateManager.DrawReligionInvites(x, contentY, width, contentHeight);
                break;
            case SubTab.Create:
                manager.ReligionStateManager.DrawReligionCreate(x, contentY, width, contentHeight);
                break;
        }
    }
}