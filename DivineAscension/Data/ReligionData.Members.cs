using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Backing field for member UIDs (serialized)
    /// </summary>
    [ProtoMember(5)]
    private List<string> _memberUIDs = new();

    /// <summary>
    ///     Ordered list of member player UIDs (founder is always first).
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<string> MemberUIDs
    {
        get
        {
            lock (Lock)
            {
                return _memberUIDs.ToList();
            }
        }
    }

    /// <summary>
    ///     Backing field for member entries (serialized)
    /// </summary>
    [ProtoMember(16)]
    private Dictionary<string, MemberEntry> _members = new();

    /// <summary>
    ///     Dictionary of member entries with cached player names.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, MemberEntry> Members
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, MemberEntry>(_members);
            }
        }
    }

    /// <summary>
    ///     Adds a member to the religion with player name.
    ///     Thread-safe.
    /// </summary>
    public void AddMember(string playerUID, string playerName)
    {
        lock (Lock)
        {
            if (!_memberUIDs.Contains(playerUID))
                _memberUIDs.Add(playerUID);

            if (!_members.ContainsKey(playerUID))
                _members[playerUID] = new MemberEntry(playerUID, playerName);
        }
    }

    /// <summary>
    ///     Removes a member from the religion.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveMember(string playerUID)
    {
        lock (Lock)
        {
            _memberRoles.Remove(playerUID);
            _members.Remove(playerUID);
            return _memberUIDs.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Moves a member to the first position in the member list (used for founder transfer).
    ///     Thread-safe.
    /// </summary>
    public void MoveToFirstMember(string playerUID)
    {
        lock (Lock)
        {
            if (_memberUIDs.Remove(playerUID))
            {
                _memberUIDs.Insert(0, playerUID);
            }
        }
    }

    /// <summary>
    ///     Checks if a player is a member of this religion.
    ///     Thread-safe.
    /// </summary>
    public bool IsMember(string playerUID)
    {
        lock (Lock)
        {
            return _memberUIDs.Contains(playerUID);
        }
    }

    /// <summary>
    ///     Gets the member count.
    ///     Thread-safe.
    /// </summary>
    public int GetMemberCount()
    {
        lock (Lock)
        {
            return _memberUIDs.Count;
        }
    }

    /// <summary>
    ///     Gets the cached player name for a member (fallback to UID if not found).
    ///     Thread-safe.
    /// </summary>
    public string GetMemberName(string playerUID)
    {
        lock (Lock)
        {
            // Special case for founder - use FounderName as fallback
            if (playerUID == FounderUID && !string.IsNullOrEmpty(FounderName))
            {
                if (_members.TryGetValue(playerUID, out var founderEntry) &&
                    !string.IsNullOrEmpty(founderEntry.PlayerName))
                    return founderEntry.PlayerName;
                return FounderName;
            }

            // For non-founders
            return _members.TryGetValue(playerUID, out var entry) && !string.IsNullOrEmpty(entry.PlayerName)
                ? entry.PlayerName
                : playerUID;
        }
    }

    /// <summary>
    ///     Updates the cached player name if the member exists.
    ///     Thread-safe.
    /// </summary>
    public void UpdateMemberName(string playerUID, string playerName)
    {
        lock (Lock)
        {
            if (_members.TryGetValue(playerUID, out var entry))
                entry.UpdateName(playerName);
        }
    }
}
