using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Tab;
using DivineAscension.GUI.State.Religion;
using DivineAscension.GUI.UI.Components.Banners;
using ImGuiNET;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Pure renderer for the Religion tab error banner.
///     Sub-tab navigation now lives in the sidebar (Phase 3b refactor) — this
///     renderer is responsible only for the per-context error banner that sits
///     at the top of the content pane.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionTabRenderer
{
    public static ReligionSubTabRenderResult Draw(
        ReligionTabViewModel viewModel,
        ImDrawListPtr drawList,
        ICoreClientAPI api)
    {
        var events = new List<SubTabEvent>();

        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var contentY = y;
        var renderedHeight = 0f;

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