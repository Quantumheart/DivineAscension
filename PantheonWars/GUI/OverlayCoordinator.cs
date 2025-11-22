using System;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages the visibility and coordination of overlay windows (Create Religion, Leave Confirmation)
///     Note: Browser and Management are now in the main window as tabs
/// </summary>
[ExcludeFromCodeCoverage]
public class OverlayCoordinator : IOverlayCoordinator
{
    private bool _showCreateReligion;
    private bool _showLeaveConfirmation;

    /// <summary>
    ///     Show the Religion Browser overlay (deprecated - now in main window)
    /// </summary>
    [Obsolete("Religion browser is now shown in main window, not as overlay")]
    public void ShowReligionBrowser()
    {
        // No-op: Browser is now in main window
    }

    /// <summary>
    ///     Close the Religion Browser overlay (deprecated - now in main window)
    /// </summary>
    [Obsolete("Religion browser is now shown in main window, not as overlay")]
    public void CloseReligionBrowser()
    {
        // No-op: Browser is now in main window
    }

    /// <summary>
    ///     Show the Religion Management overlay (deprecated - now in main window tabs)
    /// </summary>
    [Obsolete("Religion management is now shown in main window tabs, not as overlay")]
    public void ShowReligionManagement()
    {
        // No-op: Management is now in main window tabs
    }

    /// <summary>
    ///     Close the Religion Management overlay (deprecated - now in main window tabs)
    /// </summary>
    [Obsolete("Religion management is now shown in main window tabs, not as overlay")]
    public void CloseReligionManagement()
    {
        // No-op: Management is now in main window tabs
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
        _showCreateReligion = false;
        _showLeaveConfirmation = false;
    }

    /// <summary>
    ///     Render all active overlays (Create Religion and Leave Confirmation only)
    /// </summary>
    public void RenderOverlays(
        ICoreClientAPI capi,
        int windowWidth,
        int windowHeight,
        GuildDialogManager manager,
        Action<string> onJoinReligionClicked,
        Action onCreateReligionClicked,
        Action<string, bool> onCreateReligionSubmit,
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
