using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.State;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Renderers.Sidebar;

/// <summary>
///     Pure mapping from manager-derived flags + counts into a
///     <see cref="SidebarViewModel" />, plus an <see cref="Apply" /> helper
///     that commits a sidebar click to <see cref="SidebarState.CurrentNav" />.
/// </summary>
public static class SidebarNavMapper
{
    public const string GroupReligionKey = "religion";
    public const string GroupCivilizationKey = "civilization";
    public const string GroupPersonalKey = "personal";

    /// <summary>
    ///     Input snapshot. Kept primitive so tests don't have to construct a
    ///     <c>GuiDialogManager</c>. Production code builds one of these from the
    ///     manager via <see cref="ContextFromManager" />.
    /// </summary>
    public readonly record struct Context(
        bool HasReligion,
        bool HasCivilization,
        bool IsCivilizationFounder,
        bool IsReligionFounder,
        int ReligionInviteCount,
        int CivilizationInviteCount,
        int UnreadNotificationCount,
        SidebarNavId CurrentNav,
        IReadOnlyDictionary<string, bool>? CollapsedGroups,
        bool IsSidebarCollapsed = false
    );

    /// <summary>
    ///     Build a <see cref="SidebarViewModel" /> from a context snapshot.
    ///     Conditional items render as disabled-with-tooltip rather than absent —
    ///     this is the visibility contract for the new sidebar.
    /// </summary>
    public static SidebarViewModel BuildViewModel(Context ctx)
    {
        var groups = new List<SidebarGroupViewModel>(3)
        {
            BuildPersonalGroup(ctx),
            BuildReligionGroup(ctx),
            BuildCivilizationGroup(ctx)
        };

        return new SidebarViewModel(ctx.IsSidebarCollapsed, ctx.CurrentNav, groups);
    }

    /// <summary>
    ///     Commit a sidebar click. <see cref="SidebarState.CurrentNav" /> is the
    ///     single source of truth for the active destination; the layout
    ///     coordinator dispatches content based on it directly.
    /// </summary>
    public static void Apply(SidebarNavId nav, GuiDialogState state)
    {
        state.Sidebar.CurrentNav = nav;
    }

    /// <summary>
    ///     Resolve the nav id the codex should land on when reopened. Honours the
    ///     player's last-visited page, but falls back to <see cref="SidebarNavId.PlayerInfo" />
    ///     when that page is disabled for the current player — e.g. a Civilization
    ///     page restored after they left the civilization (#474). PlayerInfo is
    ///     always reachable.
    /// </summary>
    public static SidebarNavId ResolveRestoreNav(SidebarNavId desired, Context ctx)
    {
        var vm = BuildViewModel(ctx);
        foreach (var group in vm.Groups)
        foreach (var item in group.Items)
            if (item.Id == desired)
                return item.IsDisabled ? SidebarNavId.PlayerInfo : desired;

        return SidebarNavId.PlayerInfo;
    }

    /// <summary>
    ///     Convenience adapter — production builds the context off the live
    ///     <c>GuiDialogManager</c>. Kept here so the mapper is the single place
    ///     that knows the manager surface; renderers only see the view model.
    /// </summary>
    public static Context ContextFromManager(GuiDialogManager manager, SidebarState sidebar)
    {
        var hasReligion = manager.HasReligion();
        var hasCivilization = manager.HasCivilization();
        var religionInvites = manager.ReligionStateManager.State.InvitesState.MyInvites?.Count ?? 0;
        var civInvites = manager.CivilizationManager.InviteState.MyInvites?.Count ?? 0;
        var isReligionFounder = manager.ReligionStateManager.State.InfoState.MyReligionInfo?.IsFounder ?? false;
        var unread = 0;
        var history = manager.NotificationManager.State.History;
        for (var i = 0; i < history.Count; i++)
            if (!history[i].Read) unread++;

        return new Context(
            hasReligion,
            hasCivilization,
            manager.IsCivilizationFounder,
            isReligionFounder,
            religionInvites,
            civInvites,
            unread,
            sidebar.CurrentNav,
            sidebar.CollapsedGroups,
            sidebar.IsCollapsed);
    }

