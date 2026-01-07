using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Systems;

[ProtoContract]
public class PlayerProgressionData
{
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
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;


    /// <summary>
    ///     Current favor points
    /// </summary>
    [ProtoMember(2)]
    public int Favor { get; set; }

    /// <summary>
    ///     Total favor earned (lifetime stat, persists across religion changes)
    /// </summary>
    [ProtoMember(3)]
    public int TotalFavorEarned { get; set; }

    /// <summary>
    ///     Dictionary of unlocked player blessings
    ///     Key: blessing ID, Value: unlock status (true if unlocked)
    /// </summary>
    [ProtoMember(4)]
    public HashSet<string> UnlockedBlessings { get; set; } = new();


    /// <summary>
    ///     Data version for migration purposes
    /// </summary>
    [ProtoMember(5)]
    public int DataVersion { get; set; } = 3;

    /// <summary>
    ///     Accumulated fractional favor (not yet awarded) for passive generation
    /// </summary>
    [ProtoMember(6)]
    public float AccumulatedFractionalFavor { get; set; }

    // === COMPUTED PROPERTIES (not serialized) ===
    public FavorRank FavorRank => CalculateRank(TotalFavorEarned);

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
    ///     Unlocks a player blessing
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        UnlockedBlessings.Add(blessingId);
    }

    /// <summary>
    ///     Checks if a blessing is unlocked
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        return UnlockedBlessings.Any(id => id == blessingId);
    }

    /// <summary>
    ///     Clears all unlocked blessings (used when switching religions)
    /// </summary>
    public void ClearUnlockedBlessings()
    {
        UnlockedBlessings.Clear();
    }

    /// <summary>
    ///     Resets favor and blessings (penalty for switching religions)
    /// </summary>
    public void ApplySwitchPenalty()
    {
        Favor = 0;
        ClearUnlockedBlessings();
    }

    private static FavorRank CalculateRank(int totalFavor)
    {
        return totalFavor switch
        {
            >= 10000 => FavorRank.Avatar,
            >= 5000 => FavorRank.Champion,
            >= 2000 => FavorRank.Zealot,
            >= 500 => FavorRank.Disciple,
            _ => FavorRank.Initiate
        };
    }
}