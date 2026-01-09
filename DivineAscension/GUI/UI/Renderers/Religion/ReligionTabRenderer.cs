using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Tab;
using DivineAscension.GUI.State.Religion;
using DivineAscension.GUI.UI.Components.Banners;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Pure renderer for the Religion tab header and error banner.
///     Accepts a view model and emits UI events. Does not mutate state or perform side effects.
/// </summary>
internal static class ReligionTabRenderer
{
    public static ReligionSubTabRenderResult Draw(
        ReligionTabViewModel viewModel,
        ImDrawListPtr drawList,
        ICoreClientAPI api)
    {
        var events = new List<SubTabEvent>();

        // Sub-tab header metrics
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        const float tabH = 36f;

        // Tab buttons: Conditionally render based on religion membership (Issue #71: 130px width, 4px spacing)
        const float tabWidth = 130f;
        const float spacing = 4f;

        // Track dynamic X position for visible tabs (avoids gaps when tabs are hidden)
        float currentX = x;

        void DrawTabButton(string label, SubTab tab, string directory = "", string iconName = "")
        {
            var isActive = viewModel.CurrentSubTab == tab;
            var clicked = ButtonRenderer.DrawButton(drawList, label, currentX, y, tabWidth, tabH,
                isActive, true,
                isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f, directory, iconName);
            if (clicked && tab != viewModel.CurrentSubTab)
            {
                events.Add(new SubTabEvent.TabChanged(tab));
            }

            currentX += tabWidth + spacing; // Advance position for next visible tab
        }

        // Always show Browse (neutral for all states)
        DrawTabButton(nameof(SubTab.Browse), SubTab.Browse, "GUI", "browse");

        // Conditional tabs based on religion membership
        if (viewModel.ShowInfoTab)
            DrawTabButton(nameof(SubTab.Info), SubTab.Info, "GUI", "info");

        if (viewModel.ShowActivityTab)
            DrawTabButton(nameof(SubTab.Activity), SubTab.Activity, "GUI", "activity");

        if (viewModel.ShowRolesTab)
            DrawTabButton(nameof(SubTab.Roles), SubTab.Roles, "GUI", "roles");

        if (viewModel.ShowInvitesTab)
            DrawTabButton(nameof(SubTab.Invites), SubTab.Invites, "GUI", "invites");

        if (viewModel.ShowCreateTab)
            DrawTabButton(nameof(SubTab.Create), SubTab.Create, "GUI", "create");

        var contentY = y + tabH + 10f;
        var renderedHeight = tabH + 10f;

        // Error banner (LastActionError has priority)
        var bannerMessage = viewModel.ErrorState.LastActionError;
        var showRetry = false;
        var effectiveTab = viewModel.CurrentSubTab;

        if (bannerMessage == null)
        {
            switch (viewModel.CurrentSubTab)
            {
                case SubTab.Browse:
                    bannerMessage = viewModel.ErrorState.BrowseError;
                    showRetry = bannerMessage != null;
                    break;
                case SubTab.Info:
                    bannerMessage = viewModel.ErrorState.InfoError;
                    showRetry = bannerMessage != null;
                    break;
                case SubTab.Create:
                    bannerMessage = viewModel.ErrorState.CreateError;
                    showRetry = false; // No retry for create errors
                    break;
            }
        }

        if (bannerMessage != null)
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, x, contentY, width, bannerMessage,
                out var retryClicked, out var dismissClicked, showRetry);
            renderedHeight += consumed;

            if (dismissClicked)
            {
                if (viewModel.ErrorState.LastActionError != null)
                {
                    events.Add(new SubTabEvent.DismissActionError());
                }
                else
                {
                    events.Add(new SubTabEvent.DismissContextError(effectiveTab));
                }
            }

            if (retryClicked)
            {
                events.Add(new SubTabEvent.RetryRequested(effectiveTab));
            }
        }

        return new ReligionSubTabRenderResult(events, renderedHeight);
    }
}