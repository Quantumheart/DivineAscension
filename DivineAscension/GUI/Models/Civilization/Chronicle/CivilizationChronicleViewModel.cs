using System;
using System.Collections.Generic;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Chronicle;

/// <summary>
/// Immutable view model for the Civilization Chronicle chapter (#369). Reuses the
/// chronicle carried on <see cref="CivilizationInfoResponsePacket" /> — the chapter
/// is a read-only, oldest-first ledger of significant events.
/// </summary>
public readonly struct CivilizationChronicleViewModel(
    bool isLoading,
    bool hasCivilization,
    string civilizationName,
    IReadOnlyList<CivilizationInfoResponsePacket.ChronicleEntryDto> chronicle,
    float x,
    float y,
    float width,
    float height,
    float scrollY)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasCivilization { get; } = hasCivilization;
    public string CivilizationName { get; } = civilizationName;

    public IReadOnlyList<CivilizationInfoResponsePacket.ChronicleEntryDto> Chronicle { get; } = chronicle;
    public bool HasChronicle => Chronicle is { Count: > 0 };

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;

    public static CivilizationChronicleViewModel Loading(float x = 0, float y = 0, float width = 0, float height = 0) =>
        new(true, false, string.Empty,
            Array.Empty<CivilizationInfoResponsePacket.ChronicleEntryDto>(),
            x, y, width, height, 0);
}
