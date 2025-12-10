using System.Collections.Generic;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Info;

public readonly struct CivilizationInfoViewModel(
    bool isLoading,
    bool hasCivilization,
    string civId,
    string civName,
    string founderName,
    bool isFounder,
    IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> memberReligions,
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
    public string FounderName { get; } = founderName;
    public bool IsFounder { get; } = isFounder;
    public IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> MemberReligions { get; } = memberReligions;
    public string InviteReligionName { get; } = inviteReligionName;
    public bool ShowDisbandConfirm { get; } = showDisbandConfirm;
    public string? KickConfirmReligionId { get; } = kickConfirmReligionId;
    public float ScrollY { get; } = scrollY;
    public float MemberScrollY { get; } = memberScrollY;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods (no side effects)
    public bool CanInvite => IsFounder && !string.IsNullOrWhiteSpace(InviteReligionName);
    public bool CanDisband => IsFounder && HasCivilization;
    public bool CanLeave => !IsFounder && HasCivilization;
    public bool IsKickConfirmOpen => !string.IsNullOrEmpty(KickConfirmReligionId);
}