using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     World-level data container for all civilization data
/// </summary>
[ProtoContract]
public class CivilizationWorldData
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public CivilizationWorldData()
    {
        Civilizations = new Dictionary<string, Civilization>();
        ReligionToCivMap = new Dictionary<string, string>();
        PendingInvites = new List<CivilizationInvite>();
        Cooldowns = new List<CivilizationCooldown>();
    }

    /// <summary>
    ///     All civilizations indexed by CivId
    /// </summary>
    [ProtoMember(1)]
    public Dictionary<string, Civilization> Civilizations { get; set; }

    /// <summary>
    ///     Quick lookup map: ReligionId -> CivId
    /// </summary>
    [ProtoMember(2)]
    public Dictionary<string, string> ReligionToCivMap { get; set; }

    /// <summary>
    ///     Pending invitations
    /// </summary>
    [ProtoMember(3)]
    public List<CivilizationInvite> PendingInvites { get; set; }

    /// <summary>
    ///     Active cooldowns
    /// </summary>
    [ProtoMember(4)]
    public List<CivilizationCooldown> Cooldowns { get; set; }

    /// <summary>
    ///     Adds a civilization to the data
    /// </summary>
    public void AddCivilization(Civilization civ)
    {
        Civilizations[civ.CivId] = civ;
        foreach (var religionId in civ.MemberReligionIds)
        {
            ReligionToCivMap[religionId] = civ.CivId;
        }
    }

    /// <summary>
    ///     Removes a civilization from the data
    /// </summary>
    public void RemoveCivilization(string civId)
    {
        if (Civilizations.TryGetValue(civId, out var civ))
        {
            // Clean up religion mappings
            foreach (var religionId in civ.MemberReligionIds)
            {
                ReligionToCivMap.Remove(religionId);
            }

            Civilizations.Remove(civId);
        }
    }

    /// <summary>
    ///     Gets the civilization a religion belongs to
    /// </summary>
    public Civilization? GetCivilizationByReligion(string religionId)
    {
        if (ReligionToCivMap.TryGetValue(religionId, out var civId))
        {
            return Civilizations.TryGetValue(civId, out var civ) ? civ : null;
        }
        return null;
    }

    /// <summary>
    ///     Adds a religion to a civilization and updates the lookup map
    /// </summary>
    public bool AddReligionToCivilization(string civId, string religionId)
    {
        if (!Civilizations.TryGetValue(civId, out var civ))
            return false;

        if (civ.AddReligion(religionId))
        {
            ReligionToCivMap[religionId] = civId;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Removes a religion from a civilization and updates the lookup map
    /// </summary>
    public bool RemoveReligionFromCivilization(string religionId)
    {
        var civ = GetCivilizationByReligion(religionId);
        if (civ == null)
            return false;

        civ.RemoveReligion(religionId);
        ReligionToCivMap.Remove(religionId);
        return true;
    }

    /// <summary>
    ///     Adds a pending invite
    /// </summary>
    public void AddInvite(CivilizationInvite invite)
    {
        PendingInvites.Add(invite);
    }

    /// <summary>
    ///     Removes an invite
    /// </summary>
    public void RemoveInvite(string inviteId)
    {
        PendingInvites.RemoveAll(i => i.InviteId == inviteId);
    }

    /// <summary>
    ///     Gets all invites for a specific religion
    /// </summary>
    public List<CivilizationInvite> GetInvitesForReligion(string religionId)
    {
        return PendingInvites.Where(i => i.ReligionId == religionId && i.IsValid).ToList();
    }

    /// <summary>
    ///     Gets a specific invite
    /// </summary>
    public CivilizationInvite? GetInvite(string inviteId)
    {
        return PendingInvites.FirstOrDefault(i => i.InviteId == inviteId);
    }

    /// <summary>
    ///     Checks if a religion has a pending invite from a civilization
    /// </summary>
    public bool HasPendingInvite(string civId, string religionId)
    {
        return PendingInvites.Any(i => i.CivId == civId && i.ReligionId == religionId && i.IsValid);
    }

    /// <summary>
    ///     Adds a cooldown for a religion
    /// </summary>
    public void AddCooldown(CivilizationCooldown cooldown)
    {
        Cooldowns.Add(cooldown);
    }

    /// <summary>
    ///     Gets the cooldown for a religion if active
    /// </summary>
    public CivilizationCooldown? GetActiveCooldown(string religionId)
    {
        return Cooldowns.FirstOrDefault(c => c.ReligionId == religionId && c.IsActive);
    }

    /// <summary>
    ///     Checks if a religion is on cooldown
    /// </summary>
    public bool IsOnCooldown(string religionId)
    {
        return Cooldowns.Any(c => c.ReligionId == religionId && c.IsActive);
    }

    /// <summary>
    ///     Cleans up expired invites and cooldowns
    /// </summary>
    public void CleanupExpired()
    {
        PendingInvites.RemoveAll(i => !i.IsValid);
        Cooldowns.RemoveAll(c => !c.IsActive);
    }
}
