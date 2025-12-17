using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Info;

/// <summary>
/// Immutable view model for the "Info" tab.
/// Contains only the data needed to render the UI and simple helpers for presentation logic.
/// </summary>
public readonly struct ReligionInfoViewModel(
    // Data
    bool isLoading,
    bool hasReligion,
    string religionUID,
    string religionName,
    string deity,
    string founderUID,
    string currentPlayerUID,
    bool isFounder,
    string? description,
    IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> members,
    IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo>? bannedPlayers,
    // Optional prestige snapshot (renderer may compute separately)
    int prestige,
    string prestigeRank,
    bool isPublic,
    // UI state for form inputs and dialogs
    string descriptionText,
    string invitePlayerName,
    bool showDisbandConfirm,
    string? kickConfirmPlayerUID,
    string? kickConfirmPlayerName,
    string? banConfirmPlayerUID,
    string? banConfirmPlayerName,
    // Layout & scrolling
    float x,
    float y,
    float width,
    float height,
    float scrollY,
    float memberScrollY,
    float banListScrollY)
{
    // Core data
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public string ReligionUID { get; } = religionUID;
    public string ReligionName { get; } = religionName;
    public string Deity { get; } = deity;
    public string FounderUID { get; } = founderUID;

    public string CurrentPlayerUID { get; } = currentPlayerUID;

    public bool IsFounder { get; } = isFounder;
    public string? Description { get; } = description;
    public IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> Members { get; } = members;

    public IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo> BannedPlayers { get; } =
        bannedPlayers ?? Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>();

    // Prestige snapshot (optional for display)
    public int Prestige { get; } = prestige;
    public string PrestigeRank { get; } = prestigeRank;
    public bool IsPublic { get; } = isPublic;

    // UI state for form inputs and dialogs
    public string DescriptionText { get; } = descriptionText;
    public string InvitePlayerName { get; } = invitePlayerName;
    public bool ShowDisbandConfirm { get; } = showDisbandConfirm;
    public string? KickConfirmPlayerUID { get; } = kickConfirmPlayerUID;
    public string? KickConfirmPlayerName { get; } = kickConfirmPlayerName;
    public string? BanConfirmPlayerUID { get; } = banConfirmPlayerUID;
    public string? BanConfirmPlayerName { get; } = banConfirmPlayerName;

    // Layout & scrolling
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;
    public float MemberScrollY { get; } = memberScrollY;
    public float BanListScrollY { get; } = banListScrollY;

    // Presentation helpers
    public int MemberCount => Members.Count;
    public bool HasMembers => Members.Count > 0;
    public bool HasBannedPlayers => BannedPlayers.Count > 0;

    /// <summary>
    /// Checks if the description text has been changed from the original
    /// </summary>
    public bool HasDescriptionChanges()
    {
        var trimmed = (DescriptionText ?? string.Empty).Trim();
        var original = Description ?? string.Empty;
        return !string.Equals(trimmed, original, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the founder's display name if present in the member list, otherwise the raw founder UID.
    /// </summary>
    public string GetFounderDisplayName()
    {
        foreach (var m in Members)
        {
            if (m.PlayerUID == FounderUID)
                return string.IsNullOrWhiteSpace(m.PlayerName) ? FounderUID : m.PlayerName;
        }

        return FounderUID;
    }

    /// <summary>
    /// Copy with updated overall scroll position for the tab content.
    /// </summary>
    public ReligionInfoViewModel WithScroll(float newScrollY) => new(
        IsLoading, HasReligion, ReligionUID, ReligionName, Deity, FounderUID, CurrentPlayerUID, IsFounder, Description,
        Members, BannedPlayers, Prestige, PrestigeRank, IsPublic,
        DescriptionText, InvitePlayerName, ShowDisbandConfirm,
        KickConfirmPlayerUID, KickConfirmPlayerName, BanConfirmPlayerUID, BanConfirmPlayerName,
        X, Y, Width, Height,
        newScrollY, MemberScrollY, BanListScrollY);

    /// <summary>
    /// Copy with updated member list scroll position.
    /// </summary>
    public ReligionInfoViewModel WithMemberScroll(float newMemberScrollY) => new(
        IsLoading, HasReligion, ReligionUID, ReligionName, Deity, FounderUID, CurrentPlayerUID, IsFounder, Description,
        Members, BannedPlayers, Prestige, PrestigeRank, IsPublic,
        DescriptionText, InvitePlayerName, ShowDisbandConfirm,
        KickConfirmPlayerUID, KickConfirmPlayerName, BanConfirmPlayerUID, BanConfirmPlayerName,
        X, Y, Width, Height,
        ScrollY, newMemberScrollY, BanListScrollY);

    /// <summary>
    /// Copy with updated banned list scroll position.
    /// </summary>
    public ReligionInfoViewModel WithBanListScroll(float newBanListScrollY) => new(
        IsLoading, HasReligion, ReligionUID, ReligionName, Deity, FounderUID, CurrentPlayerUID, IsFounder, Description,
        Members, BannedPlayers, Prestige, PrestigeRank, IsPublic,
        DescriptionText, InvitePlayerName, ShowDisbandConfirm,
        KickConfirmPlayerUID, KickConfirmPlayerName, BanConfirmPlayerUID, BanConfirmPlayerName,
        X, Y, Width, Height,
        ScrollY, MemberScrollY, newBanListScrollY);

    /// <summary>
    /// Convenience factory for empty/loading state when no religion data is available yet.
    /// </summary>
    public static ReligionInfoViewModel Loading(float x = 0, float y = 0, float width = 0, float height = 0) => new(
        true, false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty,
        Array.Empty<PlayerReligionInfoResponsePacket.MemberInfo>(),
        Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>(),
        0, string.Empty, true,
        string.Empty, string.Empty, false, null, null, null, null,
        x, y, width, height,
        0, 0, 0);
}