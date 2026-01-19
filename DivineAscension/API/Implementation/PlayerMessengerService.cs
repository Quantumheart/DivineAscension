using System;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Implementation of IPlayerMessengerService that wraps player.SendMessage() for testability.
/// </summary>
public class PlayerMessengerService : IPlayerMessengerService
{
    private readonly IWorldService _worldService;
    private readonly IReligionManager? _religionManager;
    private readonly ICivilizationManager? _civilizationManager;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    /// <param name="worldService">World service for accessing players</param>
    /// <param name="religionManager">Religion manager for religion broadcasts (optional)</param>
    /// <param name="civilizationManager">Civilization manager for civilization broadcasts (optional)</param>
    public PlayerMessengerService(
        IWorldService worldService,
        IReligionManager? religionManager = null,
        ICivilizationManager? civilizationManager = null)
    {
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager;
        _civilizationManager = civilizationManager;
    }

    public void SendMessage(IServerPlayer player, string message, EnumChatType type = EnumChatType.Notification)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (string.IsNullOrEmpty(message)) return;

        player.SendMessage(0, message, type);
    }

    public void SendSuccess(IServerPlayer player, string message)
    {
        SendMessage(player, message, EnumChatType.CommandSuccess);
    }

    public void SendError(IServerPlayer player, string message)
    {
        SendMessage(player, message, EnumChatType.CommandError);
    }

    public void SendInfo(IServerPlayer player, string message)
    {
        SendMessage(player, message, EnumChatType.Notification);
    }

    public void BroadcastMessage(string message, EnumChatType type = EnumChatType.Notification)
    {
        if (string.IsNullOrEmpty(message)) return;

        var players = _worldService.GetAllOnlinePlayers();
        foreach (var player in players)
        {
            player.SendMessage(0, message, type);
        }
    }

    public void BroadcastToReligion(string religionUID, string message)
    {
        if (_religionManager == null)
            throw new InvalidOperationException("ReligionManager not provided to PlayerMessengerService");

        if (string.IsNullOrEmpty(message)) return;

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        var allPlayers = _worldService.GetAllOnlinePlayers();
        foreach (var player in allPlayers.Where(p => religion.MemberUIDs.Contains(p.PlayerUID)))
        {
            player.SendMessage(0, message, EnumChatType.Notification);
        }
    }

    public void BroadcastToCivilization(string civilizationId, string message)
    {
        if (_civilizationManager == null)
            throw new InvalidOperationException("CivilizationManager not provided to PlayerMessengerService");

        if (string.IsNullOrEmpty(message)) return;

        var civilization = _civilizationManager.GetCivilization(civilizationId);
        if (civilization == null) return;

        // Get all member UIDs from all religions in the civilization
        var memberUIDs = civilization.MemberReligionIds
            .Select(ruid => _religionManager?.GetReligion(ruid))
            .Where(r => r != null)
            .SelectMany(r => r!.MemberUIDs)
            .Distinct()
            .ToHashSet();

        var allPlayers = _worldService.GetAllOnlinePlayers();
        foreach (var player in allPlayers.Where(p => memberUIDs.Contains(p.PlayerUID)))
        {
            player.SendMessage(0, message, EnumChatType.Notification);
        }
    }

    public void SendLocalizedMessage(IServerPlayer player, string localizationKey, EnumChatType type = EnumChatType.Notification, params object[] args)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (string.IsNullOrEmpty(localizationKey)) return;

        var message = args.Length > 0
            ? LocalizationService.Instance.Get(localizationKey, args)
            : LocalizationService.Instance.Get(localizationKey);

        player.SendMessage(0, message, type);
    }
}
