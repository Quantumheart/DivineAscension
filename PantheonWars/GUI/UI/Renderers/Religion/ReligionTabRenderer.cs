using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Banners;
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
        var state = manager.ReligionState;
        var drawList = ImGui.GetWindowDrawList();

        // Sub-tab header
        var tabX = x;
        var tabY = y;
        var tabW = width;
        var tabH = 36f;

        // Tab buttons: Browse | My Religion | Activity | Bonuses | Create
        var tabWidth = 130f;
        var spacing = 6f;
        var prevTab = state.CurrentSubTab;

        DrawTabButton("Browse", 0);
        DrawTabButton("My Religion", 1);
        DrawTabButton("Activity", 2);
        DrawTabButton("Bonuses", 3);
        DrawTabButton("Create", 4);

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

                // Clear context-specific errors when switching into a tab
                switch (index)
                {
                    case 0:
                        // Browse
                        state.BrowseError = null;
                        break;
                    case 1:
                        // My Religion
                        state.MyReligionError = null;
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
        bool showRetry = false;
        int effectiveTab = state.CurrentSubTab;

        if (bannerMessage == null)
        {
            switch (state.CurrentSubTab)
            {
                case 0:
                    bannerMessage = state.BrowseError;
                    showRetry = bannerMessage != null;
                    break;
                case 1:
                    bannerMessage = state.MyReligionError;
                    showRetry = bannerMessage != null;
                    break;
                case 4:
                    bannerMessage = state.CreateError;
                    showRetry = false; // Don't show retry for create errors
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
            }

            if (retryClicked)
            {
                switch (effectiveTab)
                {
                    case 0:
                        manager.RequestReligionList(state.DeityFilter);
                        break;
                    case 1:
                        manager.RequestPlayerReligionInfo();
                        break;
                }
            }
        }

        // Route to appropriate sub-renderer based on current sub-tab
        switch (state.CurrentSubTab)
        {
            case 0:
                ReligionBrowseRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 1:
                ReligionMyReligionRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 2:
                ReligionActivityRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 3:
                ReligionBonusesRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 4:
                ReligionCreateRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
        }
    }
}
