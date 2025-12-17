using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.List;

public readonly struct ReligionListViewModel(
    IReadOnlyList<ReligionListResponsePacket.ReligionInfo> religions,
    bool isLoading,
    float scrollY,
    string? selectedReligionUID,
    float x,
    float y,
    float width,
    float height)
{
    public IReadOnlyList<ReligionListResponsePacket.ReligionInfo> Religions { get; } = religions;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;
    public string? SelectedReligionUID { get; } = selectedReligionUID;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
}