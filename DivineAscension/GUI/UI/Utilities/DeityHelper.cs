using System;
using System.Numerics;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized helper for deity-related UI operations
///     Provides deity colors, titles, and lists for consistent UI presentation
/// </summary>
internal static class DeityHelper
{
    /// <summary>
    ///     All domain names in order (Utility-focused system - 4 domains)
    /// </summary>
    public static readonly string[] DomainNames =
    {
        "Craft", "Wild", "Harvest", "Stone"
    };

    /// <summary>
    ///     Legacy: All deity names (now domain names) - alias for DomainNames
    /// </summary>
    public static string[] DeityNames => DomainNames;

    /// <summary>
    ///     Get the thematic color for a domain (by name string)
    /// </summary>
    public static Vector4 GetDomainColor(string domain)
    {
        return domain switch
        {
            "Craft" => new Vector4(0.8f, 0.2f, 0.2f, 1.0f), // Red - Forge & Craft
            "Wild" => new Vector4(0.4f, 0.8f, 0.3f, 1.0f), // Green - Hunt & Wild
            "Harvest" => new Vector4(0.9f, 0.9f, 0.6f, 1.0f), // Light yellow - Agriculture & Light
            "Stone" => new Vector4(0.5f, 0.4f, 0.2f, 1.0f), // Brown - Earth & Stone
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f) // Grey - Unknown
        };
    }

    /// <summary>
    ///     Get the thematic color for a domain (by name string) - alias for GetDomainColor
    /// </summary>
    public static Vector4 GetDeityColor(string domain) => GetDomainColor(domain);

    /// <summary>
    ///     Get the domain title (by name string) - alias for GetDomainTitle
    /// </summary>
    public static string GetDeityTitle(string domain) => GetDomainTitle(domain);

    /// <summary>
    ///     Convert domain name string to DeityDomain enum - alias for ParseDomain
    /// </summary>
    public static DeityDomain ParseDeityType(string domainName) => ParseDomain(domainName);

    /// <summary>
    ///     Get the thematic color for a domain (by enum)
    /// </summary>
    public static Vector4 GetDeityColor(DeityDomain domain)
    {
        return domain switch
        {
            DeityDomain.Craft => new Vector4(0.8f, 0.2f, 0.2f, 1.0f), // Red - Forge & Craft
            DeityDomain.Wild => new Vector4(0.4f, 0.8f, 0.3f, 1.0f), // Green - Hunt & Wild
            DeityDomain.Harvest => new Vector4(0.9f, 0.9f, 0.6f, 1.0f), // Light yellow - Agriculture & Light
            DeityDomain.Stone => new Vector4(0.5f, 0.4f, 0.2f, 1.0f), // Brown - Earth & Stone
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f) // Grey - Unknown
        };
    }

    /// <summary>
    ///     Get the domain title/description (by domain name string)
    /// </summary>
    public static string GetDomainTitle(string domain)
    {
        return domain switch
        {
            "Craft" => "Domain of the Forge & Craft",
            "Wild" => "Domain of the Hunt & Wild",
            "Harvest" => "Domain of Agriculture & Light",
            "Stone" => "Domain of Earth & Stone",
            _ => "Unknown Domain"
        };
    }

    /// <summary>
    ///     Get the domain title/description (by enum)
    /// </summary>
    public static string GetDeityTitle(DeityDomain domain)
    {
        return domain switch
        {
            DeityDomain.Craft => "Domain of the Forge & Craft",
            DeityDomain.Wild => "Domain of the Hunt & Wild",
            DeityDomain.Harvest => "Domain of Agriculture & Light",
            DeityDomain.Stone => "Domain of Earth & Stone",
            _ => "Unknown Domain"
        };
    }

    /// <summary>
    ///     Convert domain name string to DeityDomain enum
    /// </summary>
    public static DeityDomain ParseDomain(string domainName)
    {
        if (Enum.TryParse<DeityDomain>(domainName, true, out var domain))
            return domain;
        return DeityDomain.None;
    }

    /// <summary>
    ///     Get formatted display text for a deity with custom name
    ///     Format: "DeityName - Domain of X" (e.g., "Khoras - Domain of the Forge & Craft")
    /// </summary>
    public static string GetDeityDisplayText(string deityName, string domain)
    {
        if (string.IsNullOrWhiteSpace(deityName))
            return GetDomainTitle(domain);
        return $"{deityName} - {GetDomainTitle(domain)}";
    }

    /// <summary>
    ///     Get formatted display text for a deity with custom name (enum version)
    ///     Format: "DeityName - Domain of X" (e.g., "Khoras - Domain of the Forge & Craft")
    /// </summary>
    public static string GetDeityDisplayText(string deityName, DeityDomain domain)
    {
        if (string.IsNullOrWhiteSpace(deityName))
            return GetDeityTitle(domain);
        return $"{deityName} - {GetDeityTitle(domain)}";
    }

    /// <summary>
    ///     Get formatted display text using just the domain (for backward compatibility)
    ///     Format: "Domain - Domain of X" (e.g., "Craft - Domain of the Forge & Craft")
    /// </summary>
    public static string GetDeityDisplayText(DeityDomain domain)
    {
        return $"{domain} - {GetDeityTitle(domain)}";
    }
}