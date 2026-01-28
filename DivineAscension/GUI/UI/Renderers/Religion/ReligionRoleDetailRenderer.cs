using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Roles;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Pure renderer for the role detail view (viewing members with a specific role).
///     Takes an immutable view model, returns events representing user interactions.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionRoleDetailRenderer
{
    /// <summary>
    ///     Renders the role detail view (members with a specific role).
    ///     Pure function: ViewModel + DrawList → RenderResult
    /// </summary>
    public static ReligionRoleDetailRenderResult Draw(
        ReligionRoleDetailViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<RoleDetailEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var currentY = y;

        // Back button (like civilization detail view)
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_BACK),
                x, currentY, 160f, 32f))
            events.Add(new RoleDetailEvent.BackToRolesClicked());
        currentY += 42f;

        // Title
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_TITLE, viewModel.ViewingRoleName),
            x, currentY, SectionHeader, ColorPalette.Gold);
        currentY += 30f;

        // Get members with this role
        var membersWithRole = viewModel.GetMembersWithRole();

        if (membersWithRole.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_NO_MEMBERS),
                x, currentY, width);
            return new ReligionRoleDetailRenderResult(events, height);
        }

        // Draw members list with dropdowns
        var dropdownWidth = 180f;
        var dropdownHeight = 28f;
        var assignableRoles = viewModel.GetAssignableRoles();
        string? openDropdownMemberUID = null;

        // Draw all member rows with dropdowns
        foreach (var memberUID in membersWithRole)
        {
            var memberName = viewModel.MemberNames.GetValueOrDefault(memberUID, memberUID);
            var memberRoleUID = viewModel.MemberRoles.GetValueOrDefault(memberUID);
            var memberRoleName = memberRoleUID != null
                ? viewModel.Roles.FirstOrDefault(r => r.RoleUID == memberRoleUID)?.RoleName ??
                  LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_UNKNOWN_ROLE)
                : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_NO_ROLE);

            // Member name on left
            TextRenderer.DrawInfoText(drawList, $"- {memberName}", x, currentY + 4f,
                width - dropdownWidth - 10f, TableHeader);

            // Role dropdown or static text on right
            if (viewModel.CanAssignRoleToMember(memberUID))
            {
                var dropdownX = x + width - dropdownWidth;
                var dropdownY = currentY;
                var isOpen = viewModel.OpenAssignRoleDropdownMemberUID == memberUID;

                // Draw dropdown button with larger font
                if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, dropdownWidth, dropdownHeight,
                        memberRoleName, isOpen, SubsectionLabel))
                    events.Add(new RoleDetailEvent.AssignRoleDropdownToggled(memberUID, !isOpen));

                // Track if this dropdown is open (for menu rendering later)
                if (isOpen) openDropdownMemberUID = memberUID;
            }
            else
            {
                // Static role text for members that can't be changed
                var reason = memberUID == viewModel.CurrentPlayerUID
                    ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_YOUR_ROLE)
                    : memberRoleUID == RoleDefaults.FOUNDER_ROLE_ID
                        ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_FOUNDER_ROLE)
                        : "";
                TextRenderer.DrawInfoText(drawList, memberRoleName + reason,
                    x + width - dropdownWidth - 10f, currentY + 4f, dropdownWidth + 10f);
            }

            currentY += 32f;
        }

        // Draw open dropdown menu (after all buttons for proper z-ordering)
        if (openDropdownMemberUID != null)
        {
            var memberUID = openDropdownMemberUID;
            var dropdownX = x + width - dropdownWidth;
            var memberIndex = membersWithRole.IndexOf(memberUID);
            var dropdownY = y + 42f + 30f + memberIndex * 32f;
            var memberRoleUID = viewModel.MemberRoles.GetValueOrDefault(memberUID);
            var currentRoleIndex = memberRoleUID != null
                ? assignableRoles.ToList().FindIndex(r => r.RoleUID == memberRoleUID)
                : -1;

            var roleNames = assignableRoles.Select(r => r.RoleName).ToArray();

            // Draw menu visual with larger font (pass button position, not menu position - component handles offset)
            Dropdown.DrawMenuVisual(drawList, dropdownX, dropdownY, dropdownWidth,
                dropdownHeight, roleNames, currentRoleIndex, 32f, SubsectionLabel);

            // Handle menu interaction
            var (selectedIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(
                dropdownX, dropdownY, dropdownWidth, dropdownHeight,
                roleNames, currentRoleIndex, 32f);

            if (selectedIndex != currentRoleIndex && selectedIndex >= 0)
            {
                // Role changed - open confirmation dialog
                var newRole = assignableRoles[selectedIndex];
                var memberName = viewModel.MemberNames.TryGetValue(memberUID, out var name) ? name : memberUID;
                events.Add(new RoleDetailEvent.AssignRoleConfirmOpen(memberUID, memberName,
                    memberRoleUID ?? string.Empty, newRole.RoleUID, newRole.RoleName));
            }

            if (shouldClose) events.Add(new RoleDetailEvent.AssignRoleDropdownToggled(memberUID, false));
        }

        // Render confirmation modal overlay (kept as modal)
        if (viewModel.ShowAssignRoleConfirm)
            DrawAssignRoleConfirmation(viewModel, drawList, events);

        return new ReligionRoleDetailRenderResult(events, height);
    }

    private static void DrawAssignRoleConfirmation(
        ReligionRoleDetailViewModel viewModel,
        ImDrawListPtr drawList,
        List<RoleDetailEvent> events)
    {
        // Get role names for confirmation message
        var currentRoleName = !string.IsNullOrEmpty(viewModel.AssignRoleConfirmCurrentRoleUID)
            ? viewModel.Roles.FirstOrDefault(r => r.RoleUID == viewModel.AssignRoleConfirmCurrentRoleUID)?.RoleName ??
              LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_UNKNOWN_ROLE)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_NO_ROLE);

        ConfirmOverlay.Draw(
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_ASSIGN_CONFIRM_TITLE),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_ASSIGN_CONFIRM_MESSAGE,
                viewModel.AssignRoleConfirmMemberName ?? "Unknown", currentRoleName,
                viewModel.AssignRoleConfirmNewRoleName ?? "Unknown"),
            out var confirmed,
            out var canceled,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLE_DETAIL_ASSIGN_CONFIRM_BUTTON));

        if (confirmed)
            events.Add(new RoleDetailEvent.AssignRoleConfirm(
                viewModel.AssignRoleConfirmMemberUID ?? string.Empty,
                viewModel.AssignRoleConfirmNewRoleUID ?? string.Empty));
        else if (canceled) events.Add(new RoleDetailEvent.AssignRoleCancel());
    }
}