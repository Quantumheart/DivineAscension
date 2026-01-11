using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSImGui;
using VSImGui.API;

namespace DivineAscension.GUI;

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
    private DivineAscensionModSystem? _divineAscensionModSystem;
    private ImGuiModSystem? _imguiModSystem;

    private GuiDialogManager? _manager;
    private ISoundManager? _soundManager;
    private Stopwatch? _stopwatch;
    private ImGuiViewportPtr _viewport;

    /// <summary>
    ///     Public accessor for the dialog manager (for network client access)
    /// </summary>
    public GuiDialogManager? DialogManager => _manager;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override double ExecuteOrder()
    {
        return 1.5; // Load after main DivineAscensionModSystem (1.0)
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _capi = api;
        _viewport = ImGui.GetMainViewport();
        _stopwatch = Stopwatch.StartNew();

        // Register keybind - VS automatically handles persistence when users change it in Controls menu
        // Default: Shift+G, but users can customize via Settings > Controls > Mod Keys
        _capi.Input.RegisterHotKey(
            "divineascensionblessings",
            "Divine Ascension: Open Dialog",
            GlKeys.G,
            HotkeyType.GUIOrOtherControls,
            shiftPressed: true);
        _capi.Input.SetHotKeyHandler("divineascensionblessings", OnToggleDialog);

        _capi.Logger.Notification("[DivineAscension] Registered keybind: Divine Ascension dialog (default: Shift+G)");

        // Initialize icon loaders
        DeityIconLoader.Initialize(_capi);
        GuiIconLoader.Initialize(_capi);
        CivilizationIconLoader.Initialize(_capi);
        BlessingIconLoader.Initialize(_capi);

        // Get DivineAscensionSystem for network communication
        _divineAscensionModSystem = _capi.ModLoader.GetModSystem<DivineAscensionModSystem>();
        _soundManager = new SoundManager(_capi);
        _manager = new GuiDialogManager(_capi, _divineAscensionModSystem!.UiService, _soundManager);
        if (_divineAscensionModSystem?.NetworkClient != null)
        {
            _divineAscensionModSystem.NetworkClient.BlessingUnlocked += OnBlessingUnlockedFromServer;
            _divineAscensionModSystem.NetworkClient.BlessingDataReceived += OnBlessingDataReceived;
            _divineAscensionModSystem.NetworkClient.ReligionStateChanged += OnReligionStateChanged;
            _divineAscensionModSystem.NetworkClient.ReligionListReceived += OnReligionListReceived;
            _divineAscensionModSystem.NetworkClient.ReligionActionCompleted += OnReligionActionCompleted;
            _divineAscensionModSystem.NetworkClient.ReligionRolesReceived += OnReligionRolesReceived;
            _divineAscensionModSystem.NetworkClient.RoleCreated += OnRoleCreated;
            _divineAscensionModSystem.NetworkClient.RolePermissionsModified += OnRolePermissionsModified;
            _divineAscensionModSystem.NetworkClient.RoleAssigned += OnRoleAssigned;
            _divineAscensionModSystem.NetworkClient.RoleDeleted += OnRoleDeleted;
            _divineAscensionModSystem.NetworkClient.FounderTransferred += OnFounderTransferred;
            _divineAscensionModSystem.NetworkClient.PlayerReligionInfoReceived += OnPlayerReligionInfoReceived;
            _divineAscensionModSystem.NetworkClient.ReligionDetailReceived += OnReligionDetailReceived;
            _divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated += OnPlayerReligionDataUpdated;
            _divineAscensionModSystem.NetworkClient.CivilizationListReceived += OnCivilizationListReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationInfoReceived += OnCivilizationInfoReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationActionCompleted += OnCivilizationActionCompleted;
        }
        else
        {
            _capi.Logger.Error(
                "[DivineAscension] DivineAscensionModSystem or NetworkClient not found! Blessing unlocking will not work.");
        }

        // Get ImGui mod system
        _imguiModSystem = _capi.ModLoader.GetModSystem<ImGuiModSystem>();
        if (_imguiModSystem != null)
        {
            _imguiModSystem.Draw += OnDraw;
            _imguiModSystem.Draw += OnDrawNotifications; // Always-active notification overlay
            _imguiModSystem.Closed += OnClose;

            // Set callback for notification manager to trigger ImGui showing
            _manager.NotificationManager.SetShowImGuiCallback(() => _imguiModSystem.Show());
        }
        else
        {
            _capi.Logger.Error("[DivineAscension] VSImGui mod not found! Blessing dialog will not work.");
        }

        // Register periodic check for data availability
        _checkDataId = _capi.Event.RegisterGameTickListener(OnCheckDataAvailability, CheckDataInterval);

        _capi.Logger.Notification("[DivineAscension] Blessing Dialog initialized");
    }


    /// <summary>
    ///     Open the blessing dialog
    /// </summary>
    internal void Open()
    {
        if (_state.IsOpen) return;

        if (!_state.IsReady)
        {
            // Request data from server
            return;
        }

        _divineAscensionModSystem!.UiService.RequestReligionList();
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

        _capi!.Logger.Debug("[DivineAscension] Blessing Dialog closed");
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
    ///     ImGui Draw callback for notifications - always active, independent of main dialog
    /// </summary>
    private CallbackGUIStatus OnDrawNotifications(float deltaSeconds)
    {
        // If no notification is visible, return Closed so ImGui doesn't capture input
        if (!_manager!.NotificationManager.State.IsVisible)
        {
            return CallbackGUIStatus.Closed;
        }

        // If main dialog is open, don't capture input - let the main dialog handle it
        if (_state.IsOpen)
        {
            // Still render the notification, but don't grab input
            return CallbackGUIStatus.Closed;
        }

        // Update notification timer
        _manager.NotificationManager.Update(deltaSeconds);

        // Get window bounds
        var window = _capi!.Gui.WindowBounds;
        var windowWidth = Math.Min(WindowBaseWidth, (int)window.OuterWidth - 128);
        var windowHeight = Math.Min(WindowBaseHeight, (int)window.OuterHeight - 128);

        // Create invisible fullscreen window to render notification overlay
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0)); // Fully transparent

        ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
        ImGui.SetNextWindowPos(new Vector2(
            _viewport.Pos.X + (_viewport.Size.X - windowWidth) / 2,
            _viewport.Pos.Y + (_viewport.Size.Y - windowHeight) / 2
        ));

        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoBackground;

        ImGui.Begin("DivineAscension Notification Overlay", flags);

        // Render notification overlay
        RankUpNotificationOverlay.Draw(
            _manager.NotificationManager.State,
            out var dismissed,
            out var viewBlessingsClicked,
            windowWidth,
            windowHeight
        );

        // Handle notification interactions
        if (dismissed)
        {
            _manager.NotificationManager.DismissCurrentNotification();
        }

        if (viewBlessingsClicked)
        {
            // Set the tab BEFORE opening the dialog to ensure it opens on the correct tab
            _state.CurrentMainTab = MainDialogTab.Blessings;

            _manager.NotificationManager.OnViewBlessingsClicked(
                () =>
                {
                    if (!_state.IsOpen) Open();
                },
                () =>
                {
                    // Tab already set above, but this ensures it's set if dialog was already open
                    _state.CurrentMainTab = MainDialogTab.Blessings;
                }
            );
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);

        // Return GrabMouse to capture input while notification is visible
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

        // Set window background color (Issue #71: Use ColorPalette.Background)
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ColorPalette.Background);

        ImGui.Begin("DivineAscension Blessing Dialog", flags);

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

        // Draw main window background (Issue #71: Use ColorPalette.Background #291e16)
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(pos, new Vector2(pos.X + width, pos.Y + height), bgColor);

        // Draw main window border (Issue #71: Use ColorPalette.BorderColor #59422f, 4px width)
        var frameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
        drawList.AddRect(pos, new Vector2(pos.X + width, pos.Y + height), frameColor, 0, ImDrawFlags.None, 4);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_capi != null && _checkDataId != 0) _capi.Event.UnregisterGameTickListener(_checkDataId);

        // Unsubscribe from ImGui events
        if (_imguiModSystem != null)
        {
            _imguiModSystem.Draw -= OnDraw;
            _imguiModSystem.Draw -= OnDrawNotifications;
            _imguiModSystem.Closed -= OnClose;
        }

        // Unsubscribe from events
        if (_divineAscensionModSystem?.NetworkClient != null)
        {
            _divineAscensionModSystem.NetworkClient.BlessingUnlocked -= OnBlessingUnlockedFromServer;
            _divineAscensionModSystem.NetworkClient.BlessingDataReceived -= OnBlessingDataReceived;
            _divineAscensionModSystem.NetworkClient.ReligionStateChanged -= OnReligionStateChanged;
            _divineAscensionModSystem.NetworkClient.ReligionListReceived -= OnReligionListReceived;
            _divineAscensionModSystem.NetworkClient.ReligionActionCompleted -= OnReligionActionCompleted;
            _divineAscensionModSystem.NetworkClient.PlayerReligionInfoReceived -= OnPlayerReligionInfoReceived;
            _divineAscensionModSystem.NetworkClient.ReligionDetailReceived -= OnReligionDetailReceived;
            _divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated -= OnPlayerReligionDataUpdated;
            _divineAscensionModSystem.NetworkClient.CivilizationListReceived -= OnCivilizationListReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationInfoReceived -= OnCivilizationInfoReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationActionCompleted -= OnCivilizationActionCompleted;
        }

        // Dispose icon loaders
        DeityIconLoader.Dispose();
        GuiIconLoader.Dispose();
        CivilizationIconLoader.Dispose();
        BlessingIconLoader.Dispose();

        _capi?.Logger.Notification("[DivineAscension] Blessing Dialog disposed");
    }
}