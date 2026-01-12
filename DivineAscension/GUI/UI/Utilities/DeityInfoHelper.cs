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
    public static DomainInfo? GetDeityInfo(string deityName)
    {
        var deityType = DomainHelper.ParseDeityType(deityName);
        return GetDeityInfo(deityType);
    }

    /// <summary>
    /// Get deity information by type
    /// </summary>
    // todo: localize the strings in this text.
    public static DomainInfo? GetDeityInfo(DeityDomain deityDomain)
    {
        return deityDomain switch
        {
            DeityDomain.Craft => new DomainInfo(
                Name: "Craft",
                Description:
                "The domain of the Forge and Craft. Followers are rewarded for their dedication to metalworking, smithing, and the creation of tools and weapons. Those who shape raw materials into works of utility and art earn the favor of this domain."
            ),
            DeityDomain.Wild => new DomainInfo(
                Name: "Wild",
                Description:
                "The domain of the Hunt and Wild. Followers are rewarded for patience, precision, and tracking. Those who master the hunt and live in harmony with the wilderness earn the favor of this domain."
            ),
            DeityDomain.Harvest => new DomainInfo(
                Name: "Harvest",
                Description:
                "The domain of Agriculture and Light. Followers are rewarded for cultivation and nurturing growth through light and warmth. Those who tend the land and bring forth abundance earn the favor of this domain."
            ),
            DeityDomain.Stone => new DomainInfo(
                Name: "Stone",
                Description:
                "The domain of Earth and Stone. Followers are rewarded for the transformative art of working with clay and earth. Those who shape pottery, form clay, and honor the elements of the ground earn the favor of this domain."
            ),
            _ => null
        };
    }
}

/// <summary>
/// Immutable record containing deity information for tooltips
/// </summary>
internal record DomainInfo(string Name, string Description);