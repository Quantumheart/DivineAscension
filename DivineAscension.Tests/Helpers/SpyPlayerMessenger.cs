using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Test spy for IPlayerMessengerService that records all messages sent.
/// </summary>
public class SpyPlayerMessenger : IPlayerMessengerService
{
    private readonly List<SentMessage> _sentMessages = new();

    /// <summary>
    /// All messages sent through this service
    /// </summary>
    public IReadOnlyList<SentMessage> SentMessages => _sentMessages;

    /// <summary>
    /// Messages sent with Success type
    /// </summary>
    public IEnumerable<string> SuccessMessages
    {
        get
        {
            foreach (var msg in _sentMessages)
            {
                if (msg.Type == EnumChatType.CommandSuccess)
                    yield return msg.Message;
            }
        }
    }

    /// <summary>
    /// Messages sent with Error type
    /// </summary>
    public IEnumerable<string> ErrorMessages
    {
        get
        {
            foreach (var msg in _sentMessages)
            {
                if (msg.Type == EnumChatType.CommandError)
                    yield return msg.Message;
            }
        }
    }

    /// <summary>
    /// Messages sent with Notification type
    /// </summary>
    public IEnumerable<string> InfoMessages
    {
        get
        {
            foreach (var msg in _sentMessages)
            {
                if (msg.Type == EnumChatType.Notification)
                    yield return msg.Message;
            }
        }
    }

    /// <summary>
    /// Broadcast messages (no specific player)
    /// </summary>
    public IEnumerable<SentMessage> BroadcastMessages
    {
        get
        {
            foreach (var msg in _sentMessages)
            {
                if (msg.Player == null)
                    yield return msg;
            }
        }
    }

    public void SendMessage(IServerPlayer player, string message, EnumChatType type = EnumChatType.Notification)
    {
        _sentMessages.Add(new SentMessage(player, message, type));
    }

    public void SendSuccess(IServerPlayer player, string message)
    {
        _sentMessages.Add(new SentMessage(player, message, EnumChatType.CommandSuccess));
    }

    public void SendError(IServerPlayer player, string message)
    {
        _sentMessages.Add(new SentMessage(player, message, EnumChatType.CommandError));
    }

    public void SendInfo(IServerPlayer player, string message)
    {
        _sentMessages.Add(new SentMessage(player, message, EnumChatType.Notification));
    }

    public void BroadcastMessage(string message, EnumChatType type = EnumChatType.Notification)
    {
        _sentMessages.Add(new SentMessage(null, message, type));
    }

    public void BroadcastToReligion(string religionUID, string message)
    {
        _sentMessages.Add(new SentMessage(null, message, EnumChatType.Notification, religionUID));
    }

    public void BroadcastToCivilization(string civilizationId, string message)
    {
        _sentMessages.Add(new SentMessage(null, message, EnumChatType.Notification, CivilizationUID: civilizationId));
    }

    public void SendLocalizedMessage(IServerPlayer player, string localizationKey, EnumChatType type = EnumChatType.Notification, params object[] args)
    {
        // For test purposes, just record the key and args
        var message = $"{localizationKey} [{string.Join(", ", args)}]";
        _sentMessages.Add(new SentMessage(player, message, type));
    }

    /// <summary>
    /// Clears all recorded messages
    /// </summary>
    public void Clear()
    {
        _sentMessages.Clear();
    }

    /// <summary>
    /// Gets message count sent to a specific player
    /// </summary>
    public int GetMessageCountForPlayer(string playerUID)
    {
        int count = 0;
        foreach (var msg in _sentMessages)
        {
            if (msg.Player?.PlayerUID == playerUID)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Checks if any message contains the specified text
    /// </summary>
    public bool ContainsMessage(string text)
    {
        foreach (var msg in _sentMessages)
        {
            if (msg.Message?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Record of a sent message
    /// </summary>
    public record SentMessage(
        IServerPlayer? Player,
        string Message,
        EnumChatType Type,
        string? ReligionUID = null,
        string? CivilizationUID = null);
}
