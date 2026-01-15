using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Activity;

public readonly struct ReligionActivityViewModel
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public List<ActivityLogResponsePacket.ActivityEntry> Entries { get; }
    public float ScrollY { get; }
    public bool IsLoading { get; }
    public string? ErrorMessage { get; }

    public ReligionActivityViewModel(float x, float y, float width, float height,
        List<ActivityLogResponsePacket.ActivityEntry> entries,
        float scrollY, bool isLoading, string? errorMessage = null)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Entries = entries;
        ScrollY = scrollY;
        IsLoading = isLoading;
        ErrorMessage = errorMessage;
    }
}