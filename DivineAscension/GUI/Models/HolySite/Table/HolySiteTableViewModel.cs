using System.Collections.Generic;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.Models.HolySite.Table;

/// <summary>
///     View model for holy site table rendering.
/// </summary>
internal readonly struct HolySiteTableViewModel(
    float x,
    float y,
    float width,
    float height,
    List<HolySiteResponsePacket.HolySiteInfo> sites,
    string? selectedSiteUID,
    bool isLoading,
    float scrollY)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public List<HolySiteResponsePacket.HolySiteInfo> Sites { get; } = sites;
    public string? SelectedSiteUID { get; } = selectedSiteUID;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;
}
