using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Layout;

namespace DivineAscension.GUI.UI;

/// <summary>
///     Thin shim that forwards to <see cref="MainLayoutCoordinator" />. The
///     coordinator owns the sidebar + content + rail split; this entry point
///     exists to preserve the historical call site in <c>GuiDialog.DrawWindow</c>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MainDialogRenderer
{
    public static void Draw(
        GuiDialogManager manager,
        GuiDialogState state,
        int windowWidth,
        int windowHeight,
        float deltaTime)
    {
        MainLayoutCoordinator.Draw(manager, state, windowWidth, windowHeight, deltaTime);
    }
}
