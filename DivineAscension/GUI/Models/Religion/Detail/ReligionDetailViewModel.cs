using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Detail;

/// <summary>
///     Immutable view model for the religion detail chapter opened from the
///     "Other Orders" browse list. Mirrors the ledger framing of the "This
///     Order" chapter (#309) — serif title, prose intro, dotted-leader stat
///     block, prose description, and a read-only roster.
/// </summary>
public readonly struct ReligionDetailViewModel(
    bool isLoading,
    string religionUID,
    string religionName,
    string deity,
    string deityName,
    string founderUID,
    string founderName,
    string prestigeRank,
    int prestigeRankIndex,
    int prestige,
    int prestigeRequired,
    bool isMaxPrestigeRank,
    bool isPublic,
    string description,
    IReadOnlyList<ReligionDetailResponsePacket.MemberInfo> members,
    float memberScrollY,
    bool canJoin,
    float x,
    float y,
    float width,
    float height)
{
    public bool IsLoading { get; } = isLoading;
    public string ReligionUID { get; } = religionUID;
    public string ReligionName { get; } = religionName;

    /// <summary>The domain (Craft, Wild, Harvest, Stone, Conquest).</summary>
    public string Deity { get; } = deity;

    /// <summary>The custom deity name for this religion.</summary>
    public string DeityName { get; } = deityName;

    public string FounderUID { get; } = founderUID;
    public string FounderName { get; } = founderName;

    public string PrestigeRank { get; } = prestigeRank;
    public int PrestigeRankIndex { get; } = prestigeRankIndex;
    public int Prestige { get; } = prestige;
    public int PrestigeRequired { get; } = prestigeRequired;
    public bool IsMaxPrestigeRank { get; } = isMaxPrestigeRank;
    public bool IsPublic { get; } = isPublic;
    public string Description { get; } = description;
    public IReadOnlyList<ReligionDetailResponsePacket.MemberInfo> Members { get; } = members;
    public float MemberScrollY { get; } = memberScrollY;
    public bool CanJoin { get; } = canJoin;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    public int MemberCount => Members?.Count ?? 0;

    public float PrestigeProgressPercentage =>
        IsMaxPrestigeRank
            ? 1f
            : PrestigeRequired > 0 ? Math.Clamp((float)Prestige / PrestigeRequired, 0f, 1f) : 0f;

    /// <summary>
    ///     Returns the founder's cached display name, falling back to UID if
    ///     a name wasn't sent in the detail packet.
    /// </summary>
    public string GetFounderDisplayName()
    {
        return string.IsNullOrWhiteSpace(FounderName) ? FounderUID : FounderName;
    }
}
