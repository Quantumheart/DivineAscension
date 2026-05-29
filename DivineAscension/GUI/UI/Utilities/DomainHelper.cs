using System;
using System.Linq;
using System.Numerics;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized helper for deity-related UI operations
///     Provides deity colors, titles, and lists for consistent UI presentation
/// </summary>
internal static class DomainHelper
{
    /// <summary>Faded-ink fallback colour for an unknown / None domain.</summary>
    private static readonly Vector4 UnknownColor = new(0.659f, 0.580f, 0.447f, 1.0f); // #A89472

    /// <summary>
    ///     All selectable domain names in order. Caravan is included only when its
    ///     domain is enabled (see <see cref="Configuration.FeatureFlags.CaravanDomainEnabled"/>),
    ///     sourced from <see cref="DeityDomains.Selectable"/>.
    /// </summary>
    public static readonly string[] DomainNames =
        DeityDomains.Selectable.Select(d => d.ToString()).ToArray();

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
    public static Vector4 GetDomainColor(string domain) => GetDeityColor(ParseDomain(domain));

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
    public static Vector4 GetDeityColor(DeityDomain domain) =>
        DeityDomainRegistry.TryGet(domain, out var meta) ? meta.PrimaryColor : UnknownColor;

    /// <summary>
    ///     Get the domain title suffix (by domain name string), e.g. "of the Craft".
    /// </summary>
    public static string GetDomainTitle(string domain) =>
        DeityDomainRegistry.TryGet(ParseDomain(domain), out var meta) ? meta.TitleSuffix : "Unknown Domain";

    /// <summary>
    ///     Get the full domain title (by enum), e.g. "Domain of the Craft".
    /// </summary>
    public static string GetDeityTitle(DeityDomain domain) =>
        DeityDomainRegistry.TryGet(domain, out var meta) ? $"Domain {meta.TitleSuffix}" : "Unknown Domain";

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