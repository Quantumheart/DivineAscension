using System;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.Interfaces;

/// <summary>
///     Interface for managing the visibility and coordination of overlay windows
/// </summary>
[Obsolete("IOverlayCoordinator and all religion overlays have been migrated to the BlessingDialog tab system. This interface will be removed in a future update.")]
public interface IOverlayCoordinator
{
    /// <summary>
    ///     Show the Religion Browser overlay
    /// </summary>
    void ShowReligionBrowser();

    /// <summary>
    ///     Close the Religion Browser overlay
    /// </summary>
    void CloseReligionBrowser();

    /// <summary>
    ///     Show the Religion Management overlay
    /// </summary>
    void ShowReligionManagement();

    /// <summary>
    ///     Close the Religion Management overlay
    /// </summary>
    void CloseReligionManagement();

    /// <summary>
    ///     Show the Create Religion overlay
    /// </summary>
    void ShowCreateReligion();

    /// <summary>
    ///     Close the Create Religion overlay
    /// </summary>
    void CloseCreateReligion();

    /// <summary>
    ///     Show the Leave Religion confirmation overlay
    /// </summary>
    void ShowLeaveConfirmation();

    /// <summary>
    ///     Close the Leave Religion confirmation overlay
    /// </summary>
    void CloseLeaveConfirmation();

    /// <summary>
    ///     Close all overlays
    /// </summary>
    void CloseAllOverlays();

    /// <summary>
    ///     Render all active overlays
    /// </summary>
    void RenderOverlays(
        ICoreClientAPI capi,
        int windowWidth,
        int windowHeight,
        BlessingDialogManager manager,
        Action<string> onJoinReligionClicked,
        Action<string> onRefreshReligionList,
        Action onCreateReligionClicked,
        Action<string, string, bool> onCreateReligionSubmit,
        Action<string> onKickMemberClicked,
        Action<string> onBanMemberClicked,
        Action<string> onUnbanMemberClicked,
        Action<string> onInvitePlayerClicked,
        Action<string> onEditDescriptionClicked,
        Action onDisbandReligionClicked,
        Action onRequestReligionInfo,
        Action onLeaveReligionCancelled,
        Action onLeaveReligionConfirmed);
}
