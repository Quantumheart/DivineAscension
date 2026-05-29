using System.Collections.Generic;
using System.Linq;
using DivineAscension.Configuration;

namespace DivineAscension.Models.Enum;

/// <summary>
///     Single source of truth for enumerating <see cref="DeityDomain" /> values.
///     Adding a domain to the enum (plus a <see cref="DeityDomainRegistry" /> entry)
///     propagates everywhere automatically — no more hand-maintained
///     <c>{ Craft, Wild, ... }</c> arrays that silently drop a domain (#558).
/// </summary>
public static class DeityDomains
{
    /// <summary>
    ///     All real domains (excludes <see cref="DeityDomain.None" />), in enum
    ///     declaration (underlying-value) order. Use for data-completeness loops
    ///     (packets, blessing preloads, asset loaders) that must cover every domain
    ///     regardless of feature flags.
    /// </summary>
    public static readonly IReadOnlyList<DeityDomain> All =
        System.Enum.GetValues<DeityDomain>().Where(d => d != DeityDomain.None).ToArray();

    /// <summary>
    ///     Lower-case codes used in JSON file names + asset paths (e.g. "craft").
    ///     Parallel to <see cref="All" />.
    /// </summary>
    public static readonly IReadOnlyList<string> AllCodes =
        All.Select(d => d.ToString().ToLowerInvariant()).ToArray();

    /// <summary>
    ///     Domains a player may currently select/worship: <see cref="All" /> minus any
    ///     domain gated behind an off feature flag (Caravan when
    ///     <see cref="FeatureFlags.CaravanDomainEnabled" /> is false). Use for UI
    ///     selectors and player-facing choice lists.
    /// </summary>
    public static readonly IReadOnlyList<DeityDomain> Selectable =
        All.Where(IsEnabled).ToArray();

    /// <summary>
    ///     Whether a domain is currently enabled for selection. Only Caravan is
    ///     feature-gated today; every other real domain is always enabled.
    /// </summary>
    public static bool IsEnabled(DeityDomain domain) =>
        domain != DeityDomain.Caravan || FeatureFlags.CaravanDomainEnabled;
}
