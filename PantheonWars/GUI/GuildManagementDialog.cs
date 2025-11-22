using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Renderers;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSImGui;
using VSImGui.API;

namespace PantheonWars.GUI;

/// <summary>
///     Main ImGui-based Guild Management Dialog for managing guilds
/// </summary>
[ExcludeFromCodeCoverage]
public partial class GuildManagementDialog : ModSystem
{
    private const int CheckDataInterval = 1000; // Check for data every 1 second
    private const int WindowBaseWidth = 1400;
    private const int WindowBaseHeight = 900;

    private ICoreClientAPI? _capi;
    private long _checkDataId;
    private ImGuiModSystem? _imguiModSystem;
    private PantheonWarsSystem? _pantheonWarsSystem;

    private GuildDialogManager? _manager;
    private Stopwatch? _stopwatch;
    private ImGuiViewportPtr _viewport;

    // State
    private readonly GuildDialogState _state = new();
    private readonly ReligionManagementState _managementState = new();

    // Overlay coordinator
    private OverlayCoordinator? _overlayCoordinator;

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
        _capi.Input.RegisterHotKey("pantheonwarsguilds", "Show/Hide Guild Management Dialog", GlKeys.P,
            HotkeyType.GUIOrOtherControls);
        _capi.Input.SetHotKeyHandler("pantheonwarsguilds", OnToggleDialog);

        // Initialize manager and overlay coordinator
        _manager = new GuildDialogManager(_capi);
        _overlayCoordinator = new OverlayCoordinator();

