using System;
using System.Collections.Generic;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Managers;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Adapters.ReligionMembers;
using PantheonWars.GUI.UI.Adapters.Religions;
using PantheonWars.Models.Enum;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Manages state for the Gui Dialog
/// </summary>
public class GuiDialogManager : IBlessingDialogManager
{
    private readonly ICoreClientAPI _capi;
    private readonly IUiService _uiService;

    public GuiDialogManager(ICoreClientAPI capi, IUiService uiService)
    {
        _capi = capi ?? throw new ArgumentNullException(nameof(capi));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        ReligionStateManager = new ReligionStateManager(capi, _uiService);
        BlessingStateManager = new BlessingStateManager(capi, _uiService);
        CivilizationManager = new CivilizationStateManager(capi, _uiService);
        // Initialize UI-only fake data provider in DEBUG builds. In Release it stays null.
#if DEBUG
        ReligionStateManager.MembersProvider = new FakeReligionMemberProvider();
        ReligionStateManager.MembersProvider.ConfigureDevSeed(500, 20251204);
        ReligionStateManager.UseReligionProvider(new FakeReligionProvider());
        ReligionStateManager.ReligionsProvider!.ConfigureDevSeed(500, 20251204);
        ReligionStateManager.RefreshReligionsFromProvider();
#endif
    }


    // Composite UI state
    public CivilizationTabState CivTabState { get; } = new();

    public ReligionStateManager ReligionStateManager { get; }
    public BlessingStateManager BlessingStateManager { get; }

    public CivilizationStateManager CivilizationManager { get; set; }


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
}