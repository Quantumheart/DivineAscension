using DivineAscension.Network;

namespace DivineAscension.GUI.State.Religion;

/// <summary>
///     Mutable state for the Roles tab within the Religion dialog.
///     Manages role data and coordinates between browse and detail sub-states.
/// </summary>
public class RolesState
{
    /// <summary>
    ///     Shared role data loaded from the server.
    /// </summary>
    public ReligionRolesResponse? RolesData { get; set; }

    /// <summary>
    ///     Whether role data is currently being loaded.
    /// </summary>
    public bool Loading { get; set; }

    /// <summary>
    ///     State for the roles browse view (role cards list).
    /// </summary>
    public RolesBrowseState BrowseState { get; } = new();

    /// <summary>
    ///     State for the role detail view (viewing members with a specific role).
    /// </summary>
    public RoleDetailState DetailState { get; } = new();

    public void Reset()
    {
        RolesData = null;
        Loading = false;
        BrowseState.Reset();
        DetailState.Reset();
    }
}