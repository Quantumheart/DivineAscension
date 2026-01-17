using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Systems;

[ProtoContract]
public class PlayerProgressionData
{
    // Thread-safety lock for UnlockedBlessings collection
    [ProtoIgnore] private readonly object _blessingLock = new();

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
    ///     Internal storage for unlocked player blessings
    /// </summary>
    [ProtoMember(103)]
    private HashSet<string> _unlockedBlessings = new();

    /// <summary>
    ///     Thread-safe read-only access to unlocked player blessings
    ///     Returns a snapshot to prevent concurrent modification
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyCollection<string> UnlockedBlessings
    {
        get
        {
            lock (_blessingLock)
            {
                return _unlockedBlessings.ToHashSet(); // Return snapshot
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
    ///     Adds favor and updates statistics
    /// </summary>
    public void AddFavor(int amount)
    {
        if (amount > 0)
        {
            Favor += amount;
            TotalFavorEarned += amount;
        }
    }

    /// <summary>
    ///     Adds fractional favor and updates statistics when accumulated amount >= 1
    /// </summary>
    public void AddFractionalFavor(float amount)
    {
        if (amount > 0)
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

    /// <summary>
    ///     Removes favor (for costs or penalties)
    /// </summary>
    public bool RemoveFavor(int amount)
    {
        if (Favor >= amount)
        {
            Favor -= amount;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Unlocks a player blessing (thread-safe)
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        lock (_blessingLock)
        {
            _unlockedBlessings.Add(blessingId);
        }
    }

    /// <summary>
    ///     Checks if a blessing is unlocked (thread-safe)
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        lock (_blessingLock)
        {
            return _unlockedBlessings.Contains(blessingId);
        }
    }

    /// <summary>
    ///     Clears all unlocked blessings (used when switching religions) (thread-safe)
    /// </summary>
    public void ClearUnlockedBlessings()
    {
        lock (_blessingLock)
        {
            _unlockedBlessings.Clear();
        }
    }

    /// <summary>
    ///     Gets count of unlocked blessings (thread-safe)
    /// </summary>
    public int GetUnlockedBlessingCount()
    {
        lock (_blessingLock)
        {
            return _unlockedBlessings.Count;
        }
    }

    /// <summary>
    ///     Resets favor and blessings (penalty for switching religions)
    /// </summary>
    public void ApplySwitchPenalty()
    {
        Favor = 0;
        ClearUnlockedBlessings();
    }
}