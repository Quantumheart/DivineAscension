using System.Collections.Generic;
using System.Numerics;
using DivineAscension.GUI.State;

namespace DivineAscension.GUI.UI.Renderers.EdgeBookmarks;

/// <summary>
///     One ribbon hanging from the right edge of the codex spread. A click
///     jumps to <see cref="Target"/>. <see cref="IsActive"/> marks the bookmark
///     whose section currently owns <c>SidebarState.CurrentNav</c>;
///     <see cref="IsDisabled"/> covers sections that don't exist yet (e.g. Help).
/// </summary>
public readonly record struct EdgeBookmarkViewModel(
    string Stamp,
    string Tooltip,
    Vector4 RibbonColor,
    SidebarNavId Target,
    bool IsActive,
    bool IsDisabled);

/// <summary>
///     Ordered set of bookmarks (top → bottom). Built by
///     <c>EdgeBookmarkMapper.BuildViewModel</c>.
/// </summary>
public readonly record struct EdgeBookmarkRibbonStack(IReadOnlyList<EdgeBookmarkViewModel> Bookmarks);
