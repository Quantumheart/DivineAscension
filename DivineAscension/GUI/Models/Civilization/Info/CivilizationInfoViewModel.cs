using System;
using System.Collections.Generic;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Info;

public readonly struct CivilizationInfoViewModel(
    bool isLoading,
    bool hasCivilization,
    string civId,
    string civName,
    string icon,
    string description,
    string descriptionText,
    bool isEditingDescription,
    string founderName,
    string founderReligionName,
    DateTime createdDate,
    bool isFounder,
    int rank,
    IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> memberReligions,
    IReadOnlyList<CivilizationInfoResponsePacket.PendingInvite> pendingInvites,
    string inviteReligionName,
    bool showDisbandConfirm,
    string? kickConfirmReligionId,
    float scrollY,
    float memberScrollY,
    float x,
    float y,
    float width,
    float height)
{
    public bool IsLoading { get; } = isLoading;
    public bool HasCivilization { get; } = hasCivilization;
    public string CivId { get; } = civId;
    public string CivName { get; } = civName;
    public string Icon { get; } = icon;
    public string Description { get; } = description;
    public string DescriptionText { get; } = descriptionText;
    public bool IsEditingDescription { get; } = isEditingDescription;
    public string FounderName { get; } = founderName;
    public string FounderReligionName { get; } = founderReligionName;
    public DateTime CreatedDate { get; } = createdDate;
    public bool IsFounder { get; } = isFounder;
    public int Rank { get; } = rank;
    public IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> MemberReligions { get; } = memberReligions;
    public IReadOnlyList<CivilizationInfoResponsePacket.PendingInvite> PendingInvites { get; } = pendingInvites;
    public string InviteReligionName { get; } = inviteReligionName;
    public bool ShowDisbandConfirm { get; } = showDisbandConfirm;
    public string? KickConfirmReligionId { get; } = kickConfirmReligionId;
    public float ScrollY { get; } = scrollY;
    public float MemberScrollY { get; } = memberScrollY;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    public int MemberCount => MemberReligions?.Count ?? 0;
    public bool CanInvite => IsFounder && !string.IsNullOrWhiteSpace(InviteReligionName);
    public bool CanDisband => IsFounder && HasCivilization;
    public bool CanLeave => !IsFounder && HasCivilization;
    public bool IsKickConfirmOpen => !string.IsNullOrEmpty(KickConfirmReligionId);
    public bool HasDescriptionChanges => Description != DescriptionText;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasPendingInvites => PendingInvites is { Count: > 0 };
}
