using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages the visibility and coordination of overlay windows (Religion Browser, Management, etc.)
/// </summary>
[ExcludeFromCodeCoverage]
public class OverlayCoordinator : IOverlayCoordinator
{
    private bool _showReligionBrowser;
    private bool _showReligionManagement;
    private bool _showCreateReligion;
    private bool _showLeaveConfirmation;

    /// <summary>
    ///     Show the Religion Browser overlay
    /// </summary>
    public void ShowReligionBrowser()
    {
        _showReligionBrowser = true;
    }

    /// <summary>
    ///     Close the Religion Browser overlay
    /// </summary>
    public void CloseReligionBrowser()
    {
        _showReligionBrowser = false;
    }

    /// <summary>
    ///     Show the Religion Management overlay
    /// </summary>
    public void ShowReligionManagement()
    {
        _showReligionManagement = true;
    }

    /// <summary>
    ///     Close the Religion Management overlay
    /// </summary>
    public void CloseReligionManagement()
    {
        _showReligionManagement = false;
    }

    /// <summary>
    ///     Show the Create Religion overlay
    /// </summary>
    public void ShowCreateReligion()
    {
        _showCreateReligion = true;
    }

    /// <summary>
    ///     Close the Create Religion overlay
    /// </summary>
    public void CloseCreateReligion()
    {
        _showCreateReligion = false;
    }

    /// <summary>
    ///     Show the Leave Religion confirmation overlay
    /// </summary>
    public void ShowLeaveConfirmation()
    {
        _showLeaveConfirmation = true;
    }

    /// <summary>
    ///     Close the Leave Religion confirmation overlay
    /// </summary>
    public void CloseLeaveConfirmation()
    {
        _showLeaveConfirmation = false;
    }

    /// <summary>
    ///     Close all overlays
    /// </summary>
    public void CloseAllOverlays()
    {
        _showReligionBrowser = false;
        _showReligionManagement = false;
        _showCreateReligion = false;
        _showLeaveConfirmation = false;
    }

    /// <summary>
    ///     Render all active overlays
    /// </summary>
    public void RenderOverlays(
        ICoreClientAPI capi,
        int windowWidth,
        int windowHeight,
        BlessingDialogManager manager,
        Action<string> onJoinReligionClicked,
        Action<string> onRefreshReligionList,
        Action onCreateReligionClicked,
        Action<string, List<string>, bool> onCreateReligionSubmit,
        Action<string> onKickMemberClicked,
        Action<string> onBanMemberClicked,
        Action<string> onUnbanMemberClicked,
        Action<string> onInvitePlayerClicked,
        Action<string> onEditDescriptionClicked,
        Action onDisbandReligionClicked,
        Action onRequestReligionInfo,
        Action onLeaveReligionCancelled,
        Action onLeaveReligionConfirmed)
    {
        // Render Religion Browser (if open and Create Religion is not open)
        if (_showReligionBrowser && !_showCreateReligion)
        {
            _showReligionBrowser = UI.Renderers.ReligionBrowserOverlay.Draw(
                capi,
                windowWidth,
                windowHeight,
                () => _showReligionBrowser = false,
                onJoinReligionClicked,
                onRefreshReligionList,
                onCreateReligionClicked,
                manager.HasReligion()
            );
        }

        // Render Create Religion overlay
        if (_showCreateReligion)
        {
            _showCreateReligion = UI.Renderers.CreateReligionOverlay.Draw(
                capi,
                windowWidth,
                windowHeight,
                () => _showCreateReligion = false,
                onCreateReligionSubmit
            );
        }

        // Render Religion Management overlay
        if (_showReligionManagement)
        {
            _showReligionManagement = UI.Renderers.ReligionManagementOverlay.Draw(
                capi,
                windowWidth,
                windowHeight,
                () => _showReligionManagement = false,
                onKickMemberClicked,
                onBanMemberClicked,
                onUnbanMemberClicked,
                onInvitePlayerClicked,
                onEditDescriptionClicked,
                onDisbandReligionClicked,
                onRequestReligionInfo
            );
        }

        // Render Leave Religion confirmation overlay
        if (_showLeaveConfirmation)
        {
            _showLeaveConfirmation = UI.Renderers.LeaveReligionConfirmOverlay.Draw(
                capi,
                windowWidth,
                windowHeight,
                manager.CurrentReligionName ?? "Unknown Religion",
                onLeaveReligionCancelled,
                onLeaveReligionConfirmed
            );
        }
    }
}
