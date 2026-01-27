using System;
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
    ///     Data version for migration purposes.
    /// </summary>
    [ProtoMember(104)]
    public int DataVersion { get; set; } = 3;

    /// <summary>
    ///     Accumulated fractional favor (not yet awarded) for passive generation
    /// </summary>
    [ProtoMember(105)]
    public float AccumulatedFractionalFavor { get; set; }

    /// <summary>
    ///     Set of discovered ruin locations (format: "x_y_z")
    ///     Tracks which ruins the player has already discovered to prevent duplicate favor awards
    /// </summary>
    [ProtoMember(106)]
    public HashSet<string> DiscoveredRuins { get; set; } = new();

    /// <summary>
    ///     Serialization helper for ProtoBuf (HashSet support)
    /// </summary>
    [ProtoMember(107)]
    private List<string> DiscoveredRuinsSerializable
    {
        get => DiscoveredRuins.ToList();
        set => DiscoveredRuins = value?.ToHashSet() ?? new();
    }

    /// <summary>
    ///     Timestamp when the player is next allowed to pray (in elapsed milliseconds).
    ///     This is an absolute expiry time, not a "last action" timestamp.
    ///     Used to enforce prayer cooldown across disconnects and server restarts.
    /// </summary>
    [ProtoMember(108)]
    public long NextPrayerAllowedTime { get; set; }

    #region Patrol System Data

    /// <summary>
    ///     Current patrol combo count.
    ///     Increments each time a patrol is completed successfully.
    ///     Resets to 0 after combo timeout (2 hours).
    /// </summary>
    [ProtoMember(109)]
    public int PatrolComboCount { get; set; }

    /// <summary>
    ///     Timestamp when the last patrol was completed (in elapsed milliseconds).
    ///     Used to determine combo timeout and completion cooldown.
    /// </summary>
    [ProtoMember(110)]
    public long LastPatrolCompletionTime { get; set; }

    /// <summary>
    ///     The multiplier value from the previous patrol completion.
    ///     Used to detect tier changes for notifications.
    /// </summary>
    [ProtoMember(111)]
    public float PatrolPreviousMultiplier { get; set; } = 1.0f;

    #endregion

    #region Branch Commitment System

    /// <summary>
    ///     Branches the player has committed to, keyed by domain (as int for ProtoBuf).
    ///     Once a branch is committed, exclusive branches become locked.
    /// </summary>
    [ProtoMember(112)]
    private Dictionary<int, string> _committedBranchesSerializable = new();

    /// <summary>
    ///     Branches that are locked out due to exclusive branch choices.
    ///     Keyed by domain (as int), value is list of locked branch names.
    ///     Uses List instead of HashSet for ProtoBuf compatibility.
    /// </summary>
    [ProtoMember(113)]
    private Dictionary<int, List<string>> _lockedBranchesSerializable = new();

    /// <summary>
    ///     Gets the committed branch for a domain (thread-safe).
    /// </summary>
    public string? GetCommittedBranch(DeityDomain domain)
    {
        lock (Lock)
        {
            return _committedBranchesSerializable.GetValueOrDefault((int)domain);
        }
    }

    /// <summary>
    ///     Checks if a branch is locked in a domain (thread-safe).
    /// </summary>
    public bool IsBranchLocked(DeityDomain domain, string branch)
    {
        lock (Lock)
        {
            return _lockedBranchesSerializable.TryGetValue((int)domain, out var locked)
                   && locked.Contains(branch);
        }
    }

    /// <summary>
    ///     Gets all locked branches for a domain (thread-safe).
    ///     Returns empty collection if no branches are locked.
    /// </summary>
    public IReadOnlyCollection<string> GetLockedBranches(DeityDomain domain)
    {
        lock (Lock)
        {
            if (_lockedBranchesSerializable.TryGetValue((int)domain, out var locked))
            {
                return locked.ToList();
            }
            return Array.Empty<string>();
        }
    }

    /// <summary>
    ///     Commits to a branch in a domain, locking out exclusive branches.
    ///     Does nothing if already committed to a branch in this domain.
    ///     Thread-safe.
    /// </summary>
    public void CommitToBranch(DeityDomain domain, string branch, IEnumerable<string>? exclusiveBranches)
    {
        if (string.IsNullOrEmpty(branch))
            return;

        lock (Lock)
        {
            var domainKey = (int)domain;

            // Only commit if not already committed to a branch in this domain
            if (_committedBranchesSerializable.ContainsKey(domainKey))
                return;

            _committedBranchesSerializable[domainKey] = branch;

            if (exclusiveBranches != null)
            {
                if (!_lockedBranchesSerializable.TryGetValue(domainKey, out var lockedList))
                {
                    lockedList = new List<string>();
                    _lockedBranchesSerializable[domainKey] = lockedList;
                }

                foreach (var excludedBranch in exclusiveBranches)
                {
                    if (!lockedList.Contains(excludedBranch))
                    {
                        lockedList.Add(excludedBranch);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Clears all branch commitments (used by admin commands).
    ///     Thread-safe.
    /// </summary>
    public void ClearBranchCommitments()
    {
        lock (Lock)
        {
            _committedBranchesSerializable.Clear();
            _lockedBranchesSerializable.Clear();
        }
    }

    /// <summary>
    ///     Clears branch commitments for a specific domain.
    ///     Thread-safe.
    /// </summary>
    public void ClearBranchCommitmentsForDomain(DeityDomain domain)
    {
        lock (Lock)
        {
            var domainKey = (int)domain;
            _committedBranchesSerializable.Remove(domainKey);
            _lockedBranchesSerializable.Remove(domainKey);
        }
    }

    #endregion

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
    ///     Also clears branch commitments since branches are domain-specific.
    ///     Thread-safe.
    /// </summary>
    public void ApplySwitchPenalty()
    {
        lock (Lock)
        {
            Favor = 0;
            _unlockedBlessings.Clear();
            _committedBranchesSerializable.Clear();
            _lockedBranchesSerializable.Clear();
        }
    }
}