        // Get PantheonWarsSystem for network communication
        _pantheonWarsSystem = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.ReligionStateChanged += OnReligionStateChanged;
            _pantheonWarsSystem.ReligionListReceived += OnReligionListReceived;
            _pantheonWarsSystem.ReligionActionCompleted += OnReligionActionCompleted;
            _pantheonWarsSystem.PlayerReligionInfoReceived += OnPlayerReligionInfoReceived;
            _pantheonWarsSystem.PlayerReligionDataUpdated += OnPlayerReligionDataUpdated;
        }
        else
        {
            _capi.Logger.Error("[PantheonWars] PantheonWarsSystem not found! Guild management will not work.");
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
            _capi.Logger.Error("[PantheonWars] VSImGui mod not found! Guild Management dialog will not work.");
        }

        // Register periodic check for data availability
        _checkDataId = _capi.Event.RegisterGameTickListener(OnCheckDataAvailability, CheckDataInterval);

        _capi.Logger.Notification("[PantheonWars] Guild Management Dialog initialized");
    }


    /// <summary>
    ///     Open the guild management dialog
    /// </summary>
    private void Open()
    {
        if (_state.IsOpen) return;

        if (!_state.IsReady)
        {
            // Request data from server
            _pantheonWarsSystem?.RequestPlayerReligionInfo();
            _capi!.ShowChatMessage("Loading guild data...");
            return;
        }

        _state.IsOpen = true;
        _imguiModSystem?.Show();

        // Initialize appropriate renderer based on guild status
        if (!_manager!.HasReligion())
        {
            // Initialize guild browser
            GuildBrowserRenderer.Initialize();
            _pantheonWarsSystem?.RequestReligionList("");
        }
        else
        {
            // Initialize management state and request guild info
            _managementState.Reset();
            _pantheonWarsSystem?.RequestPlayerReligionInfo();
        }

        _capi!.Logger.Debug("[PantheonWars] Guild Management Dialog opened");
    }

    /// <summary>
    ///     Close the guild management dialog
    /// </summary>
    private void Close()
    {
        if (!_state.IsOpen) return;

        _state.IsOpen = false;

        // TODO: Add close sound in Phase 5
        // _capi.Gui.PlaySound(new AssetLocation("pantheonwars", "sounds/click.ogg"), false, 0.3f);

        _capi!.Logger.Debug("[PantheonWars] Guild Management Dialog closed");
    }

    /// <summary>
    ///     ImGui Draw callback - called every frame when dialog is open
    /// </summary>
    private CallbackGUIStatus OnDraw(float deltaSeconds)
    {
        if (!_state.IsOpen) return CallbackGUIStatus.Closed;

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
    ///     Draw the main guild management dialog window
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

        ImGui.Begin("PantheonWars Guild Management Dialog", flags);

        // Track window position for drawing
        var windowPos = ImGui.GetWindowPos();
        _state.WindowPosX = windowPos.X;
        _state.WindowPosY = windowPos.Y;

        // Draw content
        DrawBackground(windowWidth, windowHeight);
        DrawMainContent(windowWidth, windowHeight);

        // Draw overlays using coordinator (only create and leave confirmation now)
        _overlayCoordinator!.RenderOverlays(
            _capi,
            windowWidth,
            windowHeight,
            _manager!,
            OnJoinReligionClicked,
            OnCreateReligionClicked,
            OnCreateReligionSubmit,
            OnKickMemberClicked,
            OnBanMemberClicked,
            OnUnbanMemberClicked,
            OnInvitePlayerClicked,
            OnEditDescriptionClicked,
            OnDisbandReligionClicked,
            OnRequestReligionInfo,
            OnLeaveReligionCancelled,
            OnLeaveReligionConfirmed
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

    /// <summary>
    ///     Draw main content area (browser or guild tabs)
    /// </summary>
    private void DrawMainContent(int windowWidth, int windowHeight)
    {
        var drawList = ImGui.GetWindowDrawList();
        const float headerHeight = 140f; // Space for guild header
        const float tabHeight = 50f; // Space for tabs
        const float padding = 20f;

        var contentY = _state.WindowPosY + padding;
        var contentHeight = windowHeight - padding * 2;

        // Check if we should show the browser (either no guild OR browsing mode enabled)
        if (!_manager!.HasReligion() || _state.ShowBrowser)
        {
            // Show guild browser
            GuildBrowserRenderer.Draw(
                _capi!,
                drawList,
                _state.WindowPosX + padding,
                contentY,
                windowWidth - padding * 2,
                contentHeight,
                OnJoinReligionClicked,
                OnCreateReligionClicked,
                _manager.HasReligion()); // userHasReligion

            // Add "Back to My Guild" button if user has a guild
            if (_manager.HasReligion())
            {
                const float backButtonWidth = 200f;
                const float backButtonHeight = 40f;
                const float backButtonPadding = 20f;
                var backButtonX = _state.WindowPosX + padding;
                var backButtonY = _state.WindowPosY + windowHeight - padding - backButtonHeight;

                if (UI.Components.Buttons.ButtonRenderer.DrawButton(
                    drawList,
                    "Back to My Guild",
                    backButtonX,
                    backButtonY,
                    backButtonWidth,
                    backButtonHeight,
                    isPrimary: false,
                    enabled: true))
                {
                    _capi!.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                        _capi.World.Player.Entity, null, false, 8f, 0.5f);
                    _state.ShowBrowser = false;
                }
            }
        }
        else
        {
            // Show guild header
            var headerContentHeight = ReligionHeaderRenderer.Draw(
                _manager,
                _capi!,
                _state.WindowPosX,
                contentY,
                windowWidth,
                OnChangeReligionClicked,
                OnManageReligionClicked,
                OnLeaveReligionClicked);

            contentY += headerContentHeight + padding;

            // Show tabs
            var tabLabels = new[] { "Overview", "Members", "Settings" };
            var selectedTab = (int)_state.CurrentTab;
            var newSelectedTab = TabControl.Draw(
                drawList,
                _state.WindowPosX + padding,
                contentY,
                windowWidth - padding * 2,
                tabHeight,
                tabLabels,
                selectedTab);

            if (newSelectedTab != selectedTab)
            {
                _state.CurrentTab = (GuildTab)newSelectedTab;
            }

            contentY += tabHeight + padding;

            // Calculate remaining content area for tabs
            var tabContentHeight = windowHeight - (contentY - _state.WindowPosY) - padding;
            var tabContentWidth = windowWidth - padding * 2;

            // Draw selected tab content
            switch (_state.CurrentTab)
            {
                case GuildTab.Overview:
                    GuildOverviewRenderer.Draw(
                        _capi!,
                        drawList,
                        _state.WindowPosX + padding,
                        contentY,
                        tabContentWidth,
                        tabContentHeight,
                        _managementState,
                        OnLeaveReligionClicked);
                    break;

                case GuildTab.Members:
                    GuildMembersTabRenderer.Draw(
                        _capi!,
                        drawList,
                        _state.WindowPosX + padding,
                        contentY,
                        tabContentWidth,
                        tabContentHeight,
                        _managementState,
                        OnKickMemberClicked,
                        OnBanMemberClicked);
                    break;

                case GuildTab.Settings:
                    GuildSettingsRenderer.Draw(
                        _capi!,
                        drawList,
                        _state.WindowPosX + padding,
                        contentY,
                        tabContentWidth,
                        tabContentHeight,
                        _managementState,
                        OnInvitePlayerClicked,
                        OnEditDescriptionClicked,
                        OnUnbanMemberClicked,
                        OnDisbandReligionClicked);
                    break;
            }
        }

        // Close button (always visible in top-right)
        DrawCloseButton(windowWidth);
    }

    /// <summary>
    ///     Draw close button in top-right corner
    /// </summary>
    private void DrawCloseButton(int windowWidth)
    {
        const float buttonSize = 32f;
        const float padding = 16f;

        var drawList = ImGui.GetWindowDrawList();
        var buttonX = _state.WindowPosX + windowWidth - padding - buttonSize;
        var buttonY = _state.WindowPosY + padding;

        if (UI.Components.Buttons.ButtonRenderer.DrawCloseButton(drawList, buttonX, buttonY, buttonSize))
        {
            _capi!.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
                _capi.World.Player.Entity, null, false, 8f, 0.5f);
            OnCloseButtonClicked();
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_capi != null && _checkDataId != 0) _capi.Event.UnregisterGameTickListener(_checkDataId);

        // Unsubscribe from events
        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.ReligionStateChanged -= OnReligionStateChanged;
            _pantheonWarsSystem.ReligionListReceived -= OnReligionListReceived;
            _pantheonWarsSystem.ReligionActionCompleted -= OnReligionActionCompleted;
            _pantheonWarsSystem.PlayerReligionInfoReceived -= OnPlayerReligionInfoReceived;
            _pantheonWarsSystem.PlayerReligionDataUpdated -= OnPlayerReligionDataUpdated;
        }

        _capi?.Logger.Notification("[PantheonWars] Guild Management Dialog disposed");
    }
}