using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Backing field for the chronicle — the permanent, narrative history of
    ///     significant religion events (#373). Unlike <see cref="_activityLog" /> this
    ///     is never trimmed.
    /// </summary>
    [ProtoMember(23)]
    private List<ChronicleEntry> _chronicle = new();

    /// <summary>
    ///     Chronicle entries oldest-first (a chronicle reads forward).
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<ChronicleEntry> Chronicle
    {
        get
        {
            lock (Lock)
            {
                return _chronicle.ToList();
            }
        }
    }

    /// <summary>
    ///     Appends a chronicle entry (thread-safe). The chronicle is never trimmed.
    /// </summary>
    public void AddChronicleEntry(ChronicleEntry entry)
    {
        if (entry == null) return;
        lock (Lock)
        {
            _chronicle.Add(entry);
        }
    }
}
