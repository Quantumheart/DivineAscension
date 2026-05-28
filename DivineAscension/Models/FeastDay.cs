using System;
using System.Collections.Generic;
using System.Text;
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

    public FeastDay(Guid feastId, string name, int month, int day, FeastKind kind)
    {
        FeastId = feastId;
        Name = name;
        Month = month;
        Day = day;
        Kind = kind;
    }

    /// <summary>
    ///     Stable identifier for remove operations (#422). Survives a rename of
    ///     <see cref="Name"/>. Custom feasts get a fresh Guid at creation;
    ///     auto Founding/Patron feasts get a deterministic Guid derived from
    ///     (religionUID, kind) so they can never collide with a custom id.
    /// </summary>
    [ProtoMember(6)] public Guid FeastId { get; set; } = Guid.Empty;

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

    /// <summary>
    ///     Deterministic Guid for an auto-seeded feast on a given religion.
    ///     Same religion + same kind ⇒ same Guid across saves, regardless of
    ///     name changes. Derived from a SHA-1 hash of "religionUID|kind".
    /// </summary>
    public static Guid DeterministicAutoFeastId(string religionUID, FeastKind kind)
    {
        var bytes = Encoding.UTF8.GetBytes($"feast|{religionUID}|{(int)kind}");
        using var sha = System.Security.Cryptography.SHA1.Create();
        var hash = sha.ComputeHash(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
