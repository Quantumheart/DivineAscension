using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;

namespace DivineAscension.Systems;

/// <summary>
///     Polls the in-game calendar each minute, firing chronicle entries and
///     member broadcasts for any religion feast day whose date matches today
///     and hasn't already fired in the current in-game year (#375).
///     Idempotency is guaranteed by stamping <c>FeastDay.LastFiredYear</c>,
///     which persists in the save — reload on the same day does not re-fire.
/// </summary>
public sealed class ReligionCalendarTicker
{
    /// <summary>
    ///     Once a minute is plenty: in-game days are minutes to hours of wall
    ///     time, and we just need to catch the date rollover.
    /// </summary>
    private const int TickIntervalMs = 60_000;

    private readonly IEventService _eventService;
    private readonly ILoggerWrapper _logger;
    private readonly IPlayerMessengerService _messengerService;
    private readonly ReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public ReligionCalendarTicker(
        ILoggerWrapper logger,
        IEventService eventService,
        IWorldService worldService,
        ReligionManager religionManager,
        IPlayerMessengerService messengerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
    }

    public void Initialize()
    {
        _eventService.RegisterGameTickListener(OnTick, TickIntervalMs);
        _logger.Notification("[DivineAscension] ReligionCalendarTicker initialized");
    }

    /// <summary>
    ///     Internal entry point — package-private so tests can drive the tick
    ///     deterministically without registering a real game tick listener.
    /// </summary>
    internal void OnTick(float _)
    {
        var (month, day) = SacredCalendar.GetCurrentMonthDay(_worldService);
        var year = SacredCalendar.GetCurrentYear(_worldService);
        if (month <= 0 || day <= 0 || year <= 0) return;

        foreach (var religion in _religionManager.GetAllReligions())
        {
            foreach (var feast in religion.FeastDays)
            {
                if (feast.Month != month || feast.Day != day) continue;
                if (feast.LastFiredYear >= year) continue;
                if (!religion.TryMarkFeastFired(feast, year)) continue;

                _religionManager.RecordFeastDay(religion.ReligionUID, feast);
                BroadcastFeast(religion.ReligionUID, feast);
            }
        }
    }

    private void BroadcastFeast(string religionUID, FeastDay feast)
    {
        var key = feast.Kind switch
        {
            FeastKind.Founding => LocalizationKeys.FEAST_FIRED_BROADCAST_FOUNDING,
            FeastKind.PatronDomain => LocalizationKeys.FEAST_FIRED_BROADCAST_PATRON,
            _ => LocalizationKeys.FEAST_FIRED_BROADCAST_CUSTOM
        };
        _messengerService.BroadcastToReligion(religionUID,
            LocalizationService.Instance.Get(key, feast.Name));
    }
}
