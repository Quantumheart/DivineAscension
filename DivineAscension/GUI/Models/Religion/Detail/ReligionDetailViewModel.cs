using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Detail;

/// <summary>
///     Immutable view model for religion detail view
/// </summary>
public readonly struct ReligionDetailViewModel(
    bool isLoading,
    string religionUID,
    string religionName,
    string deity,
    string prestigeRank,
    int prestige,
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
    public string Deity { get; } = deity;
    public string PrestigeRank { get; } = prestigeRank;
    public int Prestige { get; } = prestige;
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
}