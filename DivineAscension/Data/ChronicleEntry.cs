using System;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     A single dated entry in a civilization's chronicle — the in-world history
///     of significant events (founding, members joining/leaving, milestones,
///     diplomacy, disband). The <see cref="Line" /> is the chronicle voice copy,
///     resolved and cached when the event occurs.
/// </summary>
[ProtoContract]
public class ChronicleEntry
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ChronicleEntry()
    {
    }

    /// <summary>
    ///     Creates a new chronicle entry.
    /// </summary>
    public ChronicleEntry(ChronicleKind kind, string line, string? relatedId, DateTime timestamp, int inGameDay,
        int year = 0, int month = 0, int dayOfMonth = 0)
    {
        EntryId = Guid.NewGuid().ToString();
        Kind = kind;
        Line = line;
        RelatedId = relatedId;
        Timestamp = timestamp;
        InGameDay = inGameDay;
        Year = year;
        Month = month;
        DayOfMonth = dayOfMonth;
    }

    /// <summary>
    ///     Unique identifier for this entry (for deduplication).
    /// </summary>
    [ProtoMember(1)]
    public string EntryId { get; set; } = string.Empty;

    /// <summary>
    ///     Real-world UTC time the event occurred. Source of truth for ordering.
    /// </summary>
    [ProtoMember(2)]
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     In-game day captured at the time of the event, for the chronicle's
    ///     "Day N" presentation.
    /// </summary>
    [ProtoMember(3)]
    public int InGameDay { get; set; }

    /// <summary>
    ///     The kind of event this entry records.
    /// </summary>
    [ProtoMember(4)]
    public ChronicleKind Kind { get; set; }

    /// <summary>
    ///     The chronicle voice line for this event, resolved and cached at write time.
    /// </summary>
    [ProtoMember(5)]
    public string Line { get; set; } = string.Empty;

    /// <summary>
    ///     Optional related identifier (religion id, milestone id, or counterpart civ id).
    /// </summary>
    [ProtoMember(6)]
    public string? RelatedId { get; set; }

    /// <summary>
    ///     In-game calendar year captured at write time. 0 for entries written before
    ///     calendar capture existed — those fall back to "Day {InGameDay}" presentation.
    /// </summary>
    [ProtoMember(7)]
    public int Year { get; set; }

    /// <summary>In-game month (1..12) captured at write time. 0 when unknown.</summary>
    [ProtoMember(8)]
    public int Month { get; set; }

    /// <summary>In-game day of the month (1-based) captured at write time. 0 when unknown.</summary>
    [ProtoMember(9)]
    public int DayOfMonth { get; set; }
}
