using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Browse;

public readonly struct ReligionBrowseViewModel(
    string[] domainFilters,
    string currentDomainFilter,
    IReadOnlyList<ReligionListResponsePacket.ReligionInfo> religions,
    bool isLoading,
    float scrollY,
    string? selectedReligionUID,
    bool userHasReligion,
    float x,
    float y,
    float width,
    float height)
{
    public string[] DomainFilters { get; } = domainFilters;
    public string CurrentDomainFilter { get; } = currentDomainFilter;
    public IReadOnlyList<ReligionListResponsePacket.ReligionInfo> Religions { get; } = religions;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;
    public string? SelectedReligionUID { get; } = selectedReligionUID;
    public bool UserHasReligion { get; } = userHasReligion;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods (no side effects)
    public int GetCurrentFilterIndex() =>
        Array.IndexOf(DomainFilters, CurrentDomainFilter);

    public bool CanJoinReligion =>
        !string.IsNullOrEmpty(SelectedReligionUID);
}