using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Backing field for unlocked blessings (serialized)
    /// </summary>
    [ProtoMember(10)]
    private Dictionary<string, bool> _unlockedBlessings = new();

    /// <summary>
    ///     Dictionary of unlocked religion blessings.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, bool> UnlockedBlessings
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, bool>(_unlockedBlessings);
            }
        }
    }

    /// <summary>
    ///     Unlocks a blessing for this religion.
    ///     Thread-safe.
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        lock (Lock)
        {
            _unlockedBlessings[blessingId] = true;
        }
    }

    /// <summary>
    ///     Locks (removes) a blessing for this religion.
    ///     Thread-safe.
    /// </summary>
    public bool LockBlessing(string blessingId)
    {
        lock (Lock)
        {
            return _unlockedBlessings.Remove(blessingId);
        }
    }

    /// <summary>
    ///     Checks if a blessing is unlocked.
    ///     Thread-safe.
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        lock (Lock)
        {
            return _unlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;
        }
    }
}
