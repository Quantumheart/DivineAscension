using System;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
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
    private readonly INetworkService _networkService;
    private readonly ReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public ReligionCalendarTicker(
        ILoggerWrapper logger,
        IEventService eventService,
        IWorldService worldService,
        ReligionManager religionManager,
        IPlayerMessengerService messengerService,
        INetworkService networkService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
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
        var daysPerMonth = _worldService.Calendar?.DaysPerMonth ?? 0;
        if (daysPerMonth <= 0) return;

        foreach (var religion in _religionManager.GetAllReligions())
        {
            foreach (var feast in religion.FeastDays)
            {
                if (feast.Month != month) continue;
                // Clamp feast.Day down to DaysPerMonth so feasts intended for
                // day-of-month > the world's month length still fire on the
                // last day of the month (#375). Vanilla DaysPerMonth presets
                // go as low as 3, so the Harvest patron (day 12) would never
                // fire on the default 9-day month otherwise.
                var effectiveDay = Math.Min(feast.Day, daysPerMonth);
                if (effectiveDay != day) continue;
                if (feast.LastFiredYear >= year) continue;
                if (!religion.TryMarkFeastFired(feast, year)) continue;

                _religionManager.RecordFeastDay(religion.ReligionUID, feast);
                BroadcastFeast(religion, feast);
                PushToast(religion, feast);
            }
        }
    }

    private void BroadcastFeast(ReligionData religion, FeastDay feast)
    {
        var key = feast.Kind switch
        {
            FeastKind.Founding => LocalizationKeys.FEAST_FIRED_BROADCAST_FOUNDING,
            FeastKind.PatronDomain => LocalizationKeys.FEAST_FIRED_BROADCAST_PATRON,
            _ => LocalizationKeys.FEAST_FIRED_BROADCAST_CUSTOM
        };
        _messengerService.BroadcastToReligion(religion.ReligionUID,
            LocalizationService.Instance.Get(key, feast.Name));
    }

    /// <summary>
    ///     Pushes a transient toast to every online member of the religion.
    ///     The toast is informational — clicking it only dismisses; the
    ///     chronicle and Letters page are the durable surfaces.
    /// </summary>
    private void PushToast(ReligionData religion, FeastDay feast)
    {
        var packet = new HolidayKeptToastPacket(
            feastName: feast.Name,
            description: LocalizationService.Instance.Get(
                LocalizationKeys.UI_HOLIDAY_TOAST_BODY_RELIGION, religion.ReligionName),
            domain: religion.PatronDomain.ToString());

        foreach (var player in _worldService.GetAllOnlinePlayers()
                     .Where(p => religion.MemberUIDs.Contains(p.PlayerUID)))
        {
            _networkService.SendToPlayer(player, packet);
        }
    }
}
