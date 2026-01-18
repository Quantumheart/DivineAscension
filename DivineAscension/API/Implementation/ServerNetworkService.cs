using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of INetworkService that wraps IServerNetworkChannel.
/// Provides a thin abstraction layer over Vintage Story's networking for improved testability.
/// </summary>
internal sealed class ServerNetworkService : INetworkService
{
    private readonly IServerNetworkChannel _channel;

    public ServerNetworkService(IServerNetworkChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public void RegisterMessageHandler<T>(Action<IServerPlayer, T> handler) where T : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _channel.SetMessageHandler<T>(handler);
    }

    public void RegisterMessageHandler<T>(string messageId, Action<IServerPlayer, T> handler) where T : class
    {
        if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException(nameof(messageId));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _channel.SetMessageHandler<T>(messageId, handler);
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

    public void SendToPlayersInRange<T>(Vec3d position, float range, T message) where T : class
    {
        if (position == null) throw new ArgumentNullException(nameof(position));
        if (message == null) throw new ArgumentNullException(nameof(message));

        _channel.SendPacket(message, position, range);
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
