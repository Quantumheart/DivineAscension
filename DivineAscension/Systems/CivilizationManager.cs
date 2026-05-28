using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Civilizations;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Manages civilizations - alliances of 1-4 religions with different deities.
///     This is a thin facade: it owns the world data and the single lock that
///     serializes all access, and it raises the public events. The actual work is
///     delegated to lock-free collaborators in <c>Systems/Civilizations/</c>
///     (membership lifecycle, capital, chronicle, persistence, queries).
/// </summary>
public class CivilizationManager : ICivilizationManager
{
    private readonly CivilizationCapitalService _capital;
    private readonly CivilizationChronicler _chronicler;
    private readonly IEventService _eventService;
    private readonly ILoggerWrapper _logger;
    private readonly CivilizationMembershipService _membership;
    private readonly CivilizationProfileService _profile;
    private readonly IReligionManager _religionManager;
    private readonly CivilizationStore _store;
    private CivilizationWorldData _data = new();

    // Optional dependency wired after construction to break the circular init order
    // between HolySiteManager and CivilizationManager. Used only for capital cascades.
    private IHolySiteManager? _holySiteManager;

    /// <summary>
    ///     Lazy-initialized lock object for thread safety using Interlocked.CompareExchange
    /// </summary>
    private object? _lock;

    public CivilizationManager(
        ILoggerWrapper logger,
        IEventService eventService,
        IPersistenceService persistenceService,
        IWorldService worldService,
        IReligionManager religionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        if (persistenceService == null) throw new ArgumentNullException(nameof(persistenceService));
        if (worldService == null) throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));

        _store = new CivilizationStore(persistenceService, logger);
        _chronicler = new CivilizationChronicler(worldService);
        _capital = new CivilizationCapitalService(logger);
        _profile = new CivilizationProfileService(logger);
        _membership = new CivilizationMembershipService(logger, religionManager, worldService, _chronicler, _capital);
    }

    private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }

            return _lock;
        }
    }

    /// <summary>
    ///     Event fired when a civilization is disbanded
    /// </summary>
    public event Action<string>? OnCivilizationDisbanded;

    /// <summary>
    ///     Event fired when a religion joins a civilization
    /// </summary>
    public event Action<string, string>? OnReligionAdded;

    /// <summary>
    ///     Event fired when a religion leaves or is removed from a civilization
    /// </summary>
    public event Action<string, string>? OnReligionRemoved;

    /// <summary>
    ///     Initializes the civilization manager
    /// </summary>
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Civilization Manager...");

        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);
        _eventService.OnGameWorldSave(OnGameWorldSave);

        _religionManager.OnReligionDeleted += HandleReligionDeleted;

        _logger.Notification("[DivineAscension] Civilization Manager initialized");
    }

    /// <summary>
    ///     Cleans up event subscriptions
    /// </summary>
    public void Dispose()
    {
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribeGameWorldSave(OnGameWorldSave);
        _religionManager.OnReligionDeleted -= HandleReligionDeleted;
        if (_holySiteManager != null)
            _holySiteManager.OnHolySiteRemoved -= HandleHolySiteRemoved;
        OnCivilizationDisbanded = null;
        OnReligionAdded = null;
        OnReligionRemoved = null;
    }

    /// <inheritdoc />
    public void RecordChronicleEntry(string civId, ChronicleKind kind, string line, string? relatedId = null)
    {
        lock (Lock)
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ != null)
                _chronicler.Record(civ, kind, line, relatedId);
        }
    }

    /// <summary>
    ///     Appends a Founding Day chronicle entry and persists. Used by the
    ///     <see cref="CivilizationCalendarTicker"/>.
    /// </summary>
    public void RecordFoundingDay(string civId)
    {
        lock (Lock)
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null) return;
            _chronicler.RecordFoundingDay(civ);
            _store.Save(_data);
        }
    }

    /// <summary>
    ///     Stamps the in-game year on the civ's Founding-Day idempotency
    ///     stamp. Returns false if the stamp is already at or beyond
    ///     <paramref name="year"/> (don't fire), true if newly stamped (fire).
    /// </summary>
    public bool TryMarkFoundingDayFired(string civId, int year)
    {
        lock (Lock)
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null) return false;
            if (civ.FoundingDayLastFiredYear >= year) return false;
            civ.FoundingDayLastFiredYear = year;
            return true;
        }
    }

    /// <summary>
    ///     Wires the holy-site manager dependency after construction so capital cascades
    ///     can fire. Called from the system initializer once both managers exist.
    /// </summary>
    public void SetHolySiteManager(IHolySiteManager holySiteManager)
    {
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _capital.SetHolySiteManager(holySiteManager);
        _holySiteManager.OnHolySiteRemoved += HandleHolySiteRemoved;
    }

    /// <summary>
    ///     Raises the public event for each recorded side-effect. Invoked while the
    ///     lock is held so subscribers observe the same serialization as before.
    /// </summary>
    private void FireEvents(IReadOnlyList<CivEvent> events)
    {
        foreach (var ev in events)
        {
            switch (ev.Kind)
            {
                case CivEventKind.Disbanded:
                    OnCivilizationDisbanded?.Invoke(ev.CivId);
                    break;
                case CivEventKind.ReligionAdded:
                    OnReligionAdded?.Invoke(ev.CivId, ev.ReligionId!);
                    break;
                case CivEventKind.ReligionRemoved:
                    OnReligionRemoved?.Invoke(ev.CivId, ev.ReligionId!);
                    break;
            }
        }
    }

    #region Event Handlers

    /// <summary>
    ///     Cascade hook: when a holy site is removed, clear the binding on any civ
    ///     whose capital points at it.
    /// </summary>
    private void HandleHolySiteRemoved(string religionUID, string siteUID)
    {
        lock (Lock)
        {
            _capital.HandleHolySiteRemoved(_data, siteUID);
        }
    }

    /// <summary>
    ///     Handles religion deletion events from ReligionManager
    /// </summary>
    private void HandleReligionDeleted(string religionId)
    {
        lock (Lock)
        {
            var events = _membership.HandleReligionDeleted(_data, religionId);
            FireEvents(events);
        }
    }

    #endregion

    #region Civilization CRUD

    /// <inheritdoc />
    public Civilization? CreateCivilization(string name, string founderUID, string founderReligionId,
        string icon = "default", string description = "", CivilizationEthos? ethosOverride = null)
    {
        lock (Lock)
        {
            return _membership.CreateCivilization(_data, name, founderUID, founderReligionId, icon, description,
                ethosOverride);
        }
    }

    /// <inheritdoc />
    public bool InviteReligion(string civId, string religionId, string inviterUID)
    {
        lock (Lock)
        {
            return _membership.InviteReligion(_data, civId, religionId, inviterUID);
        }
    }

    /// <inheritdoc />
    public bool AcceptInvite(string inviteId, string accepterUID)
    {
        lock (Lock)
        {
            var result = _membership.AcceptInvite(_data, inviteId, accepterUID);
            FireEvents(result.Events);
            return result.Success;
        }
    }

    /// <inheritdoc />
    public bool DeclineInvite(string inviteId, string declinerUID)
    {
        lock (Lock)
        {
            return _membership.DeclineInvite(_data, inviteId, declinerUID);
        }
    }

    /// <inheritdoc />
    public bool LeaveReligion(string religionId, string requesterUID)
    {
        lock (Lock)
        {
            var result = _membership.LeaveReligion(_data, religionId, requesterUID);
            FireEvents(result.Events);
            return result.Success;
        }
    }

    /// <inheritdoc />
    public bool KickReligion(string civId, string religionId, string kickerUID)
    {
        lock (Lock)
        {
            var result = _membership.KickReligion(_data, civId, religionId, kickerUID);
            FireEvents(result.Events);
            return result.Success;
        }
    }

    /// <inheritdoc />
    public bool DisbandCivilization(string civId, string requesterUID)
    {
        lock (Lock)
        {
            var result = _membership.DisbandCivilization(_data, civId, requesterUID);
            FireEvents(result.Events);
            return result.Success;
        }
    }

    /// <inheritdoc />
    public bool UpdateCivilizationIcon(string civId, string requestorUID, string icon)
    {
        lock (Lock)
        {
            return _profile.UpdateIcon(_data, civId, requestorUID, icon);
        }
    }

    /// <inheritdoc />
    public bool UpdateCivilizationDescription(string civId, string requestorUID, string description)
    {
        lock (Lock)
        {
            return _profile.UpdateDescription(_data, civId, requestorUID, description);
        }
    }

    /// <inheritdoc />
    public bool SetCapital(string civId, string requestorUID, string capitalName, string? holySiteId)
    {
        lock (Lock)
        {
            return _capital.SetCapital(_data, civId, requestorUID, capitalName, holySiteId);
        }
    }

    #endregion

    #region Query Methods

    /// <inheritdoc />
    public Civilization? GetCivilization(string civId)
    {
        lock (Lock)
        {
            return _data.Civilizations.GetValueOrDefault(civId);
        }
    }

    /// <inheritdoc />
    public Civilization? GetCivilizationByReligion(string religionId)
    {
        lock (Lock)
        {
            return _data.GetCivilizationByReligion(religionId);
        }
    }

    /// <inheritdoc />
    public Civilization? GetCivilizationByPlayer(string playerUID)
    {
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
            return null;

        lock (Lock)
        {
            return _data.GetCivilizationByReligion(religion.ReligionUID);
        }
    }

    /// <inheritdoc />
    public IEnumerable<Civilization> GetAllCivilizations()
    {
        lock (Lock)
        {
            return _data.Civilizations.Values.ToList();
        }
    }

    /// <inheritdoc />
    public HashSet<DeityDomain> GetCivDeityTypes(string civId)
    {
        lock (Lock)
        {
            return CivilizationQueries.GetDeityTypes(_data, _religionManager, civId);
        }
    }

    /// <inheritdoc />
    public List<ReligionData> GetCivReligions(string civId)
    {
        lock (Lock)
        {
            return CivilizationQueries.GetReligions(_data, _religionManager, civId);
        }
    }

    /// <inheritdoc />
    public List<CivilizationInvite> GetInvitesForReligion(string religionId)
    {
        lock (Lock)
        {
            return _data.GetInvitesForReligion(religionId);
        }
    }

    /// <inheritdoc />
    public List<CivilizationInvite> GetInvitesForCiv(string civId)
    {
        lock (Lock)
        {
            return _data.GetInvitesForCivilization(civId);
        }
    }

    /// <inheritdoc />
    public void UpdateMemberCounts()
    {
        lock (Lock)
        {
            foreach (var civ in _data.Civilizations.Values.ToList())
            {
                var totalMembers = 0;
                foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
                {
                    var religion = _religionManager.GetReligion(religionId);
                    if (religion != null) totalMembers += religion.MemberUIDs.Count;
                }

                civ.MemberCount = totalMembers;
            }
        }
    }

    #endregion

    #region Persistence

    private void OnSaveGameLoaded()
    {
        lock (Lock)
        {
            _data = _store.Load();
        }
    }

    private void OnGameWorldSave()
    {
        lock (Lock)
        {
            _store.Save(_data);
        }
    }

    #endregion
}
