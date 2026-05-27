using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Current prestige rank of the religion
    /// </summary>
    [ProtoMember(6)]
    public PrestigeRank PrestigeRank { get; set; } = PrestigeRank.Fledgling;

    /// <summary>
    ///     Current prestige points
    /// </summary>
    [ProtoMember(7)]
    public int Prestige { get; set; }

    /// <summary>
    ///     Total prestige earned (lifetime stat, used for ranking)
    /// </summary>
    [ProtoMember(8)]
    public int TotalPrestige { get; set; }

    /// <summary>
    ///     Accumulated fractional prestige (not yet awarded).
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    [ProtoMember(20)]
    public float AccumulatedFractionalPrestige { get; set; }

    /// <summary>
    ///     Adds fractional prestige and updates statistics when accumulated amount >= 1.
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    public void AddFractionalPrestige(float amount)
    {
        lock (Lock)
        {
            if (amount > 0)
            {
                AccumulatedFractionalPrestige += amount;

                // Award integer prestige when we have accumulated >= 1.0
                if (AccumulatedFractionalPrestige >= 1.0f)
                {
                    var prestigeToAward = (int)AccumulatedFractionalPrestige;
                    AccumulatedFractionalPrestige -= prestigeToAward; // Keep the fractional remainder

                    Prestige += prestigeToAward;
                    TotalPrestige += prestigeToAward;
                }
            }
        }
    }

    /// <summary>
    ///     Updates the prestige rank based on total prestige earned
    /// </summary>
    public void UpdatePrestigeRank()
    {
        PrestigeRank = TotalPrestige switch
        {
            >= 50000 => PrestigeRank.Mythic,
            >= 25000 => PrestigeRank.Legendary,
            >= 10000 => PrestigeRank.Renowned,
            >= 2500 => PrestigeRank.Established,
            _ => PrestigeRank.Fledgling
        };
    }

    /// <summary>
    ///     Adds prestige and updates statistics
    /// </summary>
    public void AddPrestige(int amount)
    {
        lock (Lock)
        {
            if (amount > 0)
            {
                Prestige += amount;
                TotalPrestige += amount;
                UpdatePrestigeRank();
            }
        }
    }

    /// <summary>
    ///     Removes prestige (for costs like unlocking religion blessings).
    ///     Thread-safe.
    /// </summary>
    /// <param name="amount">Amount of prestige to remove</param>
    /// <returns>True if successful, false if insufficient prestige</returns>
    public bool RemovePrestige(int amount)
    {
        lock (Lock)
        {
            if (Prestige >= amount)
            {
                Prestige -= amount;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Refunds prestige when a religion blessing is struck (#479, slice 5). Credits spendable
    ///     prestige only — <see cref="TotalPrestige"/> is untouched, so the prestige rank cannot
    ///     flicker down. Mirrors the personal-side spendable-favor refund on unlearn.
    ///     Thread-safe.
    /// </summary>
    public void RefundPrestige(int amount)
    {
        lock (Lock)
        {
            if (amount > 0)
            {
                Prestige += amount;
            }
        }
    }
}
