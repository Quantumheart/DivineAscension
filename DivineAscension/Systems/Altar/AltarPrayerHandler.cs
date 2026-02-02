using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.Altar.Pipeline;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Altar;

/// <summary>
/// Handles player prayer interactions at altars.
/// Delegates processing to the prayer pipeline for validation, rewards, and effects.
/// </summary>
public class AltarPrayerHandler(
    AltarEventEmitter altarEventEmitter,
    IPrayerPipeline pipeline,
    IPlayerMessengerService messenger,
    ITimeService timeService,
    ILoggerWrapper logger)
    : IDisposable
{
    private readonly AltarEventEmitter _altarEventEmitter =
        altarEventEmitter ?? throw new ArgumentNullException(nameof(altarEventEmitter));

    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerMessengerService _messenger =
        messenger ?? throw new ArgumentNullException(nameof(messenger));

    private readonly IPrayerPipeline _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    private readonly ITimeService _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));

    public void Dispose()
    {
        _altarEventEmitter.OnAltarUsed -= OnAltarUsed;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Altar Prayer Handler...");
        _altarEventEmitter.OnAltarUsed += OnAltarUsed;
        _logger.Notification("[DivineAscension] Altar Prayer Handler initialized");
    }

    [ExcludeFromCodeCoverage]
    private void OnAltarUsed(IPlayer player, BlockSelection blockSel)
    {
        if (player is not IServerPlayer serverPlayer)
            return;

        _logger.Debug($"[DivineAscension] Player {player.PlayerName} used altar at {blockSel.Position}");

        var context = new PrayerContext
        {
            PlayerUID = player.PlayerUID,
            PlayerName = player.PlayerName,
            AltarPosition = blockSel.Position,
            Offering = player.Entity.RightHandItemSlot?.Itemstack,
            CurrentTime = _timeService.ElapsedMilliseconds,
            Player = player
        };

        _pipeline.Execute(context);

        var chatType = context.Success ? EnumChatType.CommandSuccess : EnumChatType.CommandError;
        _messenger.SendMessage(serverPlayer, context.Message!, chatType);

        if (context.Success)
        {
            _logger.Debug(
                $"[DivineAscension] {player.PlayerName} prayed, awarded {context.FavorAwarded} favor and {context.PrestigeAwarded} prestige");
        }
    }
}