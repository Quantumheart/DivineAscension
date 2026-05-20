using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.State;

namespace DivineAscension.GUI.UI.Layout;

/// <summary>
///     Top-level dispatcher for the dialog body. Phase 1 forwards verbatim to the
///     existing tab-based <see cref="MainDialogRenderer" />; Phase 3 swaps the body
///     to a sidebar + master/detail + right-rail layout.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MainLayoutCoordinator
{
    public static void Draw(
        GuiDialogManager manager,
        GuiDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime)
    {
        MainDialogRenderer.Draw(manager, state, windowWidth, windowHeight, deltaTime);
    }
}
