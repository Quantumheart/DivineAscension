using System;
using System.Numerics;
using ImGuiNET;

namespace DivineAscension.GUI.Models.Letters;

/// <summary>
///     One letter row inside the shared Letters renderer. Adapters build
///     these from their area-specific invite data — the renderer is
///     deliberately ignorant of religion / civilization concepts.
/// </summary>
/// <param name="Id">Stable identifier echoed back through accept/refuse events.</param>
/// <param name="SenderText">Pre-formatted sender line (e.g. <c>"From Order of the Forge"</c>).</param>
/// <param name="GlyphPainter">
///     Paints the glyph that sits between the envelope and the sender text.
///     Glyphs in this codebase are drawn as primitives rather than text
///     codepoints (see <see cref="DivineAscension.GUI.UI.Renderers.Utilities.DomainGlyphRenderer" />
///     and the banner / envelope helpers on
///     <see cref="DivineAscension.GUI.UI.Renderers.Utilities.ChromeRenderer" />),
///     so the adapter passes a painter rather than a string.
/// </param>
/// <param name="QuoteLine">Quoted line shown below the sender, indented to the text column.</param>
/// <param name="IsUnread">Reserved for follow-up unread-state work; renderer currently ignores it.</param>
public readonly record struct LetterEntry(
    string Id,
    string SenderText,
    Action<ImDrawListPtr, Vector2, Vector2> GlyphPainter,
    string QuoteLine,
    bool IsUnread = false,
    bool ShowActions = true);
