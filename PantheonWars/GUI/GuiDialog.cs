using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Managers;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSImGui;
using VSImGui.API;

namespace PantheonWars.GUI;

/// <summary>
///     Main ImGui-based Blessing Dialog for viewing and unlocking blessings
/// </summary>
[ExcludeFromCodeCoverage]
public partial class GuiDialog : ModSystem
{
    private const int CheckDataInterval = 1000; // Check for data every 1 second
    private const int WindowBaseWidth = 1400;
    private const int WindowBaseHeight = 900;

    // State
    private readonly GuiDialogState _state = new();

    private ICoreClientAPI? _capi;
    private long _checkDataId;
    private ImGuiModSystem? _imguiModSystem;

    private GuiDialogManager? _manager;
    private PantheonWarsSystem? _pantheonWarsSystem;
    private ISoundManager? _soundManager;
    private Stopwatch? _stopwatch;
    private ImGuiViewportPtr _viewport;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override double ExecuteOrder()
    {
        return 1.5; // Load after main PantheonWarsSystem (1.0)
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _capi = api;
        _viewport = ImGui.GetMainViewport();
        _stopwatch = Stopwatch.StartNew();

        // Register keybind (P key to open)
        _capi.Input.RegisterHotKey("pantheonwarsblessings", "Show/Hide Blessing Dialog", GlKeys.G,
            HotkeyType.GUIOrOtherControls, shiftPressed: true);
        _capi.Input.SetHotKeyHandler("pantheonwarsblessings", OnToggleDialog);

        // Initialize deity icon loader
        DeityIconLoader.Initialize(_capi);

        // Get PantheonWarsSystem for network communication
        _pantheonWarsSystem = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        _soundManager = new SoundManager(_capi);
        _manager = new GuiDialogManager(_capi, _pantheonWarsSystem!.UiService, _soundManager);
        if (_pantheonWarsSystem?.NetworkClient != null)
        {
            _pantheonWarsSystem.NetworkClient.BlessingUnlocked += OnBlessingUnlockedFromServer;
            _pantheonWarsSystem.NetworkClient.BlessingDataReceived += OnBlessingDataReceived;
            _pantheonWarsSystem.NetworkClient.ReligionStateChanged += OnReligionStateChanged;
            _pantheonWarsSystem.NetworkClient.ReligionListReceived += OnReligionListReceived;
            _pantheonWarsSystem.NetworkClient.ReligionActionCompleted += OnReligionActionCompleted;
            _pantheonWarsSystem.NetworkClient.ReligionRolesReceived += OnReligionRolesReceived;
            _pantheonWarsSystem.NetworkClient.RoleCreated += OnRoleCreated;
            _pantheonWarsSystem.NetworkClient.RolePermissionsModified += OnRolePermissionsModified;
            _pantheonWarsSystem.NetworkClient.RoleAssigned += OnRoleAssigned;
            _pantheonWarsSystem.NetworkClient.RoleDeleted += OnRoleDeleted;
            _pantheonWarsSystem.NetworkClient.FounderTransferred += OnFounderTransferred;
            _pantheonWarsSystem.NetworkClient.PlayerReligionInfoReceived += OnPlayerReligionInfoReceived;
            _pantheonWarsSystem.NetworkClient.PlayerReligionDataUpdated += OnPlayerReligionDataUpdated;
            _pantheonWarsSystem.NetworkClient.CivilizationListReceived += OnCivilizationListReceived;
            _pantheonWarsSystem.NetworkClient.CivilizationInfoReceived += OnCivilizationInfoReceived;
            _pantheonWarsSystem.NetworkClient.CivilizationActionCompleted += OnCivilizationActionCompleted;
        }
        else
        {
            _capi.Logger.Error(
                "[PantheonWars] PantheonWarsSystem or NetworkClient not found! Blessing unlocking will not work.");
        }

        // Get ImGui mod system
        _imguiModSystem = _capi.ModLoader.GetModSystem<ImGuiModSystem>();
        if (_imguiModSystem != null)
        {
            _imguiModSystem.Draw += OnDraw;
            _imguiModSystem.Closed += OnClose;
        }
        else
        {
            _capi.Logger.Error("[PantheonWars] VSImGui mod not found! Blessing dialog will not work.");
        }

        // Register periodic check for data availability
        _checkDataId = _capi.Event.RegisterGameTickListener(OnCheckDataAvailability, CheckDataInterval);

        _capi.Logger.Notification("[PantheonWars] Blessing Dialog initialized");
    }


    /// <summary>
    ///     Open the blessing dialog
    /// </summary>
    private void Open()
    {
        if (_state.IsOpen) return;

        if (!_state.IsReady)
        {
            // Request data from server
            return;
        }

        _pantheonWarsSystem!.UiService.RequestReligionList();
        _state.IsOpen = true;
        _imguiModSystem?.Show();
    }

