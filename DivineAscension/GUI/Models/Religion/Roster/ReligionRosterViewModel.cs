using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Roster;

/// <summary>
/// Immutable view model for the Roster chapter (II.ii — issue #313).
/// Built from the same PlayerReligionInfoResponsePacket that feeds II.i,
/// plus RosterState for invite input and per-row expansion.
/// </summary>
public readonly struct ReligionRosterViewModel(
    bool isLoading,
    bool hasReligion,
    string religionUID,
    string religionName,
    string currentPlayerUID,
    bool isFounder,
    IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> members,
    IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo>? bannedPlayers,
    string? expandedMemberUID,
    string? expandedBanUID,
    string invitePlayerName,
    bool showInviteDialog,
    string? strikeConfirmPlayerUID,
    string? strikeConfirmPlayerName,
    float x,
    float y,
    float width,
    float height,
    float scrollY)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public string ReligionUID { get; } = religionUID;
    public string ReligionName { get; } = religionName;
    public string CurrentPlayerUID { get; } = currentPlayerUID;
    public bool IsFounder { get; } = isFounder;
    public IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> Members { get; } = members;

    public IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo> BannedPlayers { get; } =
        bannedPlayers ?? Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>();

    public string? ExpandedMemberUID { get; } = expandedMemberUID;
    public string? ExpandedBanUID { get; } = expandedBanUID;
    public string InvitePlayerName { get; } = invitePlayerName;
    public bool ShowInviteDialog { get; } = showInviteDialog;
    public string? StrikeConfirmPlayerUID { get; } = strikeConfirmPlayerUID;
    public string? StrikeConfirmPlayerName { get; } = strikeConfirmPlayerName;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;

    public int MemberCount => Members.Count;
    public bool HasMembers => Members.Count > 0;
    public bool HasBannedPlayers => BannedPlayers.Count > 0;

    public static ReligionRosterViewModel Loading(float x = 0, float y = 0, float width = 0, float height = 0) =>
        new(true, false, string.Empty, string.Empty, string.Empty, false,
            Array.Empty<PlayerReligionInfoResponsePacket.MemberInfo>(),
            Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>(),
            null, null, string.Empty, false, null, null,
            x, y, width, height, 0f);
}
