using DivineAscension.API.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Spy implementation of INetworkService for testing.
/// Records all sent messages and allows manual triggering of handlers.
/// </summary>
public sealed class SpyNetworkService : INetworkService
{
    public enum SendType
    {
        ToPlayer,
        ToAll,
        ToOthers,
        Broadcast
    }

    private readonly Dictionary<Type, Delegate> _handlers = new();
    private readonly List<SentMessage> _sentMessages = new();

    public int MessageCount => _sentMessages.Count;

    public void RegisterMessageHandler<T>(NetworkClientMessageHandler<T> handler) where T : class
    {
        _handlers[typeof(T)] = handler;
    }

    public void SendToPlayer<T>(IServerPlayer player, T message) where T : class
    {
        _sentMessages.Add(new SentMessage(player, message, SendType.ToPlayer));
    }

    public void SendToAllPlayers<T>(T message) where T : class
    {
        _sentMessages.Add(new SentMessage(null, message, SendType.ToAll));
    }

    public void SendToOthers<T>(IServerPlayer excludePlayer, T message) where T : class
    {
        _sentMessages.Add(new SentMessage(excludePlayer, message, SendType.ToOthers));
    }

    public void Broadcast<T>(T message) where T : class
    {
        _sentMessages.Add(new SentMessage(null, message, SendType.Broadcast));
    }

    // Test helper methods
    public void SimulateReceive<T>(IServerPlayer player, T message) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var handler))
        {
            ((NetworkClientMessageHandler<T>)handler)(player, message);
        }
    }

    public IReadOnlyList<SentMessage> GetSentMessages() => _sentMessages.AsReadOnly();

    public IEnumerable<T> GetSentMessages<T>() where T : class
    {
        return _sentMessages
            .Where(m => m.Message is T)
            .Select(m => (T)m.Message);
    }

    public SentMessage? GetLastSentMessage() => _sentMessages.LastOrDefault();

    public T? GetLastSentMessage<T>() where T : class
    {
        return _sentMessages
            .Where(m => m.Message is T)
            .Select(m => (T)m.Message)
            .LastOrDefault();
    }

    public void Clear()
    {
        _sentMessages.Clear();
        _handlers.Clear();
    }

    public sealed record SentMessage(
        IServerPlayer? Player,
        object Message,
        SendType Type);
}

/// <summary>
/// Spy implementation of IClientNetworkService for testing.
/// Records all sent messages and allows manual triggering of handlers.
/// </summary>
public sealed class SpyClientNetworkService : IClientNetworkService
{
    private readonly Dictionary<Type, Delegate> _handlers = new();
    private readonly List<object> _sentMessages = new();

    public int MessageCount => _sentMessages.Count;

    public void RegisterMessageHandler<T>(NetworkServerMessageHandler<T> handler) where T : class
    {
        _handlers[typeof(T)] = handler;
    }

    public void SendToServer<T>(T message) where T : class
    {
        _sentMessages.Add(message);
    }

    // Test helper methods
    public void SimulateReceive<T>(T message) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var handler))
        {
            ((NetworkServerMessageHandler<T>)handler)(message);
        }
    }

    public IReadOnlyList<object> GetSentMessages() => _sentMessages.AsReadOnly();

    public IEnumerable<T> GetSentMessages<T>() where T : class
    {
        return _sentMessages.OfType<T>();
    }

    public T? GetLastSentMessage<T>() where T : class
    {
        return _sentMessages.OfType<T>().LastOrDefault();
    }

    public void Clear()
    {
        _sentMessages.Clear();
        _handlers.Clear();
    }
}