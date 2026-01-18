using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of INetworkService that wraps IServerNetworkChannel.
/// Provides a thin abstraction layer over Vintage Story's networking for improved testability.
/// </summary>
internal sealed class ServerNetworkService(IServerNetworkChannel channel) : INetworkService
{
    private readonly IServerNetworkChannel _channel = channel ?? throw new ArgumentNullException(nameof(channel));

    public void RegisterMessageHandler<T>(NetworkClientMessageHandler<T> handler) where T : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _channel.SetMessageHandler(handler);
    }

    public void SendToPlayer<T>(IServerPlayer player, T message) where T : class
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (message == null) throw new ArgumentNullException(nameof(message));

        _channel.SendPacket(message, player);
    }

    public void SendToAllPlayers<T>(T message) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        _channel.BroadcastPacket(message);
    }

    public void SendToOthers<T>(IServerPlayer excludePlayer, T message) where T : class
    {
        if (excludePlayer == null) throw new ArgumentNullException(nameof(excludePlayer));
        if (message == null) throw new ArgumentNullException(nameof(message));

        _channel.BroadcastPacket(message, excludePlayer);
    }

    public void Broadcast<T>(T message) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        _channel.BroadcastPacket(message);
    }
}