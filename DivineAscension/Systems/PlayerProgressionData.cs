using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Systems;

[ProtoContract]
public class PlayerProgressionData
{
    /// <summary>
    ///     Lazy-initialized lock object for thread safety.
    ///     Uses Interlocked.CompareExchange to work safely with ProtoBuf deserialization.
    /// </summary>
    [ProtoIgnore]
    private object? _lock;

    [ProtoIgnore]
    private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }
            return _lock;
        }
    }

    /// <summary>
    ///     Creates new player religion data
    /// </summary>
    public PlayerProgressionData(string id)
    {
        Id = id;
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public PlayerProgressionData()
    {
    }

    /// <summary>
    ///     Player's unique identifier
    /// </summary>
    [ProtoMember(100)]
    public string Id { get; set; } = string.Empty;


    /// <summary>
    ///     Current favor points
    /// </summary>
    [ProtoMember(101)]
    public int Favor { get; set; }

    /// <summary>
    ///     Total favor earned (lifetime stat, persists across religion changes)
    /// </summary>
    [ProtoMember(102)]
    public int TotalFavorEarned { get; set; }

    /// <summary>
    ///     Backing field for unlocked player blessings.
    /// </summary>
    [ProtoMember(103)]
    private HashSet<string> _unlockedBlessings = new();

    /// <summary>
    ///     Read-only view of unlocked player blessings.
    ///     Returns a snapshot for thread-safe enumeration.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyCollection<string> UnlockedBlessings
    {
        get
        {
            lock (Lock)
            {
                return _unlockedBlessings.ToList();
            }
        }
    }


    /// <summary>
    ///     Data version for migration purposes
    /// </summary>
    [ProtoMember(104)]
    public int DataVersion { get; set; } = 3;

    /// <summary>
    ///     Accumulated fractional favor (not yet awarded) for passive generation
    /// </summary>
    [ProtoMember(105)]
    public float AccumulatedFractionalFavor { get; set; }

    /// <summary>
    ///     Adds favor and updates statistics.
    ///     Thread-safe.
    /// </summary>
    public void AddFavor(int amount)
    {
        if (amount > 0)
        {
            lock (Lock)
            {
                Favor += amount;
                TotalFavorEarned += amount;
            }
        }
    }

    /// <summary>
    ///     Adds fractional favor and updates statistics when accumulated amount >= 1.
    ///     Thread-safe.
    /// </summary>
    public void AddFractionalFavor(float amount)
    {
        if (amount > 0)
        {
            lock (Lock)
            {
                AccumulatedFractionalFavor += amount;

                // Award integer favor when we have accumulated >= 1.0
                if (AccumulatedFractionalFavor >= 1.0f)
                {
                    var favorToAward = (int)AccumulatedFractionalFavor;
                    AccumulatedFractionalFavor -= favorToAward; // Keep the fractional remainder

                    Favor += favorToAward;
                    TotalFavorEarned += favorToAward;
                }
            }
        }
    }

    /// <summary>
    ///     Removes favor (for costs or penalties).
    ///     Thread-safe.
    /// </summary>
    public bool RemoveFavor(int amount)
    {
        lock (Lock)
        {
            if (Favor >= amount)
            {
                Favor -= amount;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Unlocks a player blessing.
    ///     Thread-safe.
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        lock (Lock)
        {
            _unlockedBlessings.Add(blessingId);
        }
    }

    /// <summary>
    ///     Locks (removes) a player blessing.
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
            return _unlockedBlessings.Contains(blessingId);
        }
    }

    /// <summary>
    ///     Clears all unlocked blessings (used when switching religions).
    ///     Thread-safe.
    /// </summary>
    public void ClearUnlockedBlessings()
    {
        lock (Lock)
        {
            _unlockedBlessings.Clear();
        }
    }

    /// <summary>
    ///     Resets favor and blessings (penalty for switching religions).
    ///     Thread-safe.
    /// </summary>
    public void ApplySwitchPenalty()
    {
        lock (Lock)
        {
            Favor = 0;
            _unlockedBlessings.Clear();
        }
    }
}