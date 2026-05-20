using DivineAscension.GUI.State.Religion;

namespace DivineAscension.GUI.State;

/// <summary>
///     State container for the Religion tab in BlessingDialog. Sub-tab nav
///     state lives on the sidebar (<see cref="SidebarState.CurrentNav" />).
/// </summary>
public class ReligionTabState
{
    public CreateState CreateState { get; } = new();

    public BrowseState BrowseState { get; } = new();

    public InfoState InfoState { get; } = new InfoState();

    public ActivityState ActivityState { get; } = new ActivityState();

    public InvitesState InvitesState { get; } = new InvitesState();

    public RolesState RolesState { get; } = new();

    public ErrorState ErrorState { get; } = new ErrorState();

    public void Reset()
    {
        BrowseState.Reset();
        InfoState.Reset();
        InvitesState.Reset();
        RolesState.Reset();
        CreateState.Reset();
        ActivityState.Reset();
        ErrorState.Reset();
    }
}