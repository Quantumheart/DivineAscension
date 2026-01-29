using System;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
/// Evaluates offering items and calculates their favor value based on domain and holy site tier.
/// </summary>
public class OfferingEvaluator : IOfferingEvaluator
{
    private readonly IOfferingLoader _offeringLoader;

    public OfferingEvaluator(IOfferingLoader offeringLoader)
    {
        _offeringLoader = offeringLoader ?? throw new ArgumentNullException(nameof(offeringLoader));
    }

    /// <inheritdoc />
    public int CalculateOfferingValue(ItemStack offering, DeityDomain domain, int holySiteTier)
    {
        var fullCode = offering.Collectible?.Code?.ToString() ?? string.Empty;

        var match = _offeringLoader.FindOfferingByItemCode(fullCode, domain);

        if (match == null)
            return 0; // Not a valid offering for this domain

        // Tier gating: check minimum holy site tier requirement
        if (holySiteTier < match.MinHolySiteTier)
            return -1; // Special value to indicate "rejected by tier gate"

        return match.Value;
    }
}
