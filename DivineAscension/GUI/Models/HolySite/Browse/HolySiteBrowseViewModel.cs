using System.Collections.Generic;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.Models.HolySite.Browse;

/// <summary>
/// ViewModel for the holy sites browse view (table display)
/// </summary>
public readonly struct HolySiteBrowseViewModel(
    float x,
    float y,
    float width,
    float height,
    Dictionary<string, List<HolySiteResponsePacket.HolySiteInfo>> sitesByReligion,
    Dictionary<string, string> religionNames,
    Dictionary<string, string> religionDomains,
    string? selectedSiteUID,
    bool isLoading,
    string? errorMsg,
    float scrollY)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public Dictionary<string, List<HolySiteResponsePacket.HolySiteInfo>> SitesByReligion { get; } = sitesByReligion;
    public Dictionary<string, string> ReligionNames { get; } = religionNames;
    public Dictionary<string, string> ReligionDomains { get; } = religionDomains;
    public string? SelectedSiteUID { get; } = selectedSiteUID;
    public bool IsLoading { get; } = isLoading;
    public string? ErrorMsg { get; } = errorMsg;
    public float ScrollY { get; } = scrollY;
}
