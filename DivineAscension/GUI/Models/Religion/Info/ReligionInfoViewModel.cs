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
    string deityName,
    string founderUID,
    string founderName,
    string currentPlayerUID,
    bool isFounder,
    string? description,
    IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> members,
    IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo>? bannedPlayers,
    // Prestige snapshot
    int prestige,
    string prestigeRank,
    int prestigeRankIndex,
    int prestigeRequired,
    bool isMaxPrestigeRank,
    bool isPublic,
    // UI state for form inputs and dialogs
    string descriptionText,
    string invitePlayerName,
    bool showDisbandConfirm,
    string? kickConfirmPlayerUID,
    string? kickConfirmPlayerName,
    string? banConfirmPlayerUID,
    string? banConfirmPlayerName,
    // Deity name editing state
    bool isEditingDeityName,
    string editDeityNameValue,
    bool isSavingDeityName,
    string? deityNameError,
    // Description editing state
    bool isEditingDescription,
    // Layout & scrolling
    float x,
    float y,
    float width,
    float height,
    float scrollY,
    float memberScrollY,
    float banListScrollY)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public string ReligionUID { get; } = religionUID;
    public string ReligionName { get; } = religionName;

    /// <summary>The domain (Craft, Wild, Harvest, Stone)</summary>
    public string Deity { get; } = deity;

    /// <summary>The custom deity name for this religion</summary>
    public string DeityName { get; } = deityName;

    public string FounderUID { get; } = founderUID;
    public string FounderName { get; } = founderName;
    public string CurrentPlayerUID { get; } = currentPlayerUID;
    public bool IsFounder { get; } = isFounder;
    public string? Description { get; } = description;
    public IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> Members { get; } = members;

    public IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo> BannedPlayers { get; } =
        bannedPlayers ?? Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>();

    public int Prestige { get; } = prestige;
    public string PrestigeRank { get; } = prestigeRank;
    public int PrestigeRankIndex { get; } = prestigeRankIndex;
    public int PrestigeRequired { get; } = prestigeRequired;
    public bool IsMaxPrestigeRank { get; } = isMaxPrestigeRank;
    public bool IsPublic { get; } = isPublic;

    public string DescriptionText { get; } = descriptionText;
    public string InvitePlayerName { get; } = invitePlayerName;
    public bool ShowDisbandConfirm { get; } = showDisbandConfirm;
    public string? KickConfirmPlayerUID { get; } = kickConfirmPlayerUID;
    public string? KickConfirmPlayerName { get; } = kickConfirmPlayerName;
    public string? BanConfirmPlayerUID { get; } = banConfirmPlayerUID;
    public string? BanConfirmPlayerName { get; } = banConfirmPlayerName;

    public bool IsEditingDeityName { get; } = isEditingDeityName;
    public string EditDeityNameValue { get; } = editDeityNameValue;
    public bool IsSavingDeityName { get; } = isSavingDeityName;
    public string? DeityNameError { get; } = deityNameError;

    public bool IsEditingDescription { get; } = isEditingDescription;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;
    public float MemberScrollY { get; } = memberScrollY;
    public float BanListScrollY { get; } = banListScrollY;

    public int MemberCount => Members.Count;
    public bool HasMembers => Members.Count > 0;
    public bool HasBannedPlayers => BannedPlayers.Count > 0;

    public float PrestigeProgressPercentage =>
        IsMaxPrestigeRank
            ? 1f
            : PrestigeRequired > 0 ? Math.Clamp((float)Prestige / PrestigeRequired, 0f, 1f) : 0f;

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
    /// Returns the founder's cached display name, falling back to UID if not available.
    /// </summary>
    public string GetFounderDisplayName()
    {
        return string.IsNullOrWhiteSpace(FounderName) ? FounderUID : FounderName;
    }

    public ReligionInfoViewModel WithScroll(float newScrollY) => new(
        IsLoading, HasReligion, ReligionUID, ReligionName, Deity, DeityName, FounderUID, FounderName, CurrentPlayerUID,
        IsFounder, Description,
        Members, BannedPlayers, Prestige, PrestigeRank, PrestigeRankIndex, PrestigeRequired, IsMaxPrestigeRank, IsPublic,
        DescriptionText, InvitePlayerName, ShowDisbandConfirm,
        KickConfirmPlayerUID, KickConfirmPlayerName, BanConfirmPlayerUID, BanConfirmPlayerName,
        IsEditingDeityName, EditDeityNameValue, IsSavingDeityName, DeityNameError,
        IsEditingDescription,
        X, Y, Width, Height,
        newScrollY, MemberScrollY, BanListScrollY);

    public ReligionInfoViewModel WithMemberScroll(float newMemberScrollY) => new(
        IsLoading, HasReligion, ReligionUID, ReligionName, Deity, DeityName, FounderUID, FounderName, CurrentPlayerUID,
        IsFounder, Description,
        Members, BannedPlayers, Prestige, PrestigeRank, PrestigeRankIndex, PrestigeRequired, IsMaxPrestigeRank, IsPublic,
        DescriptionText, InvitePlayerName, ShowDisbandConfirm,
        KickConfirmPlayerUID, KickConfirmPlayerName, BanConfirmPlayerUID, BanConfirmPlayerName,
        IsEditingDeityName, EditDeityNameValue, IsSavingDeityName, DeityNameError,
        IsEditingDescription,
        X, Y, Width, Height,
        ScrollY, newMemberScrollY, BanListScrollY);

    public ReligionInfoViewModel WithBanListScroll(float newBanListScrollY) => new(
        IsLoading, HasReligion, ReligionUID, ReligionName, Deity, DeityName, FounderUID, FounderName, CurrentPlayerUID,
        IsFounder, Description,
        Members, BannedPlayers, Prestige, PrestigeRank, PrestigeRankIndex, PrestigeRequired, IsMaxPrestigeRank, IsPublic,
        DescriptionText, InvitePlayerName, ShowDisbandConfirm,
        KickConfirmPlayerUID, KickConfirmPlayerName, BanConfirmPlayerUID, BanConfirmPlayerName,
        IsEditingDeityName, EditDeityNameValue, IsSavingDeityName, DeityNameError,
        IsEditingDescription,
        X, Y, Width, Height,
        ScrollY, MemberScrollY, newBanListScrollY);

    public static ReligionInfoViewModel Loading(float x = 0, float y = 0, float width = 0, float height = 0) => new(
        true, false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        false, string.Empty,
        Array.Empty<PlayerReligionInfoResponsePacket.MemberInfo>(),
        Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>(),
        0, string.Empty, 0, 0, false, true,
        string.Empty, string.Empty, false, null, null, null, null,
        false, string.Empty, false, null,
        false,
        x, y, width, height,
        0, 0, 0);
}
