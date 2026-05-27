using System;
using System.Collections.Generic;
using System.Threading;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Stores religion-specific data for persistence.
///     This class is thread-safe for concurrent access.
///     The type is split into partials by concern: see ReligionData.Members.cs,
///     ReligionData.Roles.cs, ReligionData.Prestige.cs, ReligionData.Blessings.cs,
///     ReligionData.Bans.cs and ReligionData.ActivityLog.cs. All partials share the
///     single <see cref="Lock"/> declared here.
/// </summary>
[ProtoContract]
public partial class ReligionData
{
    // Thread-safety: Lazy lock initialization using Interlocked.CompareExchange
    // This is safe for ProtoBuf deserialization and avoids race conditions
    [ProtoIgnore] private object? _lock;
    [ProtoIgnore] private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }
            return _lock;
        }
    }
    /// <summary>
    ///     Creates a new religion with the specified parameters
    /// </summary>
    public ReligionData(string religionUID, string religionName, DeityDomain patronDomain, string patronName,
        string founderUID, string founderName)
    {
        ReligionUID = religionUID;
        ReligionName = religionName;
        PatronDomain = patronDomain;
        PatronName = patronName;
        FounderUID = founderUID;
        FounderName = founderName;
        _memberUIDs = new List<string> { founderUID }; // Founder is first member
        _members = new Dictionary<string, MemberEntry>
        {
            [founderUID] = new(founderUID, founderName)
        };
        CreationDate = DateTime.UtcNow;
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ReligionData()
    {
    }

    /// <summary>
    ///     Unique identifier for this religion
    /// </summary>
    [ProtoMember(1)]
    public string ReligionUID { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the religion (e.g., "Knights of the Forge")
    /// </summary>
    [ProtoMember(2)]
    public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     The patron deity's domain (permanent, cannot be changed).
    ///     Renamed from Domain in Pantheon 2.0 for clarity — religions have a primary patron,
    ///     but players may accumulate favor with secondary deities.
    /// </summary>
    [ProtoMember(3)]
    public DeityDomain PatronDomain { get; set; } = DeityDomain.None;

    /// <summary>
    ///     Player UID of the religion founder
    /// </summary>
    [ProtoMember(4)]
    public string FounderUID { get; set; } = string.Empty;

    /// <summary>
    ///     When the religion was created
    /// </summary>
    [ProtoMember(9)]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Whether this is a public religion (anyone can join) or private (invite-only)
    /// </summary>
    [ProtoMember(11)]
    public bool IsPublic { get; set; } = true;

    /// <summary>
    ///     Religion description or manifesto set by the founder
    /// </summary>
    [ProtoMember(12)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Cached founder name for quick access
    /// </summary>
    [ProtoMember(17)]
    public string FounderName { get; set; } = string.Empty;

    /// <summary>
    ///     The custom name of the patron deity this religion worships (required).
    ///     Allows religions with the same patron domain to have uniquely named deities.
    /// </summary>
    [ProtoMember(18)]
    public string PatronName { get; set; } = string.Empty;

    /// <summary>
    ///     Short one-line motto/creed (flavor, ~80 chars).
    /// </summary>
    [ProtoMember(21)]
    public string Motto { get; set; } = string.Empty;

    /// <summary>
    ///     Long-form founding myth / origin story (flavor, ~2000 chars).
    ///     Distinct from <see cref="Description"/> which is the manifesto/purpose.
    /// </summary>
    [ProtoMember(22)]
    public string FoundingMyth { get; set; } = string.Empty;

    /// <summary>
    ///     Checks if a player is the founder
    /// </summary>
    public bool IsFounder(string playerUID)
    {
        return FounderUID == playerUID;
    }

    /// <summary>
    ///     Updates the founder name and the founder's member entry
    /// </summary>
    public void UpdateFounderName(string founderName)
    {
        FounderName = founderName;
        UpdateMemberName(FounderUID, founderName);
    }
}

/// <summary>
///     Represents an invitation for a player to join a religion
/// </summary>
[ProtoContract]
public class ReligionInvite
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ReligionInvite()
    {
    }

    /// <summary>
    ///     Creates a new religion invite
    /// </summary>
    public ReligionInvite(string inviteId, string religionId, string playerUID, DateTime sentDate)
    {
        InviteId = inviteId;
        ReligionId = religionId;
        PlayerUID = playerUID;
        SentDate = sentDate;
        ExpiresDate = sentDate.AddDays(7);
    }

    /// <summary>
    ///     Unique identifier for the invite
    /// </summary>
    [ProtoMember(1)]
    public string InviteId { get; set; } = string.Empty;

    /// <summary>
    ///     Religion ID this invite is for
    /// </summary>
    [ProtoMember(2)]
    public string ReligionId { get; set; } = string.Empty;

    /// <summary>
    ///     Player UID being invited
    /// </summary>
    [ProtoMember(3)]
    public string PlayerUID { get; set; } = string.Empty;

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
