using System;
using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Adapters.Civilizations;
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
        ReligionStateManager.UseReligionProvider(new FakeReligionProvider());
        ReligionStateManager.ReligionsProvider!.ConfigureDevSeed(500, 20251204);
        ReligionStateManager.RefreshReligionsFromProvider();

        // Initialize civilization fake provider
        CivilizationManager.UseCivilizationProvider(new FakeCivilizationProvider());
        CivilizationManager.CivilizationProvider!.ConfigureDevSeed(25, 20251217);
        CivilizationManager.RefreshCivilizationsFromProvider();
#endif
    }


    // Composite UI state
    public CivilizationTabState CivTabState { get; } = new();

    public ReligionStateManager ReligionStateManager { get; }
    public BlessingStateManager BlessingStateManager { get; }

    public CivilizationStateManager CivilizationManager { get; set; }

    public INotificationManager NotificationManager => _notificationManager;


    public List<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions { get; set; } = new();

    public bool IsCivilizationFounder => !string.IsNullOrEmpty(ReligionStateManager.CurrentReligionUID) &&
                                         !string.IsNullOrEmpty(CivilizationManager.CivilizationFounderReligionUID) &&
                                         ReligionStateManager.CurrentReligionUID ==
                                         CivilizationManager.CivilizationFounderReligionUID;


    // Data loaded flags
    public bool IsDataLoaded { get; set; }

    /// <summary>
    ///     Initialize dialog state from player's current religion data
    /// </summary>
    public void Initialize(string? religionUID, DeityType deity, string? religionName, int favorRank = 0,
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
    }
}