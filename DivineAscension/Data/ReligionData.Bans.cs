using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Backing field for banned players (serialized)
    /// </summary>
    [ProtoMember(13)]
    private Dictionary<string, BanEntry> _bannedPlayers = new();

    /// <summary>
    ///     Dictionary of banned players.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, BanEntry> BannedPlayers
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, BanEntry>(_bannedPlayers);
            }
        }
    }

    /// <summary>
    ///     Adds a banned player to the religion's ban list.
    ///     Thread-safe.
    /// </summary>
    public void AddBannedPlayer(string playerUID, BanEntry entry)
    {
        lock (Lock)
        {
            _bannedPlayers[playerUID] = entry;
        }
    }

    /// <summary>
    ///     Removes a banned player from the religion's ban list.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveBannedPlayer(string playerUID)
    {
        lock (Lock)
        {
            return _bannedPlayers.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Checks if a player is banned from this religion (including expired bans).
    ///     Thread-safe.
    /// </summary>
    public bool IsBanned(string playerUID)
    {
        lock (Lock)
        {
            if (!_bannedPlayers.TryGetValue(playerUID, out var banEntry))
                return false;

            // Check if ban has expired
            if (banEntry.ExpiresAt.HasValue && banEntry.ExpiresAt.Value <= DateTime.UtcNow)
                return false;

            return true;
        }
    }

    /// <summary>
    ///     Gets the ban entry for a specific player.
    ///     Thread-safe.
    /// </summary>
    public BanEntry? GetBannedPlayer(string playerUID)
    {
        lock (Lock)
        {
            if (_bannedPlayers.TryGetValue(playerUID, out var banEntry))
            {
                // Check if expired
                if (banEntry.ExpiresAt.HasValue && banEntry.ExpiresAt.Value <= DateTime.UtcNow)
                    return null;

                return banEntry;
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets all active (non-expired) bans.
    ///     Thread-safe.
    /// </summary>
    public List<BanEntry> GetActiveBans()
    {
        lock (Lock)
        {
            CleanupExpiredBansInternal();
            return _bannedPlayers.Values.ToList();
        }
    }

    /// <summary>
    ///     Removes expired bans from the ban list.
    ///     Thread-safe.
    /// </summary>
    public void CleanupExpiredBans()
    {
        lock (Lock)
        {
            CleanupExpiredBansInternal();
        }
    }

    /// <summary>
    ///     Internal implementation for cleanup (call within lock).
    /// </summary>
    private void CleanupExpiredBansInternal()
    {
        var now = DateTime.UtcNow;
        var expiredBans = _bannedPlayers
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var playerUID in expiredBans) _bannedPlayers.Remove(playerUID);
    }
}
