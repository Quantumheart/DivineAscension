using System;
using System.Collections.Generic;
using System.Linq;
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
    ///     Last in-game year an advance-notice toast fired for this feast
    ///     (one in-game day before the feast). Same idempotency guarantee
    ///     as <see cref="LastFiredYear"/> — at most one advance toast per
    ///     in-game year, even across save/reload.
    /// </summary>
    [ProtoMember(7)] public int LastAdvanceFiredYear { get; set; }

    /// <summary>
    ///     Fixed patron's-day per domain (#375). Sourced from
    ///     <see cref="DeityDomainRegistry"/> so the ticker, the religion manager,
    ///     and tests share one source of truth (#558) — adding a domain to the
    ///     registry seeds its patron feast automatically.
    /// </summary>
    public static readonly IReadOnlyDictionary<DeityDomain, (int Month, int Day)> DomainHolyDay =
        DeityDomainRegistry.All.ToDictionary(m => m.Domain, m => m.HolyDay);

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
