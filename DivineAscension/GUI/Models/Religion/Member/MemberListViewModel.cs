using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Member;

/// <summary>
/// Immutable view model for the member list component.
/// </summary>
public readonly struct MemberListViewModel(
    float x,
    float y,
    float width,
    float height,
    float scrollY,
    IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> members,
    string currentPlayerUID)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;
    public IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> Members { get; } = members;
    public string CurrentPlayerUID { get; } = currentPlayerUID;
}