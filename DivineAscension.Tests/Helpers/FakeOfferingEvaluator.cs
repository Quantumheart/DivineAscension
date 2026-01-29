using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IOfferingEvaluator for testing.
/// Provides configurable return values for offering evaluation.
/// </summary>
public class FakeOfferingEvaluator : IOfferingEvaluator
{
    /// <summary>
    /// Default value to return when no specific configuration is set.
    /// </summary>
    public int DefaultReturnValue { get; set; } = 0;

    /// <summary>
    /// Dictionary mapping item codes to their configured return values.
    /// Key format: "{itemCode}:{domain}" (e.g., "game:ingot-copper:Craft")
    /// </summary>
    private readonly Dictionary<string, int> _configuredValues = new();

    /// <summary>
    /// List of all calls made to CalculateOfferingValue for verification.
    /// </summary>
    public List<(ItemStack Offering, DeityDomain Domain, int HolySiteTier)> Calls { get; } = new();

    /// <inheritdoc />
    public int CalculateOfferingValue(ItemStack offering, DeityDomain domain, int holySiteTier)
    {
        Calls.Add((offering, domain, holySiteTier));

        var itemCode = offering?.Collectible?.Code?.ToString() ?? string.Empty;
        var key = $"{itemCode}:{domain}";

        if (_configuredValues.TryGetValue(key, out var configuredValue))
        {
            return configuredValue;
        }

        return DefaultReturnValue;
    }

    /// <summary>
    /// Configures a specific return value for an item code and domain combination.
    /// </summary>
    /// <param name="itemCode">The item code to configure</param>
    /// <param name="domain">The deity domain</param>
    /// <param name="value">The value to return (positive = valid, -1 = tier rejected, 0 = invalid)</param>
    public void SetOfferingValue(string itemCode, DeityDomain domain, int value)
    {
        _configuredValues[$"{itemCode}:{domain}"] = value;
    }

    /// <summary>
    /// Clears all configured values and recorded calls.
    /// </summary>
    public void Reset()
    {
        _configuredValues.Clear();
        Calls.Clear();
        DefaultReturnValue = 0;
    }
}
