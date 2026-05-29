using System;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems;

/// <summary>
///     Server-authoritative grant/revoke of Vintage Story character-trait codes (#559).
///     Wraps <c>EntityPlayer.WatchedAttributes["extraTraits"]</c> + <see cref="CharacterSystem"/>
///     re-apply so DA can express stat payloads as vanilla traits instead of hand-rolled
///     <c>entity.Stats.Set</c> calls.
/// </summary>
public class PlayerTraitService : IDisposable
{
    private const string ExtraTraitsAttr = "extraTraits";
    private const string CharacterClassAttr = "characterClass";

    private readonly IEventService _eventService;
    private readonly ILoggerWrapper _logger;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly ICoreServerAPI? _sapi;
    private readonly IWorldService _worldService;

    private CharacterSystem? _characterSystem;
    private bool _characterSystemResolveAttempted;

    public PlayerTraitService(
        ILoggerWrapper logger,
        IEventService eventService,
        IWorldService worldService,
        IPlayerProgressionDataManager playerProgressionDataManager,
        ICoreServerAPI? sapi)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _playerProgressionDataManager = playerProgressionDataManager
                                        ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _sapi = sapi;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing PlayerTraitService...");
        _eventService.OnPlayerJoin(OnPlayerJoin);
        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);
        _logger.Notification("[DivineAscension] PlayerTraitService initialized");
    }

    public void Dispose()
    {
        _eventService.UnsubscribePlayerJoin(OnPlayerJoin);
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
    }

    /// <summary>
    ///     Grants a trait code to the player. Idempotent. Returns true if the code
    ///     was newly granted (false if already present).
    /// </summary>
    public bool GrantTrait(IServerPlayer player, string code)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        return GrantTraitInternal(player.PlayerUID, player, code);
    }

    /// <summary>
    ///     Grants a trait code by player UID. Used for offline-safe paths (cascade
    ///     revoke on religion disband etc.). When the player is online, stats are
    ///     re-applied live; when offline, the next <c>PlayerJoin</c> picks it up.
    /// </summary>
    public bool GrantTrait(string playerUID, string code)
    {
        if (string.IsNullOrWhiteSpace(playerUID)) return false;
        var player = _worldService.GetPlayerByUID(playerUID);
        return GrantTraitInternal(playerUID, player, code);
    }

    /// <summary>
    ///     Revokes a trait code from the player. Returns true if the code was present.
    /// </summary>
    public bool RevokeTrait(IServerPlayer player, string code)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        return RevokeTraitInternal(player.PlayerUID, player, code);
    }

    /// <summary>
    ///     Revokes a trait code by player UID. Offline-safe; see <see cref="GrantTrait(string, string)"/>.
    /// </summary>
    public bool RevokeTrait(string playerUID, string code)
    {
        if (string.IsNullOrWhiteSpace(playerUID)) return false;
        var player = _worldService.GetPlayerByUID(playerUID);
        return RevokeTraitInternal(playerUID, player, code);
    }

    private bool GrantTraitInternal(string playerUID, IServerPlayer? player, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var data = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);
        var added = data.AddGrantedTraitCode(code);

        if (player != null)
        {
            SyncExtraTraitsAttribute(player, data);
            ReapplyCharacterClass(player);
        }

        if (added)
            _logger.Notification($"[DivineAscension] Granted trait '{code}' to {playerUID}");
        return added;
    }

    private bool RevokeTraitInternal(string playerUID, IServerPlayer? player, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var data = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);
        var removed = data.RemoveGrantedTraitCode(code);

        if (player != null)
        {
            SyncExtraTraitsAttribute(player, data);
            ReapplyCharacterClass(player);
        }

        if (removed)
            _logger.Notification($"[DivineAscension] Revoked trait '{code}' from {playerUID}");
        return removed;
    }

    /// <summary>
    ///     Whether the given trait code has been granted to the player by Divine Ascension.
    /// </summary>
    public bool HasGrantedTrait(string playerUID, string code)
    {
        if (string.IsNullOrWhiteSpace(playerUID) || string.IsNullOrWhiteSpace(code))
            return false;
        var data = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);
        return data.HasGrantedTraitCode(code);
    }

    /// <summary>
    ///     Re-applies the granted trait codes for an already-online player. Used by the
    ///     SaveGameLoaded hook so reload-while-online doesn't drop stats.
    /// </summary>
    public void ReapplyForPlayer(IServerPlayer player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var data = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        SyncExtraTraitsAttribute(player, data);
        ReapplyCharacterClass(player);
    }

    private void OnPlayerJoin(IServerPlayer player)
    {
        // Defer one tick so vanilla CharacterSystem.Event_PlayerJoinServer (which
        // sets the default class and applies vanilla trait stats) has already run.
        if (_sapi != null)
            _sapi.Event.EnqueueMainThreadTask(() => ReapplyForPlayer(player), "da-trait-reapply");
        else
            ReapplyForPlayer(player);
    }

    private void OnSaveGameLoaded()
    {
        foreach (var p in _worldService.GetAllOnlinePlayers())
            ReapplyForPlayer(p);
    }

    private void SyncExtraTraitsAttribute(IServerPlayer player, PlayerProgressionData data)
    {
        var entity = player.Entity;
        if (entity == null) return;

        var attr = entity.WatchedAttributes;
        var existing = attr.GetStringArray(ExtraTraitsAttr) ?? Array.Empty<string>();

        // DA owns the "da_" prefix. Strip every existing DA code (we may be revoking one)
        // and re-issue exactly the current granted set. Foreign codes are left untouched.
        var desired = existing
            .Where(c => !string.IsNullOrEmpty(c) && !c.StartsWith("da_", StringComparison.Ordinal))
            .Concat(data.GrantedTraitCodes)
            .Distinct()
            .ToArray();

        attr.SetStringArray(ExtraTraitsAttr, desired);
    }

    private void ReapplyCharacterClass(IServerPlayer player)
    {
        var charSys = ResolveCharacterSystem();
        if (charSys == null) return;

        var entity = player.Entity;
        if (entity == null) return;

        var classCode = entity.WatchedAttributes.GetString(CharacterClassAttr);
        if (string.IsNullOrEmpty(classCode)) return;

        try
        {
            charSys.setCharacterClass(entity, classCode, initializeGear: false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to re-apply character class for {player.PlayerUID}: {ex.Message}");
        }
    }

    private CharacterSystem? ResolveCharacterSystem()
    {
        if (_characterSystem != null) return _characterSystem;
        if (_characterSystemResolveAttempted) return null;
        if (_sapi == null) return null;

        _characterSystemResolveAttempted = true;
        _characterSystem = _sapi.ModLoader.GetModSystem<CharacterSystem>();
        if (_characterSystem == null)
            _logger.Warning("[DivineAscension] CharacterSystem not loaded; trait stats won't apply");
        return _characterSystem;
    }
}
