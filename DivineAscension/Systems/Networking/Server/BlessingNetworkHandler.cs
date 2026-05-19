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

    public BlessingNetworkHandler(
        ILogger logger,
        BlessingRegistry blessingRegistry,
        BlessingEffectSystem blessingEffectSystem,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        INetworkService networkService,
        IPlayerMessengerService messengerService,
        IWorldService worldService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));
        _blessingEffectSystem = blessingEffectSystem ?? throw new ArgumentNullException(nameof(blessingEffectSystem));
        _playerProgressionDataManager = playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    public void RegisterHandlers()
    {
        // Register handlers for blessing system packets
        _networkService.RegisterMessageHandler<BlessingUnlockRequestPacket>(OnBlessingUnlockRequest);
        _networkService.RegisterMessageHandler<BlessingDataRequestPacket>(OnBlessingDataRequest);
    }

    public void Dispose()
    {
        // No resources to dispose
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

                // Skip cost check here - we'll handle it atomically below
                var (canUnlock, reason) = _blessingRegistry.CanUnlockBlessing(fromPlayer.PlayerUID, playerFavorRank, playerData, religion, blessing, skipCostCheck: true);
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
    ///     Handle blessing data request from client
    /// </summary>
    private void OnBlessingDataRequest(IServerPlayer fromPlayer, BlessingDataRequestPacket packet)
    {
        _logger.Debug($"[DivineAscension] Blessing data requested by {fromPlayer.PlayerName}");

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