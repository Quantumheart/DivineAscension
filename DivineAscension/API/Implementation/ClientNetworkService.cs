using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Client;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Client-side implementation of IClientNetworkService that wraps IClientNetworkChannel.
/// Provides a thin abstraction layer over Vintage Story's client networking for improved testability.
/// </summary>
internal sealed class ClientNetworkService(IClientNetworkChannel channel) : IClientNetworkService
{
    private readonly IClientNetworkChannel _channel = channel ?? throw new ArgumentNullException(nameof(channel));

    public void RegisterMessageHandler<T>(NetworkServerMessageHandler<T> handler) where T : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _channel.SetMessageHandler(handler);
    }

    public void SendToServer<T>(T message) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        _channel.SendPacket(message);
    }
}