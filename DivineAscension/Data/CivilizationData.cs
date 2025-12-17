using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Represents a civilization - an alliance of 2-4 religions with different deities
/// </summary>
[ProtoContract]
public class Civilization
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public Civilization()
    {
    }

    /// <summary>
    ///     Creates a new civilization with initial parameters
    /// </summary>
    public Civilization(string civId, string name, string founderUID, string founderReligionId)
    {
        CivId = civId;
        Name = name;
        FounderUID = founderUID;
        FounderReligionUID = founderReligionId;
        MemberReligionIds = [founderReligionId];
        CreatedDate = DateTime.UtcNow;
    }

    /// <summary>
    ///     Unique identifier for the civilization
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the civilization
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Player UID of the civilization founder
    /// </summary>
    [ProtoMember(3)]
    public string FounderUID { get; set; } = string.Empty;


    /// <summary>
    ///     Unique identifier of the religion of the civilization's founder
    /// </summary>
    [ProtoMember(4)]
    public string FounderReligionUID { get; set; } = string.Empty;


    /// <summary>
    ///     List of religion UIDs that are members (2-4 religions)
    /// </summary>
    [ProtoMember(5)]
    public List<string> MemberReligionIds { get; set; } = new();

    /// <summary>
    ///     When the civilization was created
    /// </summary>
    [ProtoMember(6)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the civilization was disbanded (null if active)
    /// </summary>
    [ProtoMember(7)]
    public DateTime? DisbandedDate { get; set; }

    /// <summary>
    ///     Cached count of total members across all religions
    /// </summary>
    [ProtoMember(8)]
    public int MemberCount { get; set; }

    /// <summary>
    ///     Icon name for the civilization (e.g., "shield", "crown")
    /// </summary>
    [ProtoMember(9)]
    public string Icon { get; set; } = "default";

    /// <summary>
    ///     Checks if the civilization has a valid number of member religions (2-4)
    /// </summary>
    public bool IsValid => MemberReligionIds.Count >= 2 && MemberReligionIds.Count <= 4;

    /// <summary>
    ///     Checks if a religion is a member of this civilization
    /// </summary>
    public bool HasReligion(string religionId)
    {
        return MemberReligionIds.Contains(religionId);
    }

    /// <summary>
    ///     Adds a religion to the civilization
    /// </summary>
    public bool AddReligion(string religionId)
    {
        if (MemberReligionIds.Count >= 4 || MemberReligionIds.Contains(religionId))
            return false;

        MemberReligionIds.Add(religionId);
        return true;
    }

    /// <summary>
    ///     Removes a religion from the civilization
    /// </summary>
    public bool RemoveReligion(string religionId)
    {
        return MemberReligionIds.Remove(religionId);
    }

    /// <summary>
    ///     Updates the civilization's icon
    /// </summary>
    public void UpdateIcon(string icon)
    {
        Icon = icon;
    }
}

/// <summary>
///     Represents an invitation for a religion to join a civilization
/// </summary>
[ProtoContract]
public class CivilizationInvite
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public CivilizationInvite()
    {
    }

    /// <summary>
    ///     Creates a new civilization invite
    /// </summary>
    public CivilizationInvite(string inviteId, string civId, string religionId, DateTime sentDate)
    {
        InviteId = inviteId;
        CivId = civId;
        ReligionId = religionId;
        SentDate = sentDate;
        ExpiresDate = sentDate.AddDays(7);
    }

    /// <summary>
    ///     Unique identifier for the invite
    /// </summary>
    [ProtoMember(1)]
    public string InviteId { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization ID this invite is for
    /// </summary>
    [ProtoMember(2)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Religion ID being invited
    /// </summary>
    [ProtoMember(3)]
    public string ReligionId { get; set; } = string.Empty;

    /// <summary>
    ///     When the invite was sent
    /// </summary>
    [ProtoMember(4)]
    public DateTime SentDate { get; set; }

    /// <summary>
    ///     When the invite expires (7 days from sent date)
    /// </summary>
    [ProtoMember(5)]
    public DateTime ExpiresDate { get; set; }

    /// <summary>
    ///     Checks if the invite is still valid (not expired)
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresDate;
}