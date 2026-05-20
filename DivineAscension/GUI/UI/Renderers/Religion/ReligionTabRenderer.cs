using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Tab;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components.Banners;
using ImGuiNET;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Pure renderer for the Religion tab error banner.
///     Sub-tab navigation lives in the sidebar — this renderer is responsible
///     only for the per-context error banner that sits at the top of the
///     content pane.
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
        var effectiveNav = viewModel.CurrentNav;

        if (bannerMessage == null)
        {
            switch (viewModel.CurrentNav)
            {
                case SidebarNavId.ReligionBrowse:
                    bannerMessage = viewModel.ErrorState.BrowseError;
                    showRetry = bannerMessage != null;
                    break;
                case SidebarNavId.ReligionInfo:
                    bannerMessage = viewModel.ErrorState.InfoError;
                    showRetry = bannerMessage != null;
                    break;
                case SidebarNavId.ReligionCreate:
                    bannerMessage = viewModel.ErrorState.CreateError;
                    showRetry = false;
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
                    events.Add(new SubTabEvent.DismissContextError(effectiveNav));
                }
            }

            if (retryClicked)
            {
                events.Add(new SubTabEvent.RetryRequested(effectiveNav));
            }
        }

        return new ReligionSubTabRenderResult(events, renderedHeight);
    }
}
