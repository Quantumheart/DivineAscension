using System;
using System.Collections.Generic;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Browse;

public readonly struct CivilizationBrowseViewModel(
    string[] deityFilters,
    string currentDeityFilter,
    IReadOnlyList<CivilizationListResponsePacket.CivilizationInfo> civilizations,
    bool isLoading,
    float scrollY,
    bool isDeityDropDownOpen,
    bool userHasReligion,
    bool userInCivilization,
    float x,
    float y,
    float width,
    float height)
{
    public string[] DeityFilters { get; } = deityFilters;
    public string CurrentDeityFilter { get; } = currentDeityFilter;
    public IReadOnlyList<CivilizationListResponsePacket.CivilizationInfo> Civilizations { get; } = civilizations;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;
    public bool IsDeityDropDownOpen { get; } = isDeityDropDownOpen;
    public bool UserHasReligion { get; } = userHasReligion;
    public bool UserInCivilization { get; } = userInCivilization;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods
    public int GetCurrentFilterIndex()
    {
        return Array.IndexOf(DeityFilters, CurrentDeityFilter);
    }
}