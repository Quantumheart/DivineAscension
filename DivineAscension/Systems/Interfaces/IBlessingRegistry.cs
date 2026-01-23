using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

public interface IBlessingRegistry
{
    /// <summary>
    ///     Initializes the blessing registry and registers all blessings
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Registers a blessing in the system
    /// </summary>
    void RegisterBlessing(Blessing blessing);

    /// <summary>
    ///     Gets a blessing by its ID
    /// </summary>
    Blessing? GetBlessing(string blessingId);

    /// <summary>
    ///     Gets all blessings for a specific deity and type
    /// </summary>
    List<Blessing> GetBlessingsForDeity(DeityDomain deity, BlessingKind? type = null);

    /// <summary>
    ///     Gets all blessings in the registry
    /// </summary>
    List<Blessing> GetAllBlessings();

    /// <summary>
    ///     Checks if a blessing can be unlocked by a player/religion
    /// </summary>
    /// <param name="playerUID">The player's UID</param>
    /// <param name="playerFavorRank">The player's current favor rank</param>
    /// <param name="playerData">The player's progression data</param>
    /// <param name="religionData">The player's religion data (can be null)</param>
    /// <param name="blessing">The blessing to check</param>
    /// <param name="skipCostCheck">If true, skips the cost check (use when cost will be deducted atomically)</param>
    /// <returns>A tuple of (canUnlock, reason)</returns>
    (bool canUnlock, string reason) CanUnlockBlessing(string playerUID,
        FavorRank playerFavorRank,
        PlayerProgressionData playerData,
        ReligionData? religionData,
        Blessing? blessing,
        bool skipCostCheck = false);
}