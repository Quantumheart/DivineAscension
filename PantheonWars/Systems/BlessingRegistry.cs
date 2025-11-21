using System.Collections.Generic;
using System.Linq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;

namespace PantheonWars.Systems;

/// <summary>
///     Registry for all blessings in the game (Phase 3.3)
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
        _api.Logger.Notification("[PantheonWars] Initializing Blessing Registry...");

        // Register all blessings from BlessingDefinitions (Phase 3.4)
        var allBlessings = BlessingDefinitions.GetAllBlessings();
        foreach (var blessing in allBlessings) RegisterBlessing(blessing);

        _api.Logger.Notification($"[PantheonWars] Blessing Registry initialized with {_blessings.Count} blessings");
    }

    /// <summary>
    ///     Registers a blessing in the system
    /// </summary>
    public void RegisterBlessing(Blessing blessing)
    {
        if (string.IsNullOrEmpty(blessing.BlessingId))
        {
            _api.Logger.Error("[PantheonWars] Cannot register blessing with empty BlessingId");
            return;
        }

        if (_blessings.ContainsKey(blessing.BlessingId))
            _api.Logger.Warning($"[PantheonWars] Blessing {blessing.BlessingId} is already registered. Overwriting...");

        _blessings[blessing.BlessingId] = blessing;
        _api.Logger.Debug($"[PantheonWars] Registered blessing: {blessing.BlessingId} ({blessing.Name})");
    }

    /// <summary>
    ///     Gets a blessing by its ID
    /// </summary>
    public Blessing? GetBlessing(string blessingId)
    {
        return _blessings.GetValueOrDefault(blessingId);
    }

    /// <summary>
    ///     Gets all blessings for a specific deity
    /// </summary>
    public List<Blessing> GetBlessingsForDeity(DeityType deity)
    {
        return _blessings.Values
            .Where(p => p.Deity == deity)
            .OrderBy(p => p.RequiredPrestigeRank)
            .ThenBy(p => p.Name)
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
    ///     Gets all universal blessings (DeityType.None) for the religion-only system
    /// </summary>
    public List<Blessing> GetUniversalBlessings()
    {
        return _blessings.Values
            .Where(p => p.Deity == DeityType.None)
            .OrderBy(p => p.RequiredPrestigeRank)
            .ThenBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    ///     Checks if a blessing can be unlocked by a player/religion
    /// </summary>
    public (bool canUnlock, string reason) CanUnlockBlessing(
        PlayerReligionData playerData,
        ReligionData? religionData,
        Blessing? blessing)
    {
        // Check if blessing exists
        if (blessing == null) return (false, "Blessing not found");

        // In the religion-only system, all blessings are religion blessings
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

        // Check deity matches (DeityType.None means universal blessing, available to all)
        if (blessing.Deity != DeityType.None && religionData.Deity != blessing.Deity)
            return (false, $"Religion deity mismatch (Blessing: {blessing.Deity}, Religion: {religionData.Deity})");

        // Check prerequisites
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