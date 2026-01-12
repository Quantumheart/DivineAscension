using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
/// Provides deity information for UI tooltips
/// Contains hardcoded deity data to avoid dependency on incomplete DeityRegistry
/// </summary>
internal static class DeityInfoHelper
{
    /// <summary>
    /// Get deity information by name
    /// </summary>
    public static DeityInfo? GetDeityInfo(string deityName)
    {
        var deityType = DeityHelper.ParseDeityType(deityName);
        return GetDeityInfo(deityType);
    }

    /// <summary>
    /// Get deity information by type
    /// </summary>
    public static DeityInfo? GetDeityInfo(DeityDomain deityDomain)
    {
        return deityDomain switch
        {
            DeityDomain.Craft => new DeityInfo(
                Name: "Craft",
                Title: "God of the Forge & Craft",
                Domain: "Forge & Craft",
                Description: "The domain of forging and crafting, rewarding those who work the forge."
            ),
            DeityDomain.Wild => new DeityInfo(
                Name: "Wild",
                Title: "Domain of the Hunt & Wild",
                Domain: "Hunt & Wild",
                Description: "The domain of the Hunt, rewarding patience, precision, and tracking."
            ),
            DeityDomain.Harvest => new DeityInfo(
                Name: "Harvest",
                Title: "Goddess of Agriculture & Light",
                Domain: "Agriculture & Light",
                Description: "The domain of cultivation and growth through light and warmth."
            ),
            DeityDomain.Stone => new DeityInfo(
                Name: "Stone",
                Title: "Goddess of Earth & Stone",
                Domain: "Earth & Stone",
                Description: "The domain representing the transformative power of working with clay and earth."
            ),
            _ => null
        };
    }
}

/// <summary>
/// Immutable record containing deity information for tooltips
/// </summary>
internal record DeityInfo(string Name, string Title, string Domain, string Description);