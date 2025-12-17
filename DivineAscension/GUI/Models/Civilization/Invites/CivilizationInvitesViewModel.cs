using System.Collections.Generic;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Invites;

public readonly struct CivilizationInvitesViewModel(
    IReadOnlyList<CivilizationInfoResponsePacket.PendingInvite> invites,
    bool isLoading,
    float scrollY,
    float x,
    float y,
    float width,
    float height)
{
    public IReadOnlyList<CivilizationInfoResponsePacket.PendingInvite> Invites { get; } = invites;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods (no side effects)
    public bool HasInvites => Invites?.Count > 0;
}