    private static SidebarGroupViewModel BuildReligionGroup(Context ctx)
    {
        var items = new List<SidebarItemViewModel>(8)
        {
            // Browse — always reachable; renders the religion list.
            Item(SidebarNavId.ReligionBrowse,
                LocalizationKeys.UI_RELIGION_TAB_BROWSE, "browse",
                ctx, isDisabled: false, disabledKey: null),
            // Info — needs a religion to make sense.
            Item(SidebarNavId.ReligionInfo,
                LocalizationKeys.UI_RELIGION_TAB_INFO, "info",
                ctx, !ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION),
            // Roster — same gate.
            Item(SidebarNavId.ReligionRoster,
                LocalizationKeys.UI_RELIGION_TAB_ROSTER, "info",
                ctx, !ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION),
            // Vows — communal blessing tree; viewable by any member, founder-gated at the unlock action.
            Item(SidebarNavId.ReligionVows,
                LocalizationKeys.UI_RELIGION_TAB_VOWS, "meditation",
                ctx, !ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION),
            // Activity log — same gate.
            Item(SidebarNavId.ReligionActivity,
                LocalizationKeys.UI_RELIGION_TAB_ACTIVITY, "activity",
                ctx, !ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION),
            // Chronicle — permanent narrative history; same gate (#373).
            Item(SidebarNavId.ReligionChronicle,
                LocalizationKeys.UI_RELIGION_TAB_CHRONICLE, "church",
                ctx, !ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION),
            // Roles — needs religion AND founder permission.
            Item(SidebarNavId.ReligionRoles,
                LocalizationKeys.UI_RELIGION_TAB_ROLES, "roles",
                ctx,
                !ctx.HasReligion || !ctx.IsReligionFounder,
                !ctx.HasReligion ? LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION
                                 : LocalizationKeys.SIDEBAR_DISABLED_FOUNDER_ONLY),
            // Invites — only when player is religion-less, badge with count.
            Item(SidebarNavId.ReligionInvites,
                LocalizationKeys.UI_RELIGION_TAB_INVITES, "invites",
                ctx, ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_RELIGION,
                badge: ctx.ReligionInviteCount),
            // Create — same as invites (mutually exclusive with membership).
            Item(SidebarNavId.ReligionCreate,
                LocalizationKeys.UI_RELIGION_TAB_CREATE, "create",
                ctx, ctx.HasReligion, LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_RELIGION)
        };

        var collapsed = ctx.CollapsedGroups != null
                        && ctx.CollapsedGroups.TryGetValue(GroupReligionKey, out var c) && c;
        return new SidebarGroupViewModel(GroupReligionKey,
            LocalizationService.Instance.Get(LocalizationKeys.SIDEBAR_GROUP_RELIGION), collapsed, items);
    }

