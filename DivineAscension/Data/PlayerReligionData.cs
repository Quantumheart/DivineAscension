using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Stores player-specific religion and favor data for persistence
/// </summary>
[ProtoContract]
public class PlayerReligionData
{
    /// <summary>
    ///     Creates new player religion data
    /// </summary>
    public PlayerReligionData(string playerUID)
    {
        PlayerUID = playerUID;
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public PlayerReligionData()
    {
    }

    /// <summary>
    ///     Player's unique identifier
    /// </summary>
    [ProtoMember(1)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     UID of the player's current religion (null if not in any religion)
    /// </summary>
    [ProtoMember(2)]
    public string? ReligionUID { get; set; } = null;

    /// <summary>
    ///     Currently active deity (cached from current religion for performance)
    /// </summary>
    [ProtoMember(3)]
    public DeityType ActiveDeity { get; set; } = DeityType.None;

    /// <summary>
    ///     Player's current favor rank (individual progression)
    /// </summary>
    [ProtoMember(4)]
    public FavorRank FavorRank { get; set; } = FavorRank.Initiate;

    /// <summary>
    ///     Current favor points
    /// </summary>
    [ProtoMember(5)]
    public int Favor { get; set; }

    /// <summary>
    ///     Total favor earned (lifetime stat, persists across religion changes)
    /// </summary>
    [ProtoMember(6)]
    public int TotalFavorEarned { get; set; }

    /// <summary>
    ///     Dictionary of unlocked player blessings
    ///     Key: blessing ID, Value: unlock status (true if unlocked)
    /// </summary>
    [ProtoMember(7)]
    public Dictionary<string, bool> UnlockedBlessings { get; set; } = new();

    /// <summary>
    ///     Last time the player switched religions (for cooldown tracking)
    /// </summary>
    [ProtoMember(8)]
    public DateTime? LastReligionSwitch { get; set; } = null;

    /// <summary>
    ///     Data version for migration purposes
    /// </summary>
    [ProtoMember(9)]
    public int DataVersion { get; set; } = 2; // Phase 3 format

    /// <summary>
    ///     Accumulated fractional favor (not yet awarded) for passive generation
    /// </summary>
    [ProtoMember(10)]
    public float AccumulatedFractionalFavor { get; set; }

    /// <summary>
    ///     Number of players killed in PvP
    /// </summary>
    [ProtoMember(11)]
    public int KillCount { get; set; } = 0;

    /// <summary>
    ///     Checks if player has a religion
    /// </summary>
    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(ReligionUID);
    }

    /// <summary>
    ///     Updates the favor rank based on total favor earned
    /// </summary>
    public void UpdateFavorRank()
    {
        FavorRank = TotalFavorEarned switch
        {
            >= 10000 => FavorRank.Avatar, // 10000+ = Avatar (Rank 4)
            >= 5000 => FavorRank.Champion, // 5000-9999 = Champion (Rank 3)
            >= 2000 => FavorRank.Zealot, // 2000-4999 = Zealot (Rank 2)
            >= 500 => FavorRank.Disciple, // 500-1999 = Disciple (Rank 1)
            _ => FavorRank.Initiate // 0-499 = Initiate (Rank 0)
        };
    }

    /// <summary>
    ///     Adds favor and updates statistics
    /// </summary>
    public void AddFavor(int amount)
    {
        if (amount > 0)
        {
            Favor += amount;
            TotalFavorEarned += amount;
            UpdateFavorRank();
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
                UpdateFavorRank();
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
        UnlockedBlessings[blessingId] = true;
    }

    /// <summary>
    ///     Checks if a blessing is unlocked
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        return UnlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;
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
        FavorRank = FavorRank.Initiate;
        ClearUnlockedBlessings();
    }
}