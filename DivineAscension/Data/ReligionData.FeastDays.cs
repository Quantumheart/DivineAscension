using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     In-game month captured at religion creation; drives the annual
    ///     Founding feast (#375). 0 for pre-#375 religions (breaking change —
    ///     no migration).
    /// </summary>
    [ProtoMember(24)]
    public int FoundingMonth { get; set; }

    /// <summary>
    ///     In-game day-of-month captured at religion creation (#375).
    /// </summary>
    [ProtoMember(25)]
    public int FoundingDay { get; set; }

    /// <summary>
    ///     Sacred calendar entries (#375). Founding + Patron are seeded at
    ///     creation; founder-added Custom entries arrive in a follow-up.
    /// </summary>
    [ProtoMember(26)]
    private List<FeastDay> _feastDays = new();

    /// <summary>
    ///     Thread-safe snapshot of the sacred calendar.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<FeastDay> FeastDays
    {
        get
        {
            lock (Lock)
            {
                return _feastDays.ToList();
            }
        }
    }

    /// <summary>
    ///     Replaces the feast list (used by the manager at religion creation).
    /// </summary>
    public void SetFeastDays(IEnumerable<FeastDay> feasts)
    {
        lock (Lock)
        {
            _feastDays = feasts?.ToList() ?? new List<FeastDay>();
        }
    }

    /// <summary>
    ///     Records that the given feast fired in <paramref name="year"/>. Returns
    ///     false if the feast was already fired this year — the ticker uses the
    ///     return value to guard chronicle/broadcast side effects.
    /// </summary>
    public bool TryMarkFeastFired(FeastDay feast, int year)
    {
        lock (Lock)
        {
            var stored = FindStored(feast);
            if (stored == null || stored.LastFiredYear >= year) return false;
            stored.LastFiredYear = year;
            return true;
        }
    }

    /// <summary>
    ///     Records that the given feast's advance toast fired in
    ///     <paramref name="year"/>. Returns false if it already fired this
    ///     year — guarantees one advance toast per in-game year.
    /// </summary>
    public bool TryMarkFeastAdvanceFired(FeastDay feast, int year)
    {
        lock (Lock)
        {
            var stored = FindStored(feast);
            if (stored == null || stored.LastAdvanceFiredYear >= year) return false;
            stored.LastAdvanceFiredYear = year;
            return true;
        }
    }

    private FeastDay? FindStored(FeastDay feast)
    {
        // Match by FeastId when both have one (post-#422); fall back to
        // legacy key for entries still carrying Guid.Empty (autos from
        // #535 saves that pre-date the FeastId field).
        return _feastDays.FirstOrDefault(f =>
            feast.FeastId != Guid.Empty && f.FeastId == feast.FeastId)
            ?? _feastDays.FirstOrDefault(f =>
                f.Kind == feast.Kind && f.Month == feast.Month && f.Day == feast.Day && f.Name == feast.Name);
    }

    /// <summary>
    ///     Number of Custom feasts currently on this religion's calendar
    ///     (#422). Auto Founding/Patron don't count toward the cap.
    /// </summary>
    public int GetCustomFeastCount()
    {
        lock (Lock)
        {
            return _feastDays.Count(f => f.Kind == FeastKind.Custom);
        }
    }

    /// <summary>
    ///     Appends a custom feast. Caller is responsible for all validation
    ///     (cap, prestige, spacing, name) — this is just the persistence hook.
    /// </summary>
    public void AddCustomFeast(FeastDay feast)
    {
        if (feast == null) return;
        lock (Lock)
        {
            _feastDays.Add(feast);
        }
    }

    /// <summary>
    ///     Removes the custom feast with the given id. Returns the removed
    ///     entry on success, or null if no Custom feast with that id exists.
    ///     Auto feasts can never be removed via this method.
    /// </summary>
    public FeastDay? RemoveCustomFeastById(Guid feastId)
    {
        lock (Lock)
        {
            var idx = _feastDays.FindIndex(f => f.Kind == FeastKind.Custom && f.FeastId == feastId);
            if (idx < 0) return null;
            var removed = _feastDays[idx];
            _feastDays.RemoveAt(idx);
            return removed;
        }
    }

    /// <summary>
    ///     Backfills <see cref="FeastDay.FeastId"/> on auto feasts loaded from
    ///     pre-#422 saves so the new id-based remove path stays consistent.
    /// </summary>
    public void BackfillAutoFeastIds()
    {
        lock (Lock)
        {
            foreach (var feast in _feastDays)
            {
                if (feast.FeastId != Guid.Empty) continue;
                if (feast.Kind == FeastKind.Custom) continue;
                feast.FeastId = FeastDay.DeterministicAutoFeastId(ReligionUID, feast.Kind);
            }
        }
    }
}
