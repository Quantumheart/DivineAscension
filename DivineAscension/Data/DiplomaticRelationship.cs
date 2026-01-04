using System;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// Represents a diplomatic relationship between two civilizations
/// </summary>
[ProtoContract]
public class DiplomaticRelationship
{
    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public DiplomaticRelationship()
    {
    }

    /// <summary>
    /// Creates a new diplomatic relationship
    /// </summary>
    public DiplomaticRelationship(
        string relationshipId,
        string civId1,
        string civId2,
        DiplomaticStatus status,
        string initiatorCivId,
        DateTime? expiresDate = null)
    {
        RelationshipId = relationshipId;
        CivId1 = civId1;
        CivId2 = civId2;
        Status = status;
        InitiatorCivId = initiatorCivId;
        EstablishedDate = DateTime.UtcNow;
        ExpiresDate = expiresDate;
        ViolationCount = 0;
    }

    /// <summary>
    /// Unique identifier for the relationship
    /// </summary>
    [ProtoMember(1)]
    public string RelationshipId { get; set; } = string.Empty;

    /// <summary>
    /// First civilization ID (order doesn't matter)
    /// </summary>
    [ProtoMember(2)]
    public string CivId1 { get; set; } = string.Empty;

    /// <summary>
    /// Second civilization ID (order doesn't matter)
    /// </summary>
    [ProtoMember(3)]
    public string CivId2 { get; set; } = string.Empty;

    /// <summary>
    /// Current diplomatic status between the civilizations
    /// </summary>
    [ProtoMember(4)]
    public DiplomaticStatus Status { get; set; }

    /// <summary>
    /// When the relationship was established
    /// </summary>
    [ProtoMember(5)]
    public DateTime EstablishedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the relationship expires (null for permanent relationships like Alliance)
    /// </summary>
    [ProtoMember(6)]
    public DateTime? ExpiresDate { get; set; }

    /// <summary>
    /// Civilization ID that initiated this relationship
    /// </summary>
    [ProtoMember(7)]
    public string InitiatorCivId { get; set; } = string.Empty;

    /// <summary>
    /// Count of PvP violations (attacks between allied/NAP civilizations)
    /// </summary>
    [ProtoMember(8)]
    public int ViolationCount { get; set; }

    /// <summary>
    /// When the relationship is scheduled to break (24-hour warning)
    /// </summary>
    [ProtoMember(9)]
    public DateTime? BreakScheduledDate { get; set; }

    /// <summary>
    /// Checks if the relationship has expired
    /// </summary>
    public bool IsExpired => ExpiresDate.HasValue && DateTime.UtcNow >= ExpiresDate.Value;

    /// <summary>
    /// Checks if the relationship is currently active
    /// </summary>
    public bool IsActive => !IsExpired && BreakScheduledDate == null;

    /// <summary>
    /// Checks if a civilization is part of this relationship
    /// </summary>
    public bool Involvescivilization(string civId)
    {
        return CivId1 == civId || CivId2 == civId;
    }

    /// <summary>
    /// Gets the other civilization in the relationship
    /// </summary>
    public string GetOtherCivilization(string civId)
    {
        if (CivId1 == civId) return CivId2;
        if (CivId2 == civId) return CivId1;
        return string.Empty;
    }

    /// <summary>
    /// Increments the violation count
    /// </summary>
    public void RecordViolation()
    {
        ViolationCount++;
    }

    /// <summary>
    /// Resets the violation count
    /// </summary>
    public void ResetViolations()
    {
        ViolationCount = 0;
    }
}