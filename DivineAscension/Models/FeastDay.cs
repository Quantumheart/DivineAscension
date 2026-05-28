using System.Collections.Generic;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Models;

/// <summary>
///     A dated holy day on a religion's sacred calendar (#375). Auto-seeded for
///     Founding + Patron at religion creation; founder-defined "Custom" entries
///     land in a follow-up issue.
/// </summary>
[ProtoContract]
public class FeastDay
{
    public FeastDay()
    {
    }

    public FeastDay(string name, int month, int day, FeastKind kind)
    {
        Name = name;
        Month = month;
        Day = day;
        Kind = kind;
    }

    [ProtoMember(1)] public string Name { get; set; } = string.Empty;

    /// <summary>In-game month, 1-based.</summary>
    [ProtoMember(2)] public int Month { get; set; }

    /// <summary>In-game day-of-month, 1-based.</summary>
    [ProtoMember(3)] public int Day { get; set; }

    [ProtoMember(4)] public FeastKind Kind { get; set; }

    /// <summary>
    ///     Last in-game year this feast fired. The ticker only fires when
    ///     <c>calendar.Year &gt; LastFiredYear</c>, which makes the day-rollover
    ///     idempotent across save/reload (the stamp persists in the save).
    /// </summary>
    [ProtoMember(5)] public int LastFiredYear { get; set; }

    /// <summary>
    ///     Fixed patron's-day per domain (#375). Defined here so the ticker, the
    ///     religion manager, and tests share a single source of truth.
    /// </summary>
    public static readonly IReadOnlyDictionary<DeityDomain, (int Month, int Day)> DomainHolyDay =
        new Dictionary<DeityDomain, (int Month, int Day)>
        {
            [DeityDomain.Craft] = (2, 1),
            [DeityDomain.Wild] = (4, 15),
            [DeityDomain.Conquest] = (7, 4),
            [DeityDomain.Harvest] = (9, 12),
            [DeityDomain.Stone] = (11, 1)
        };
}
