using PantheonWars.GUI.State;

namespace PantheonWars.GUI.Models.Civilization.Tab;

public readonly struct CivilizationTabViewModel(
    CivilizationSubTab currentSubTab,
    string? lastActionError,
    string? browseError,
    string? infoError,
    string? invitesError,
    bool isViewingDetails,
    bool hasReligion,
    bool hasCivilization,
    float x,
    float y,
    float width,
    float height)
{
    public CivilizationSubTab CurrentSubTab { get; } = currentSubTab;
    public string? LastActionError { get; } = lastActionError;
    public string? BrowseError { get; } = browseError;
    public string? InfoError { get; } = infoError;
    public string? InvitesError { get; } = invitesError;
    public bool IsViewingDetails { get; } = isViewingDetails;
    public bool HasReligion { get; } = hasReligion;
    public bool HasCivilization { get; } = hasCivilization;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods (no side effects)
    public bool ShowInvitesTab => HasReligion; // Only religion founders see Invites tab
    public bool ShowCreateTab => HasReligion && !HasCivilization; // Only show if user has religion but no civilization
}