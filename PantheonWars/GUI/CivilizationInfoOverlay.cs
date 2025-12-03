using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.Models.Enum;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSImGui;
using VSImGui.API;

namespace PantheonWars.GUI;

/// <summary>
///     [OBSOLETE] Compact HUD overlay showing civilization info
///     DEPRECATED: Civilization UI has been migrated to BlessingDialog (Civilization tab)
///     This file will be removed in a future update.
/// </summary>
[Obsolete("Use BlessingDialog Civilization tab instead. This overlay will be removed in a future update.")]
[ExcludeFromCodeCoverage]
public class CivilizationInfoOverlay : ModSystem
{
    private const int OverlayWidth = 250;
    private const int OverlayHeight = 120;

    private ICoreClientAPI? _capi;
    private ImGuiModSystem? _imguiModSystem;
    private PantheonWarsSystem? _pantheonWarsSystem;

    private bool _isVisible;
    private CivilizationInfoResponsePacket.CivilizationDetails? _currentCivilization;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override double ExecuteOrder()
    {
        return 1.7; // Load after CivilizationDialog (1.6)
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _capi = api;

        // Register keybind (Shift+C to toggle overlay)
        _capi.Input.RegisterHotKey("pantheonwarscivoverlay", "Toggle Civilization Overlay",
            GlKeys.C, HotkeyType.GUIOrOtherControls, shiftPressed: true);
        _capi.Input.SetHotKeyHandler("pantheonwarscivoverlay", OnToggleOverlay);

        // Get PantheonWarsSystem
        _pantheonWarsSystem = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.CivilizationInfoReceived += OnCivilizationInfoReceived;
        }

        // Get ImGui mod system
        _imguiModSystem = _capi.ModLoader.GetModSystem<ImGuiModSystem>();
        if (_imguiModSystem != null)
        {
            _imguiModSystem.Draw += OnDraw;
        }

        _capi.Logger.Notification("[PantheonWars] Civilization Info Overlay initialized (Shift+C to toggle)");
    }

    /// <summary>
    ///     Toggle overlay visibility
    /// </summary>
    private bool OnToggleOverlay(KeyCombination keyCombination)
    {
        _isVisible = !_isVisible;

        if (_isVisible)
        {
            // Request fresh civilization data when showing
            _pantheonWarsSystem?.RequestCivilizationInfo("");
            _imguiModSystem?.Show();
        }

        _capi!.Logger.Debug($"[PantheonWars] Civilization overlay toggled: {_isVisible}");
        return true;
    }

    /// <summary>
    ///     ImGui Draw callback
    /// </summary>
    private CallbackGUIStatus OnDraw(float deltaSeconds)
    {
        if (!_isVisible) return CallbackGUIStatus.Closed;

        // Auto-hide if no civilization
        if (_currentCivilization == null)
        {
            _isVisible = false;
            return CallbackGUIStatus.Closed;
        }

        DrawOverlay();

        return CallbackGUIStatus.GrabMouse; // Allow interaction with overlay
    }

    /// <summary>
    ///     Draw the compact overlay
    /// </summary>
    private void DrawOverlay()
    {
        var viewport = ImGui.GetMainViewport();

        // Position in top-right corner (with padding)
        ImGui.SetNextWindowPos(new Vector2(
            viewport.Pos.X + viewport.Size.X - OverlayWidth - 16f,
            viewport.Pos.Y + 80f // Below typical UI elements
        ));
        ImGui.SetNextWindowSize(new Vector2(OverlayWidth, OverlayHeight));

        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 12f));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.16f, 0.12f, 0.09f, 0.9f)); // Semi-transparent

        ImGui.Begin("CivilizationOverlay", flags);

        // Civilization name
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), _currentCivilization!.Name);

        ImGui.Spacing();

        // Member count
        var memberCount = _currentCivilization.MemberReligions.Count;
        ImGui.Text($"Members: {memberCount}/4");

        // Deity indicators
        ImGui.Text("Deities:");
        ImGui.SameLine();

        const float iconSize = 12f;
        const float iconSpacing = 4f;
        var startX = ImGui.GetCursorPosX();
        var startY = ImGui.GetCursorPosY();

        foreach (var member in _currentCivilization.MemberReligions)
        {
            if (Enum.TryParse<DeityType>(member.Deity, out var deityType))
            {
                var color = GetDeityColor(deityType);
                var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

                var drawList = ImGui.GetWindowDrawList();
                var windowPos = ImGui.GetWindowPos();
                var iconPos = new Vector2(windowPos.X + startX, windowPos.Y + startY);

                // Draw small colored square
                drawList.AddRectFilled(
                    iconPos,
                    new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize),
                    colorU32,
                    2f
                );

                startX += iconSize + iconSpacing;
            }
        }

        ImGui.SetCursorPosY(startY + iconSize + 4f);

        // Quick actions
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.SmallButton("Manage"))
        {
            var civilizationDialog = _capi!.ModLoader.GetModSystem<CivilizationDialog>();
            civilizationDialog?.Open(initialTab: 1);
            _isVisible = false; // Hide overlay when opening full dialog
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Hide"))
        {
            _isVisible = false;
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    /// <summary>
    ///     Get deity color for display
    /// </summary>
    private Vector4 GetDeityColor(DeityType deity)
    {
        return deity switch
        {
            DeityType.Khoras => new Vector4(0.8f, 0.2f, 0.2f, 1f), // Red
            DeityType.Lysa => new Vector4(0.2f, 0.8f, 0.2f, 1f), // Green
            DeityType.Aethra => new Vector4(1f, 0.9f, 0.4f, 1f), // Gold/yellow
            DeityType.Gaia => new Vector4(0.5f, 0.4f, 0.2f, 1f), // Brown
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1f) // Gray default
        };
    }

    // ===== EVENT HANDLERS =====

    private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        _currentCivilization = packet.Details;

        // Auto-hide if no civilization
        if (_currentCivilization == null && _isVisible)
        {
            _isVisible = false;
            _capi!.Logger.Debug("[PantheonWars] Auto-hiding civilization overlay (no civilization)");
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.CivilizationInfoReceived -= OnCivilizationInfoReceived;
        }

        _capi?.Logger.Notification("[PantheonWars] Civilization Info Overlay disposed");
    }
}
