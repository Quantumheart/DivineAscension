using System;
using System.Collections.Generic;
using PantheonWars.Network;

namespace PantheonWars.GUI.Models.Religion.Browse;

public readonly struct ReligionBrowseViewModel(
    string[] deityFilters,
    string currentDeityFilter,
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
    public string[] DeityFilters { get; } = deityFilters;
    public string CurrentDeityFilter { get; } = currentDeityFilter;
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
        Array.IndexOf(DeityFilters, CurrentDeityFilter);

    public bool CanJoinReligion =>
        !string.IsNullOrEmpty(SelectedReligionUID);
}