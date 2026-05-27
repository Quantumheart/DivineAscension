using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Chronicle;

/// <summary>
/// Immutable view model for the Religion Chronicle chapter (#373). Reuses the
/// chronicle carried on <see cref="PlayerReligionInfoResponsePacket" /> — the
/// chapter is a read-only, oldest-first ledger of significant events.
/// </summary>
public readonly struct ReligionChronicleViewModel(
    bool isLoading,
    bool hasReligion,
    string religionName,
    string deity,
    IReadOnlyList<PlayerReligionInfoResponsePacket.ChronicleEntryDto> chronicle,
    float x,
    float y,
    float width,
    float height,
    float scrollY)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public string ReligionName { get; } = religionName;

    /// <summary>The patron domain string (Craft, Wild, …) — drives the chapter glyph.</summary>
    public string Deity { get; } = deity;

    public IReadOnlyList<PlayerReligionInfoResponsePacket.ChronicleEntryDto> Chronicle { get; } = chronicle;
    public bool HasChronicle => Chronicle is { Count: > 0 };

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;

    public static ReligionChronicleViewModel Loading(float x = 0, float y = 0, float width = 0, float height = 0) =>
        new(true, false, string.Empty, string.Empty,
            Array.Empty<PlayerReligionInfoResponsePacket.ChronicleEntryDto>(),
            x, y, width, height, 0);
}
