using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Per-frame chrome state that pure renderers can read without holding a
///     reference to the dialog manager. Populated by
///     <see cref="DivineAscension.GUI.UI.Layout.MainLayoutCoordinator" /> at the
///     start of every Draw, before any pane renderer runs.
///
///     Currently carries only the player's patron domain (used by
///     <c>ChapterStripRenderer</c> to tint chapter drop caps in the player's
///     ink when the pane has no domain of its own). Add fields here when other
///     renderers need ambient player/world context without touching the
///     manager wiring.
/// </summary>
internal static class ChromeContext
{
    /// <summary>
    ///     Player's patron deity domain for this frame. Null when the player
    ///     has no religion or their religion has no parsable domain.
    /// </summary>
    public static DeityDomain? PlayerPatronDomain { get; private set; }

    public static void SetFrame(DeityDomain? playerPatronDomain)
    {
        PlayerPatronDomain = playerPatronDomain;
    }
}