    private static SidebarGroupViewModel BuildCivilizationGroup(Context ctx)
    {
        var items = new List<SidebarItemViewModel>(10)
        {
            // Browse — always reachable.
            Item(SidebarNavId.CivilizationBrowse,
                LocalizationKeys.UI_CIVILIZATION_TAB_BROWSE, "browse",
                ctx, isDisabled: false, disabledKey: null),
            // Standing of Realms leaderboard — visible to all players, even those
            // with no civilization (you can survey the world stage from outside).
            Item(SidebarNavId.CivilizationLeaderboard,
                LocalizationKeys.UI_CIVILIZATION_TAB_LEADERBOARD, "achievement",
                ctx, isDisabled: false, disabledKey: null),
            Item(SidebarNavId.CivilizationInfo,
                LocalizationKeys.UI_CIVILIZATION_TAB_INFO, "info",
                ctx, !ctx.HasCivilization, LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION),
            // Invites — only when player has a religion but no civ membership.
            Item(SidebarNavId.CivilizationInvites,
                LocalizationKeys.UI_CIVILIZATION_TAB_INVITES, "invites",
                ctx,
                !ctx.HasReligion || ctx.HasCivilization,
                !ctx.HasReligion ? LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION
                                 : LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_CIVILIZATION,
                badge: ctx.CivilizationInviteCount),
            Item(SidebarNavId.CivilizationCreate,
                LocalizationKeys.UI_CIVILIZATION_TAB_CREATE, "create",
                ctx,
                !ctx.HasReligion || ctx.HasCivilization,
                !ctx.HasReligion ? LocalizationKeys.SIDEBAR_DISABLED_NEED_RELIGION
                                 : LocalizationKeys.SIDEBAR_DISABLED_ALREADY_IN_CIVILIZATION),
            Item(SidebarNavId.CivilizationDiplomacy,
                LocalizationKeys.UI_CIVILIZATION_TAB_DIPLOMACY, "diplomacy",
                ctx, !ctx.HasCivilization, LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION),
            // Propose Accord — founder-only authoring page, sibling of Accords (#330).
            // Disabled to non-founders; server still enforces founder gate.
            Item(SidebarNavId.CivilizationProposeAccord,
                LocalizationKeys.UI_CIVILIZATION_TAB_PROPOSE_ACCORD, "create",
                ctx,
                !ctx.HasCivilization || !ctx.IsCivilizationFounder,
                !ctx.HasCivilization
                    ? LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION
                    : LocalizationKeys.SIDEBAR_DISABLED_FOUNDER_ONLY),
            Item(SidebarNavId.CivilizationHolySites,
                LocalizationKeys.UI_CIVILIZATION_TAB_HOLYSITES, "holysite",
                ctx, !ctx.HasCivilization, LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION),
            Item(SidebarNavId.CivilizationMilestones,
                LocalizationKeys.UI_CIVILIZATION_TAB_MILESTONES, "achievement",
                ctx, !ctx.HasCivilization, LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION),
            // Chronicle — permanent narrative history of the realm; same gate.
            Item(SidebarNavId.CivilizationChronicle,
                LocalizationKeys.UI_CIVILIZATION_TAB_CHRONICLE, "activity",
                ctx, !ctx.HasCivilization, LocalizationKeys.SIDEBAR_DISABLED_NEED_CIVILIZATION)
        };

        var collapsed = ctx.CollapsedGroups != null
                        && ctx.CollapsedGroups.TryGetValue(GroupCivilizationKey, out var c) && c;
        return new SidebarGroupViewModel(GroupCivilizationKey,
            LocalizationService.Instance.Get(LocalizationKeys.SIDEBAR_GROUP_CIVILIZATION), collapsed, items);
    }

    private static SidebarGroupViewModel BuildPersonalGroup(Context ctx)
    {
        var items = new List<SidebarItemViewModel>(2)
        {
            // "You" — player identity readout + notification feed; always reachable.
            Item(SidebarNavId.PlayerInfo,
                LocalizationKeys.UI_TAB_PLAYER_INFO, "info",
                ctx, isDisabled: false, disabledKey: null,
                badge: ctx.UnreadNotificationCount),
            // Blessings is always reachable; the per-deity gating happens inside
            // the content pane via DeitySelectorRenderer.
            Item(SidebarNavId.Blessings,
                LocalizationKeys.UI_TAB_BLESSINGS, "meditation",
                ctx, isDisabled: false, disabledKey: null)
        };

        var collapsed = ctx.CollapsedGroups != null
                        && ctx.CollapsedGroups.TryGetValue(GroupPersonalKey, out var c) && c;
        return new SidebarGroupViewModel(GroupPersonalKey,
            LocalizationService.Instance.Get(LocalizationKeys.SIDEBAR_GROUP_PERSONAL), collapsed, items);
    }

    private static SidebarItemViewModel Item(SidebarNavId id, string labelKey, string iconName,
        Context ctx, bool isDisabled, string? disabledKey, int badge = 0)
    {
        return new SidebarItemViewModel(
            id,
            LocalizationService.Instance.Get(labelKey),
            iconName,
            badge,
            IsActive: ctx.CurrentNav == id,
            IsDisabled: isDisabled,
            DisabledTooltipKey: isDisabled ? disabledKey : null);
    }
}
