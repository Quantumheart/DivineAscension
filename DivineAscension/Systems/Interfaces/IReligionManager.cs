using System;
using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

public interface IReligionManager : IDisposable
{
    /// <summary>
    ///     Event fired when a religion is deleted (either manually or automatically)
    /// </summary>
    event Action<string>? OnReligionDeleted;

    /// <summary>
    ///     Initializes the religion manager
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Creates a new religion
    /// </summary>
    ReligionData CreateReligion(string name, DeityType deity, string founderUID, bool isPublic);

    /// <summary>
    ///     Adds a member to a religion
    /// </summary>
    void AddMember(string religionUID, string playerUID);

    /// <summary>
    ///     Removes a member from a religion
    /// </summary>
    void RemoveMember(string religionUID, string playerUID);

    /// <summary>
    ///     Gets the religion a player belongs to
    /// </summary>
    ReligionData? GetPlayerReligion(string playerUID);

    /// <summary>
    ///     Gets a religion by UID
    /// </summary>
    ReligionData? GetReligion(string religionUID);

    /// <summary>
    ///     Gets a religion by name
    /// </summary>
    ReligionData? GetReligionByName(string name);

    /// <summary>
    ///     Gets the active deity for a player
    /// </summary>
    DeityType GetPlayerActiveDeity(string playerUID);

    /// <summary>
    ///     Checks if a player can join a religion
    /// </summary>
    bool CanJoinReligion(string religionUID, string playerUID);

    /// <summary>
    ///     Invites a player to a religion
    /// </summary>
    bool InvitePlayer(string religionUID, string playerUID, string inviterUID);

    /// <summary>
    ///     Checks if a player has an invitation to a religion
    /// </summary>
    bool HasInvitation(string playerUID, string religionUID);

    /// <summary>
    ///     Removes an invitation (called after accepting or declining)
    /// </summary>
    void RemoveInvitation(string playerUID, string religionUID);

    /// <summary>
    ///     Gets all invitations for a player
    /// </summary>
    List<ReligionInvite> GetPlayerInvitations(string playerUID);

    /// <summary>
    ///     Accepts a religion invite
    /// </summary>
    (bool, string, string) AcceptInvite(string inviteId, string playerUID);

    /// <summary>
    ///     Declines a religion invite
    /// </summary>
    bool DeclineInvite(string inviteId, string playerUID);

    /// <summary>
    ///     Checks if a player has a religion
    /// </summary>
    bool HasReligion(string playerUID);

    /// <summary>
    ///     Gets all religions
    /// </summary>
    List<ReligionData> GetAllReligions();

    /// <summary>
    ///     Gets religions by deity
    /// </summary>
    List<ReligionData> GetReligionsByDeity(DeityType deity);

    /// <summary>
    ///     Deletes a religion (founder only)
    /// </summary>
    bool DeleteReligion(string religionUID, string requesterUID);

    /// <summary>
    ///     Bans a player from a religion
    /// </summary>
    bool BanPlayer(string religionUID, string playerUID, string bannedByUID, string reason = "",
        int? expiryDays = null);

    /// <summary>
    ///     Unbans a player from a religion
    /// </summary>
    bool UnbanPlayer(string religionUID, string playerUID);

    /// <summary>
    ///     Checks if a player is banned from a religion
    /// </summary>
    bool IsBanned(string religionUID, string playerUID);

    /// <summary>
    ///     Gets the ban details for a player
    /// </summary>
    BanEntry? GetBanDetails(string religionUID, string playerUID);

    /// <summary>
    ///     Gets all banned players for a religion
    /// </summary>
    List<BanEntry> GetBannedPlayers(string religionUID);

    /// <summary>
    ///     Manually triggers a save of all religion data
    /// </summary>
    void TriggerSave();

    void OnSaveGameLoaded();
    void OnGameWorldSave();

    /// <summary>
    ///     Saves the given religion data.
    /// </summary>
    /// <param name="religionData">The religion data to save.</param>
    void Save(ReligionData religionData);
}