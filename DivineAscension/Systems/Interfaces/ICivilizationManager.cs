using System;
using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

public interface ICivilizationManager
{
    /// <summary>
    ///     Event fired when a civilization is disbanded
    /// </summary>
    event Action<string>? OnCivilizationDisbanded;

    /// <summary>
    ///     Initializes the civilization manager
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Cleans up event subscriptions
    /// </summary>
    void Dispose();

    /// <summary>
    ///     Creates a new civilization
    /// </summary>
    /// <param name="name">Name of the civilization</param>
    /// <param name="founderUID">Player UID of the founder</param>
    /// <param name="founderReligionId">Religion ID of the founder</param>
    /// <param name="icon">Optional icon name for the civilization (defaults to "default")</param>
    /// <param name="description">Optional description for the civilization</param>
    /// <returns>The created civilization, or null if creation failed</returns>
    Civilization? CreateCivilization(string name, string founderUID, string founderReligionId, string icon = "default",
        string description = "");

    /// <summary>
    ///     Invites a religion to join a civilization
    /// </summary>
    bool InviteReligion(string civId, string religionId, string inviterUID);

    /// <summary>
    ///     Accepts an invitation to join a civilization
    /// </summary>
    bool AcceptInvite(string inviteId, string accepterUID);

    /// <summary>
    ///     Declines an invitation to join a civilization
    /// </summary>
    bool DeclineInvite(string inviteId, string declinerUID);

    /// <summary>
    ///     A religion leaves a civilization voluntarily
    /// </summary>
    bool LeaveReligion(string religionId, string requesterUID);

    /// <summary>
    ///     Kicks a religion from a civilization
    /// </summary>
    bool KickReligion(string civId, string religionId, string kickerUID);

    /// <summary>
    ///     Disbands a civilization
    /// </summary>
    bool DisbandCivilization(string civId, string requesterUID);

    /// <summary>
    ///     Updates a civilization's icon
    /// </summary>
    /// <param name="civId">ID of the civilization</param>
    /// <param name="requestorUID">Player UID requesting the update</param>
    /// <param name="icon">New icon name</param>
    /// <returns>True if successful, false otherwise</returns>
    bool UpdateCivilizationIcon(string civId, string requestorUID, string icon);

    /// <summary>
    ///     Updates a civilization's description
    /// </summary>
    /// <param name="civId">ID of the civilization</param>
    /// <param name="requestorUID">Player UID requesting the update</param>
    /// <param name="description">New description text</param>
    /// <returns>True if successful, false otherwise</returns>
    bool UpdateCivilizationDescription(string civId, string requestorUID, string description);

    /// <summary>
    ///     Gets a civilization by ID
    /// </summary>
    Civilization? GetCivilization(string civId);

    /// <summary>
    ///     Gets the civilization a religion belongs to
    /// </summary>
    Civilization? GetCivilizationByReligion(string religionId);

    /// <summary>
    ///     Gets the civilization a player belongs to (via their religion)
    /// </summary>
    Civilization? GetCivilizationByPlayer(string playerUID);

    /// <summary>
    ///     Gets all civilizations
    /// </summary>
    IEnumerable<Civilization> GetAllCivilizations();

    /// <summary>
    ///     Gets all deity types in a civilization
    /// </summary>
    HashSet<DeityDomain> GetCivDeityTypes(string civId);

    /// <summary>
    ///     Gets all religions in a civilization
    /// </summary>
    List<ReligionData> GetCivReligions(string civId);

    /// <summary>
    ///     Gets all pending invites for a religion
    /// </summary>
    List<CivilizationInvite> GetInvitesForReligion(string religionId);

    /// <summary>
    ///     Gets all pending invites for a civilization
    /// </summary>
    List<CivilizationInvite> GetInvitesForCiv(string civId);

    /// <summary>
    ///     Updates member counts for all civilizations (should be called when religion membership changes)
    /// </summary>
    void UpdateMemberCounts();
}