using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     World-level data container for all religion invitation data
/// </summary>
[ProtoContract]
public class ReligionWorldData
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ReligionWorldData()
    {
        PendingInvites = new List<ReligionInvite>();
    }

    /// <summary>
    ///     Pending invitations
    /// </summary>
    [ProtoMember(1)]
    public List<ReligionInvite> PendingInvites { get; set; }

    /// <summary>
    ///     Adds a pending invite
    /// </summary>
    public void AddInvite(ReligionInvite invite)
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
    ///     Gets all invites for a specific player
    /// </summary>
    public List<ReligionInvite> GetInvitesForPlayer(string playerUID)
    {
        return PendingInvites.Where(i => i.PlayerUID == playerUID && i.IsValid).ToList();
    }

    /// <summary>
    ///     Gets all invites for a specific religion
    /// </summary>
    public List<ReligionInvite> GetInvitesForReligion(string religionId)
    {
        return PendingInvites.Where(i => i.ReligionId == religionId && i.IsValid).ToList();
    }

    /// <summary>
    ///     Gets a specific invite
    /// </summary>
    public ReligionInvite? GetInvite(string inviteId)
    {
        return PendingInvites.FirstOrDefault(i => i.InviteId == inviteId);
    }

    /// <summary>
    ///     Checks if a player has a pending invite from a religion
    /// </summary>
    public bool HasPendingInvite(string religionId, string playerUID)
    {
        return PendingInvites.Any(i => i.ReligionId == religionId && i.PlayerUID == playerUID && i.IsValid);
    }

    /// <summary>
    ///     Cleans up expired invites
    /// </summary>
    public void CleanupExpired()
    {
        PendingInvites.RemoveAll(i => !i.IsValid);
    }
}