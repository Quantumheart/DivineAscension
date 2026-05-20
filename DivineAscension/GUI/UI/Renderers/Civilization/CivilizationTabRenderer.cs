using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components.Banners;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Pure renderer for the Civilization tab error banner.
///     Sub-tab navigation now lives in the sidebar (Phase 3b refactor) — this
///     renderer is responsible only for the per-context error banner.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationTabRenderer
{
    public static CivilizationTabRendererResult Draw(
        CivilizationTabViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<SubTabEvent>();

        var contentY = vm.Y;
        var renderedHeight = 0f;

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