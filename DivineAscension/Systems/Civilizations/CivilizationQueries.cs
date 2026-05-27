using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     Lock-free read helpers over <see cref="CivilizationWorldData" />. Shared by
///     the facade's query methods and the membership service so the previous
///     "_Unlocked" method pairs are no longer needed — the facade owns the lock.
/// </summary>
internal static class CivilizationQueries
{
    public static HashSet<DeityDomain> GetDeityTypes(CivilizationWorldData data,
        IReligionManager religionManager, string civId)
    {
        var civ = data.Civilizations.GetValueOrDefault(civId);
        if (civ == null)
            return new HashSet<DeityDomain>();

        var deities = new HashSet<DeityDomain>();
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            var religion = religionManager.GetReligion(religionId);
            if (religion != null) deities.Add(religion.PatronDomain);
        }

        return deities;
    }

    public static List<ReligionData> GetReligions(CivilizationWorldData data,
        IReligionManager religionManager, string civId)
    {
        var civ = data.Civilizations.GetValueOrDefault(civId);
        if (civ == null)
            return new List<ReligionData>();

        var religions = new List<ReligionData>();
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            var religion = religionManager.GetReligion(religionId);
            if (religion != null) religions.Add(religion);
        }

        return religions;
    }
}
