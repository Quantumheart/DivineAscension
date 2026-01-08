using System.Linq;
using DivineAscension.Data;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
///     Handles migration of player data from older versions to current format
/// </summary>
internal static class PlayerDataMigration
{
    /// <summary>
    ///     Migrates PlayerReligionData (v2) to PlayerProgressionData (v3)
    /// </summary>
    /// <param name="oldData">The old PlayerReligionData object</param>
    /// <param name="religionManager">ReligionManager for validating religion membership</param>
    /// <param name="logger">Logger for migration messages</param>
    /// <returns>Migrated PlayerProgressionData</returns>
    public static PlayerProgressionData MigrateV2ToV3(
        PlayerReligionData oldData,
        IReligionManager religionManager,
        ILogger logger)
    {
        var newData = new PlayerProgressionData(oldData.PlayerUID)
        {
            Favor = oldData.Favor,
            TotalFavorEarned = oldData.TotalFavorEarned,
            AccumulatedFractionalFavor = oldData.AccumulatedFractionalFavor,
            DataVersion = 3
        };

        // Convert Dictionary<string, bool> to HashSet<string>
        // Only include blessings that are marked as true
        foreach (var blessing in oldData.UnlockedBlessings.Where(b => b.Value))
        {
            newData.UnlockedBlessings.Add(blessing.Key);
        }

        // Validate religion membership if present in old data
        if (!string.IsNullOrEmpty(oldData.ReligionUID))
        {
            var actualReligionId = religionManager.GetPlayerReligionId(oldData.PlayerUID);

            if (actualReligionId != oldData.ReligionUID)
            {
                if (actualReligionId != null)
                {
                    logger.Warning(
                        $"[DivineAscension Migration] Player {oldData.PlayerUID} had mismatched religion: " +
                        $"stored={oldData.ReligionUID}, actual={actualReligionId}. Using actual from index.");
                }
                else
                {
                    logger.Warning(
                        $"[DivineAscension Migration] Player {oldData.PlayerUID} had religion {oldData.ReligionUID} " +
                        $"in data but is not a member according to ReligionManager. Religion removed.");
                }
            }
            else
            {
                logger.Debug(
                    $"[DivineAscension Migration] Player {oldData.PlayerUID} religion validated: {actualReligionId}");
            }
        }

        // Log migration details
        logger.Notification(
            $"[DivineAscension Migration] Migrated player {oldData.PlayerUID} from v2 to v3: " +
            $"Favor={newData.Favor}, TotalFavor={newData.TotalFavorEarned}, " +
            $"Blessings={newData.UnlockedBlessings.Count}, Rank={newData.FavorRank}");

        return newData;
    }

    /// <summary>
    ///     Detects if data needs migration based on version
    /// </summary>
    public static bool NeedsMigration(int dataVersion)
    {
        return dataVersion < 3;
    }

    /// <summary>
    ///     Gets the migration path description for logging
    /// </summary>
    public static string GetMigrationPath(int fromVersion, int toVersion)
    {
        return $"v{fromVersion} → v{toVersion}";
    }
}