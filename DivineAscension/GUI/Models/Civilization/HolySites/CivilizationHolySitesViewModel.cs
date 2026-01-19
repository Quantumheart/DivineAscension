using System.Collections.Generic;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.Models.Civilization.HolySites;

public readonly struct CivilizationHolySitesViewModel(
    Dictionary<string, List<HolySiteResponsePacket.HolySiteInfo>> sitesByReligion,
    Dictionary<string, string> religionNames,
    Dictionary<string, string> religionDomains,
    HashSet<string> expandedReligions,
    bool isLoading,
    string? errorMsg,
    float scrollY,
    float x,
    float y,
    float width,
    float height)
{
    public Dictionary<string, List<HolySiteResponsePacket.HolySiteInfo>> SitesByReligion { get; } = sitesByReligion;
    public Dictionary<string, string> ReligionNames { get; } = religionNames;
    public Dictionary<string, string> ReligionDomains { get; } = religionDomains;
    public HashSet<string> ExpandedReligions { get; } = expandedReligions;
    public bool IsLoading { get; } = isLoading;
    public string? ErrorMsg { get; } = errorMsg;
    public float ScrollY { get; } = scrollY;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
}
