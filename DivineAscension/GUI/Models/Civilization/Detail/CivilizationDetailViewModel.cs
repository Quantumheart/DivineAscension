using System;
using System.Collections.Generic;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Detail;

public readonly struct CivilizationDetailViewModel(
    bool isLoading,
    string civId,
    string civName,
    string founderName,
    string founderReligionName,
    IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> memberReligions,
    DateTime createdDate,
    string description,
    float memberScrollY,
    bool canRequestToJoin,
    float x,
    float y,
    float width,
    float height)
{
    public bool IsLoading { get; } = isLoading;
    public string CivId { get; } = civId;
    public string CivName { get; } = civName;
    public string FounderName { get; } = founderName;
    public string FounderReligionName { get; } = founderReligionName;
    public IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> MemberReligions { get; } = memberReligions;
    public DateTime CreatedDate { get; } = createdDate;
    public string Description { get; } = description;
    public float MemberScrollY { get; } = memberScrollY;
    public bool CanRequestToJoin { get; } = canRequestToJoin;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    // Helper methods (no side effects)
    public int MemberCount => MemberReligions?.Count ?? 0;
    public bool IsFull => MemberCount >= 4; // Max 8 religions (one per deity type)
}