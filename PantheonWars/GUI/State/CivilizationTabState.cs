using PantheonWars.GUI.State.Civilization;

namespace PantheonWars.GUI.State;

/// <summary>
///     Holds all client-side state for the Civilization tab within the BlessingDialog.
/// </summary>
public class CivilizationTabState
{
    public CivilizationSubTab CurrentSubTab { get; set; } = CivilizationSubTab.Browse;

    public BrowseState BrowseState { get; } = new();

    public DetailState DetailState { get; } = new();

    public InfoState InfoState { get; } = new();

    public InviteState InviteState { get; } = new();

    public CreateState CreateState { get; } = new();

    // Error messages (null = no error)
    public string? LastActionError { get; set; }
    public string? CreateError { get; set; }

    // Confirmation flags
    public bool ShowDisbandConfirm { get; set; }
    public string? KickConfirmReligionId { get; set; }


    /// <summary>
    ///     Reset the entire civilization state to defaults.
    /// </summary>
    public void Reset()
    {
        CurrentSubTab = 0;
        BrowseState.Reset();
        DetailState.Reset();
        InfoState.Reset();
        InviteState.Reset();
        CreateState.Reset();

        // Errors
        LastActionError = null;
        CreateError = null;

        // Confirmations
        ShowDisbandConfirm = false;
        KickConfirmReligionId = null;
    }
}

public enum CivilizationSubTab
{
    Browse,
    MyCiv,
    Invites,
    Create
}