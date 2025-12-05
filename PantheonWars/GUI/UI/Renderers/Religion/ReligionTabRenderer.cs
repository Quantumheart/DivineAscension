using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components.Banners;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     Main coordinator for the Religion tab in BlessingDialog
///     Renders sub-tabs and routes to appropriate sub-renderers
/// </summary>
internal static class ReligionTabRenderer
{
    public static void Draw(
        BlessingDialogManager manager,
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

        DrawTabButton("Browse", (int)ReligionSubTab.Browse);
        DrawTabButton("My Religion", (int)ReligionSubTab.MyReligion);
        DrawTabButton("Activity", (int)ReligionSubTab.Activity);
        if (!manager.HasReligion())
        {
            DrawTabButton("Invites", (int)ReligionSubTab.Invites);
            DrawTabButton("Create", (int)ReligionSubTab.Create);
        }

        void DrawTabButton(string label, int tabIndex)
        {
            var tx = tabX + tabIndex * (tabWidth + spacing);
            var isActive = state.CurrentSubTab == (ReligionSubTab)tabIndex;
            var clicked = ButtonRenderer.DrawButton(drawList, label, tx, tabY, tabWidth, tabH,
                isActive, true,
                isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f);
            if (clicked)
            {
                state.CurrentSubTab = (ReligionSubTab)tabIndex;

                // Clear transient action error on tab change
                state.LastActionError = null;

                // Clear context-specific errors and request data when switching into a tab
                switch (tabIndex)
                {
                    case 0:
                        // Browse
                        state.BrowseError = null;
                        break;
                    case 1:
                        // My Religion
                        state.MyReligionError = null;
                        break;
                    case 3:
                        // Invites - request player religion info to get invitations
                        state.InvitesError = null;
                        state.IsInvitesLoading = true;
                        manager.ReligionStateManager.RequestPlayerReligionInfo();
                        break;
                    case 4:
                        // Create
                        state.CreateError = null;
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
                case ReligionSubTab.Browse:
                    bannerMessage = state.BrowseError;
                    showRetry = bannerMessage != null;
                    break;
                case ReligionSubTab.MyReligion:
                    bannerMessage = state.MyReligionError;
                    showRetry = bannerMessage != null;
                    break;
                case ReligionSubTab.Create:
                    bannerMessage = state.CreateError;
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
                if (state.LastActionError != null) state.LastActionError = null;
                else
                    switch (effectiveTab)
                    {
                        case 0:
                            state.BrowseError = null;
                            break;
                        case 1:
                            state.MyReligionError = null;
                            break;
                        case 4:
                            state.CreateError = null;
                            break;
                    }
            }

            if (retryClicked)
                switch (effectiveTab)
                {
                    case 0:
                        manager.ReligionStateManager.RequestReligionList(state.DeityFilter);
                        break;
                    case 1:
                        manager.ReligionStateManager.RequestPlayerReligionInfo();
                        break;
                }
        }

        // Route to appropriate sub-renderer based on current sub-tab
        switch (state.CurrentSubTab)
        {
            case ReligionSubTab.Browse:
                manager.ReligionStateManager.DrawReligionBrowse(x, contentY, width, contentHeight);
                break;
            case ReligionSubTab.MyReligion:
                ReligionInfoRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case ReligionSubTab.Activity:
                ReligionActivityRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case ReligionSubTab.Invites:
                manager.ReligionStateManager.DrawReligionInvites(x, contentY, width, contentHeight);
                break;
            case ReligionSubTab.Create:
                manager.ReligionStateManager.DrawReligionCreate(x, contentY, width, contentHeight);
                break;
        }
    }
}