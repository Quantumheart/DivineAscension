using System;
using PantheonWars.Models.Enum;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     Stores player-specific deity data for persistence (LEGACY - Phase 1-2)
///     DEPRECATED: Use PlayerReligionData from Phase 3 religion system instead
///     This model will be removed in v2.0.0
/// </summary>
[Obsolete("Use PlayerReligionData instead. This class will be removed in v2.0.0")]
[ProtoContract]
public class PlayerDeityData
{
    public PlayerDeityData()
    {
    }

    public PlayerDeityData(string playerUID)
    {
        PlayerUID = playerUID;
        PledgeDate = DateTime.UtcNow;
    }

    /// <summary>
    ///     Player's unique identifier
    /// </summary>
    [ProtoMember(1)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     Currently pledged deity
    /// </summary>
    [ProtoMember(2)]
    public DeityType DeityType { get; set; } = DeityType.None;

    /// <summary>
    ///     Current divine favor amount
    /// </summary>
    [ProtoMember(3)]
    public int DivineFavor { get; set; }

    /// <summary>
    ///     Current devotion rank
    /// </summary>
    [ProtoMember(4)]
    public DevotionRank DevotionRank { get; set; } = DevotionRank.Initiate;

    /// <summary>
    ///     Total favor earned (lifetime stat)
    /// </summary>
    [ProtoMember(5)]
    public int TotalFavorEarned { get; set; }

    /// <summary>
    ///     Total kills while pledged to current deity
    /// </summary>
    [ProtoMember(6)]
    public int KillCount { get; set; } = 0;

    /// <summary>
    ///     Timestamp when player first pledged to current deity
    /// </summary>
    [ProtoMember(7)]
    public DateTime PledgeDate { get; set; } = DateTime.MinValue;

    /// <summary>
    ///     Last time player switched deities (for cooldown tracking)
    /// </summary>
    [ProtoMember(8)]
    public DateTime? LastDeitySwitch { get; set; } = null;

    /// <summary>
    ///     Version number for data migration
    /// </summary>
    [ProtoMember(9)]
    public int DataVersion { get; set; } = 1;

    /// <summary>
    ///     Accumulated fractional favor (not yet awarded)
    /// </summary>
    [ProtoMember(10)]
    public float AccumulatedFractionalFavor { get; set; }

    /// <summary>
    ///     Checks if player has pledged to a deity
    /// </summary>
    public bool HasDeity()
    {
        return DeityType != DeityType.None;
    }

    /// <summary>
    ///     Updates the devotion rank based on total favor earned
    /// </summary>
    public void UpdateDevotionRank()
    {
        // Devotion rank thresholds
        DevotionRank = TotalFavorEarned switch
        {
            >= 10000 => DevotionRank.Avatar,
            >= 5000 => DevotionRank.Champion,
            >= 2000 => DevotionRank.Zealot,
            >= 500 => DevotionRank.Disciple,
            _ => DevotionRank.Initiate
        };
    }

    /// <summary>
    ///     Adds favor and updates statistics (integer version)
    /// </summary>
    public void AddFavor(int amount)
    {
        if (amount > 0)
        {
            DivineFavor += amount;
            TotalFavorEarned += amount;
            UpdateDevotionRank();
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

                DivineFavor += favorToAward;
                TotalFavorEarned += favorToAward;
                UpdateDevotionRank();
            }
        }
    }

    /// <summary>
    ///     Removes favor (for ability costs or penalties)
    /// </summary>
    public bool RemoveFavor(int amount)
    {
        if (DivineFavor >= amount)
        {
            DivineFavor -= amount;
            return true;
        }

        return false;
    }
}