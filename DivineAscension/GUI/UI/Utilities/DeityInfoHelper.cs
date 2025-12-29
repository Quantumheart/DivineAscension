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
    public static DeityInfo? GetDeityInfo(DeityType deityType)
    {
        return deityType switch
        {
            DeityType.Khoras => new DeityInfo(
                Name: "Khoras",
                Title: "God of the Forge & Craft",
                Domain: "Forge & Craft",
                Description: "The God of Forging and crafting, Khoras represents crafting."
            ),
            DeityType.Lysa => new DeityInfo(
                Name: "Lysa",
                Title: "Goddess of the Hunt & Wild",
                Domain: "Hunt & Wild",
                Description: "The Goddess of the Hunt, Lysa rewards patience, precision, and tracking."
            ),
            DeityType.Aethra => new DeityInfo(
                Name: "Aethra",
                Title: "Goddess of Agriculture & Light",
                Domain: "Agriculture & Light",
                Description: "Aethra represents cultivation and growth through light and warmth."
            ),
            DeityType.Gaia => new DeityInfo(
                Name: "Gaia",
                Title: "Goddess of Earth & Stone",
                Domain: "Earth & Stone",
                Description: "Gaia represents the transformative power of working with clay and earth."
            ),
            _ => null
        };
    }
}

/// <summary>
/// Immutable record containing deity information for tooltips
/// </summary>
internal record DeityInfo(string Name, string Title, string Domain, string Description);
