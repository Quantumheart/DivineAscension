using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.API.Implementation;
using DivineAscension.Constants;
using DivineAscension.API.Interfaces;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.Models.Enum;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
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
    private const int PageTurnDebounceMs = 150;

    private SidebarNavId _lastObservedNav;
    private long _lastPageTurnTickMs;

    // State
    private readonly GuiDialogState _state = new();

    private ICoreClientAPI? _capi;
    private long _checkDataId;
    private DivineAscensionModSystem? _divineAscensionModSystem;
    private ImGuiModSystem? _imguiModSystem;

    // API Services
    private IInputService? _inputService;

    private GuiDialogManager? _manager;
    private IModLoaderService? _modLoaderService;
    private ISoundManager? _soundManager;
    private Stopwatch? _stopwatch;
    private ImGuiViewportPtr _viewport;

    // GUI Logger - static so it can be accessed from partial class methods and icon loaders
    private static ILoggerWrapper? _logger;

    /// <summary>
    ///     Public accessor for the GUI logger (for icon loaders and utilities)
    /// </summary>
    public static ILoggerWrapper? Logger => _logger;

    /// <summary>
    ///     Public accessor for the dialog manager (for network client access)
    /// </summary>
    public GuiDialogManager? DialogManager => _manager;

    /// <summary>
    ///     Public accessor for dialog state. Network handlers read the active
    ///     sidebar destination via <c>State.Sidebar.CurrentNav</c> when they
    ///     need to decide whether to push fresh data to an open view.
    /// </summary>
    public GuiDialogState State => _state;

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

        // Initialize GUI logger (static, accessible from all GUI code)
        _logger = LoggingService.Instance.CreateLogger("GUI");

        // Initialize API services
        _inputService = new ClientInputService(api.Input);
        _modLoaderService = new ModLoaderService(api.ModLoader);

        // Note: ImGui clipboard callbacks disabled - using manual clipboard handling
        // in TextInput.ClipboardCallback instead. The native callbacks conflict with
        // wl-paste on Wayland, causing focus issues when both systems access clipboard.
        // ImGuiClipboardHelper.SetupClipboardCallbacks(api);

        // Menu access is now gated on lectern interaction — the server sends an
        // OpenMenuPacket after a player right-clicks a lectern. See
        // LecternInteractionHandler. No global hotkey is registered in release.
#if DEBUG
        // Dev convenience: Shift+G still opens/closes the dialog locally,
        // bypassing the server-authoritative lectern check.
        _inputService.RegisterHotKey("divineascensionblessings", "Show/Hide Blessing Dialog (debug)", GlKeys.G,
            HotkeyType.GUIOrOtherControls, shiftPressed: true);
        _inputService.SetHotKeyHandler("divineascensionblessings", OnToggleDialog);
