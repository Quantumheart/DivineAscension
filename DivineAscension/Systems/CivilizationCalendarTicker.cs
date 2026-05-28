using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;

namespace DivineAscension.Systems;

/// <summary>
///     Polls the in-game calendar each minute, firing a chronicle entry and
///     member broadcast for each civilization's annual Founding Day.
///     Idempotency stamp lives on the civ itself
///     (<c>Civilization.FoundingDayLastFiredYear</c>) and persists in the
///     save, so reload on the same day does not re-fire. Mirrors the
///     religion <c>ReligionCalendarTicker</c>; per the design discussion,
///     civilizations have a single auto holiday and no custom calendar.
/// </summary>
public sealed class CivilizationCalendarTicker
{
    private const int TickIntervalMs = 60_000;

    private readonly IEventService _eventService;
    private readonly ILoggerWrapper _logger;
    private readonly IPlayerMessengerService _messengerService;
    private readonly CivilizationManager _civilizationManager;
    private readonly IWorldService _worldService;

    public CivilizationCalendarTicker(
        ILoggerWrapper logger,
        IEventService eventService,
        IWorldService worldService,
        CivilizationManager civilizationManager,
        IPlayerMessengerService messengerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _civilizationManager = civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));
        _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
    }

    public void Initialize()
    {
        _eventService.RegisterGameTickListener(OnTick, TickIntervalMs);
        _logger.Notification("[DivineAscension] CivilizationCalendarTicker initialized");
    }

    /// <summary>Internal so tests can drive the tick deterministically.</summary>
    internal void OnTick(float _)
    {
        var (month, day) = SacredCalendar.GetCurrentMonthDay(_worldService);
        var year = SacredCalendar.GetCurrentYear(_worldService);
        if (month <= 0 || day <= 0 || year <= 0) return;

        var daysPerMonth = 0;
        try { daysPerMonth = _worldService.Calendar?.DaysPerMonth ?? 0; }
        catch { return; }
        if (daysPerMonth <= 0) return;

        foreach (var civ in _civilizationManager.GetAllCivilizations())
        {
            if (civ.FoundingMonth <= 0 || civ.FoundingDay <= 0) continue;
            if (civ.FoundingMonth != month) continue;
            // Clamp Founding Day down to DaysPerMonth — captured value can be
            // valid for the world's calendar at creation but become unreachable
            // if a server later shrinks DaysPerMonth. The feast then fires on
            // the last day of the month, same convention as religion feasts.
            var effectiveDay = Math.Min(civ.FoundingDay, daysPerMonth);
            if (effectiveDay != day) continue;
            if (civ.FoundingDayLastFiredYear >= year) continue;
            if (!_civilizationManager.TryMarkFoundingDayFired(civ.CivId, year)) continue;

            _civilizationManager.RecordFoundingDay(civ.CivId);
            _messengerService.BroadcastToCivilization(civ.CivId,
                Services.LocalizationService.Instance.Get(LocalizationKeys.CIV_FOUNDING_DAY_BROADCAST));
        }
    }
}
