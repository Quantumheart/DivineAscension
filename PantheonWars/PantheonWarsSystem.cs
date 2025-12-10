using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PantheonWars.Network;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems;
using PantheonWars.Systems.Networking.Server;
using PantheonWars.Systems.Patches;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars;

[ExcludeFromCodeCoverage]
public class PantheonWarsSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "pantheonwars";
    private BlessingEffectSystem? _blessingEffectSystem;
    private BlessingRegistry? _blessingRegistry;

    // Client-side systems
    private ICoreClientAPI? _capi;

    // Use interfaces for better testability and dependency injection
    private CivilizationManager? _civilizationManager;
    private IClientNetworkChannel? _clientChannel;
    private DeityRegistry? _deityRegistry;
    private FavorSystem? _favorSystem;

    private Harmony? _harmony;
    private PlayerReligionDataManager? _playerReligionDataManager;
    private ReligionManager? _religionManager;

    // Server-side systems
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _serverChannel;
    private PlayerDataNetworkHandler? _playerDataNetworkHandler;
    private BlessingNetworkHandler? _blessingNetworkHandler;
    private ReligionNetworkHandler? _religionNetworkHandler;

    public string ModName => "pantheonwars";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification("[PantheonWars] Mod loaded!");

        // Register Harmony Patches
        if (_harmony == null)
        {
            _harmony = new Harmony("com.pantheonwars.patches");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            api.Logger.Notification("[PantheonWars] Harmony patches registered.");
        }

        // Register network channel and message types
        api.Network.RegisterChannel(NETWORK_CHANNEL)
            .RegisterMessageType<PlayerReligionDataPacket>()
            .RegisterMessageType<ReligionListRequestPacket>()
            .RegisterMessageType<ReligionListResponsePacket>()
            .RegisterMessageType<PlayerReligionInfoRequestPacket>()
            .RegisterMessageType<PlayerReligionInfoResponsePacket>()
            .RegisterMessageType<ReligionActionRequestPacket>()
            .RegisterMessageType<ReligionActionResponsePacket>()
            .RegisterMessageType<CreateReligionRequestPacket>()
            .RegisterMessageType<CreateReligionResponsePacket>()
            .RegisterMessageType<EditDescriptionRequestPacket>()
            .RegisterMessageType<EditDescriptionResponsePacket>()
            .RegisterMessageType<BlessingUnlockRequestPacket>()
            .RegisterMessageType<BlessingUnlockResponsePacket>()
            .RegisterMessageType<BlessingDataRequestPacket>()
            .RegisterMessageType<BlessingDataResponsePacket>()
            .RegisterMessageType<ReligionStateChangedPacket>()
            .RegisterMessageType<CivilizationListRequestPacket>()
            .RegisterMessageType<CivilizationListResponsePacket>()
            .RegisterMessageType<CivilizationInfoRequestPacket>()
            .RegisterMessageType<CivilizationInfoResponsePacket>()
            .RegisterMessageType<CivilizationActionRequestPacket>()
            .RegisterMessageType<CivilizationActionResponsePacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;

        // Setup network channel and handlers
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _serverChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerMessageReceived);
        SetupServerNetworking(api);

        // Initialize all server systems using the initializer
        var result = PantheonWarsSystemInitializer.InitializeServerSystems(api, _serverChannel);

        // Store references to managers for disposal and event subscriptions
        _deityRegistry = result.DeityRegistry;
        _religionManager = result.ReligionManager;
        _civilizationManager = result.CivilizationManager;
        _playerReligionDataManager = result.PlayerReligionDataManager;
        _favorSystem = result.FavorSystem;
        _blessingRegistry = result.BlessingRegistry;
        _blessingEffectSystem = result.BlessingEffectSystem;
        _playerDataNetworkHandler = result.PlayerDataNetworkHandler;
        _blessingNetworkHandler = result.BlessingNetworkHandler;
        _religionNetworkHandler = result.ReligionNetworkHandler;

        api.Logger.Notification("[PantheonWars] Server-side initialization complete");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _capi = api;
        api.Logger.Notification("[PantheonWars] Initializing client-side systems...");

        // Setup network handlers
        SetupClientNetworking(api);


        api.Logger.Notification("[PantheonWars] Client-side initialization complete");
    }

    public override void Dispose()
    {
        base.Dispose();

        // Unpatch Harmony
        _harmony?.UnpatchAll("com.pantheonwars.patches");

        // Cleanup network handlers
        _playerDataNetworkHandler?.Dispose();
        _blessingNetworkHandler?.Dispose();
        _religionNetworkHandler?.Dispose();

        // Cleanup systems
        _favorSystem?.Dispose();
        _playerReligionDataManager?.Dispose();
        _religionManager?.Dispose();

        // Clear static events
        PitKilnPatches.ClearSubscribers();
    }

    #region Server Networking

    private void SetupServerNetworking(ICoreServerAPI api)
    {
        // Register handlers for civilization system packets
        _serverChannel!.SetMessageHandler<CivilizationListRequestPacket>(OnCivilizationListRequest);
        _serverChannel.SetMessageHandler<CivilizationInfoRequestPacket>(OnCivilizationInfoRequest);
        _serverChannel.SetMessageHandler<CivilizationActionRequestPacket>(OnCivilizationActionRequest);
    }

    private void OnServerMessageReceived(IServerPlayer fromPlayer, PlayerReligionDataPacket packet)
    {
        // Handle any client-to-server messages here
        // Currently not used, but necessary for channel setup
        // Future implementation: Handle deity selection from client dialog
    }

    /// <summary>
    ///     Handle civilization list request from client
    /// </summary>
    private void OnCivilizationListRequest(IServerPlayer fromPlayer, CivilizationListRequestPacket packet)
    {
        _sapi!.Logger.Debug(
            $"[PantheonWars] Civilization list requested by {fromPlayer.PlayerName}, filter: '{packet.FilterDeity}'");

        var civilizations = _civilizationManager!.GetAllCivilizations().ToList();
        var civInfoList = new List<CivilizationListResponsePacket.CivilizationInfo>();

        foreach (var civ in civilizations)
        {
            var religions = _civilizationManager.GetCivReligions(civ.CivId);
            var deities = religions.Select(r => r.Deity.ToString()).Distinct().ToList();
            var religionNames = religions.Select(r => r.ReligionName).ToList();

            // Apply deity filter if specified
            if (!string.IsNullOrEmpty(packet.FilterDeity))
            {
                // Check if any religion in this civilization has the filtered deity
                var hasFilteredDeity = religions.Any(r =>
                    string.Equals(r.Deity.ToString(), packet.FilterDeity, StringComparison.OrdinalIgnoreCase));

                if (!hasFilteredDeity) continue; // Skip this civilization if it doesn't have the filtered deity
            }

            civInfoList.Add(new CivilizationListResponsePacket.CivilizationInfo
            {
                CivId = civ.CivId,
                Name = civ.Name,
                FounderUID = civ.FounderUID,
                FounderReligionUID = civ.FounderReligionUID,
                MemberCount = civ.MemberReligionIds.Count,
                MemberDeities = deities,
                MemberReligionNames = religionNames
            });
        }

        var response = new CivilizationListResponsePacket(civInfoList);
        _serverChannel!.SendPacket(response, fromPlayer);
        _sapi!.Logger.Debug(
            $"[PantheonWars] Sent {civInfoList.Count} civilizations (out of {civilizations.Count} total) with filter '{packet.FilterDeity}'");
    }

    /// <summary>
    ///     Handle civilization info request from client
    ///     If civId is empty, returns the civilization for the player's current religion
    /// </summary>
    private void OnCivilizationInfoRequest(IServerPlayer fromPlayer, CivilizationInfoRequestPacket packet)
    {
        _sapi!.Logger.Debug(
            $"[PantheonWars] Civilization info requested by {fromPlayer.PlayerName} for {packet.CivId}");

        var civId = packet.CivId;

        // If civId is empty, look up the player's religion's civilization
        if (string.IsNullOrEmpty(civId))
        {
            var playerReligion = _religionManager!.GetPlayerReligion(fromPlayer.PlayerUID);
            if (playerReligion == null)
            {
                _serverChannel!.SendPacket(new CivilizationInfoResponsePacket(), fromPlayer);
                return;
            }

            var religionCiv = _civilizationManager!.GetCivilizationByReligion(playerReligion.ReligionUID);
            if (religionCiv == null)
            {
                // Player's religion is not in any civilization.
                // In this case, still return an invites list so the client can render the "Invites" tab.
                var detailsForInvitesOnly = new CivilizationInfoResponsePacket.CivilizationDetails
                {
                    CivId = string.Empty, // signals "no civilization" to the client
                    Name = string.Empty,
                    FounderUID = string.Empty,
                    FounderReligionUID = string.Empty,
                    CreatedDate = default,
                    MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>(),
                    PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>()
                };

                var invitesForReligion = _civilizationManager.GetInvitesForReligion(playerReligion.ReligionUID);
                foreach (var invite in invitesForReligion)
                {
                    var invitingCiv = _civilizationManager.GetCivilization(invite.CivId);
                    var civName = invitingCiv?.Name ?? invite.CivId;
                    detailsForInvitesOnly.PendingInvites.Add(new CivilizationInfoResponsePacket.PendingInvite
                    {
                        InviteId = invite.InviteId,
                        ReligionId = invite.ReligionId,
                        // For invites targeted at this religion, expose the inviting civilization name
                        ReligionName = civName,
                        ExpiresAt = invite.ExpiresDate
                    });
                }

                _serverChannel!.SendPacket(new CivilizationInfoResponsePacket(detailsForInvitesOnly), fromPlayer);
                return;
            }

            civId = religionCiv.CivId;
        }

        var civ = _civilizationManager!.GetCivilization(civId);
        if (civ == null)
        {
            _serverChannel!.SendPacket(new CivilizationInfoResponsePacket(), fromPlayer);
            return;
        }

        // Get civilization founder's player name
        var founderPlayer = _sapi.World.PlayerByUid(civ.FounderUID);
        var founderPlayerName = founderPlayer?.PlayerName ?? civ.FounderUID;

        // Get founding religion name
        var founderReligion = _religionManager!.GetReligion(civ.FounderReligionUID);
        var founderReligionName = founderReligion?.ReligionName ?? "Unknown";

        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = civ.CivId,
            Name = civ.Name,
            FounderUID = civ.FounderUID,
            FounderName = founderPlayerName,
            FounderReligionUID = civ.FounderReligionUID,
            FounderReligionName = founderReligionName,
            CreatedDate = civ.CreatedDate,
            MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>(),
            PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>()
        };

        // Get member religion details
        var religions = _civilizationManager.GetCivReligions(civ.CivId);
        foreach (var religion in religions)
        {
            // Get religion founder's player name
            var religionFounderPlayer = _sapi.World.PlayerByUid(religion.FounderUID);
            var religionFounderName = religionFounderPlayer?.PlayerName ?? religion.FounderUID;

            details.MemberReligions.Add(new CivilizationInfoResponsePacket.MemberReligion
            {
                ReligionId = religion.ReligionUID,
                ReligionName = religion.ReligionName,
                Deity = religion.Deity.ToString(),
                FounderReligionUID = civ.FounderReligionUID,
                FounderUID = religion.FounderUID,
                FounderName = religionFounderName,
                MemberCount = religion.MemberUIDs.Count
            });
        }

        // Get pending invites (only show to founder)
        if (civ.FounderUID == fromPlayer.PlayerUID)
        {
            var invites = _civilizationManager.GetInvitesForCiv(civ.CivId);
            foreach (var invite in invites)
            {
                var targetReligion = _religionManager!.GetReligion(invite.ReligionId);
                if (targetReligion != null)
                    details.PendingInvites.Add(new CivilizationInfoResponsePacket.PendingInvite
                    {
                        InviteId = invite.InviteId,
                        ReligionId = invite.ReligionId,
                        ReligionName = targetReligion.ReligionName,
                        ExpiresAt = invite.ExpiresDate
                    });
            }
        }

        var response = new CivilizationInfoResponsePacket(details);
        _serverChannel!.SendPacket(response, fromPlayer);
    }

    /// <summary>
    ///     Handle civilization action request from client
    /// </summary>
    private void OnCivilizationActionRequest(IServerPlayer fromPlayer, CivilizationActionRequestPacket packet)
    {
        _sapi!.Logger.Debug(
            $"[PantheonWars] Civilization action '{packet.Action}' requested by {fromPlayer.PlayerName}");

        var response = new CivilizationActionResponsePacket();
        response.Action = packet.Action;

        try
        {
            switch (packet.Action.ToLower())
            {
                case "create":
                    var playerData = _playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                    if (string.IsNullOrEmpty(playerData.ReligionUID))
                    {
                        response.Success = false;
                        response.Message = "You must be in a religion to create a civilization.";
                        break;
                    }

                    var newCiv = _civilizationManager!.CreateCivilization(packet.Name, fromPlayer.PlayerUID,
                        playerData.ReligionUID);
                    if (newCiv != null)
                    {
                        response.Success = true;
                        response.Message = $"Civilization '{newCiv.Name}' created successfully!";
                        response.CivId = newCiv.CivId;
                    }
                    else
                    {
                        response.Success = false;
                        response.Message =
                            "Failed to create civilization. Check name requirements and cooldown status.";
                    }

                    break;

                case "invite":
                    // Look up religion by name (packet.TargetId contains religion name from UI)
                    var targetReligion = _religionManager!.GetReligionByName(packet.TargetId);
                    if (targetReligion == null)
                    {
                        response.Success = false;
                        response.Message = $"Religion '{packet.TargetId}' not found.";
                        break;
                    }

                    var success = _civilizationManager!.InviteReligion(packet.CivId, targetReligion.ReligionUID,
                        fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? $"Invitation sent to '{targetReligion.ReligionName}' successfully!"
                        : "Failed to send invitation. Check permissions and civilization requirements.";

                    // Notify all members of invited religion if invitation succeeded
                    if (success)
                    {
                        var civ = _civilizationManager!.GetCivilization(packet.CivId);
                        if (civ != null)
                        {
                            int notifiedCount = 0;
                            int offlineCount = 0;

                            foreach (var memberUID in targetReligion.MemberUIDs)
                            {
                                var memberPlayer = _sapi!.World.PlayerByUid(memberUID) as IServerPlayer;
                                if (memberPlayer != null)
                                {
                                    var statePacket = new ReligionStateChangedPacket
                                    {
                                        Reason = $"Your religion has been invited to join the civilization '{civ.Name}'",
                                        HasReligion = true
                                    };
                                    _serverChannel!.SendPacket(statePacket, memberPlayer);
                                    notifiedCount++;
                                }
                                else
                                {
                                    offlineCount++;
                                }
                            }

                            _sapi!.Logger.Notification(
                                $"[PantheonWars] Civilization invitation sent to {targetReligion.ReligionName}: " +
                                $"{notifiedCount} members notified, {offlineCount} offline");
                        }
                    }

                    response.CivId = packet.CivId;
                    break;

                case "accept":
                    success = _civilizationManager!.AcceptInvite(packet.TargetId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "You have joined the civilization!"
                        : "Failed to accept invitation. It may have expired or the civilization is full.";
                    break;

                case "leave":
                    playerData = _playerReligionDataManager!.GetOrCreatePlayerData(fromPlayer.PlayerUID);
                    if (string.IsNullOrEmpty(playerData.ReligionUID))
                    {
                        response.Success = false;
                        response.Message = "You are not in a religion.";
                        break;
                    }

                    // Get religion to check if player is the founder
                    var playerReligion = _religionManager!.GetReligion(playerData.ReligionUID);
                    if (playerReligion == null)
                    {
                        response.Success = false;
                        response.Message = "Your religion was not found.";
                        break;
                    }

                    // Check if player is the religion founder (only founders can leave)
                    if (playerReligion.FounderUID != fromPlayer.PlayerUID)
                    {
                        response.Success = false;
                        response.Message = "Only religion founders can leave a civilization.";
                        break;
                    }

                    // Check if player is the civilization founder
                    var playerCiv = _civilizationManager!.GetCivilizationByReligion(playerData.ReligionUID);
                    if (playerCiv != null && playerCiv.FounderUID == fromPlayer.PlayerUID)
                    {
                        response.Success = false;
                        response.Message = "Civilization founders must disband instead of leaving.";
                        break;
                    }

                    success = _civilizationManager!.LeaveReligion(playerData.ReligionUID, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "You have left the civilization. A 7-day cooldown has been applied."
                        : "Failed to leave civilization.";
                    break;

                case "kick":
                    success = _civilizationManager!.KickReligion(packet.CivId, packet.TargetId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "Religion kicked from civilization. A 7-day cooldown has been applied."
                        : "Failed to kick religion. Only the civilization founder can kick members.";
                    response.CivId = packet.CivId;
                    break;

                case "disband":
                    success = _civilizationManager!.DisbandCivilization(packet.CivId, fromPlayer.PlayerUID);
                    response.Success = success;
                    response.Message = success
                        ? "Civilization disbanded successfully."
                        : "Failed to disband civilization. Only the founder can disband.";
                    response.CivId = packet.CivId;
                    break;

                default:
                    response.Success = false;
                    response.Message = $"Unknown action: {packet.Action}";
                    break;
            }
        }
        catch (Exception ex)
        {
            _sapi!.Logger.Error($"[PantheonWars] Error handling civilization action '{packet.Action}': {ex}");
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
        }

        _serverChannel!.SendPacket(response, fromPlayer);
    }


    #endregion

    #region Client Networking

    private void SetupClientNetworking(ICoreClientAPI api)
    {
        _clientChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _clientChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerPlayerDataUpdate);
        _clientChannel.SetMessageHandler<ReligionListResponsePacket>(OnReligionListResponse);
        _clientChannel.SetMessageHandler<PlayerReligionInfoResponsePacket>(OnPlayerReligionInfoResponse);
        _clientChannel.SetMessageHandler<ReligionActionResponsePacket>(OnReligionActionResponse);
        _clientChannel.SetMessageHandler<CreateReligionResponsePacket>(OnCreateReligionResponse);
        _clientChannel.SetMessageHandler<EditDescriptionResponsePacket>(OnEditDescriptionResponse);
        _clientChannel.SetMessageHandler<BlessingUnlockResponsePacket>(OnBlessingUnlockResponse);
        _clientChannel.SetMessageHandler<BlessingDataResponsePacket>(OnBlessingDataResponse);
        _clientChannel.SetMessageHandler<ReligionStateChangedPacket>(OnReligionStateChanged);
        _clientChannel.SetMessageHandler<CivilizationListResponsePacket>(OnCivilizationListResponse);
        _clientChannel.SetMessageHandler<CivilizationInfoResponsePacket>(OnCivilizationInfoResponse);
        _clientChannel.SetMessageHandler<CivilizationActionResponsePacket>(OnCivilizationActionResponse);
        _clientChannel.RegisterMessageType(typeof(PlayerReligionDataPacket));
    }

    private void OnServerPlayerDataUpdate(PlayerReligionDataPacket packet)
    {
        // Trigger event for BlessingDialog and other UI components
        PlayerReligionDataUpdated?.Invoke(packet);
    }

    private void OnReligionListResponse(ReligionListResponsePacket packet)
    {
        ReligionListReceived?.Invoke(packet);
    }

    private void OnPlayerReligionInfoResponse(PlayerReligionInfoResponsePacket packet)
    {
        PlayerReligionInfoReceived?.Invoke(packet);
    }

    private void OnReligionActionResponse(ReligionActionResponsePacket packet)
    {
        ReligionActionCompleted?.Invoke(packet);
    }

    private void OnCreateReligionResponse(CreateReligionResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);

            // Request fresh blessing data (now in a religion)
            // Use a small delay to ensure server has processed the religion creation
            _capi?.Event.RegisterCallback(dt =>
            {
                var request = new BlessingDataRequestPacket();
                _clientChannel?.SendPacket(request);
            }, 100);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _capi?.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.3f);
        }
    }

    private void OnEditDescriptionResponse(EditDescriptionResponsePacket packet)
    {
        if (packet.Success)
            _capi?.ShowChatMessage(packet.Message);
        else
            _capi?.ShowChatMessage($"Error: {packet.Message}");
    }

    private void OnBlessingUnlockResponse(BlessingUnlockResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);
            _capi?.Logger.Notification($"[PantheonWars] Blessing unlocked: {packet.BlessingId}");

            // Trigger blessing unlock event for UI refresh
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");
            _capi?.Logger.Warning($"[PantheonWars] Failed to unlock blessing: {packet.Message}");

            // Trigger event even on failure so UI can update
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
    }

    private void OnBlessingDataResponse(BlessingDataResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Received blessing data: HasReligion={packet.HasReligion}");

        // Trigger event for BlessingDialog to consume
        BlessingDataReceived?.Invoke(packet);
    }

    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi?.Logger.Notification($"[PantheonWars] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi?.ShowChatMessage(packet.Reason);

        // Trigger event for BlessingDialog to refresh its data
        ReligionStateChanged?.Invoke(packet);
    }

    private void OnCivilizationListResponse(CivilizationListResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Received civilization list: {packet.Civilizations.Count} civilizations");
        CivilizationListReceived?.Invoke(packet);
    }

    private void OnCivilizationInfoResponse(CivilizationInfoResponsePacket packet)
    {
        _capi?.Logger.Debug("[PantheonWars] Received civilization info");
        CivilizationInfoReceived?.Invoke(packet);
    }

    private void OnCivilizationActionResponse(CivilizationActionResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Civilization action '{packet.Action}' response: {packet.Success}");

        // Show message to user
        if (!string.IsNullOrEmpty(packet.Message)) _capi?.ShowChatMessage(packet.Message);

        CivilizationActionCompleted?.Invoke(packet);
    }

    /// <summary>
    ///     Request blessing data from the server
    /// </summary>
    public void RequestBlessingData()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request blessing data: client channel not initialized");
            return;
        }

        var request = new BlessingDataRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent blessing data request to server");
    }

    /// <summary>
    ///     Send a blessing unlock request to the server
    /// </summary>
    public void RequestBlessingUnlock(string blessingId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot unlock blessing: client channel not initialized");
            return;
        }

        var request = new BlessingUnlockRequestPacket(blessingId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent unlock request for blessing: {blessingId}");
    }

    /// <summary>
    ///     Request religion list from the server
    /// </summary>
    public void RequestReligionList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion list: client channel not initialized");
            return;
        }

        var request = new ReligionListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent religion list request with filter: {deityFilter}");
    }

    /// <summary>
    ///     Send a religion action request to the server (join, leave, kick, invite)
    /// </summary>
    public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot perform religion action: client channel not initialized");
            return;
        }

        var request = new ReligionActionRequestPacket(action, religionUID, targetPlayerUID);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent religion action request: {action}");
    }

    /// <summary>
    ///     Request to create a new religion
    /// </summary>
    public void RequestCreateReligion(string religionName, string deity, bool isPublic)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot create religion: client channel not initialized");
            return;
        }

        var request = new CreateReligionRequestPacket(religionName, deity, isPublic);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent create religion request: {religionName}, {deity}");
    }

    /// <summary>
    ///     Request player's religion info (for management overlay)
    /// </summary>
    public void RequestPlayerReligionInfo()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion info: client channel not initialized");
            return;
        }

        var request = new PlayerReligionInfoRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent player religion info request");
    }

    /// <summary>
    ///     Request to edit religion description
    /// </summary>
    public void RequestEditDescription(string religionUID, string description)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot edit description: client channel not initialized");
            return;
        }

        var request = new EditDescriptionRequestPacket(religionUID, description);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent edit description request");
    }

    /// <summary>
    ///     Request list of all civilizations
    /// </summary>
    public void RequestCivilizationList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization list: client channel not initialized");
            return;
        }

        var request = new CivilizationListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization list request with filter: '{deityFilter}'");
    }

    /// <summary>
    ///     Request detailed information about a specific civilization
    /// </summary>
    public void RequestCivilizationInfo(string civId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization info: client channel not initialized");
            return;
        }

        var request = new CivilizationInfoRequestPacket(civId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization info request for {civId}");
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization action: client channel not initialized");
            return;
        }

        var request = new CivilizationActionRequestPacket(action, civId, targetId, name);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization action request: {action}");
    }

    /// <summary>
    ///     Event fired when player religion data is updated from the server
    /// </summary>
    public event Action<PlayerReligionDataPacket>? PlayerReligionDataUpdated;

    /// <summary>
    ///     Event fired when blessing data is received from the server
    /// </summary>
    public event Action<BlessingDataResponsePacket>? BlessingDataReceived;

    /// <summary>
    ///     Event fired when a blessing unlock response is received from the server
    ///     Parameters: (blessingId, success)
    /// </summary>
    public event Action<string, bool>? BlessingUnlocked;

    /// <summary>
    ///     Event fired when the player's religion state changes (disbanded, kicked, etc.)
    /// </summary>
    public event Action<ReligionStateChangedPacket>? ReligionStateChanged;

    /// <summary>
    ///     Event fired when religion list is received from server
    /// </summary>
    public event Action<ReligionListResponsePacket>? ReligionListReceived;

    /// <summary>
    ///     Event fired when religion action is completed (join, leave, etc.)
    /// </summary>
    public event Action<ReligionActionResponsePacket>? ReligionActionCompleted;

    /// <summary>
    ///     Event fired when player religion info is received from server
    /// </summary>
    public event Action<PlayerReligionInfoResponsePacket>? PlayerReligionInfoReceived;

    /// <summary>
    ///     Event fired when civilization list is received from server
    /// </summary>
    public event Action<CivilizationListResponsePacket>? CivilizationListReceived;

    /// <summary>
    ///     Event fired when civilization info is received from server
    /// </summary>
    public event Action<CivilizationInfoResponsePacket>? CivilizationInfoReceived;

    /// <summary>
    ///     Event fired when civilization action is completed (create, invite, accept, leave, kick, disband)
    /// </summary>
    public event Action<CivilizationActionResponsePacket>? CivilizationActionCompleted;

    #endregion
}