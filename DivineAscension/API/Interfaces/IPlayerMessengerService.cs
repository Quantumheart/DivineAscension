using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for sending chat messages to players.
/// Separates messaging concerns from business logic for testability.
/// </summary>
public interface IPlayerMessengerService
{
    /// <summary>
    /// Sends a message to a specific player.
    /// </summary>
    /// <param name="player">The player to send the message to</param>
    /// <param name="message">The message content</param>
    /// <param name="type">The chat message type (notification, error, success, etc.)</param>
    void SendMessage(IServerPlayer player, string message, EnumChatType type = EnumChatType.Notification);

    /// <summary>
    /// Sends a success message to a player (green text).
    /// </summary>
    /// <param name="player">The player to send the message to</param>
    /// <param name="message">The success message</param>
    void SendSuccess(IServerPlayer player, string message);

    /// <summary>
    /// Sends an error message to a player (red text).
    /// </summary>
    /// <param name="player">The player to send the message to</param>
    /// <param name="message">The error message</param>
    void SendError(IServerPlayer player, string message);

    /// <summary>
    /// Sends an informational message to a player.
    /// </summary>
    /// <param name="player">The player to send the message to</param>
    /// <param name="message">The info message</param>
    void SendInfo(IServerPlayer player, string message);

    /// <summary>
    /// Broadcasts a message to all online players.
    /// </summary>
    /// <param name="message">The message to broadcast</param>
    /// <param name="type">The chat message type</param>
    void BroadcastMessage(string message, EnumChatType type = EnumChatType.Notification);

    /// <summary>
    /// Broadcasts a message to all members of a specific religion.
    /// </summary>
    /// <param name="religionUID">The religion ID to broadcast to</param>
    /// <param name="message">The message to send</param>
    void BroadcastToReligion(string religionUID, string message);

    /// <summary>
    /// Broadcasts a message to all members of religions in a specific civilization.
    /// </summary>
    /// <param name="civilizationId">The civilization ID to broadcast to</param>
    /// <param name="message">The message to send</param>
    void BroadcastToCivilization(string civilizationId, string message);

    /// <summary>
    /// Sends a localized message to a player with format arguments.
    /// </summary>
    /// <param name="player">The player to send the message to</param>
    /// <param name="localizationKey">The localization key</param>
    /// <param name="type">The chat message type</param>
    /// <param name="args">Format arguments for the localized string</param>
    void SendLocalizedMessage(IServerPlayer player, string localizationKey, EnumChatType type = EnumChatType.Notification, params object[] args);
}
