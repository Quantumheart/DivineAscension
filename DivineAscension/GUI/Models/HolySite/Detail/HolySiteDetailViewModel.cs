using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.Models.HolySite.Detail;

/// <summary>
/// ViewModel for the holy sites detail view (selected site details)
/// </summary>
public readonly struct HolySiteDetailViewModel(
    float x,
    float y,
    float width,
    float height,
    HolySiteResponsePacket.HolySiteDetailInfo siteDetails,
    string currentPlayerUID,
    bool isEditingName,
    string? editingNameValue,
    bool isEditingDescription,
    string? editingDescriptionValue,
    bool isLoading,
    string? errorMsg)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public HolySiteResponsePacket.HolySiteDetailInfo SiteDetails { get; } = siteDetails;
    public string CurrentPlayerUID { get; } = currentPlayerUID;
    public bool IsEditingName { get; } = isEditingName;
    public string? EditingNameValue { get; } = editingNameValue;
    public bool IsEditingDescription { get; } = isEditingDescription;
    public string? EditingDescriptionValue { get; } = editingDescriptionValue;
    public bool IsLoading { get; } = isLoading;
    public string? ErrorMsg { get; } = errorMsg;

    /// <summary>
    /// Check if the current player is the consecrator (founder) of this holy site
    /// </summary>
    public bool IsConsecrator => SiteDetails.FounderUID == CurrentPlayerUID;
}
