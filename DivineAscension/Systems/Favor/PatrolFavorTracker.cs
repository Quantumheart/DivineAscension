using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.HolySite;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
/// Tracks patrol completion for Conquest domain players.
/// Awards favor/prestige for visiting 2+ holy sites within their civilization
/// within a time window. Includes a persistent combo system and distance-based rewards.
/// </summary>
public class PatrolFavorTracker : IFavorTracker, IDisposable
{
    #region Configuration Constants

    /// <summary>Maximum time to complete a patrol (30 minutes)</summary>
    private const long PATROL_WINDOW_MS = 30 * 60 * 1000;

    /// <summary>Reset current patrol if idle for 5 minutes</summary>
    private const long PATROL_IDLE_TIMEOUT_MS = 5 * 60 * 1000;

    /// <summary>Cooldown between patrol completions (60 minutes)</summary>
    private const long PATROL_COMPLETION_COOLDOWN_MS = 60 * 60 * 1000;

    /// <summary>Reset combo multiplier after 2 hours</summary>
    private const long COMBO_TIMEOUT_MS = 2 * 60 * 60 * 1000;

    /// <summary>Minimum sites required to complete a patrol</summary>
    private const int MIN_SITES_FOR_PATROL = 2;

    /// <summary>Base favor for completing a 2-site patrol</summary>
    private const float BASE_FAVOR_2_SITES = 15f;

    /// <summary>Additional favor per extra site beyond 2</summary>
    private const float FAVOR_PER_EXTRA_SITE = 10f;

    /// <summary>Bonus favor for visiting all civilization sites</summary>
    private const float FULL_CIRCUIT_BONUS = 25f;

    /// <summary>Favor per block traveled</summary>
    private const float FAVOR_PER_BLOCK = 0.005f;

    /// <summary>Speed bonus for completing in under 10 minutes</summary>
    private const float SPEED_BONUS_FAST = 10f;

    /// <summary>Speed bonus for completing in under 20 minutes</summary>
    private const float SPEED_BONUS_MEDIUM = 5f;

    /// <summary>Time threshold for fast completion bonus (10 minutes)</summary>
    private const long FAST_COMPLETION_MS = 10 * 60 * 1000;

    /// <summary>Time threshold for medium completion bonus (20 minutes)</summary>
    private const long MEDIUM_COMPLETION_MS = 20 * 60 * 1000;

    #endregion

    #region Dependencies

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly ILoggerWrapper _logger;
    private readonly ITimeService _timeService;
    private readonly IFavorSystem _favorSystem;
    private readonly IHolySiteAreaTracker _holySiteAreaTracker;
    private readonly ICivilizationManager _civilizationManager;
    private readonly IHolySiteManager _holySiteManager;
    private readonly IReligionManager _religionManager;
    private readonly IPlayerMessengerService _messenger;
    private readonly IEventService _eventService;

    #endregion

    #region State

    /// <summary>
    /// In-memory patrol sessions per player (not persisted - resets on disconnect)
    /// </summary>
    private readonly Dictionary<string, PatrolSession> _activeSessions = new();

    private bool _disposed;

    #endregion

    public PatrolFavorTracker(
        IPlayerProgressionDataManager playerProgressionDataManager,
        ILoggerWrapper logger,
        ITimeService timeService,
        IFavorSystem favorSystem,
        IHolySiteAreaTracker holySiteAreaTracker,
        ICivilizationManager civilizationManager,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        IPlayerMessengerService messenger,
        IEventService eventService)
    {
        _playerProgressionDataManager = playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
        _holySiteAreaTracker = holySiteAreaTracker ?? throw new ArgumentNullException(nameof(holySiteAreaTracker));
        _civilizationManager = civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    /// <inheritdoc />
    public DeityDomain DeityDomain => DeityDomain.Conquest;

    /// <inheritdoc />
    public void Initialize()
    {
        _logger.Debug($"{SystemConstants.LogPrefix} Initializing PatrolFavorTracker...");

        // Subscribe to holy site area events
        _holySiteAreaTracker.OnPlayerEnteredHolySite += OnPlayerEnteredHolySite;
        _holySiteAreaTracker.OnPlayerExitedHolySite += OnPlayerExitedHolySite;

        // Subscribe to player events to clean up sessions
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);

        _logger.Debug($"{SystemConstants.LogPrefix} PatrolFavorTracker initialized");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _holySiteAreaTracker.OnPlayerEnteredHolySite -= OnPlayerEnteredHolySite;
        _holySiteAreaTracker.OnPlayerExitedHolySite -= OnPlayerExitedHolySite;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);

