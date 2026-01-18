using System.Collections.Generic;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
///     Registry for all blessings in the game
/// </summary>
public class BlessingRegistry : IBlessingRegistry
{
    private readonly ICoreAPI _api;
    private readonly IBlessingLoader? _blessingLoader;
    private readonly Dictionary<string, Blessing> _blessings = new();

    /// <summary>
    ///     Creates a BlessingRegistry with JSON-based blessing loading.
    /// </summary>
    /// <param name="api">The Vintage Story API</param>
    /// <param name="blessingLoader">The blessing loader for JSON assets</param>
    public BlessingRegistry(ICoreAPI api, IBlessingLoader? blessingLoader = null)
    {
        _api = api;
        _blessingLoader = blessingLoader;
    }

    /// <summary>
    ///     Initializes the blessing registry and registers all blessings.
    ///     Attempts to load from JSON first, falls back to hardcoded definitions.
    /// </summary>
    public void Initialize()
    {
        _api.Logger.Notification("[DivineAscension] Initializing Blessing Registry...");

        List<Blessing> allBlessings;

        // Try to load from JSON first
        if (_blessingLoader != null)
        {
            allBlessings = _blessingLoader.LoadBlessings();

            if (_blessingLoader.LoadedSuccessfully && allBlessings.Count > 0)
            {
                _api.Logger.Notification(
                    $"[DivineAscension] Loaded {allBlessings.Count} blessings from JSON assets");
            }
            else
            {
                _api.Logger.Warning(
                    "[DivineAscension] Failed to load blessings from JSON, falling back to hardcoded definitions");
                allBlessings = LoadFallbackBlessings();
            }
        }
        else
        {
            // No loader provided, use fallback
            allBlessings = LoadFallbackBlessings();
        }

        foreach (var blessing in allBlessings)
        {
            RegisterBlessing(blessing);
        }

        _api.Logger.Notification($"[DivineAscension] Blessing Registry initialized with {_blessings.Count} blessings");
    }

    /// <summary>
    ///     Loads blessings from hardcoded definitions as a fallback.
    /// </summary>
    [System.Obsolete("Use JSON-based loading via IBlessingLoader. Fallback only.")]
    private List<Blessing> LoadFallbackBlessings()
    {
        _api.Logger.Debug("[DivineAscension] Using fallback hardcoded blessing definitions");
#pragma warning disable CS0618 // Type or member is obsolete
        return BlessingDefinitions.GetAllBlessings();
#pragma warning restore CS0618
    }

    /// <summary>
    ///     Registers a blessing in the system
    /// </summary>
    public void RegisterBlessing(Blessing blessing)
    {
        if (string.IsNullOrEmpty(blessing.BlessingId))
        {
            _api.Logger.Error("[DivineAscension] Cannot register blessing with empty BlessingId");
            return;
        }

        if (_blessings.ContainsKey(blessing.BlessingId))
            _api.Logger.Warning(
                $"[DivineAscension] Blessing {blessing.BlessingId} is already registered. Overwriting...");

        _blessings[blessing.BlessingId] = blessing;
        _api.Logger.Debug($"[DivineAscension] Registered blessing: {blessing.BlessingId} ({blessing.Name})");
    }

    /// <summary>
    ///     Gets a blessing by its ID
    /// </summary>
    public Blessing? GetBlessing(string blessingId)
    {
        return _blessings.GetValueOrDefault(blessingId);
    }

    /// <summary>
    ///     Gets all blessings for a specific deity and type
    /// </summary>
    public List<Blessing> GetBlessingsForDeity(DeityDomain deity, BlessingKind? type = null)
    {
        var query = _blessings.Values.Where(p => p.Domain == deity);

        if (type.HasValue) query = query.Where(p => p.Kind == type.Value);

        return query.OrderBy(p => p.RequiredFavorRank)
            .ThenBy(p => p.RequiredPrestigeRank)
            .ToList();
    }

    /// <summary>
    ///     Gets all blessings in the registry
    /// </summary>
    public List<Blessing> GetAllBlessings()
    {
        return _blessings.Values.ToList();
    }

    /// <summary>
    ///     Checks if a blessing can be unlocked by a player/religion
    /// </summary>
    public (bool canUnlock, string reason) CanUnlockBlessing(string playerUID,
        FavorRank playerFavorRank,
        PlayerProgressionData playerData,
        ReligionData? religionData,
        Blessing? blessing)
    {
        // Check if blessing exists
        if (blessing == null) return (false, "Blessing not found");

        // Check blessing type and corresponding requirements
        if (blessing.Kind == BlessingKind.Player)
        {
            if (religionData == null) return (false, "Not in a religion");

            // Check if already unlocked
            if (playerData.IsBlessingUnlocked(blessing.BlessingId)) return (false, "Blessing already unlocked");

            // Check favor rank requirement
            if (playerFavorRank < (FavorRank)blessing.RequiredFavorRank)
            {
                var requiredRank = (FavorRank)blessing.RequiredFavorRank;
                return (false, $"Requires {requiredRank} favor rank (Current: {playerFavorRank})");
            }

            // Check deity matches
            if (religionData!.Domain != blessing.Domain)
                return (false, $"Requires deity: {blessing.Domain} (Current: {religionData!.Domain})");

            // Check prerequisites
            if (blessing.PrerequisiteBlessings != null)
                foreach (var prereqId in blessing.PrerequisiteBlessings)
                    if (!playerData.IsBlessingUnlocked(prereqId))
                    {
                        var prereqBlessing = GetBlessing(prereqId);
                        var prereqName = prereqBlessing?.Name ?? prereqId;
                        return (false, $"Requires prerequisite blessing: {prereqName}");
                    }

            return (true, "Can unlock");
        }

        // BlessingType.Religion
        // Check if player has a religion
        if (religionData == null) return (false, "Not in a religion");

        // Check if already unlocked
        if (religionData.UnlockedBlessings.TryGetValue(blessing.BlessingId, out var unlocked) && unlocked)
            return (false, "Blessing already unlocked");

        // Check prestige rank requirement
        if (religionData.PrestigeRank < (PrestigeRank)blessing.RequiredPrestigeRank)
        {
            var requiredRank = (PrestigeRank)blessing.RequiredPrestigeRank;
            return (false, $"Religion requires {requiredRank} prestige rank (Current: {religionData.PrestigeRank})");
        }

        // Check deity matches
        if (religionData.Domain != blessing.Domain)
            return (false, $"Religion deity mismatch (Blessing: {blessing.Domain}, Religion: {religionData.Domain})");

        // Check prerequisites
        if (blessing.PrerequisiteBlessings != null)
            foreach (var prereqId in blessing.PrerequisiteBlessings)
                if (!religionData.UnlockedBlessings.TryGetValue(prereqId, out var prereqUnlocked) || !prereqUnlocked)
                {
                    var prereqBlessing = GetBlessing(prereqId);
                    var prereqName = prereqBlessing?.Name ?? prereqId;
                    return (false, $"Requires prerequisite blessing: {prereqName}");
                }

        return (true, "Can unlock");
    }
}