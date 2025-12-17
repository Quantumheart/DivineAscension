using System.Collections.Generic;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Religion.Header;

public readonly struct ReligionHeaderViewModel(
    bool hasReligion,
    bool hasCivilization,
    string? currentCivilizationName,
    IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> memberReligions,
    DeityType currentDeity,
    string? currentReligionName,
    int religionMemberCount,
    string? playerRoleInReligion,
    PlayerFavorProgress playerFavorProgress,
    ReligionPrestigeProgress religionPrestigeProgress,
    bool isCivilizationFounder,
    string? civilizationIcon,
    float x,
    float y,
    float width)
{
    public bool HasReligion { get; } = hasReligion;
    public bool HasCivilization { get; } = hasCivilization;
    public string? CurrentCivilizationName { get; } = currentCivilizationName;

    public IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions { get; } =
        memberReligions;

    public DeityType CurrentDeity { get; } = currentDeity;
    public string? CurrentReligionName { get; } = currentReligionName;
    public int ReligionMemberCount { get; } = religionMemberCount;
    public string? PlayerRoleInReligion { get; } = playerRoleInReligion;
    public PlayerFavorProgress PlayerFavorProgress { get; } = playerFavorProgress;
    public ReligionPrestigeProgress ReligionPrestigeProgress { get; } = religionPrestigeProgress;
    public bool IsCivilizationFounder { get; } = isCivilizationFounder;
    public string? CivilizationIcon { get; } = civilizationIcon;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
}