using System.Collections.Generic;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.Systems.Interfaces;

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
    List<Blessing> GetBlessingsForDeity(DeityType deity, BlessingKind? type = null);

    /// <summary>
    ///     Gets all blessings in the registry
    /// </summary>
    List<Blessing> GetAllBlessings();

    /// <summary>
    ///     Checks if a blessing can be unlocked by a player/religion
    /// </summary>
    (bool canUnlock, string reason) CanUnlockBlessing(
        PlayerReligionData playerData,
        ReligionData? religionData,
        Blessing? blessing);
}