    /// <summary>
    ///     Close the blessing dialog
    /// </summary>
    private void Close()
    {
        if (!_state.IsOpen) return;

        _state.IsOpen = false;

        _capi!.Logger.Debug("[PantheonWars] Blessing Dialog closed");
    }

    /// <summary>
    ///     ImGui Draw callback - called every frame when dialog is open
    /// </summary>
    private CallbackGUIStatus OnDraw(float deltaSeconds)
    {
        if (!_state.IsOpen) return CallbackGUIStatus.Closed;

        // Close requested by UI (e.g., top-right X button)
        if (_state.RequestClose)
        {
            _state.RequestClose = false;
            Close();
            return CallbackGUIStatus.Closed;
        }

        // Allow ESC to close
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            Close();
            return CallbackGUIStatus.Closed;
        }

        DrawWindow();

        return CallbackGUIStatus.GrabMouse;
    }

    /// <summary>
    ///     ImGui Closed callback
    /// </summary>
    private void OnClose()
    {
        if (_state.IsOpen) Close();
    }

    /// <summary>
    ///     Draw the main blessing dialog window
    /// </summary>
    private void DrawWindow()
    {
        var window = _capi!.Gui.WindowBounds;
        var deltaTime = _stopwatch!.ElapsedMilliseconds / 1000f;
        _stopwatch.Restart();

        // Calculate window size (constrained to screen)
        var windowWidth = Math.Min(WindowBaseWidth, (int)window.OuterWidth - 128);
        var windowHeight = Math.Min(WindowBaseHeight, (int)window.OuterHeight - 128);

        // Set window style (no borders, no title bar, no padding, centered)
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

        // Position window at center of screen
        ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
        ImGui.SetNextWindowPos(new Vector2(
            _viewport.Pos.X + (_viewport.Size.X - windowWidth) / 2,
            _viewport.Pos.Y + (_viewport.Size.Y - windowHeight) / 2
        ));

        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse;

        // Set window background color
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.16f, 0.12f, 0.09f, 1.0f)); // Dark brown

        ImGui.Begin("PantheonWars Blessing Dialog", flags);

        // Track window position for drawing
        var windowPos = ImGui.GetWindowPos();
        _state.WindowPosX = windowPos.X;
        _state.WindowPosY = windowPos.Y;

        // Draw content
        DrawBackground(windowWidth, windowHeight);

        // Draw UI using BlessingUIRenderer coordinator (Phase 4)
        MainDialogRenderer.Draw(
            _manager!,
            _state,
            windowWidth,
            windowHeight,
            deltaTime
        );

        ImGui.End();
        ImGui.PopStyleColor(); // Pop window background color
        ImGui.PopStyleVar(4); // Pop all 4 style vars
    }

    /// <summary>
    ///     Draw placeholder background (Phase 1)
    /// </summary>
    private void DrawBackground(int width, int height)
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = new Vector2(_state.WindowPosX, _state.WindowPosY);

        // Draw dark brown background rectangle
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.12f, 0.09f, 1.0f)); // #2a1f16
        drawList.AddRectFilled(pos, new Vector2(pos.X + width, pos.Y + height), bgColor);

        // Draw lighter brown frame/border
        var frameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.24f, 0.18f, 0.13f, 1.0f)); // #3d2e20
        drawList.AddRect(pos, new Vector2(pos.X + width, pos.Y + height), frameColor, 0, ImDrawFlags.None, 4);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_capi != null && _checkDataId != 0) _capi.Event.UnregisterGameTickListener(_checkDataId);

        // Unsubscribe from events
        if (_pantheonWarsSystem?.NetworkClient != null)
        {
            _pantheonWarsSystem.NetworkClient.BlessingUnlocked -= OnBlessingUnlockedFromServer;
            _pantheonWarsSystem.NetworkClient.BlessingDataReceived -= OnBlessingDataReceived;
            _pantheonWarsSystem.NetworkClient.ReligionStateChanged -= OnReligionStateChanged;
            _pantheonWarsSystem.NetworkClient.ReligionListReceived -= OnReligionListReceived;
            _pantheonWarsSystem.NetworkClient.ReligionActionCompleted -= OnReligionActionCompleted;
            _pantheonWarsSystem.NetworkClient.PlayerReligionInfoReceived -= OnPlayerReligionInfoReceived;
            _pantheonWarsSystem.NetworkClient.PlayerReligionDataUpdated -= OnPlayerReligionDataUpdated;
            _pantheonWarsSystem.NetworkClient.CivilizationListReceived -= OnCivilizationListReceived;
            _pantheonWarsSystem.NetworkClient.CivilizationInfoReceived -= OnCivilizationInfoReceived;
            _pantheonWarsSystem.NetworkClient.CivilizationActionCompleted -= OnCivilizationActionCompleted;
        }

        // Dispose deity icon loader
        DeityIconLoader.Dispose();

        _capi?.Logger.Notification("[PantheonWars] Blessing Dialog disposed");
    }
}