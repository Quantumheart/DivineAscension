namespace DivineAscension.GUI.State;

/// <summary>
///     Flat navigation identifier for the upcoming sidebar layout. Each value names
///     one selectable destination (master pane). Routing wiring lands in Phase 3 of
///     the UI refactor; defined now because <see cref="SidebarState.CurrentNav" />
///     needs a type.
/// </summary>
public enum SidebarNavId
{
    ReligionInfo = 0,
    ReligionRoster,
    ReligionActivity,
    ReligionRoles,
    ReligionInvites,
    ReligionCreate,
    ReligionBrowse,
    PlayerInfo,
    Blessings,
    CivilizationInfo,
    CivilizationInvites,
    CivilizationCreate,
    CivilizationDiplomacy,
    CivilizationProposeAccord,
    CivilizationHolySites,
    CivilizationMilestones,
    CivilizationBrowse,
    ReligionVows,
    CivilizationLeaderboard,
    ReligionChronicle
}
