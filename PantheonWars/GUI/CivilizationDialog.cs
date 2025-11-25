using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
///     Full-featured civilization management dialog with tabs for browsing, managing, invites, and creation
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationDialog : ModSystem
{
    private const int WindowWidth = 900;
    private const int WindowHeight = 700;

    private ICoreClientAPI? _capi;
    private ImGuiModSystem? _imguiModSystem;
    private PantheonWarsSystem? _pantheonWarsSystem;

    private bool _isOpen;
    private int _currentTab; // 0=Browse, 1=My Civ, 2=Invites, 3=Create

    // State
    private List<CivilizationListResponsePacket.CivilizationInfo> _allCivilizations = new();
    private CivilizationInfoResponsePacket.CivilizationDetails? _myCivilization;
    private List<CivilizationInfoResponsePacket.PendingInvite> _myInvites = new();
    private string _deityFilter = "";
    private string _searchText = "";
    private string _createCivName = "";
    private string _inviteReligionName = "";

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override double ExecuteOrder()
    {
        return 1.6; // Load after BlessingDialog (1.5)
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _capi = api;

        // Get PantheonWarsSystem
        _pantheonWarsSystem = _capi.ModLoader.GetModSystem<PantheonWarsSystem>();
        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.CivilizationListReceived += OnCivilizationListReceived;
            _pantheonWarsSystem.CivilizationInfoReceived += OnCivilizationInfoReceived;
            _pantheonWarsSystem.CivilizationActionCompleted += OnCivilizationActionCompleted;
        }

        // Get ImGui mod system
        _imguiModSystem = _capi.ModLoader.GetModSystem<ImGuiModSystem>();
        if (_imguiModSystem != null)
        {
            _imguiModSystem.Draw += OnDraw;
        }

        _capi.Logger.Notification("[PantheonWars] Civilization Dialog initialized");
    }

    /// <summary>
    ///     Open the civilization dialog
    /// </summary>
    public void Open(int initialTab = 0)
    {
        if (_isOpen) return;

        _isOpen = true;
        _currentTab = initialTab;
        _imguiModSystem?.Show();

        // Request initial data
        RefreshData();

        _capi!.Logger.Debug("[PantheonWars] Civilization Dialog opened");
    }

    /// <summary>
    ///     Close the civilization dialog
    /// </summary>
    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _capi!.Logger.Debug("[PantheonWars] Civilization Dialog closed");
    }

    /// <summary>
    ///     Refresh data from server
    /// </summary>
    private void RefreshData()
    {
        _pantheonWarsSystem?.RequestCivilizationList(_deityFilter);
        _pantheonWarsSystem?.RequestCivilizationInfo(""); // My civilization
    }

    /// <summary>
    ///     ImGui Draw callback
    /// </summary>
    private CallbackGUIStatus OnDraw(float deltaSeconds)
    {
        if (!_isOpen) return CallbackGUIStatus.Closed;

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
    ///     Draw the main civilization dialog window
    /// </summary>
    private void DrawWindow()
    {
        var window = _capi!.Gui.WindowBounds;
        var viewport = ImGui.GetMainViewport();

        // Center window
        ImGui.SetNextWindowSize(new Vector2(WindowWidth, WindowHeight));
        ImGui.SetNextWindowPos(new Vector2(
            viewport.Pos.X + (viewport.Size.X - WindowWidth) / 2,
            viewport.Pos.Y + (viewport.Size.Y - WindowHeight) / 2
        ));

        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.16f, 0.12f, 0.09f, 1.0f)); // Dark brown
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);

        ImGui.Begin("Civilization Management", flags);

        DrawHeader();
        DrawTabs();
        DrawTabContent();
        DrawFooter();

        ImGui.End();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    /// <summary>
    ///     Draw dialog header with title and close button
    /// </summary>
    private void DrawHeader()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 16f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 16f);

        ImGui.PushFont(ImGui.GetFont());
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "Civilization Management");
        ImGui.PopFont();

        // Close button (top right)
        ImGui.SameLine(WindowWidth - 90f); // More padding: 40 (button width) + 50 (padding from edge)
        if (ImGui.Button("X", new Vector2(50f, 24f))) // Wider button
        {
            Close();
        }

        ImGui.Separator();
    }

    /// <summary>
    ///     Draw tab bar
    /// </summary>
    private void DrawTabs()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);

        if (ImGui.BeginTabBar("CivilizationTabs"))
        {
            if (ImGui.BeginTabItem("Browse"))
            {
                _currentTab = 0;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("My Civilization"))
            {
                _currentTab = 1;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Invitations"))
            {
                _currentTab = 2;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Create"))
            {
                _currentTab = 3;
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    /// <summary>
    ///     Draw content for current tab
    /// </summary>
    private void DrawTabContent()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);

        // Content area width
        const float contentWidth = WindowWidth - 32f;
        ImGui.BeginChild("TabContent", new Vector2(contentWidth, WindowHeight - 160f), true);

        switch (_currentTab)
        {
            case 0:
                DrawBrowseTab(contentWidth);
                break;
            case 1:
                DrawMyCivilizationTab(contentWidth);
                break;
            case 2:
                DrawInvitesTab(contentWidth);
                break;
            case 3:
                DrawCreateTab();
                break;
        }

        ImGui.EndChild();
    }

    /// <summary>
    ///     Draw Browse tab - list all civilizations
    /// </summary>
    private void DrawBrowseTab(float contentWidth)
    {
        ImGui.Text("Browse All Civilizations");
        ImGui.Separator();

        // Filter bar
        ImGui.Text("Filter by deity:");
        ImGui.SameLine();

        var deities = new[] { "All", "Khoras", "Lysa", "Morthen", "Aethra", "Umbros", "Tharos", "Gaia", "Vex" };
        var currentDeityIndex = Array.IndexOf(deities, string.IsNullOrEmpty(_deityFilter) ? "All" : _deityFilter);
        if (currentDeityIndex < 0) currentDeityIndex = 0;

        if (ImGui.Combo("##DeityFilter", ref currentDeityIndex, deities, deities.Length))
        {
            _deityFilter = currentDeityIndex == 0 ? "" : deities[currentDeityIndex];
            _pantheonWarsSystem?.RequestCivilizationList(_deityFilter);
        }

        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
            _pantheonWarsSystem?.RequestCivilizationList(_deityFilter);
        }

        ImGui.Spacing();

        // Civilization list
        if (_allCivilizations.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No civilizations found.");
        }
        else
        {
            foreach (var civ in _allCivilizations)
            {
                ImGui.PushID(civ.CivId);

                // Civilization card - use contentWidth minus padding
                float cardWidth = contentWidth - 48f; // More padding to account for scrollbar + borders
                ImGui.BeginChild($"CivCard_{civ.CivId}", new Vector2(cardWidth, 90f), true); // Taller card

                // Store starting position
                var startY = ImGui.GetCursorPosY();

                // Left side info
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), civ.Name);
                ImGui.Text($"Members: {civ.MemberCount}/4 religions");

                // Show deity indicators
                ImGui.Text("Deities: ");
                ImGui.SameLine();
                foreach (var deity in civ.MemberDeities)
                {
                    if (Enum.TryParse<DeityType>(deity, out var deityType))
                    {
                        var color = GetDeityColor(deityType);
                        ImGui.TextColored(color, $"[{deity}] ");
                        ImGui.SameLine();
                    }
                }
                ImGui.NewLine();

                // View Details button - position at absolute position within card
                const float buttonWidth = 110f;
                const float buttonHeight = 32f;
                ImGui.SetCursorPos(new Vector2(cardWidth - buttonWidth - 12f, startY + 29f)); // Centered in 90px card
                if (ImGui.Button("View Details", new Vector2(buttonWidth, buttonHeight)))
                {
                    _pantheonWarsSystem?.RequestCivilizationInfo(civ.CivId);
                }

                ImGui.EndChild();
                ImGui.PopID();

                ImGui.Spacing();
            }
        }
    }

    /// <summary>
    ///     Draw My Civilization tab - manage members and invites
    /// </summary>
    private void DrawMyCivilizationTab(float contentWidth)
    {
        if (_myCivilization == null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f),
                "You are not in a civilization. Join one or create your own!");
            return;
        }

        ImGui.Text($"Civilization: {_myCivilization.Name}");
        ImGui.Text($"Founded: {_myCivilization.CreatedDate:yyyy-MM-dd}");
        ImGui.Separator();

        ImGui.Spacing();
        ImGui.Text($"Member Religions ({_myCivilization.MemberReligions.Count}/4):");
        ImGui.Separator();

        // Member list
        foreach (var member in _myCivilization.MemberReligions)
        {
            ImGui.PushID(member.ReligionId);

            if (Enum.TryParse<DeityType>(member.Deity, out var deityType))
            {
                var color = GetDeityColor(deityType);
                ImGui.TextColored(color, $"[{member.Deity}]");
                ImGui.SameLine();
            }

            ImGui.Text($"{member.ReligionName} ({member.MemberCount} members)");

            // Kick button (only for founder, can't kick self)
            var isFounder = _myCivilization.FounderReligionUID == member.ReligionId;
            if (isFounder)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), "=== Founder ===");
            }
            else
            {
                // Show kick button for non-founder religions (TODO: check if player is civ founder)
                if (!isFounder)
                {
                    ImGui.SameLine(contentWidth - 100f); // 80 (button width) + 20 (padding)
                    if (ImGui.Button($"Kick##{member.ReligionId}", new Vector2(80f, 24f)))
                    {
                        _pantheonWarsSystem?.RequestCivilizationAction("kick", _myCivilization.CivId,
                            member.ReligionName);
                    }
                }
            }

            ImGui.PopID();
            ImGui.Spacing();
        }

        ImGui.Spacing();
        ImGui.Separator();

        // Invite section (only for founder)
        ImGui.Text("Invite Religion:");
        ImGui.InputText("##InviteReligion", ref _inviteReligionName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Send Invite"))
        {
            if (!string.IsNullOrEmpty(_inviteReligionName))
            {
                _pantheonWarsSystem?.RequestCivilizationAction("invite", _myCivilization.CivId, _inviteReligionName);
                _inviteReligionName = "";
            }
        }

        ImGui.Spacing();

        // Pending invites (only visible to founder)
        if (_myCivilization.PendingInvites.Count > 0)
        {
            ImGui.Text("Pending Invitations:");
            ImGui.Separator();

            foreach (var invite in _myCivilization.PendingInvites)
            {
                ImGui.PushID(invite.InviteId);
                ImGui.Text($"{invite.ReligionName}");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f),
                    $"(expires {invite.ExpiresAt:yyyy-MM-dd})");
                ImGui.PopID();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        // Leave/Disband buttons
        if (ImGui.Button("Leave Civilization", new Vector2(150f, 32f)))
        {
            _pantheonWarsSystem?.RequestCivilizationAction("leave");
        }

        ImGui.SameLine();
        if (ImGui.Button("Disband Civilization", new Vector2(150f, 32f)))
        {
            _pantheonWarsSystem?.RequestCivilizationAction("disband", _myCivilization.CivId);
        }
    }

    /// <summary>
    ///     Draw Invites tab - view and accept invitations
    /// </summary>
    private void DrawInvitesTab(float contentWidth)
    {
        ImGui.Text("Your Civilization Invitations");
        ImGui.Separator();

        if (_myInvites.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No pending invitations.");
            return;
        }

        foreach (var invite in _myInvites)
        {
            ImGui.PushID(invite.InviteId);

            float cardWidth = contentWidth - 48f; // Match Browse tab padding
            ImGui.BeginChild($"InviteCard_{invite.InviteId}", new Vector2(cardWidth, 60f), true);

            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"Invitation to civilization");
            ImGui.Text($"From: {invite.ReligionName}");
            ImGui.Text($"Expires: {invite.ExpiresAt:yyyy-MM-dd HH:mm}");

            // Position buttons relative to card width
            const float buttonWidth = 80f;
            const float buttonSpacing = 8f;
            const float buttonPadding = 12f;
            ImGui.SameLine(cardWidth - (buttonWidth * 2 + buttonSpacing + buttonPadding));
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 40f);
            if (ImGui.Button("Accept", new Vector2(buttonWidth, 28f)))
            {
                _pantheonWarsSystem?.RequestCivilizationAction("accept", "", invite.InviteId);
            }

            ImGui.SameLine();
            if (ImGui.Button("Decline", new Vector2(buttonWidth, 28f)))
            {
                // TODO: Add decline action
                _capi?.ShowChatMessage("Decline invitation functionality coming soon!");
            }

            ImGui.EndChild();
            ImGui.PopID();

            ImGui.Spacing();
        }
    }

    /// <summary>
    ///     Draw Create tab - form to create new civilization
    /// </summary>
    private void DrawCreateTab()
    {
        ImGui.Text("Create a New Civilization");
        ImGui.Separator();

        ImGui.Spacing();
        ImGui.Text("Requirements:");
        ImGui.BulletText("You must be a religion founder");
        ImGui.BulletText("Your religion must not be in another civilization");
        ImGui.BulletText("Name must be 3-32 characters");
        ImGui.BulletText("No cooldowns active");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Civilization Name:");
        ImGui.InputText("##CivName", ref _createCivName, 32);

        ImGui.Spacing();

        if (ImGui.Button("Create Civilization", new Vector2(180f, 36f)))
        {
            if (!string.IsNullOrEmpty(_createCivName) && _createCivName.Length >= 3)
            {
                _pantheonWarsSystem?.RequestCivilizationAction("create", "", "", _createCivName);
                _createCivName = "";
            }
            else
            {
                _capi?.ShowChatMessage("Civilization name must be 3-32 characters.");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear", new Vector2(80f, 36f)))
        {
            _createCivName = "";
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(
            "Once created, you can invite 2-4 religions with different deities to join your civilization. " +
            "Work together to build a powerful alliance!");
    }

    /// <summary>
    ///     Draw footer with action buttons
    /// </summary>
    private void DrawFooter()
    {
        ImGui.Separator();
        ImGui.Spacing();

        // Position buttons at bottom - use available space rather than absolute position
        if (ImGui.Button("Refresh Data", new Vector2(120f, 32f)))
        {
            RefreshData();
        }

        ImGui.SameLine(WindowWidth - 152f); // 120 (button width) + 32 (padding)
        if (ImGui.Button("Close", new Vector2(120f, 32f)))
        {
            Close();
        }
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

    private void OnCivilizationListReceived(CivilizationListResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] CivDialog received {packet.Civilizations.Count} civilizations");
        _allCivilizations = packet.Civilizations;
    }

    private void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] CivDialog received civilization info: {packet.Details?.Name ?? "null"}");
        _myCivilization = packet.Details;
    }

    private void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
    {
        _capi!.Logger.Debug($"[PantheonWars] CivDialog action completed: {packet.Success}");
        if (packet.Success)
        {
            // Refresh data after successful action
            RefreshData();

            // Switch to appropriate tab
            if (packet.Message.Contains("created") || packet.Message.Contains("joined"))
            {
                _currentTab = 1; // My Civilization tab
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_pantheonWarsSystem != null)
        {
            _pantheonWarsSystem.CivilizationListReceived -= OnCivilizationListReceived;
            _pantheonWarsSystem.CivilizationInfoReceived -= OnCivilizationInfoReceived;
            _pantheonWarsSystem.CivilizationActionCompleted -= OnCivilizationActionCompleted;
        }

        _capi?.Logger.Notification("[PantheonWars] Civilization Dialog disposed");
    }
}
