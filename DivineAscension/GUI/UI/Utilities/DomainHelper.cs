using System;
using System.Numerics;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized helper for deity-related UI operations
///     Provides deity colors, titles, and lists for consistent UI presentation
/// </summary>
internal static class DomainHelper
{
    /// <summary>
    ///     All domain names in order (6 domains)
    /// </summary>
    public static readonly string[] DomainNames =
    {
        "Craft", "Wild", "Conquest", "Harvest", "Stone", "Caravan"
    };

    /// <summary>
    ///     Legacy: All deity names (now domain names) - alias for DomainNames
    /// </summary>
    public static string[] DeityNames => DomainNames;

    /// <summary>
    ///     Get the thematic color for a domain (by name string). Tints are
    ///     earthed so each domain reads as a manuscript ink rather than a
    ///     saturated UI colour — they sit on the parchment page next to
    ///     the gold / lapis / vermilion accent inks without clashing.
    /// </summary>
    public static Vector4 GetDomainColor(string domain)
    {
        return domain switch
        {
            "Craft" => new Vector4(0.698f, 0.416f, 0.165f, 1.0f), // #B26A2A copper — Forge & Craft
            "Wild" => new Vector4(0.361f, 0.431f, 0.165f, 1.0f), // #5C6E2A olive — Hunt & Wild
            "Conquest" => new Vector4(0.557f, 0.180f, 0.122f, 1.0f), // #8E2E1F dried-blood red — Domination & Victory
            "Harvest" => new Vector4(0.627f, 0.463f, 0.157f, 1.0f), // #A07628 wheat ochre — Agriculture & Light
            "Stone" => new Vector4(0.369f, 0.329f, 0.282f, 1.0f), // #5E5448 warm slate — Earth & Stone
            "Caravan" => new Vector4(0.761f, 0.541f, 0.118f, 1.0f), // #C28A1E road ochre — Trade & Wayfaring
            _ => new Vector4(0.659f, 0.580f, 0.447f, 1.0f) // #A89472 faded ink — Unknown
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
    ///     Get the thematic color for a domain (by enum). See
    ///     <see cref="GetDomainColor(string)" /> for the manuscript-ink rationale.
    /// </summary>
    public static Vector4 GetDeityColor(DeityDomain domain)
    {
        return domain switch
        {
            DeityDomain.Craft => new Vector4(0.698f, 0.416f, 0.165f, 1.0f), // #B26A2A copper
            DeityDomain.Wild => new Vector4(0.361f, 0.431f, 0.165f, 1.0f), // #5C6E2A olive
            DeityDomain.Conquest => new Vector4(0.557f, 0.180f, 0.122f, 1.0f), // #8E2E1F dried-blood red
            DeityDomain.Harvest => new Vector4(0.627f, 0.463f, 0.157f, 1.0f), // #A07628 wheat ochre
            DeityDomain.Stone => new Vector4(0.369f, 0.329f, 0.282f, 1.0f), // #5E5448 warm slate
            DeityDomain.Caravan => new Vector4(0.761f, 0.541f, 0.118f, 1.0f), // #C28A1E road ochre
            _ => new Vector4(0.659f, 0.580f, 0.447f, 1.0f) // #A89472 faded ink
        };
    }

    /// <summary>
    ///     Get the domain title/description (by domain name string)
    /// </summary>
    public static string GetDomainTitle(string domain)
    {
        return domain switch
        {
            "Craft" => "of the Craft",
            "Wild" => "of the Wild",
            "Conquest" => "of Conquest",
            "Harvest" => "of the Harvest",
            "Stone" => "of the Stone",
            "Caravan" => "of the Caravan",
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
            DeityDomain.Craft => "Domain of the Craft",
            DeityDomain.Wild => "Domain of the Wild",
            DeityDomain.Conquest => "Domain of Conquest",
            DeityDomain.Harvest => "Domain of the Harvest",
            DeityDomain.Stone => "Domain of the Stone",
            DeityDomain.Caravan => "Domain of the Caravan",
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
}