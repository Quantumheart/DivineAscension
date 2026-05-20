using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Utilities;

namespace DivineAscension.GUI.UI.Renderers.EdgeBookmarks;

/// <summary>
///     Build the right-edge bookmark stack from manager flags + the current
///     nav. One ribbon per main section: Religion (R), Blessings (B),
///     Civilization (C), Help (?). Each ribbon's default jump target adapts
///     to membership: if the player has no religion, R jumps to Browse; same
///     for Civilization.
/// </summary>
public static class EdgeBookmarkMapper
{
    public readonly record struct Context(
        bool HasReligion,
        bool HasCivilization,
        SidebarNavId CurrentNav);

    public static EdgeBookmarkRibbonStack BuildViewModel(Context ctx)
    {
        var bookmarks = new List<EdgeBookmarkViewModel>(4)
        {
            new(
                Stamp: "R",
                Tooltip: "Religion",
                RibbonColor: DomainHelper.GetDomainColor("Craft"),
                Target: ctx.HasReligion ? SidebarNavId.ReligionInfo : SidebarNavId.ReligionBrowse,
                IsActive: IsReligionNav(ctx.CurrentNav),
                IsDisabled: false),
            new(
                Stamp: "B",
                Tooltip: "Blessings",
                RibbonColor: DomainHelper.GetDomainColor("Harvest"),
                Target: SidebarNavId.Blessings,
                IsActive: ctx.CurrentNav == SidebarNavId.Blessings,
                IsDisabled: false),
            new(
                Stamp: "C",
                Tooltip: "Civilization",
                RibbonColor: DomainHelper.GetDomainColor("Stone"),
                Target: ctx.HasCivilization ? SidebarNavId.CivilizationInfo : SidebarNavId.CivilizationBrowse,
                IsActive: IsCivilizationNav(ctx.CurrentNav),
                IsDisabled: false),
            new(
                Stamp: "?",
                Tooltip: "Help (coming soon)",
                RibbonColor: ColorPalette.Grey,
                Target: ctx.CurrentNav,
                IsActive: false,
                IsDisabled: true)
        };
        return new EdgeBookmarkRibbonStack(bookmarks);
    }

    private static bool IsReligionNav(SidebarNavId nav) => nav is
        SidebarNavId.ReligionBrowse or
        SidebarNavId.ReligionInfo or
        SidebarNavId.ReligionActivity or
        SidebarNavId.ReligionRoles or
        SidebarNavId.ReligionInvites or
        SidebarNavId.ReligionCreate;

    private static bool IsCivilizationNav(SidebarNavId nav) => nav is
        SidebarNavId.CivilizationBrowse or
        SidebarNavId.CivilizationInfo or
        SidebarNavId.CivilizationInvites or
        SidebarNavId.CivilizationCreate or
        SidebarNavId.CivilizationDiplomacy or
        SidebarNavId.CivilizationHolySites or
        SidebarNavId.CivilizationMilestones;
}
