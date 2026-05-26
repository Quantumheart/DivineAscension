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
    DeityDomain domain,
    string description = "")
{
    /// <summary>
    ///     Longest description rendered on the single-line envelope quote before
    ///     it is truncated with an ellipsis. Descriptions are capped at 200 chars
    ///     server-side; this keeps the quote inside one row at typical pane widths.
    /// </summary>
    private const int MaxQuoteChars = 100;

    public string InviteId { get; } = inviteId;
    public string ReligionName { get; } = religionName;
    public DateTime ExpiresAt { get; } = expiresAt;
    public DeityDomain Domain { get; } = domain;
    public string Description { get; } = description;

    public string FormattedExpiration => $"Expires: {ExpiresAt:yyyy-MM-dd HH:mm}";

    /// <summary>
    ///     Builds the quoted line shown under the sender. Uses the inviting
    ///     religion's description (collapsed to one line, truncated, wrapped in
    ///     quotation marks) when set, otherwise the supplied default quote.
    /// </summary>
    public string BuildQuoteLine(string defaultQuote)
    {
        if (string.IsNullOrWhiteSpace(Description))
            return defaultQuote;

        var collapsed = string.Join(' ', Description.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (collapsed.Length > MaxQuoteChars)
            collapsed = collapsed.Substring(0, MaxQuoteChars).TrimEnd() + "…";

        return $"\"{collapsed}\"";
    }
}