#endif

        // Initialize icon loaders
        DeityIconLoader.Initialize(_capi);
        GuiIconLoader.Initialize(_capi);
        CivilizationIconLoader.Initialize(_capi);
        BlessingIconLoader.Initialize(_capi);

        // Preload textures to prevent stuttering on first GUI open
        PreloadTextures();

        // Get DivineAscensionSystem for network communication
        _divineAscensionModSystem = _modLoaderService.GetModSystem<DivineAscensionModSystem>();
        _soundManager = new SoundManager(_capi);
        _manager = new GuiDialogManager(_capi, _divineAscensionModSystem!.UiService, _soundManager);

        // In-content actions (e.g. "Create new religion" from Browse) need to
        // move the sidebar; wire the callback so the manager can request it
        // without holding a reference to the dialog state.
        _manager.ReligionStateManager.NavRedirectRequested = nav => _state.Sidebar.CurrentNav = nav;

        if (_divineAscensionModSystem?.NetworkClient != null)
        {
            _divineAscensionModSystem.NetworkClient.BlessingUnlocked += OnBlessingUnlockedFromServer;
            _divineAscensionModSystem.NetworkClient.BlessingUnlearned += OnBlessingUnlearnedFromServer;
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
            _divineAscensionModSystem.NetworkClient.FeastDayAddCompleted += OnFeastDayAddCompleted;
            _divineAscensionModSystem.NetworkClient.FeastDayRemoveCompleted += OnFeastDayRemoveCompleted;
            _divineAscensionModSystem.NetworkClient.HolidayKeptToastReceived += OnHolidayKeptToast;
            _divineAscensionModSystem.NetworkClient.DeityNameChanged += OnDeityNameChanged;
            _divineAscensionModSystem.NetworkClient.ReligionDetailReceived += OnReligionDetailReceived;
            _divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated += OnPlayerReligionDataUpdated;
            _divineAscensionModSystem.NetworkClient.CivilizationListReceived += OnCivilizationListReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationInfoReceived += OnCivilizationInfoReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationActionCompleted += OnCivilizationActionCompleted;
            _divineAscensionModSystem.NetworkClient.ActivityLogReceived += OnActivityLogReceived;
            _divineAscensionModSystem.NetworkClient.AvailableDomainsReceived += OnAvailableDomainsReceived;
            _divineAscensionModSystem.NetworkClient.HolySiteDataReceived += OnHolySiteDataReceived;
            _divineAscensionModSystem.NetworkClient.HolySiteUpdated += OnHolySiteUpdated;
            _divineAscensionModSystem.NetworkClient.MilestoneProgressReceived += OnMilestoneProgressReceived;
            _divineAscensionModSystem.NetworkClient.LeaderboardReceived += OnLeaderboardReceived;
            _divineAscensionModSystem.NetworkClient.OpenMenuRequested += OnOpenMenuRequested;
            _divineAscensionModSystem.NetworkClient.CloseMenuRequested += OnCloseMenuRequested;
        }
        else
        {
            _logger?.Error(
                "[DivineAscension] DivineAscensionModSystem or NetworkClient not found! Blessing unlocking will not work.");
        }

        // Get ImGui mod system
        _imguiModSystem = _modLoaderService.GetModSystem<ImGuiModSystem>();
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
            _logger?.Error("[DivineAscension] VSImGui mod not found! Blessing dialog will not work.");
        }

        // Register periodic check for data availability
        _checkDataId = _capi.Event.RegisterGameTickListener(OnCheckDataAvailability, CheckDataInterval);

        _logger?.Notification("[DivineAscension] Blessing Dialog initialized");
    }


    /// <summary>
    ///     Open the blessing dialog
    /// </summary>
    internal void Open()
    {
        if (_state.IsOpen) return;

        if (!_state.IsReady)
        {
            // Data race (#487): the open landed before blessing data arrived (common in the
            // first ~1s after login). Queue the open, fire a data request now (the periodic
            // tick keeps retrying with backoff), and echo "Loading…" so the click isn't silent.
            _state.PendingOpen = true;
            _divineAscensionModSystem?.NetworkClient?.RequestBlessingData();
            _divineAscensionModSystem?.NetworkClient?.RequestAvailableDomains();
            _logger?.Debug("[DivineAscension] Open requested before data ready — queued pending open, requested data.");
            _capi?.ShowChatMessage(LocalizationService.Instance.Get(LocalizationKeys.UI_DIALOG_LOADING));
            return;
        }

        _divineAscensionModSystem!.UiService.RequestReligionList();

        // Request civilization data to populate header immediately
        _divineAscensionModSystem!.UiService.RequestCivilizationList(string.Empty);
        _divineAscensionModSystem!.UiService.RequestCivilizationInfo(string.Empty);

        // Old top-tab UI fired RequestPlayerReligionInfo whenever the Religion
        // tab was clicked; the sidebar layout (Phase 3b) has no equivalent
        // click on open, so fire it once here so Info / Activity / Roles can
        // render without waiting for the user to nav away and back.
        if (_manager != null)
        {
            if (_manager.HasReligion())
                _manager.ReligionStateManager.State.InfoState.Loading = true;
            else
                _manager.ReligionStateManager.State.InvitesState.Loading = true;
            _manager.ReligionStateManager.RequestPlayerReligionInfo();
        }

        _state.IsOpen = true;
        _imguiModSystem?.Show();
        _lastObservedNav = _state.Sidebar.CurrentNav;
        PlayPageTurn();
    }

    private void PlayPageTurn()
    {
        var now = Environment.TickCount64;
        if (now - _lastPageTurnTickMs < PageTurnDebounceMs) return;
        _lastPageTurnTickMs = now;
        _soundManager?.Play(SoundType.PageTurn, SoundVolume.Quiet);
    }

    /// <summary>
    ///     Close the blessing dialog
    /// </summary>
    private void Close()
    {
        if (!_state.IsOpen) return;

        _state.IsOpen = false;

        // Persist the most recent window size captured during DrawWindow (snapshot
        // lives on _state; DrawWindow refreshes it each frame) and the page the
        // player was on, so the codex reopens where they left off (#474).
        _divineAscensionModSystem?.SaveUiPrefs(
            (int)_state.WindowWidth, (int)_state.WindowHeight, _state.Sidebar.CurrentNav);

        _logger?.Debug("[DivineAscension] Blessing Dialog closed");
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

        // Allow ESC to close — unless a confirm modal is open, in which case Esc cancels
        // the modal (ConfirmOverlay consumes it) and the dialog stays put (#455).
        if (ImGui.IsKeyPressed(ImGuiKey.Escape) && !ModalInputGuard.IsBlocking)
        {
            Close();
            return CallbackGUIStatus.Closed;
        }

        if (_state.Sidebar.CurrentNav != _lastObservedNav)
        {
            _lastObservedNav = _state.Sidebar.CurrentNav;
            PlayPageTurn();
        }

        UiScale.SyncFromGameSettings();
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

        UiScale.SyncFromGameSettings();

        // Toast anchors to the bottom-right of the full viewport.
        var windowWidth = _viewport.Size.X;
        var windowHeight = _viewport.Size.Y;

        // Create invisible fullscreen window to render notification overlay
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0)); // Fully transparent

        ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
        ImGui.SetNextWindowPos(new Vector2(_viewport.Pos.X, _viewport.Pos.Y));

        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoBackground;

        ImGui.Begin("DivineAscension Notification Overlay", flags);

        // Render notification overlay. The dialog itself is meant to be
        // hidden unless the player interacts with a lectern, so all toasts
        // dismiss-only — no click-through navigation.
        RankUpNotificationOverlay.Draw(
            _manager.NotificationManager.State,
            out var dismissed,
            out _,
            windowWidth,
            windowHeight
        );

        if (dismissed)
            _manager.NotificationManager.DismissCurrentNotification();

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);

        // Non-modal toast — don't grab mouse, let gameplay continue underneath.
        return CallbackGUIStatus.Closed;
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

        // Initial window size from persisted UiPrefs (clamped to screen).
        // Applied only on first use; user-driven resizes within a session and
        // restored sizes across sessions stick from then on.
        var uiPrefs = _divineAscensionModSystem?.Config.UiPrefs;
        var prefsW = uiPrefs?.WindowWidth ?? WindowBaseWidth;
        var prefsH = uiPrefs?.WindowHeight ?? WindowBaseHeight;
        if (prefsW <= 0) prefsW = WindowBaseWidth;
        if (prefsH <= 0) prefsH = WindowBaseHeight;
        var initialW = Math.Min(prefsW, (int)window.OuterWidth - 128);
        var initialH = Math.Min(prefsH, (int)window.OuterHeight - 128);

        // Set window style (no borders, no title bar, no padding, centered)
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

        // Clamp interactive resize to the game window. Min keeps the dialog
        // usable; max prevents drag-resizing off-screen and out of bounds.
        const float minW = 800f;
        const float minH = 500f;
        var maxW = MathF.Max(minW, (float)window.OuterWidth - 32f);
        var maxH = MathF.Max(minH, (float)window.OuterHeight - 32f);
        ImGui.SetNextWindowSizeConstraints(new Vector2(minW, minH), new Vector2(maxW, maxH));

        // Detect if the live dialog has drifted outside the game window — can
        // happen when the user shrinks the game window mid-session, or when an
        // imgui.ini from a different display layout is loaded. When violated,
        // promote SetNextWindowSize/Pos to ImGuiCond.Always so the dialog snaps
        // back inside.
        var sizeOob = _state.WindowWidth > 0f &&
                      (_state.WindowWidth > maxW || _state.WindowHeight > maxH);
        var posOob = _state.WindowWidth > 0f &&
                     (_state.WindowPosX < _viewport.Pos.X
                      || _state.WindowPosY < _viewport.Pos.Y
                      || _state.WindowPosX + _state.WindowWidth > _viewport.Pos.X + _viewport.Size.X
                      || _state.WindowPosY + _state.WindowHeight > _viewport.Pos.Y + _viewport.Size.Y);
        var sizeCond = sizeOob ? ImGuiCond.Always : ImGuiCond.FirstUseEver;
        var posCond = (sizeOob || posOob) ? ImGuiCond.Always : ImGuiCond.FirstUseEver;

        // Size + center on first open only (or forced when out-of-bounds).
        ImGui.SetNextWindowSize(new Vector2(initialW, initialH), sizeCond);
        ImGui.SetNextWindowPos(new Vector2(
            _viewport.Pos.X + (_viewport.Size.X - initialW) / 2,
            _viewport.Pos.Y + (_viewport.Size.Y - initialH) / 2
        ), posCond);

        // NoSavedSettings stops ImGui from writing this dialog's size/pos into
        // imgui.ini. Our UiPrefs is the single source of truth for persisted
        // size, so a stale imgui.ini from a previous monitor layout never
        // overrides the clamped FirstUseEver values.
        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoSavedSettings;

        // Set window background color (Issue #71: Use ColorPalette.Background)
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ColorPalette.Background);
        // Make ImGui's native resize grip visible (it ships nearly invisible against
        // our dark background). Also drives the resize cursor on hover.
        ImGui.PushStyleColor(ImGuiCol.ResizeGrip,
            ColorPalette.WithAlpha(ColorPalette.BorderColor, 0.6f));
        ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered,
            ColorPalette.WithAlpha(ColorPalette.Gold, 0.85f));
        ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, ColorPalette.Gold);

        ImGui.Begin("DivineAscension Blessing Dialog", flags);

        // Track live window position + size (drives renderer layout and the
        // size-on-close persistence in Close()).
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        _state.WindowPosX = windowPos.X;
        _state.WindowPosY = windowPos.Y;
        _state.WindowWidth = windowSize.X;
        _state.WindowHeight = windowSize.Y;

        var windowWidth = (int)windowSize.X;
        var windowHeight = (int)windowSize.Y;

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
        ImGui.PopStyleColor(4); // WindowBg + 3 ResizeGrip variants
        ImGui.PopStyleVar(4); // Pop all 4 style vars
    }

    /// <summary>
    ///     Preload textures during initialization to prevent stuttering on first GUI open
    /// </summary>
    private void PreloadTextures()
    {
        _logger?.Debug("[DivineAscension] Preloading GUI textures...");

        // Preload all deity icons (5 icons: Craft, Wild, Harvest, Stone, Conquest)
        DeityIconLoader.PreloadAllTextures();

        // Preload common civilization icons
        CivilizationIconLoader.PreloadTexture("default");

        // Preload common GUI icons (tab icons and frequently used buttons)
        var commonGuiIcons = new[]
        {
            "browse", "create", "info", "activity", "invites", "roles", // Religion tab icons
            "castle", "diplomacy", "temple", "holysite", // Civilization tab icons
            "meditation", "back", "choice", "hazard-sign", "church", "up", "down" // Common button icons
        };

        foreach (var iconName in commonGuiIcons)
        {
            GuiIconLoader.GetTextureId("gui", iconName);
        }

        // Note: Blessing icons cannot be preloaded here because we don't have blessing data yet
        // They will be preloaded in OnBlessingDataReceived after blessing data is received

        _logger?.Debug("[DivineAscension] GUI texture preload complete");
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

        // Resize affordance: three diagonal strokes in the bottom-right corner.
        // Pairs with the brightened ImGuiCol.ResizeGrip so the resize area is
        // visible even before hover.
        DrawResizeHandleGlyph(drawList, pos, width, height);
    }

    /// <summary>
    ///     Three diagonal strokes inset from the bottom-right corner, indicating
    ///     the window can be dragged to resize.
    /// </summary>
    private static void DrawResizeHandleGlyph(ImDrawListPtr drawList, Vector2 pos, int width, int height)
    {
        const float inset = 6f;
        const float stroke = 1.5f;
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.85f));
        var cornerX = pos.X + width - inset;
        var cornerY = pos.Y + height - inset;

        // Three nested diagonals at 4/8/12 px lengths, parallel to the corner.
        for (var i = 0; i < 3; i++)
        {
            var len = 4f + i * 4f;
            drawList.AddLine(
                new Vector2(cornerX - len, cornerY),
                new Vector2(cornerX, cornerY - len),
                color,
                stroke);
        }
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
            _divineAscensionModSystem.NetworkClient.BlessingUnlearned -= OnBlessingUnlearnedFromServer;
            _divineAscensionModSystem.NetworkClient.BlessingDataReceived -= OnBlessingDataReceived;
            _divineAscensionModSystem.NetworkClient.ReligionStateChanged -= OnReligionStateChanged;
            _divineAscensionModSystem.NetworkClient.ReligionListReceived -= OnReligionListReceived;
            _divineAscensionModSystem.NetworkClient.ReligionActionCompleted -= OnReligionActionCompleted;
            _divineAscensionModSystem.NetworkClient.PlayerReligionInfoReceived -= OnPlayerReligionInfoReceived;
            _divineAscensionModSystem.NetworkClient.FeastDayAddCompleted -= OnFeastDayAddCompleted;
            _divineAscensionModSystem.NetworkClient.FeastDayRemoveCompleted -= OnFeastDayRemoveCompleted;
            _divineAscensionModSystem.NetworkClient.HolidayKeptToastReceived -= OnHolidayKeptToast;
            _divineAscensionModSystem.NetworkClient.DeityNameChanged -= OnDeityNameChanged;
            _divineAscensionModSystem.NetworkClient.ReligionDetailReceived -= OnReligionDetailReceived;
            _divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated -= OnPlayerReligionDataUpdated;
            _divineAscensionModSystem.NetworkClient.CivilizationListReceived -= OnCivilizationListReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationInfoReceived -= OnCivilizationInfoReceived;
            _divineAscensionModSystem.NetworkClient.CivilizationActionCompleted -= OnCivilizationActionCompleted;
            _divineAscensionModSystem.NetworkClient.ActivityLogReceived -= OnActivityLogReceived;
            _divineAscensionModSystem.NetworkClient.AvailableDomainsReceived -= OnAvailableDomainsReceived;
            _divineAscensionModSystem.NetworkClient.HolySiteDataReceived -= OnHolySiteDataReceived;
            _divineAscensionModSystem.NetworkClient.MilestoneProgressReceived -= OnMilestoneProgressReceived;
            _divineAscensionModSystem.NetworkClient.LeaderboardReceived -= OnLeaderboardReceived;
            _divineAscensionModSystem.NetworkClient.OpenMenuRequested -= OnOpenMenuRequested;
            _divineAscensionModSystem.NetworkClient.CloseMenuRequested -= OnCloseMenuRequested;
        }

        // Dispose icon loaders
        DeityIconLoader.Dispose();
        GuiIconLoader.Dispose();
        CivilizationIconLoader.Dispose();
        BlessingIconLoader.Dispose();

        _logger?.Notification("[DivineAscension] Blessing Dialog disposed");
    }
}