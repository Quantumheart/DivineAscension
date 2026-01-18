using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for sending/receiving network messages on the Divine Ascension channel (server-side).
/// Wraps IServerNetworkChannel for testability.
/// </summary>
public interface INetworkService
{
    /// <summary>
    /// Register a message handler for a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="handler">The handler callback that receives the sender player and message.</param>
    void RegisterMessageHandler<T>(NetworkClientMessageHandler<T> handler) where T : class;

    /// <summary>
    /// Send a message to a specific player.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="player">The target player.</param>
    /// <param name="message">The message to send.</param>
    void SendToPlayer<T>(IServerPlayer player, T message) where T : class;

    /// <summary>
    /// Send a message to all connected players.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    void SendToAllPlayers<T>(T message) where T : class;

    /// <summary>
    /// Send a message to all players except the specified one.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="excludePlayer">The player to exclude.</param>
    /// <param name="message">The message to send.</param>
    void SendToOthers<T>(IServerPlayer excludePlayer, T message) where T : class;

    /// <summary>
    /// Broadcast a message to all connected clients.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to broadcast.</param>
    void Broadcast<T>(T message) where T : class;
}

/// <summary>
/// Client-side network service for receiving server messages.
/// Wraps IClientNetworkChannel for testability.
/// </summary>
public interface IClientNetworkService
{
    /// <summary>
    /// Register a message handler for a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="handler">The handler callback that receives the message.</param>
    void RegisterMessageHandler<T>(NetworkServerMessageHandler<T> handler) where T : class;

    /// <summary>
    /// Send a message to the server.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    void SendToServer<T>(T message) where T : class;
}