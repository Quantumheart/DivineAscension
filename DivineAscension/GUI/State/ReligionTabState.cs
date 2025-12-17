using DivineAscension.GUI.State.Religion;

namespace DivineAscension.GUI.State;

/// <summary>
///     State container for the Religion tab in BlessingDialog
///     Follows the same pattern as CivilizationState
/// </summary>
public class ReligionTabState
{
    private readonly ActivityState _activityState = new ActivityState();

    // Tab navigation
    public SubTab CurrentSubTab { get; set; } // 0=Browse, 1=Religion Info, 2=Activity, 3=Create, 4=Activity, 5=Roles

    // Error handling

    public CreateState CreateState { get; } = new();

    public BrowseState BrowseState { get; } = new();

    public InfoState InfoState { get; } = new InfoState();

    public InvitesState InvitesState { get; } = new InvitesState();

    public RolesState RolesState { get; } = new();

    public ErrorState ErrorState { get; } = new ErrorState();

    /// <summary>
    ///     Reset all state to default values
    /// </summary>
    public void Reset()
    {
        CurrentSubTab = SubTab.Browse;
        BrowseState.Reset();
        InfoState.Reset();
        InvitesState.Reset();
        RolesState.Reset();
        CreateState.Reset();

        // _activityState.ActivityLog.Clear();
        _activityState.Reset();
        ErrorState.Reset();
    }
}