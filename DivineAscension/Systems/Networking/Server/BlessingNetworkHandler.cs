using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles blessing-related network requests from clients.
///     Manages blessing unlocks (both player and religion blessings) and blessing data requests.
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingNetworkHandler : IServerNetworkHandler
{
    private static readonly DeityDomain[] AllDeities =
    {
        DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest,
        DeityDomain.Harvest, DeityDomain.Stone
    };

    private readonly ILogger _logger;
    private readonly BlessingRegistry _blessingRegistry;
    private readonly BlessingEffectSystem _blessingEffectSystem;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly INetworkService _networkService;
    private readonly IPlayerMessengerService _messengerService;
    private readonly IWorldService _worldService;
    private readonly IBlessingUnlearnService _unlearnService;
    private readonly IReligionBlessingUnlearnService _religionUnlearnService;
    private readonly Configuration.GameBalanceConfig _gameBalanceConfig;
    private readonly IFreeRespecWindow _freeRespecWindow;

    public BlessingNetworkHandler(
        ILogger logger,
        BlessingRegistry blessingRegistry,
        BlessingEffectSystem blessingEffectSystem,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        INetworkService networkService,
        IPlayerMessengerService messengerService,
        IWorldService worldService,
        IBlessingUnlearnService unlearnService,
        IReligionBlessingUnlearnService religionUnlearnService,
        Configuration.GameBalanceConfig gameBalanceConfig,
        IFreeRespecWindow freeRespecWindow)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));
        _blessingEffectSystem = blessingEffectSystem ?? throw new ArgumentNullException(nameof(blessingEffectSystem));
        _playerProgressionDataManager = playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _unlearnService = unlearnService ?? throw new ArgumentNullException(nameof(unlearnService));
        _religionUnlearnService = religionUnlearnService ?? throw new ArgumentNullException(nameof(religionUnlearnService));
        _gameBalanceConfig = gameBalanceConfig ?? throw new ArgumentNullException(nameof(gameBalanceConfig));
        _freeRespecWindow = freeRespecWindow ?? throw new ArgumentNullException(nameof(freeRespecWindow));
    }

    public void RegisterHandlers()
    {
        // Register handlers for blessing system packets
        _networkService.RegisterMessageHandler<BlessingUnlockRequestPacket>(OnBlessingUnlockRequest);
        _networkService.RegisterMessageHandler<UnlearnBlessingRequestPacket>(OnUnlearnBlessingRequest);
        _networkService.RegisterMessageHandler<UnlearnReligionBlessingRequestPacket>(OnUnlearnReligionBlessingRequest);
        _networkService.RegisterMessageHandler<BlessingDataRequestPacket>(OnBlessingDataRequest);

        // Push fresh blessing data to everyone when the free-respec window opens/closes so the
        // banner and refund preview update live without a manual dialog reopen (#462).
        _freeRespecWindow.Changed += OnFreeRespecWindowChanged;
    }

    public void Dispose()
    {
        _freeRespecWindow.Changed -= OnFreeRespecWindowChanged;
    }

    private void OnFreeRespecWindowChanged()
    {
        foreach (var player in _worldService.GetAllOnlinePlayers())
            SendBlessingData(player);
    }

    private void OnBlessingUnlockRequest(IServerPlayer fromPlayer, BlessingUnlockRequestPacket packet)
    {
        string message;
        var success = false;

        try
        {
            var blessing = _blessingRegistry.GetBlessing(packet.BlessingId);
            if (blessing == null)
            {
                message = LocalizationService.Instance.Get(LocalizationKeys.NET_BLESSING_NOT_FOUND, packet.BlessingId);
            }
            else
            {
                var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
                var playerFavorRank = _playerProgressionDataManager.GetPlayerFavorRank(fromPlayer.PlayerUID, blessing.Domain);
                var slotCapFavorRank = _playerProgressionDataManager.GetPlayerPatronFavorRank(fromPlayer.PlayerUID);

                // Skip cost check here - we'll handle it atomically below
                var (canUnlock, reason) = _blessingRegistry.CanUnlockBlessing(fromPlayer.PlayerUID, playerFavorRank, slotCapFavorRank, playerData, religion, blessing, skipCostCheck: true);
                if (!canUnlock)
                {
                    message = reason;
                }
                else
                {
                    // Unlock the blessing
                    if (blessing.Kind == BlessingKind.Player)
                    {
                        if (religion == null)
                        {
                            message = LocalizationService.Instance.Get(LocalizationKeys
                                .NET_BLESSING_MUST_BE_IN_RELIGION_PLAYER);
                        }
                        else
                        {
                            // Atomically deduct favor cost (includes sufficiency check).
                            // Non-patron blessings cost 1.5x; capstones are patron-only and always 1.0x.
                            var adjustedCost = BlessingRegistry.AdjustedCost(blessing, religion);
                            if (adjustedCost > 0 && !playerData.RemoveFavor(blessing.Domain, adjustedCost))
                            {
                                message = LocalizationService.Instance.Get(
                                    LocalizationKeys.CMD_BLESSING_ERROR_INSUFFICIENT_FAVOR,
                                    adjustedCost, playerData.GetFavor(blessing.Domain));
                            }
                            else
                            {
                                success = _playerProgressionDataManager.UnlockPlayerBlessing(fromPlayer.PlayerUID,
                                    packet.BlessingId);
                                if (success)
                                {
                                    // Commit to branch if this blessing has one (locks exclusive branches)
                                    if (!string.IsNullOrEmpty(blessing.Branch))
                                    {
                                        playerData.CommitToBranch(blessing.Domain, blessing.Branch, blessing.ExclusiveBranches);
                                    }

                                    _blessingEffectSystem.RefreshPlayerBlessings(fromPlayer.PlayerUID);
                                    message = LocalizationService.Instance.Get(
                                        LocalizationKeys.NET_BLESSING_SUCCESS_UNLOCKED, blessing.Name);

                                    // Notify player data changed (triggers event that sends HUD update to client)
                                    _playerProgressionDataManager.NotifyPlayerDataChanged(fromPlayer.PlayerUID);
                                }
                                else
                                {
                                    message = LocalizationService.Instance.Get(LocalizationKeys
                                        .NET_BLESSING_FAILED_TO_UNLOCK);
                                }
                            }
                        }
                    }
                    else // Religion blessing
                    {
                        if (religion == null)
                        {
                            message = LocalizationService.Instance.Get(LocalizationKeys
                                .NET_BLESSING_MUST_BE_IN_RELIGION_RELIGION);
                        }
                        else if (!religion.IsFounder(fromPlayer.PlayerUID))
                        {
                            message = LocalizationService.Instance.Get(LocalizationKeys
                                .NET_BLESSING_ONLY_FOUNDER_CAN_UNLOCK);
                        }
                        else
                        {
                            // Atomically deduct prestige cost (includes sufficiency check).
                            // Non-patron religion blessings cost 1.5x; capstones are patron-only and always 1.0x.
                            var adjustedCost = BlessingRegistry.AdjustedCost(blessing, religion);
                            if (adjustedCost > 0 && !religion.RemovePrestige(adjustedCost))
                            {
                                message = LocalizationService.Instance.Get(
                                    LocalizationKeys.CMD_BLESSING_ERROR_INSUFFICIENT_PRESTIGE,
                                    adjustedCost, religion.Prestige);
                            }
                            else
                            {
                                religion.UnlockBlessing(packet.BlessingId);
                                _blessingEffectSystem.RefreshReligionBlessings(religion.ReligionUID);
                                message = LocalizationService.Instance.Get(
                                    LocalizationKeys.NET_BLESSING_SUCCESS_UNLOCKED_FOR_RELIGION, blessing.Name);
                                success = true;

                            // Notify all members
                            foreach (var memberUid in religion.MemberUIDs)
                            {
                                // Notify player data changed (triggers event that sends HUD update to client)
                                _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                                var member = _worldService.GetPlayerByUID(memberUid) as IServerPlayer;
                                if (member != null)
                                    _messengerService.SendMessage(
                                        member,
                                        LocalizationService.Instance.Get(
                                            LocalizationKeys.NET_BLESSING_UNLOCKED_NOTIFICATION, blessing.Name),
                                        EnumChatType.Notification
                                    );
                            }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.NET_BLESSING_ERROR_UNLOCKING, ex.Message);
            _logger.Error($"[DivineAscension] Blessing unlock error: {ex}");
        }

        var response = new BlessingUnlockResponsePacket(success, message, packet.BlessingId);
        _networkService.SendToPlayer(fromPlayer, response);
    }

    /// <summary>
    ///     Handle an unlearn request: removes an owned personal blessing and its prerequisite
    ///     cascade, refunding 50% of each blessing's favor cost to spendable favor (epic #425 —
    ///     #459, #460). Server-authoritative — all eligibility is re-checked here regardless of
    ///     client state.
    /// </summary>
    private void OnUnlearnBlessingRequest(IServerPlayer fromPlayer, UnlearnBlessingRequestPacket packet)
    {
        string message;
        var success = false;
        var refundedFavor = 0;
        var struckIds = new List<string>();

        try
        {
            var blessing = _blessingRegistry.GetBlessing(packet.BlessingId);
            var result = _unlearnService.UnlearnBlessing(fromPlayer.PlayerUID, packet.BlessingId);
            var blessingName = blessing?.Name ?? packet.BlessingId;

            switch (result.Outcome)
            {
                case UnlearnOutcome.Success:
                    success = true;
                    refundedFavor = result.RefundedFavor;
                    if (result.StruckBlessingIds != null)
                        struckIds.AddRange(result.StruckBlessingIds);
                    // StruckCount includes the target; >1 means dependent children cascaded too.
                    message = result.StruckCount > 1
                        ? LocalizationService.Instance.Get(
                            LocalizationKeys.NET_BLESSING_UNLEARN_CASCADE_SUCCESS,
                            blessingName, result.StruckCount - 1, result.RefundedFavor)
                        : LocalizationService.Instance.Get(
                            LocalizationKeys.NET_BLESSING_UNLEARN_SUCCESS, blessingName, result.RefundedFavor);
                    break;
                case UnlearnOutcome.NotOwned:
                    message = LocalizationService.Instance.Get(
                        LocalizationKeys.NET_BLESSING_UNLEARN_NOT_OWNED, blessingName);
                    break;
                case UnlearnOutcome.NotPlayerBlessing:
                    message = LocalizationService.Instance.Get(
                        LocalizationKeys.NET_BLESSING_UNLEARN_NOT_PERSONAL);
                    break;
                default: // BlessingNotFound
                    message = LocalizationService.Instance.Get(
                        LocalizationKeys.NET_BLESSING_NOT_FOUND, packet.BlessingId);
                    break;
            }
        }
        catch (Exception ex)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.NET_BLESSING_ERROR_UNLEARNING, ex.Message);
            _logger.Error($"[DivineAscension] Blessing unlearn error: {ex}");
        }

        var response = new UnlearnBlessingResponsePacket(success, message, packet.BlessingId, refundedFavor, struckIds);
        _networkService.SendToPlayer(fromPlayer, response);
    }

    /// <summary>
    ///     Handle a religion-blessing strike request: founder-only, removes an inscribed religion
    ///     blessing and its prerequisite cascade, refunding a fraction of each blessing's prestige
    ///     cost to the religion's spendable prestige (epic #479, slice 5 — #484). Server-authoritative:
    ///     founder status and ownership are re-checked here regardless of client state. On success
    ///     every member is notified and their blessing data refreshed so the inscribe counter updates.
    /// </summary>
    private void OnUnlearnReligionBlessingRequest(IServerPlayer fromPlayer, UnlearnReligionBlessingRequestPacket packet)
    {
        string message;
        var success = false;
        var refundedPrestige = 0;
        var struckIds = new List<string>();

        try
        {
            var blessing = _blessingRegistry.GetBlessing(packet.BlessingId);
            var blessingName = blessing?.Name ?? packet.BlessingId;
            var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);

            if (religion == null)
            {
                message = LocalizationService.Instance.Get(
                    LocalizationKeys.NET_BLESSING_MUST_BE_IN_RELIGION_RELIGION);
            }
            else if (!religion.IsFounder(fromPlayer.PlayerUID))
            {
                message = LocalizationService.Instance.Get(
                    LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_NOT_FOUNDER);
            }
            else
            {
                var result = _religionUnlearnService.UnlearnReligionBlessing(religion.ReligionUID, packet.BlessingId);
                switch (result.Outcome)
                {
                    case ReligionUnlearnOutcome.Success:
                        success = true;
                        refundedPrestige = result.RefundedPrestige;
                        if (result.StruckBlessingIds != null)
                            struckIds.AddRange(result.StruckBlessingIds);
                        // StruckCount includes the target; >1 means dependent children cascaded too.
                        message = result.StruckCount > 1
                            ? LocalizationService.Instance.Get(
                                LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_CASCADE_SUCCESS,
                                blessingName, result.StruckCount - 1, result.RefundedPrestige)
                            : LocalizationService.Instance.Get(
                                LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_SUCCESS,
                                blessingName, result.RefundedPrestige);

                        // Notify every member and refresh their blessing data so the inscribe
                        // counter and tree reflect the struck cascade live.
                        foreach (var memberUid in religion.MemberUIDs)
                        {
                            _playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);

                            if (_worldService.GetPlayerByUID(memberUid) is IServerPlayer member)
                            {
                                SendBlessingData(member);
                                _messengerService.SendMessage(
                                    member,
                                    LocalizationService.Instance.Get(
                                        LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_NOTIFICATION, blessingName),
                                    EnumChatType.Notification);
                            }
                        }
                        break;
                    case ReligionUnlearnOutcome.NotOwned:
                        message = LocalizationService.Instance.Get(
                            LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_NOT_OWNED, blessingName);
                        break;
                    case ReligionUnlearnOutcome.NotReligionBlessing:
                        message = LocalizationService.Instance.Get(
                            LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_NOT_RELIGION);
                        break;
                    case ReligionUnlearnOutcome.ReligionNotFound:
                        message = LocalizationService.Instance.Get(
                            LocalizationKeys.NET_BLESSING_MUST_BE_IN_RELIGION_RELIGION);
                        break;
                    default: // BlessingNotFound
                        message = LocalizationService.Instance.Get(
                            LocalizationKeys.NET_BLESSING_NOT_FOUND, packet.BlessingId);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_BLESSING_STRIKE_ERROR, ex.Message);
            _logger.Error($"[DivineAscension] Religion blessing strike error: {ex}");
        }

        var response = new UnlearnReligionBlessingResponsePacket(
            success, message, packet.BlessingId, refundedPrestige, struckIds);
        _networkService.SendToPlayer(fromPlayer, response);
    }

    /// <summary>
    ///     Handle blessing data request from client
    /// </summary>
    private void OnBlessingDataRequest(IServerPlayer fromPlayer, BlessingDataRequestPacket packet)
    {
        _logger.Debug($"[DivineAscension] Blessing data requested by {fromPlayer.PlayerName}");
        SendBlessingData(fromPlayer);
    }

    /// <summary>
    ///     Builds and sends the full blessing-data snapshot to one player. Shared by the
    ///     client-request handler and the free-respec window push so both produce identical state.
    /// </summary>
    private void SendBlessingData(IServerPlayer fromPlayer)
    {
        var response = new BlessingDataResponsePacket();

        try
        {
            var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(fromPlayer.PlayerUID);

            var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
            if (religion == null)
            {
                response.HasReligion = false;
                _networkService.SendToPlayer(fromPlayer, response);
                return;
            }

            var patron = religion.PatronDomain;

            response.HasReligion = true;
            response.ReligionUID = religion.ReligionUID;
            response.ReligionName = religion.ReligionName;
            response.PatronDomain = patron;
            response.PatronName = religion.PatronName;
            response.PrestigeRank = (int)religion.PrestigeRank;
            response.CurrentPrestige = religion.Prestige;
            // While the free-respec window is open, refunds are 100% and the banner shows (#462).
            response.FreeRespecActive = _freeRespecWindow.IsActive;
            response.UnlearnRefundPercent =
                _freeRespecWindow.IsActive ? 1f : _gameBalanceConfig.UnlearnRefundPercent;

            foreach (var domain in AllDeities)
            {
                response.FavorByDeity[domain] = playerData.GetFavor(domain);
                response.FavorRanksByDeity[domain] =
                    (int)_playerProgressionDataManager.GetPlayerFavorRank(fromPlayer.PlayerUID, domain);
                response.TotalFavorEarnedByDeity[domain] = playerData.GetTotalFavorEarned(domain);
            }

            BlessingDataResponsePacket.BlessingInfo ToInfo(Models.Blessing p) =>
                new()
                {
                    BlessingId = p.BlessingId,
                    Name = p.Name,
                    Description = p.Description,
                    RequiredFavorRank = p.RequiredFavorRank,
                    RequiredPrestigeRank = p.RequiredPrestigeRank,
                    PrerequisiteBlessings = p.PrerequisiteBlessings ?? new List<string>(),
                    Category = (int)p.Category,
                    StatModifiers = p.StatModifiers ?? new Dictionary<string, float>(),
                    IconName = p.IconName,
                    Cost = p.Cost,
                    Branch = p.Branch,
                    ExclusiveBranches = p.ExclusiveBranches,
                    Domain = p.Domain,
                    RequiresPatron = p.RequiresPatron
                };

            foreach (var domain in AllDeities)
            {
                response.PlayerBlessings.AddRange(
                    _blessingRegistry.GetBlessingsForDeity(domain, BlessingKind.Player).Select(ToInfo));
                response.ReligionBlessings.AddRange(
                    _blessingRegistry.GetBlessingsForDeity(domain, BlessingKind.Religion).Select(ToInfo));

                var committedBranch = playerData.GetCommittedBranch(domain);
                if (committedBranch != null)
                    response.CommittedBranches[(int)domain] = committedBranch;
                var lockedBranches = playerData.GetLockedBranches(domain);
                if (lockedBranches.Count > 0)
                    response.LockedBranches[(int)domain] = lockedBranches.ToList();
            }

            // Get unlocked player blessings
            response.UnlockedPlayerBlessings = playerData.UnlockedBlessings
                .ToList();

            // Get unlocked religion blessings
            response.UnlockedReligionBlessings = religion.UnlockedBlessings
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            _logger.Debug(
                $"[DivineAscension] Sending blessing data: {response.PlayerBlessings.Count} player, {response.ReligionBlessings.Count} religion");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error loading blessing data: {ex}");
            response.HasReligion = false;
        }

        _networkService.SendToPlayer(fromPlayer, response);
    }
}