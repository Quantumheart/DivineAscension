using DivineAscension.GUI.State;

namespace DivineAscension.GUI.UI.Renderers.Sidebar;

/// <summary>
///     Pure flattener over a <see cref="SidebarViewModel" /> that yields the
///     ordered, enabled-only page sequence used by the page-turn footer and
///     keyboard arrows. The sidebar is the single source of truth for ordering
///     and enablement; this helper just walks it.
/// </summary>
public static class PageTurnNavigator
{
    /// <summary>
    ///     Position of <see cref="SidebarViewModel.CurrentNav" /> inside the
    ///     flattened enabled-page sequence, plus the previous / next ids the
    ///     turn-page buttons should target. <see cref="Previous" /> and
    ///     <see cref="Next" /> are <c>null</c> at the boundaries (no wrap).
    ///     When the current nav isn't a member of the enabled list (e.g. the
    ///     player just lost membership and is sitting on a now-disabled page)
    ///     both are <c>null</c> and <see cref="Index" /> reports <c>-1</c>.
    /// </summary>
    public readonly record struct PagePosition(
        int Index,
        int Total,
        SidebarNavId? Previous,
        SidebarNavId? Next);

    /// <summary>
    ///     Compute the page position for <paramref name="vm" />'s active nav.
    ///     Disabled items are skipped — the page chain only walks readable
    ///     pages. Group headers are not pages.
    ///     <para>
    ///         If the current nav is itself disabled (e.g. dialog opens on a
    ///         founder-only page for a non-founder), <see cref="Index" /> is
    ///         <c>-1</c> but <see cref="Previous" /> / <see cref="Next" /> still
    ///         resolve relative to the active nav's sidebar position, so the
    ///         page-turn buttons stay usable as an escape.
    ///     </para>
    /// </summary>
    public static PagePosition Compute(SidebarViewModel vm)
    {
        var total = 0;
        var idx = -1;
        SidebarNavId? prev = null;
        SidebarNavId? next = null;
        SidebarNavId? lastEnabled = null;
        var seenActive = false;

        foreach (var group in vm.Groups)
        {
            for (var i = 0; i < group.Items.Count; i++)
            {
                var item = group.Items[i];
                var isActive = item.Id == vm.CurrentNav;

                if (isActive && !seenActive)
                {
                    seenActive = true;
                    prev = lastEnabled;
                    if (!item.IsDisabled) idx = total;
                }
                else if (seenActive && next == null && !item.IsDisabled)
                {
                    next = item.Id;
                }

                if (!item.IsDisabled)
                {
                    lastEnabled = item.Id;
                    total++;
                }
            }
        }

        return new PagePosition(idx, total, prev, next);
    }
}