        _activeSessions.Clear();
    }

    #region Event Handlers

    /// <summary>
    /// Handles player entering a holy site - records visit for patrol tracking.
    /// </summary>
    private void OnPlayerEnteredHolySite(IServerPlayer player, HolySiteData site)
    {
        var playerUID = player.PlayerUID;

        // Only track Conquest domain players
        if (!IsConquestDomainPlayer(playerUID))
        {
            return;
        }

        // Only track civilization sites (not just own religion)
        var civSites = GetCivilizationHolySites(playerUID);
        if (!civSites.Any(s => s.SiteUID == site.SiteUID))
        {
            _logger.Debug($"{SystemConstants.LogPrefix} Player {player.PlayerName} entered non-civilization site '{site.SiteName}' - not tracking");
            return;
        }

        var currentTime = _timeService.ElapsedMilliseconds;
        var session = GetOrCreateSession(playerUID, currentTime);

        // Check for session reset conditions
        if (ShouldResetCurrentPatrol(session, currentTime))
        {
            _logger.Debug($"{SystemConstants.LogPrefix} Resetting patrol session for {player.PlayerName} (timeout or idle)");
            ResetSession(session, currentTime);
        }

        // Record the visit
        RecordSiteVisit(player, session, site, currentTime);

        // Check if patrol can be completed
        TryCompletePatrol(player, session, civSites.Count, currentTime);
    }

    /// <summary>
    /// Handles player exiting a holy site - updates last activity time.
    /// </summary>
    private void OnPlayerExitedHolySite(IServerPlayer player, HolySiteData site)
    {
        var playerUID = player.PlayerUID;

        if (_activeSessions.TryGetValue(playerUID, out var session))
        {
            session.LastSiteVisitTime = _timeService.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// Handles player leaving religion - cleans up session.
    /// </summary>
    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        _activeSessions.Remove(player.PlayerUID);
    }

    /// <summary>
    /// Handles player disconnect - cleans up session.
    /// </summary>
    private void OnPlayerDisconnect(IServerPlayer player)
    {
        _activeSessions.Remove(player.PlayerUID);
    }

    #endregion

    #region Patrol Logic

    /// <summary>
    /// Records a site visit in the patrol session.
    /// </summary>
    private void RecordSiteVisit(IServerPlayer player, PatrolSession session, HolySiteData site, long currentTime)
    {
        var siteUID = site.SiteUID;

        // Don't record duplicate visits
        if (session.VisitedSiteUIDs.Contains(siteUID))
        {
            _logger.Debug($"{SystemConstants.LogPrefix} Player {player.PlayerName} re-entered site '{site.SiteName}' - not counting duplicate");
            return;
        }

        // Record the visit
        session.VisitedSiteUIDs.Add(siteUID);
        session.LastSiteVisitTime = currentTime;

        // Store position for distance calculation (in visit order)
        var playerPos = player.Entity.Pos.AsBlockPos;
        session.VisitPositions.Add(playerPos);

        _logger.Debug($"{SystemConstants.LogPrefix} Player {player.PlayerName} visited site '{site.SiteName}' ({session.VisitedSiteUIDs.Count} sites in patrol)");

        // Notify player of patrol progress
        var civSites = GetCivilizationHolySites(player.PlayerUID);
        if (session.VisitedSiteUIDs.Count == 1)
        {
            var message = LocalizationService.Instance.Get(
                "divineascension:patrol.started",
                MIN_SITES_FOR_PATROL - 1);
            _messenger.SendMessage(player, message);
        }
        else if (session.VisitedSiteUIDs.Count < MIN_SITES_FOR_PATROL)
        {
            var message = LocalizationService.Instance.Get(
                "divineascension:patrol.progress",
                session.VisitedSiteUIDs.Count,
                MIN_SITES_FOR_PATROL,
                MIN_SITES_FOR_PATROL - session.VisitedSiteUIDs.Count);
            _messenger.SendMessage(player, message);
        }
    }

    /// <summary>
    /// Attempts to complete a patrol if conditions are met.
    /// </summary>
    private void TryCompletePatrol(IServerPlayer player, PatrolSession session, int civTotalSites, long currentTime)
    {
        var playerUID = player.PlayerUID;

        // Need minimum sites to complete
        if (session.VisitedSiteUIDs.Count < MIN_SITES_FOR_PATROL)
        {
            return;
        }

        // Check cooldown
        if (!_playerProgressionDataManager.TryGetPlayerData(playerUID, out var playerData))
        {
            return;
        }

        if (IsOnPatrolCooldown(playerData!, currentTime))
        {
            var remainingMs = playerData!.LastPatrolCompletionTime + PATROL_COMPLETION_COOLDOWN_MS - currentTime;
            var remainingMins = (int)(remainingMs / 60000);
            var cooldownMessage = LocalizationService.Instance.Get("divineascension:patrol.cooldown", remainingMins);
            _messenger.SendMessage(player, cooldownMessage);
            return;
        }

        // Calculate rewards
        var reward = CalculateReward(session, civTotalSites, playerData!, currentTime);

        // Check combo timeout and reset if needed
        if (ShouldResetCombo(playerData!, currentTime))
        {
            _logger.Debug($"{SystemConstants.LogPrefix} Resetting combo for {player.PlayerName} (timeout)");
            playerData!.PatrolComboCount = 0;
            playerData.PatrolPreviousMultiplier = 1.0f;
        }

        // Get combo multiplier before incrementing
        var oldMultiplier = GetComboMultiplier(playerData!.PatrolComboCount);
        var finalReward = reward * oldMultiplier;

        // Increment combo
        playerData!.PatrolComboCount++;
        var newMultiplier = GetComboMultiplier(playerData.PatrolComboCount);

        // Calculate full circuit before resetting session
        var isFullCircuit = session.VisitedSiteUIDs.Count >= civTotalSites && civTotalSites > 1;

        // Award favor/prestige
        _favorSystem.AwardFavorForAction(playerUID, "patrol completion", finalReward, DeityDomain.Conquest);

        // Update persistence
        playerData.LastPatrolCompletionTime = currentTime;
        playerData.PatrolPreviousMultiplier = newMultiplier;

        // Reset session for next patrol
        ResetSession(session, currentTime);
        var comboTier = GetComboTierName(playerData.PatrolComboCount);

        // Build completion message
        var message = LocalizationService.Instance.Get("divineascension:patrol.complete", finalReward);

        if (isFullCircuit)
        {
            message += " " + LocalizationService.Instance.Get("divineascension:patrol.full_circuit");
        }

        if (!string.IsNullOrEmpty(comboTier))
        {
            var localizedTier = LocalizationService.Instance.Get($"divineascension:patrol.tier.{comboTier.ToLowerInvariant()}");
            message += " " + LocalizationService.Instance.Get("divineascension:patrol.combo", localizedTier, newMultiplier);
        }

        _messenger.SendMessage(player, message);

        // Notify on tier change
        if (newMultiplier > oldMultiplier)
        {
            NotifyTierChange(player, oldMultiplier, newMultiplier);
        }

        _logger.Debug($"{SystemConstants.LogPrefix} Player {player.PlayerName} completed patrol: {finalReward:F1} favor, combo {playerData.PatrolComboCount}");
    }

    /// <summary>
    /// Calculates the total reward for completing a patrol.
    /// </summary>
    private float CalculateReward(PatrolSession session, int civTotalSites, PlayerProgressionData playerData, long currentTime)
    {
        var sitesVisited = session.VisitedSiteUIDs.Count;
        var elapsedMs = currentTime - session.PatrolStartTime;

        // Base reward (15 for 2 sites, +10 per extra)
        var reward = BASE_FAVOR_2_SITES + FAVOR_PER_EXTRA_SITE * Math.Max(0, sitesVisited - 2);

        // Full circuit bonus
        if (sitesVisited >= civTotalSites && civTotalSites > 1)
        {
            reward += FULL_CIRCUIT_BONUS;
        }

        // Distance bonus
        reward += CalculateDistanceBonus(session);

        // Speed bonus
        if (elapsedMs < FAST_COMPLETION_MS)
        {
            reward += SPEED_BONUS_FAST;
        }
        else if (elapsedMs < MEDIUM_COMPLETION_MS)
        {
            reward += SPEED_BONUS_MEDIUM;
        }

        return reward;
    }

    /// <summary>
    /// Calculates the distance bonus based on total path distance.
    /// </summary>
    private float CalculateDistanceBonus(PatrolSession session)
    {
        if (session.VisitPositions.Count < 2)
        {
            return 0f;
        }

        // Calculate total distance between sites (in visit order)
        var totalDistance = 0f;

        for (int i = 1; i < session.VisitPositions.Count; i++)
        {
            var dx = session.VisitPositions[i].X - session.VisitPositions[i - 1].X;
            var dy = session.VisitPositions[i].Y - session.VisitPositions[i - 1].Y;
            var dz = session.VisitPositions[i].Z - session.VisitPositions[i - 1].Z;
            totalDistance += (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        return totalDistance * FAVOR_PER_BLOCK;
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Gets or creates a patrol session for a player.
    /// </summary>
    private PatrolSession GetOrCreateSession(string playerUID, long currentTime)
    {
        if (!_activeSessions.TryGetValue(playerUID, out var session))
        {
            session = new PatrolSession
            {
                PatrolStartTime = currentTime,
                LastSiteVisitTime = currentTime
            };
            _activeSessions[playerUID] = session;
        }

        return session;
    }

    /// <summary>
    /// Resets a patrol session for a new patrol attempt.
    /// </summary>
    private void ResetSession(PatrolSession session, long currentTime)
    {
        session.VisitedSiteUIDs.Clear();
        session.VisitPositions.Clear();
        session.PatrolStartTime = currentTime;
        session.LastSiteVisitTime = currentTime;
    }

    /// <summary>
    /// Checks if the current patrol should be reset due to timeout or idle.
    /// </summary>
    private bool ShouldResetCurrentPatrol(PatrolSession session, long currentTime)
    {
        // Reset if exceeded patrol window
        if (currentTime - session.PatrolStartTime > PATROL_WINDOW_MS)
        {
            return true;
        }

        // Reset if idle too long
        if (session.VisitedSiteUIDs.Count > 0 && currentTime - session.LastSiteVisitTime > PATROL_IDLE_TIMEOUT_MS)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the combo should be reset due to timeout.
    /// </summary>
    internal bool ShouldResetCombo(PlayerProgressionData data, long currentTime)
    {
        if (data.PatrolComboCount == 0)
        {
            return false;
        }

        return currentTime - data.LastPatrolCompletionTime > COMBO_TIMEOUT_MS;
    }

    /// <summary>
    /// Checks if the player is on patrol completion cooldown.
    /// </summary>
    internal bool IsOnPatrolCooldown(PlayerProgressionData data, long currentTime)
    {
        if (data.LastPatrolCompletionTime == 0)
        {
            return false;
        }

        return currentTime - data.LastPatrolCompletionTime < PATROL_COMPLETION_COOLDOWN_MS;
    }

    #endregion

    #region Civilization Queries

    /// <summary>
    /// Gets all holy sites in the player's civilization.
    /// </summary>
    internal List<HolySiteData> GetCivilizationHolySites(string playerUID)
    {
        var civ = _civilizationManager.GetCivilizationByPlayer(playerUID);
        if (civ == null)
        {
            // If not in a civilization, just get own religion's sites
            var religion = _religionManager.GetPlayerReligion(playerUID);
            if (religion == null)
            {
                return new List<HolySiteData>();
            }

            return _holySiteManager.GetReligionHolySites(religion.ReligionUID);
        }

        // Get all holy sites from all religions in the civilization
        var sites = new List<HolySiteData>();
        var religions = _civilizationManager.GetCivReligions(civ.CivId);

        foreach (var religion in religions)
        {
            sites.AddRange(_holySiteManager.GetReligionHolySites(religion.ReligionUID));
        }

        return sites;
    }

    /// <summary>
    /// Checks if a player is a Conquest domain follower.
    /// </summary>
    internal bool IsConquestDomainPlayer(string playerUID)
    {
        return _playerProgressionDataManager.GetPlayerDeityType(playerUID) == DeityDomain.Conquest;
    }

    #endregion

    #region Combo System

    /// <summary>
    /// Gets the combo multiplier for a given combo count.
    /// </summary>
    internal float GetComboMultiplier(int comboCount)
    {
        return comboCount switch
        {
            <= 1 => 1.0f,
            <= 3 => 1.15f,   // Vigilant
            <= 7 => 1.3f,    // Dedicated
            <= 14 => 1.5f,   // Tireless
            _ => 1.75f       // Legendary
        };
    }

    /// <summary>
    /// Gets the combo tier name for a given combo count.
    /// </summary>
    internal string GetComboTierName(int comboCount)
    {
        return comboCount switch
        {
            <= 1 => "",
            <= 3 => "Vigilant",
            <= 7 => "Dedicated",
            <= 14 => "Tireless",
            _ => "Legendary"
        };
    }

    /// <summary>
    /// Notifies the player when their combo tier increases.
    /// </summary>
    private void NotifyTierChange(IServerPlayer player, float oldMult, float newMult)
    {
        // Determine which tier we reached
        var tierKey = "";
        if (newMult >= 1.75f && oldMult < 1.75f)
            tierKey = "legendary";
        else if (newMult >= 1.5f && oldMult < 1.5f)
            tierKey = "tireless";
        else if (newMult >= 1.3f && oldMult < 1.3f)
            tierKey = "dedicated";
        else if (newMult >= 1.15f && oldMult < 1.15f)
            tierKey = "vigilant";

        if (!string.IsNullOrEmpty(tierKey))
        {
            var tierName = LocalizationService.Instance.Get($"divineascension:patrol.tier.{tierKey}");
            var message = LocalizationService.Instance.Get("divineascension:patrol.tier_reached", tierName, newMult);
            _messenger.SendMessage(player, message);
        }
    }

    #endregion

    #region Session Data

    /// <summary>
    /// In-memory session state for a player's current patrol.
    /// Not persisted - resets on disconnect or patrol completion.
    /// </summary>
    private class PatrolSession
    {
        /// <summary>Holy site UIDs visited in this patrol</summary>
        public HashSet<string> VisitedSiteUIDs { get; set; } = new();

        /// <summary>Player positions when visiting sites, in visit order (for distance calc)</summary>
        public List<BlockPos> VisitPositions { get; set; } = new();

        /// <summary>When the patrol started (elapsed ms)</summary>
        public long PatrolStartTime { get; set; }

        /// <summary>When the player last visited a site (for idle detection)</summary>
        public long LastSiteVisitTime { get; set; }
    }

    #endregion
}
