using DivineAscension.GUI.State.Religion;

namespace DivineAscension.GUI.Models.Religion.Tab;

public readonly struct ReligionTabViewModel(
    SubTab currentSubTab,
    ErrorState errorState,
    bool hasReligion,
    float x,
    float y,
    float width,
    float height)
{
    public SubTab CurrentSubTab { get; } = currentSubTab;
    public ErrorState ErrorState { get; } = errorState;
    public bool HasReligion { get; } = hasReligion;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Tab visibility based on religion membership
    public bool ShowInfoTab => HasReligion;
    public bool ShowActivityTab => HasReligion;
    public bool ShowRolesTab => HasReligion;
    public bool ShowInvitesTab => !HasReligion;
    public bool ShowCreateTab => !HasReligion;
}