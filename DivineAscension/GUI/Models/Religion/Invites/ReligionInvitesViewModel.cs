using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Models.Religion.Invites;

/// <summary>
/// Immutable view model for the Letters chapter (religion invites).
/// </summary>
public readonly struct ReligionInvitesViewModel(
    IReadOnlyList<InviteData> invites,
    bool isLoading,
    float scrollY,
    float x,
    float y,
    float width,
    float height)
{
    public IReadOnlyList<InviteData> Invites { get; } = invites;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;

    // Layout
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    public bool HasInvites => Invites.Count > 0;
}

/// <summary>
/// Simplified invite data for rendering. Decoupled from the network packet.
/// </summary>
public readonly struct InviteData(
    string inviteId,
    string religionName,
    DateTime expiresAt,
    DeityDomain domain)
{
    public string InviteId { get; } = inviteId;
    public string ReligionName { get; } = religionName;
    public DateTime ExpiresAt { get; } = expiresAt;
    public DeityDomain Domain { get; } = domain;

    public string FormattedExpiration => $"Expires: {ExpiresAt:yyyy-MM-dd HH:mm}";
}
