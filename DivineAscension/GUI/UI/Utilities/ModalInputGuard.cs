namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Tracks whether a modal overlay (any <see cref="Components.Overlays.ConfirmOverlay" />)
///     is open, so the dialog chrome can stop reacting to clicks that fall through behind it.
///
///     Why this exists: the dialog renders in immediate mode with manual hit-testing, so a
///     modal's dim backdrop is purely visual — the sidebar, page-turn, title close, and pane
///     content all still test the mouse on the same frame. <see cref="ConfirmOverlay" /> calls
///     <see cref="MarkOpen" /> whenever it draws; chrome consults <see cref="IsBlocking" /> to
///     suppress its own interactions while a modal is up.
///
///     Frame model: a modal persists across frames (it is state-driven), but it is *drawn*
///     during content dispatch — after the chrome has already been drawn this frame. So the
///     gate reads the previous frame's mark (<see cref="IsBlocking" />), which is correct for
///     every frame after the one on which the modal first opens. The root layout calls
///     <see cref="BeginFrame" /> once per frame to roll the mark forward.
/// </summary>
internal static class ModalInputGuard
{
    private static bool _markedThisFrame;
    private static bool _blockingThisFrame;

    /// <summary>True when a modal was drawn on the previous frame and chrome should ignore input.</summary>
    public static bool IsBlocking => _blockingThisFrame;

    /// <summary>Called by a modal overlay each frame it draws.</summary>
    public static void MarkOpen() => _markedThisFrame = true;

    /// <summary>
    ///     Promotes the previous frame's mark into <see cref="IsBlocking" /> and resets the
    ///     mark for the new frame. Must be called exactly once per frame at the dialog root,
    ///     before any chrome is drawn.
    /// </summary>
    public static void BeginFrame()
    {
        _blockingThisFrame = _markedThisFrame;
        _markedThisFrame = false;
    }
}
