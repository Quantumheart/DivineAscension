using DivineAscension.GUI.State;

namespace DivineAscension.GUI.Models.Civilization.Tab;

public readonly struct CivilizationTabViewModel(
    SidebarNavId currentNav,
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
    public SidebarNavId CurrentNav { get; } = currentNav;
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

    public bool ShowInfoTab => HasCivilization;
    public bool ShowInvitesTab => HasReligion && !HasCivilization;
    public bool ShowCreateTab => HasReligion && !HasCivilization;
    public bool ShowDiplomacyTab => HasCivilization;
    public bool ShowHolySitesTab => HasCivilization;
    public bool ShowMilestonesTab => HasCivilization;
}
