using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Adapters.Bans;
using DivineAscension.GUI.UI.Adapters.Civilizations;
using DivineAscension.GUI.UI.Adapters.Diplomacy;
using DivineAscension.GUI.UI.Adapters.ReligionInvites;
using DivineAscension.GUI.UI.Adapters.ReligionMembers;
using DivineAscension.GUI.UI.Adapters.Religions;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Client;

namespace DivineAscension.GUI;

/// <summary>
///     Manages state for the Gui Dialog
/// </summary>
public class GuiDialogManager : IBlessingDialogManager
{
    private readonly ICoreClientAPI _capi;
    private readonly INotificationManager _notificationManager;
    private readonly IUiService _uiService;

    public GuiDialogManager(ICoreClientAPI capi, IUiService uiService, ISoundManager soundManager)
    {
        _capi = capi ?? throw new ArgumentNullException(nameof(capi));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        ReligionStateManager = new ReligionStateManager(capi, _uiService, soundManager);
        BlessingStateManager = new BlessingStateManager(capi, _uiService, soundManager);
        CivilizationManager = new CivilizationStateManager(capi, _uiService, soundManager);
        _notificationManager = new NotificationManager(soundManager);
        // Initialize UI-only fake data provider in DEBUG builds. In Release it stays null.
#if DEBUG
        ReligionStateManager.MembersProvider = new FakeReligionMemberProvider();
        ReligionStateManager.MembersProvider.ConfigureDevSeed(500, 20251204);
        var fakeReligionProvider = new FakeReligionProvider();
        ReligionStateManager.UseReligionProvider(fakeReligionProvider);
        ReligionStateManager.ReligionsProvider!.ConfigureDevSeed(500, 20251204);
        ReligionStateManager.UseReligionDetailProvider(new FakeReligionDetailProvider(fakeReligionProvider));
        ReligionStateManager.RefreshReligionsFromProvider();

        // Fake Letters (religion invites) for styling the Letters chapter without a server.
        var fakeInvitesProvider = new FakeReligionInvitesProvider();
        fakeInvitesProvider.ConfigureDevSeed(4, 20260521);
        ReligionStateManager.UseInvitesProvider(fakeInvitesProvider);

        // Fake banned players for styling the Stricken-from-the-Ledger section of the Roster.
        var fakeBanListProvider = new FakeBanListProvider();
        fakeBanListProvider.ConfigureDevSeed(5, 20260526);
        ReligionStateManager.UseBanListProvider(fakeBanListProvider);

        // Initialize civilization fake providers
        var fakeCivProvider = new FakeCivilizationProvider();
        CivilizationManager.UseCivilizationProvider(fakeCivProvider);
        CivilizationManager.CivilizationProvider!.ConfigureDevSeed(25, 20251217);
        CivilizationManager.UseCivilizationDetailProvider(new FakeCivilizationDetailProvider(fakeCivProvider));
        CivilizationManager.RefreshCivilizationsFromProvider();

        // Dev membership: park the player as founder of the first fake realm
        // so HasCivilization() == true and the Accords / Propose chapters can
        // render. Without this the diplomacy pages stop at the no-civilization
        // empty state.
        var devCiv = fakeCivProvider.GetCivilizations().FirstOrDefault();
        if (devCiv != null)
        {
            CivilizationManager.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
            {
                CivId = devCiv.civId,
                Name = devCiv.name,
                FounderUID = devCiv.founderUID,
                FounderReligionUID = devCiv.founderReligionUID,
                Icon = devCiv.icon,
                Description = devCiv.description,
                IsFounder = true,
                Rank = 3,
                MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>(),
                PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>(),
            });
        }

        // Seeded diplomacy so Accords + Propose render without a server.
        var fakeDiplomacyProvider = new FakeDiplomacyProvider();
        fakeDiplomacyProvider.ConfigureDevSeed(20260522);
        CivilizationManager.UseDiplomacyProvider(fakeDiplomacyProvider);
        CivilizationManager.RequestDiplomacyInfo();
#endif
    }


    // Composite UI state
    public CivilizationTabState CivTabState { get; } = new();

    public ReligionStateManager ReligionStateManager { get; }
    public BlessingStateManager BlessingStateManager { get; }

    public CivilizationStateManager CivilizationManager { get; set; }

    public INotificationManager NotificationManager => _notificationManager;


    public List<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions { get; set; } = new();

    public string CurrentPlayerUID => _capi?.World?.Player?.PlayerUID ?? string.Empty;

    /// <summary>
    ///     Whether the current player is the civilization founder.
    ///     This value is computed by the server and cached in the state manager.
    /// </summary>
    public bool IsCivilizationFounder => CivilizationManager.UserIsCivilizationFounder;


    // Data loaded flags
    public bool IsDataLoaded { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, DeityDomain deity, string? religionName, int favorRank = 0,
        int prestigeRank = 0)
    {
        ReligionStateManager.Initialize(religionUID, deity, religionName, favorRank, prestigeRank);
        IsDataLoaded = true;
        BlessingStateManager.State.Reset();

        // Update civilization manager's religion state
        UpdateCivilizationReligionState();
    }

    /// <summary>
    ///     Reset all state
    /// </summary>
    public void Reset()
    {
        ReligionStateManager.Reset();
        BlessingStateManager.State.Reset();
        CivTabState.Reset();
        // Keep blessing UI state reset here (for backward compatibility)
        IsDataLoaded = false;
    }

    /// <summary>
    ///     Check if player has a religion
    /// </summary>
    public bool HasReligion() => ReligionStateManager.HasReligion();

    /// <summary>
    ///     Check if player's religion is in a civilization
    /// </summary>
    public bool HasCivilization()
    {
        return CivilizationManager.HasCivilization();
    }

    /// <summary>
    ///     Update civilization manager's religion state from religion manager
    /// </summary>
    public void UpdateCivilizationReligionState()
    {
        CivilizationManager.UserHasReligion = HasReligion();
        CivilizationManager.UserIsReligionFounder =
            ReligionStateManager.State.InfoState.MyReligionInfo?.IsFounder ?? false;
        CivilizationManager.UserPrestigeRank = ReligionStateManager.CurrentPrestigeRank;
        CivilizationManager.UserIsCivilizationFounder = IsCivilizationFounder;
    }
}