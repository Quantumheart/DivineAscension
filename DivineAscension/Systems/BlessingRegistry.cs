using System.Collections.Generic;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
///     Registry for all blessings in the game
/// </summary>
public class BlessingRegistry : IBlessingRegistry
{
    private readonly ICoreAPI _api;
    private readonly Dictionary<string, Blessing> _blessings = new();

    // ReSharper disable once ConvertToPrimaryConstructor
    public BlessingRegistry(ICoreAPI api)
    {
        _api = api;
    }

    /// <summary>
    ///     Initializes the blessing registry and registers all blessings
    /// </summary>
    public void Initialize()
    {
        _api.Logger.Notification("[DivineAscension] Initializing Blessing Registry...");

        // Register all blessings from BlessingDefinitions
        var allBlessings = BlessingDefinitions.GetAllBlessings();
        foreach (var blessing in allBlessings) RegisterBlessing(blessing);

        _api.Logger.Notification($"[DivineAscension] Blessing Registry initialized with {_blessings.Count} blessings");
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
    public List<Blessing> GetBlessingsForDeity(DeityType deity, BlessingKind? type = null)
    {
        var query = _blessings.Values.Where(p => p.Deity == deity);

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
    public (bool canUnlock, string reason) CanUnlockBlessing(PlayerProgressionData playerData,
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
            if (playerData.FavorRank < (FavorRank)blessing.RequiredFavorRank)
            {
                var requiredRank = (FavorRank)blessing.RequiredFavorRank;
                return (false, $"Requires {requiredRank} favor rank (Current: {playerData.FavorRank})");
            }

            // Check deity matches
            if (religionData!.Deity != blessing.Deity)
                return (false, $"Requires deity: {blessing.Deity} (Current: {religionData!.Deity})");

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
        if (religionData.Deity != blessing.Deity)
            return (false, $"Religion deity mismatch (Blessing: {blessing.Deity}, Religion: {religionData.Deity})");

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