using DivineAscension.Models.Enum;
using Vintagestory.API.Common;

namespace DivineAscension.Services.Interfaces;

/// <summary>
/// Service for evaluating offering items and calculating their favor value.
/// Encapsulates the logic for determining if an item is a valid offering for a domain
/// and what bonus favor it provides.
/// </summary>
public interface IOfferingEvaluator
{
    /// <summary>
    /// Calculate the favor value of an offering for a specific domain and holy site tier.
    /// </summary>
    /// <param name="offering">The item stack being offered</param>
    /// <param name="domain">The deity domain to match against</param>
    /// <param name="holySiteTier">The tier of the holy site (1-3)</param>
    /// <returns>
    /// Positive value if valid and tier-acceptable (the favor bonus),
    /// -1 if rejected by tier gate (holy site tier too low for this offering),
    /// 0 if not a valid offering for this domain
    /// </returns>
    int CalculateOfferingValue(ItemStack offering, DeityDomain domain, int holySiteTier);
}
