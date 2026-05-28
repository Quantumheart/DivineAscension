using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.HolySites;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Renderers.HolySites;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the civilization holy-sites chapter (browse table and per-site detail)
///     and reduces both their event streams. Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationHolySitesPresenter(CivilizationStateManager owner)
{
    [ExcludeFromCodeCoverage]
    public void Draw(float x, float y, float width, float height)
    {
        // Check if viewing detail
        if (!string.IsNullOrEmpty(owner.State.HolySitesState.Detail.ViewingSiteUID))
        {
            DrawDetail(x, y, width, height);
            return;
        }

        // Otherwise show browse table
        DrawBrowse(x, y, width, height);
    }

    [ExcludeFromCodeCoverage]
    private void DrawBrowse(float x, float y, float width, float height)
    {
        var religionNames = owner.CivilizationMemberReligions?
            .ToDictionary(r => r.ReligionId, r => r.ReligionName)
            ?? new Dictionary<string, string>();

        var religionDomains = owner.CivilizationMemberReligions?
            .ToDictionary(r => r.ReligionId, r => r.Domain)
            ?? new Dictionary<string, string>();

        var vm = new CivilizationHolySitesViewModel(
            owner.State.HolySitesState.Browse.SitesByReligion,
            religionNames,
            religionDomains,
            owner.State.HolySitesState.Browse.ExpandedReligions,
            owner.CurrentCivilizationName,
            owner.State.HolySitesState.Browse.IsLoading,
            owner.State.HolySitesState.Browse.ErrorMsg,
            owner.State.HolySitesState.Browse.ScrollY,
            x, y, width, height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationHolySitesRenderer.Draw(vm, drawList);
        ProcessEvents(result.Events);
    }

    [ExcludeFromCodeCoverage]
    private void DrawDetail(float x, float y, float width, float height)
    {
        var siteDetails = owner.State.HolySitesState.Detail.ViewingSiteDetails;
        if (siteDetails == null)
        {
            // Show loading state
            ImGui.SetCursorPos(new System.Numerics.Vector2(x, y));
            ImGui.Text("Loading holy site details...");
            return;
        }

        var vm = new DivineAscension.GUI.Models.HolySite.Detail.HolySiteDetailViewModel(
            x, y, width, height,
            siteDetails,
            owner.ClientApi.World.Player.PlayerUID,
            owner.State.HolySitesState.Detail.IsEditingName,
            owner.State.HolySitesState.Detail.EditingNameValue,
            owner.State.HolySitesState.Detail.IsEditingDescription,
            owner.State.HolySitesState.Detail.EditingDescriptionValue,
            owner.State.HolySitesState.Detail.IsLoading,
            owner.State.HolySitesState.Detail.ErrorMsg);

        var drawList = ImGui.GetWindowDrawList();
        var result = HolySiteDetailRenderer.Draw(vm, drawList);
        ProcessDetailEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<HolySitesEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case HolySitesEvent.RefreshClicked:
                    owner.RequestCivilizationHolySites();
                    break;

                case HolySitesEvent.ScrollChanged scroll:
                    owner.State.HolySitesState.Browse.ScrollY = scroll.NewScrollY;
                    break;

                case HolySitesEvent.ReligionToggled toggle:
                    if (!owner.State.HolySitesState.Browse.ExpandedReligions.Add(toggle.ReligionUID))
                        owner.State.HolySitesState.Browse.ExpandedReligions.Remove(toggle.ReligionUID);
                    break;

                case HolySitesEvent.SiteSelected selected:
                    owner.State.HolySitesState.Browse.SelectedSiteUID = selected.SiteUID;
                    owner.State.HolySitesState.Detail.ViewingSiteUID = selected.SiteUID;
                    RequestHolySiteDetail(selected.SiteUID);
                    break;
            }
        }
    }

    public void ProcessDetailEvents(IReadOnlyList<Events.HolySite.DetailEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case Events.HolySite.DetailEvent.BackToBrowseClicked:
                    // Navigate back to browse view
                    owner.State.HolySitesState.Detail.ViewingSiteUID = null;
                    owner.State.HolySitesState.Detail.ViewingSiteDetails = null;
                    owner.State.HolySitesState.Detail.IsEditingName = false;
                    owner.State.HolySitesState.Detail.IsEditingDescription = false;
                    break;

                case Events.HolySite.DetailEvent.MarkClicked:
                    HandleMarkWaypoint();
                    break;

                case Events.HolySite.DetailEvent.RenameClicked:
                    owner.State.HolySitesState.Detail.IsEditingName = true;
                    owner.State.HolySitesState.Detail.EditingNameValue =
                        owner.State.HolySitesState.Detail.ViewingSiteDetails?.SiteName ?? "";
                    break;

                case Events.HolySite.DetailEvent.RenameValueChanged valueChanged:
                    // Update the editing value as the user types
                    owner.State.HolySitesState.Detail.EditingNameValue = valueChanged.NewValue;
                    break;

                case Events.HolySite.DetailEvent.RenameSave save:
                    SendRenameRequest(
                        owner.State.HolySitesState.Detail.ViewingSiteUID!,
                        save.NewName);
                    break;

                case Events.HolySite.DetailEvent.RenameCancel:
                    owner.State.HolySitesState.Detail.IsEditingName = false;
                    owner.State.HolySitesState.Detail.EditingNameValue = null;
                    break;

                case Events.HolySite.DetailEvent.EditDescriptionClicked:
                    owner.State.HolySitesState.Detail.IsEditingDescription = true;
                    owner.State.HolySitesState.Detail.EditingDescriptionValue =
                        owner.State.HolySitesState.Detail.ViewingSiteDetails?.Description ?? "";
                    break;

                case Events.HolySite.DetailEvent.DescriptionValueChanged descValueChanged:
                    // Update the editing value as the user types
                    owner.State.HolySitesState.Detail.EditingDescriptionValue = descValueChanged.NewValue;
                    break;

                case Events.HolySite.DetailEvent.DescriptionSave save:
                    SendDescriptionUpdateRequest(
                        owner.State.HolySitesState.Detail.ViewingSiteUID!,
                        save.Description);
                    break;

                case Events.HolySite.DetailEvent.DescriptionCancel:
                    owner.State.HolySitesState.Detail.IsEditingDescription = false;
                    owner.State.HolySitesState.Detail.EditingDescriptionValue = null;
                    break;

                case Events.HolySite.DetailEvent.StartRitualClicked startRitual:
                    SendStartRitualRequest(
                        owner.State.HolySitesState.Detail.ViewingSiteUID!,
                        startRitual.TargetTier);
                    break;

                case Events.HolySite.DetailEvent.CancelRitualClicked:
                    SendCancelRitualRequest(
                        owner.State.HolySitesState.Detail.ViewingSiteUID!);
                    break;
            }
        }
    }

    private void RequestHolySiteDetail(string siteUID)
    {
        owner.State.HolySitesState.Detail.IsLoading = true;
        owner.UiService.RequestHolySiteDetail(siteUID);
    }

    private void SendRenameRequest(string siteUID, string newName)
    {
        owner.UiService.UpdateHolySite("rename", siteUID, newName);
    }

    private void SendDescriptionUpdateRequest(string siteUID, string description)
    {
        owner.UiService.UpdateHolySite("edit_description", siteUID, description);
    }

    private void SendStartRitualRequest(string siteUID, int targetTier)
    {
        owner.UiService.RequestStartRitual(siteUID, targetTier);
    }

    private void SendCancelRitualRequest(string siteUID)
    {
        owner.UiService.RequestCancelRitual(siteUID);
    }

    private void HandleMarkWaypoint()
    {
        var site = owner.State.HolySitesState.Detail.ViewingSiteDetails;
        if (site == null) return;

        try
        {
            // Get named color for the waypoint based on domain
            var colorName = GetDomainColorName(site.Domain);

            // Convert absolute world coordinates to relative coordinates (relative to spawn)
            // Vintage Story's /waypoint addat command expects coordinates relative to spawn
            var spawnPos = owner.ClientApi.World.DefaultSpawnPosition;
            var relativeX = site.Center.X - (int)spawnPos.X;
            var relativeY = site.Center.Y;
            var relativeZ = site.Center.Z - (int)spawnPos.Z;

            // Use /waypoint addati command with relative coordinates and custom icon
            // Format: /waypoint addati [icon] [x] [y] [z] [pinned] [color] [title]
            // Using star1 icon for holy sites (more distinctive than circle)
            var waypointCommand = $"/waypoint addati star1 {relativeX} {relativeY} {relativeZ} false {colorName} {site.SiteName}";

            owner.ClientApi.Logger.Debug($"[DivineAscension] Absolute coords: ({site.Center.X},{site.Center.Y},{site.Center.Z}), Spawn: ({(int)spawnPos.X},{(int)spawnPos.Y},{(int)spawnPos.Z}), Relative: ({relativeX},{relativeY},{relativeZ})");
            owner.ClientApi.Logger.Debug($"[DivineAscension] Sending waypoint command: {waypointCommand}");
            owner.ClientApi.SendChatMessage(waypointCommand);

            // Show success message to player
            var successMessage = LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_WAYPOINT_ADDED)
                .Replace("{0}", site.SiteName);
            owner.ClientApi.ShowChatMessage(successMessage);
        }
        catch (System.Exception ex)
        {
            owner.ClientApi.Logger.Error($"[DivineAscension] Failed to add waypoint: {ex.Message}");
            owner.ClientApi.ShowChatMessage("Failed to add waypoint. Please try again.");
        }
    }

    /// <summary>
    /// Gets a hex color from Vintage Story's 36 supported colors based on deity domain
    /// These are the exact hex values from WaypointMapLayer.hexcolors array
    /// Note: Vintage Story's color parser accepts colors WITH the # prefix
    /// </summary>
    private static string GetDomainColorName(string domain)
    {
        return domain switch
        {
            "Craft" => "red",           // Use named color - more reliable
            "Wild" => "green",          // Use named color
            "Conquest" => "crimson",    // Use named color
            "Harvest" => "orange",      // Use named color
            "Stone" => "saddlebrown",   // Use named color
            _ => "gray"                 // Use named color
        };
    }
}
