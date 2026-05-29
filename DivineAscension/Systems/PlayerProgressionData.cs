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
    [ProtoIgnore] private object? _lock;

    /// <summary>
    ///     Backing field for unlocked player blessings.
    /// </summary>
    [ProtoMember(103)] private HashSet<string> _unlockedBlessings = new();

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
    ///     Player's unique identifier
    /// </summary>
    [ProtoMember(100)]
    public string Id { get; set; } = string.Empty;


    /// <summary>
    ///     Current favor points per deity domain.
    /// </summary>
    [ProtoMember(101)]
    public Dictionary<DeityDomain, int> FavorByDeity { get; set; } = new();

    /// <summary>
    ///     Total favor earned per deity domain (lifetime stat, persists across religion changes).
    /// </summary>
    [ProtoMember(102)]
    public Dictionary<DeityDomain, int> TotalFavorEarnedByDeity { get; set; } = new();

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
    ///     Data version (internal bookkeeping only — Pantheon 2.0 no longer carries cross-version save migrations).
    /// </summary>
    [ProtoMember(104)]
    public int DataVersion { get; set; } = 9;

    /// <summary>
    ///     Soft cap on tracked discovered chunks per player. Past this, <see cref="TryAddDiscoveredChunk"/>
    ///     stops awarding to bound memory growth for explorers who roam tens of thousands of chunks.
    /// </summary>
    public const int DiscoveredChunksSoftCap = 100_000;

    /// <summary>
    ///     Soft cap on tracked trader entity IDs per player. Mirrors <see cref="DiscoveredChunksSoftCap"/>
    ///     but for the Caravan first-trader-encounter bonus.
    /// </summary>
    public const int DiscoveredTradersSoftCap = 10_000;

    /// <summary>
    ///     Accumulated fractional favor per deity domain (not yet awarded) for passive generation.
    /// </summary>
    [ProtoMember(105)]
    public Dictionary<DeityDomain, float> AccumulatedFractionalFavorByDeity { get; set; } = new();

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
    ///     [DEPRECATED v5] Timestamp when the player is next allowed to pray (in elapsed milliseconds).
    ///     Kept for one version to allow migration. Use NextPrayerAllowedTimeUtc instead.
    /// </summary>
    [ProtoMember(108)]
    public long NextPrayerAllowedTime { get; set; }

    /// <summary>
    ///     Gets current favor for a domain (thread-safe).
    /// </summary>
    public int GetFavor(DeityDomain domain)
    {
        lock (Lock)
        {
            return FavorByDeity.GetValueOrDefault(domain);
        }
    }

    /// <summary>
    ///     Gets lifetime total favor earned for a domain (thread-safe).
    /// </summary>
    public int GetTotalFavorEarned(DeityDomain domain)
    {
        lock (Lock)
        {
            return TotalFavorEarnedByDeity.GetValueOrDefault(domain);
        }
    }

    /// <summary>
    ///     Gets accumulated fractional favor for a domain (thread-safe).
    /// </summary>
    public float GetAccumulatedFractionalFavor(DeityDomain domain)
    {
        lock (Lock)
        {
            return AccumulatedFractionalFavorByDeity.GetValueOrDefault(domain);
        }
    }

    /// <summary>
    ///     Adds favor for a deity and updates lifetime statistics.
    ///     Thread-safe.
    /// </summary>
    public void AddFavor(DeityDomain domain, int amount)
    {
        if (amount <= 0)
            return;

        lock (Lock)
        {
            FavorByDeity[domain] = FavorByDeity.GetValueOrDefault(domain) + amount;
            TotalFavorEarnedByDeity[domain] = TotalFavorEarnedByDeity.GetValueOrDefault(domain) + amount;
        }
    }

    /// <summary>
    ///     Adds fractional favor for a deity and awards integer favor when accumulated >= 1.
    ///     Thread-safe.
    /// </summary>
    public void AddFractionalFavor(DeityDomain domain, float amount)
    {
        if (amount <= 0)
            return;

        lock (Lock)
        {
            var accumulated = AccumulatedFractionalFavorByDeity.GetValueOrDefault(domain) + amount;

            if (accumulated >= 1.0f)
            {
                var favorToAward = (int)accumulated;
                accumulated -= favorToAward;

                FavorByDeity[domain] = FavorByDeity.GetValueOrDefault(domain) + favorToAward;
                TotalFavorEarnedByDeity[domain] = TotalFavorEarnedByDeity.GetValueOrDefault(domain) + favorToAward;
            }

            AccumulatedFractionalFavorByDeity[domain] = accumulated;
        }
    }

    /// <summary>
    ///     Sets current favor for a deity (admin/testing path). Thread-safe.
    /// </summary>
    public void SetFavor(DeityDomain domain, int amount)
    {
        lock (Lock)
        {
            FavorByDeity[domain] = amount;
        }
    }

    /// <summary>
    ///     Sets total favor earned for a deity (admin/testing path). Thread-safe.
    /// </summary>
    public void SetTotalFavorEarned(DeityDomain domain, int amount)
    {
        lock (Lock)
        {
            TotalFavorEarnedByDeity[domain] = amount;
        }
    }

    /// <summary>
    ///     Removes favor for a deity (for costs or penalties).
    ///     Returns false if insufficient favor.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveFavor(DeityDomain domain, int amount)
    {
        lock (Lock)
        {
            var current = FavorByDeity.GetValueOrDefault(domain);
            if (current >= amount)
            {
                FavorByDeity[domain] = current - amount;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Adds favor to spendable balance only, without touching lifetime totals.
    ///     Used for unlearn refunds so favor rank cannot flicker (epic #425, decision 1).
    ///     Thread-safe.
    /// </summary>
    public void AddSpendableFavor(DeityDomain domain, int amount)
    {
        if (amount <= 0)
            return;

        lock (Lock)
        {
            FavorByDeity[domain] = FavorByDeity.GetValueOrDefault(domain) + amount;
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
    ///     Resets favor for the abandoned deity and clears blessings + branch commitments
    ///     (penalty for switching religions). Favor with other deities is preserved.
    ///     Thread-safe.
    /// </summary>
    public void ApplySwitchPenalty(DeityDomain abandonedDomain)
    {
        lock (Lock)
        {
            FavorByDeity.Remove(abandonedDomain);
            AccumulatedFractionalFavorByDeity.Remove(abandonedDomain);
            _unlockedBlessings.Clear();
            _committedBranchesSerializable.Clear();
            _lockedBranchesSerializable.Clear();
        }
    }

    #region Patrol System Data

    /// <summary>
    ///     Current patrol combo count.
    ///     Increments each time a patrol is completed successfully.
    ///     Resets to 0 after combo timeout (2 hours).
    /// </summary>
    [ProtoMember(109)]
    public int PatrolComboCount { get; set; }

    /// <summary>
    ///     [DEPRECATED v5] Timestamp when the last patrol was completed (in elapsed milliseconds).
    ///     Kept for one version to allow migration. Use LastPatrolCompletionTimeUtc instead.
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
    [ProtoMember(112)] private Dictionary<int, string> _committedBranchesSerializable = new();

    /// <summary>
    ///     Branches that are locked out due to exclusive branch choices.
    ///     Keyed by domain (as int), value is list of locked branch names.
    ///     Uses List instead of HashSet for ProtoBuf compatibility.
    /// </summary>
    [ProtoMember(113)] private Dictionary<int, List<string>> _lockedBranchesSerializable = new();

    /// <summary>
    ///     UTC timestamp when the player is next allowed to pray.
    ///     Uses wall-clock time to survive server restarts.
    /// </summary>
    [ProtoMember(114)]
    public DateTime? NextPrayerAllowedTimeUtc { get; set; }

    /// <summary>
    ///     UTC timestamp when the last patrol was completed.
    ///     Uses wall-clock time to survive server restarts.
    ///     Used for both cooldown (1 hour) and combo timeout (2 hours).
    /// </summary>
    [ProtoMember(115)]
    public DateTime? LastPatrolCompletionTimeUtc { get; set; }

    /// <summary>
    ///     Set of chunk keys the player has visited (packed long form from <c>ChunkPos.ToChunkIndex3D</c>).
    ///     Drives the Wayfaring favor source for the Caravan domain. v6 saves load with an empty set
    ///     (no retroactive rewards).
    /// </summary>
    [ProtoMember(116)] private HashSet<long> _discoveredChunks = new();

    /// <summary>
    ///     Set of trader entity IDs the player has earned the first-encounter bonus from
    ///     (Caravan Wayfaring branch).
    /// </summary>
    [ProtoMember(117)] private HashSet<long> _discoveredTraderEntityIds = new();

    /// <summary>
    ///     Currently-placed Caravan Shrine position, or <c>null</c> when the player has no
    ///     shrine placed. Enforces the one-shrine-per-player rule across save/reload.
    ///     Stored as a 3-int triple so negative world coords round-trip cleanly.
    /// </summary>
    [ProtoMember(118)] public int? PlacedCaravanShrineX { get; set; }

    [ProtoMember(119)] public int? PlacedCaravanShrineY { get; set; }

    [ProtoMember(120)] public int? PlacedCaravanShrineZ { get; set; }

    /// <summary>
    ///     True when the player currently has a Caravan Shrine placed in the world.
    /// </summary>
    [ProtoIgnore]
    public bool HasPlacedCaravanShrine =>
        PlacedCaravanShrineX.HasValue && PlacedCaravanShrineY.HasValue && PlacedCaravanShrineZ.HasValue;

    /// <summary>
    ///     Records the position of the player's newly-placed Caravan Shrine. Thread-safe.
    /// </summary>
    public void SetPlacedCaravanShrine(int x, int y, int z)
    {
        lock (Lock)
        {
            PlacedCaravanShrineX = x;
            PlacedCaravanShrineY = y;
            PlacedCaravanShrineZ = z;
        }
    }

    /// <summary>
    ///     Clears the recorded Caravan Shrine position. Thread-safe.
    /// </summary>
    public void ClearPlacedCaravanShrine()
    {
        lock (Lock)
        {
            PlacedCaravanShrineX = null;
            PlacedCaravanShrineY = null;
            PlacedCaravanShrineZ = null;
        }
    }

    /// <summary>
    ///     True when the recorded shrine pos matches the supplied coords. Thread-safe.
    /// </summary>
    public bool IsPlacedCaravanShrineAt(int x, int y, int z)
    {
        lock (Lock)
        {
            return PlacedCaravanShrineX == x && PlacedCaravanShrineY == y && PlacedCaravanShrineZ == z;
        }
    }

    /// <summary>
    ///     Count of trader entities already credited for first-encounter bonus (thread-safe).
    /// </summary>
    public int DiscoveredTraderCount
    {
        get
        {
            lock (Lock)
            {
                return _discoveredTraderEntityIds.Count;
            }
        }
    }

    /// <summary>
    ///     Checks if the player has already encountered a trader entity (thread-safe).
    /// </summary>
    public bool HasDiscoveredTrader(long entityId)
    {
        lock (Lock)
        {
            return _discoveredTraderEntityIds.Contains(entityId);
        }
    }

    /// <summary>
    ///     Records a trader-entity first encounter. Returns <c>true</c> if newly added and the caller
    ///     should award the bonus. <c>false</c> if already known, or if <see cref="DiscoveredTradersSoftCap"/>
    ///     has been reached. Thread-safe.
    /// </summary>
    public bool TryAddDiscoveredTrader(long entityId)
    {
        lock (Lock)
        {
            if (_discoveredTraderEntityIds.Count >= DiscoveredTradersSoftCap)
                return false;

            return _discoveredTraderEntityIds.Add(entityId);
        }
    }

    /// <summary>
    ///     Count of discovered chunks (thread-safe).
    /// </summary>
    public int DiscoveredChunkCount
    {
        get
        {
            lock (Lock)
            {
                return _discoveredChunks.Count;
            }
        }
    }

    /// <summary>
    ///     Checks if the player has already visited a chunk (thread-safe).
    /// </summary>
    public bool HasDiscoveredChunk(long chunkKey)
    {
        lock (Lock)
        {
            return _discoveredChunks.Contains(chunkKey);
        }
    }

    /// <summary>
    ///     Records a chunk visit. Returns <c>true</c> if the chunk was newly added and the caller
    ///     should award favor. Returns <c>false</c> if the chunk was already known, or if the soft
    ///     cap (<see cref="DiscoveredChunksSoftCap"/>) has been reached. Thread-safe.
    /// </summary>
    public bool TryAddDiscoveredChunk(long chunkKey)
    {
        lock (Lock)
        {
            if (_discoveredChunks.Count >= DiscoveredChunksSoftCap)
                return false;

            return _discoveredChunks.Add(chunkKey);
        }
    }

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

    #region Granted Trait Codes (#559)

    /// <summary>
    ///     VS character-trait codes granted to this player by Divine Ascension
    ///     (religion membership, blessings, favor tiers — wired in by later slices).
    ///     Mirrors what is written into <c>EntityPlayer.WatchedAttributes["extraTraits"]</c>;
    ///     re-applied on player join so stats survive restart.
    /// </summary>
    [ProtoMember(121)] private HashSet<string> _grantedTraitCodes = new();

    /// <summary>
    ///     Snapshot of granted trait codes for thread-safe enumeration.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyCollection<string> GrantedTraitCodes
    {
        get
        {
            lock (Lock)
            {
                return _grantedTraitCodes.ToList();
            }
        }
    }

    /// <summary>
    ///     Adds a granted trait code. Returns true if it was newly added. Thread-safe.
    /// </summary>
    public bool AddGrantedTraitCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
        lock (Lock)
        {
            return _grantedTraitCodes.Add(code);
        }
    }

    /// <summary>
    ///     Removes a granted trait code. Returns true if it was present. Thread-safe.
    /// </summary>
    public bool RemoveGrantedTraitCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
        lock (Lock)
        {
            return _grantedTraitCodes.Remove(code);
        }
    }

    /// <summary>
    ///     Whether the given trait code has been granted by Divine Ascension. Thread-safe.
    /// </summary>
    public bool HasGrantedTraitCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
        lock (Lock)
        {
            return _grantedTraitCodes.Contains(code);
        }
    }

    #endregion
}