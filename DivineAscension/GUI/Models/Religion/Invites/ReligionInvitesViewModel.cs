using System;
using System.Collections.Generic;

namespace DivineAscension.GUI.Models.Religion.Invites;

/// <summary>
/// Immutable view model for religion invites display
/// Contains only the data needed to render the invites list
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

    /// <summary>
    /// Checks if there are any invites to display
    /// </summary>
    public bool HasInvites => Invites.Count > 0;

    /// <summary>
    /// Gets display message for empty state
    /// </summary>
    public string EmptyStateMessage => IsLoading
        ? "Loading invitations..."
        : "No pending invitations.";
}

/// <summary>
/// Simplified invite data for rendering
/// Decouples from network packet structure
/// </summary>
public readonly struct InviteData(
    string inviteId,
    string religionName,
    DateTime expiresAt)
{
    public string InviteId { get; } = inviteId;
    public string ReligionName { get; } = religionName;
    public DateTime ExpiresAt { get; } = expiresAt;

    public string FormattedExpiration => $"Expires: {ExpiresAt:yyyy-MM-dd HH:mm}";
}