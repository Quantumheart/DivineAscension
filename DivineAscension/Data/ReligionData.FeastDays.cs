using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models;
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
            var stored = _feastDays.FirstOrDefault(f =>
                f.Kind == feast.Kind && f.Month == feast.Month && f.Day == feast.Day && f.Name == feast.Name);
            if (stored == null || stored.LastFiredYear >= year) return false;
            stored.LastFiredYear = year;
            return true;
        }
    }
}
