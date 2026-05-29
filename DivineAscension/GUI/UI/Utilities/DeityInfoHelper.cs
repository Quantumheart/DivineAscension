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
    // todo: localize the strings in this text (TooltipDescription lives in DeityDomainRegistry).
    public static DomainInfo? GetDeityInfo(DeityDomain deityDomain)
    {
        return DeityDomainRegistry.TryGet(deityDomain, out var meta)
            ? new DomainInfo(deityDomain.ToString(), meta.TooltipDescription)
            : null;
    }
}

/// <summary>
/// Immutable record containing deity information for tooltips
/// </summary>
internal record DomainInfo(string Name, string Description);