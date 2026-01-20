using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Roles;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Pure renderer for the roles browse view (role cards list).
///     Takes an immutable view model, returns events representing user interactions.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionRolesBrowseRenderer
{
    private const float RoleCardHeight = 160f;
    private const float RoleCardSpacing = 12f;
    private const float ScrollbarWidth = 16f;

    /// <summary>
    ///     Renders the roles browse view (role cards list).
    ///     Pure function: ViewModel + DrawList → RenderResult
    /// </summary>
    public static ReligionRolesBrowseRenderResult Draw(
        ReligionRolesBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<RolesBrowseEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var currentY = y;

        // Loading state
        if (viewModel.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_LOADING),
                x, currentY + 8f, width);
            return new ReligionRolesBrowseRenderResult(events, height);
        }

        if (!viewModel.HasReligion)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_NOT_IN_RELIGION),
                x, currentY + 8f, width);
            return new ReligionRolesBrowseRenderResult(events, height);
        }

        if (!viewModel.HasRolesData)
        {
            var errorMsg = viewModel.RolesData?.ErrorMessage ??
                           LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_FAILED);
            TextRenderer.DrawInfoText(drawList, errorMsg, x, currentY + 8f, width);
            return new ReligionRolesBrowseRenderResult(events, height);
        }

        // Compute content height
        var contentHeight = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeight - height);

        // Mouse wheel scroll
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = viewModel.ScrollY;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new RolesBrowseEvent.ScrollChanged(newScrollY));
                }
            }
        }

        // Clip to visible area and offset drawing by scroll
        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);
        currentY = y - scrollY;

        // Header
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_TITLE),
            x, currentY, 16f, ColorPalette.Gold);
        currentY += 30f;

        // Create Role button (if player can manage roles)
        if (viewModel.CanManageRoles())
        {
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_CREATE_BUTTON),
                    x, currentY, 200f, 32f))
                events.Add(new RolesBrowseEvent.CreateRoleOpen());
            currentY += 42f;
        }

        // Render each role
        var roles = viewModel.Roles.OrderBy(r => r.DisplayOrder).ThenBy(r => r.RoleName).ToList();
        foreach (var role in roles)
        {
            currentY = DrawRoleCard(drawList, viewModel, role, x, currentY, width - ScrollbarWidth, events);
            currentY += RoleCardSpacing;
        }

        drawList.PopClipRect();

        // Render overlays
        if (viewModel.ShowCreateRoleDialog) DrawCreateRoleDialog(viewModel, drawList, events);

        if (viewModel.ShowRoleEditor) DrawRoleEditor(viewModel, drawList, events);

        if (viewModel.ShowDeleteConfirm) DrawDeleteConfirmation(viewModel, drawList, events);

        return new ReligionRolesBrowseRenderResult(events, contentHeight);
    }

    private static float DrawRoleCard(
        ImDrawListPtr drawList,
        ReligionRolesBrowseViewModel viewModel,
        RoleData role,
        float x,
        float y,
        float width,
        List<RolesBrowseEvent> events)
    {
        var cardHeight = RoleCardHeight;
        var padding = 12f;

        // Card background
        var cardStart = new Vector2(x, y);
        var cardEnd = new Vector2(x + width, y + cardHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.6f);
        var borderColor =
            ImGui.ColorConvertFloat4ToU32(role.IsDefault ? ColorPalette.Gold * 0.5f : ColorPalette.LightBrown * 0.7f);
        drawList.AddRectFilled(cardStart, cardEnd, bgColor, 4f);
        drawList.AddRect(cardStart, cardEnd, borderColor, 4f, ImDrawFlags.None, 1.5f);

        var currentY = y + padding;
        var contentX = x + padding;
        var contentWidth = width - padding * 2;

        // Role name header
        var roleNameColor = role.IsProtected ? ColorPalette.Gold : ColorPalette.White;
        TextRenderer.DrawLabel(drawList, role.RoleName, contentX, currentY, 14f, roleNameColor);

        // Member count badge
        var memberCount = viewModel.GetMemberCount(role.RoleUID);
        var badge = LocalizationService.Instance.Get(
            LocalizationKeys.UI_RELIGION_ROLES_MEMBER_COUNT,
            memberCount,
            memberCount != 1 ? "s" : "");
        var badgeSize = ImGui.CalcTextSize(badge);
        var badgeX = x + width - padding - badgeSize.X - 8f;
        var badgeY = currentY;
        var badgeRect = new Vector2(badgeX - 4f, badgeY - 2f);
        var badgeRectEnd = new Vector2(badgeX + badgeSize.X + 4f, badgeY + badgeSize.Y + 2f);
        drawList.AddRectFilled(badgeRect, badgeRectEnd, ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.5f),
            2f);
        drawList.AddText(new Vector2(badgeX, badgeY), ImGui.ColorConvertFloat4ToU32(ColorPalette.White), badge);

        currentY += 24f;

        // Protection/default status
        if (role.IsProtected)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_SYSTEM_ROLE),
                contentX, currentY, contentWidth);
            currentY += 16f;
        }
        else if (role.IsDefault)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_DEFAULT_ROLE),
                contentX, currentY, contentWidth);
            currentY += 16f;
        }

        // Permissions summary
        var permCount = role.Permissions.Count;
        var permText = role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_ALL_PERMISSIONS)
            : LocalizationService.Instance.Get(
                LocalizationKeys.UI_RELIGION_ROLES_PERMISSIONS_COUNT,
                permCount,
                permCount != 1 ? "s" : "");

        if (permCount > 0 && permCount <= 3 && role.RoleUID != RoleDefaults.FOUNDER_ROLE_ID)
        {
            // Show first 3 permissions
            foreach (var perm in role.Permissions.Take(3))
            {
                var displayName = RolePermissions.GetDisplayName(perm);
                TextRenderer.DrawInfoText(drawList, $"+ {displayName}", contentX, currentY, contentWidth);
                currentY += 16f;
            }
        }
        else
        {
            TextRenderer.DrawInfoText(drawList, permText, contentX, currentY, contentWidth);
            currentY += 20f;
        }

        // Action buttons
        var buttonY = y + cardHeight - padding - 32f;
        var buttonX = contentX;
        var buttonWidth = 120f;
        var buttonSpacing = 8f;

        // View Details button (CHANGED from ViewRoleMembersOpen to ViewRoleDetailsClicked)
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_VIEW_DETAILS),
                buttonX, buttonY, buttonWidth, 28f))
            events.Add(new RolesBrowseEvent.ViewRoleDetailsClicked(role.RoleUID, role.RoleName));
        buttonX += buttonWidth + buttonSpacing;

        // Edit button (if can manage and not founder)
        if (viewModel.CanEditRole(role))
        {
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_EDIT),
                    buttonX, buttonY, buttonWidth, 28f))
                events.Add(new RolesBrowseEvent.EditRoleOpen(role.RoleUID));
            buttonX += buttonWidth + buttonSpacing;
        }

        // Delete button (if can delete)
        if (viewModel.CanDeleteRole(role))
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_DELETE),
                    buttonX, buttonY, buttonWidth, 28f, false, true,
                    ColorPalette.Red * 0.8f))
                events.Add(new RolesBrowseEvent.DeleteRoleOpen(role.RoleUID, role.RoleName));

        return y + cardHeight;
    }

    private static void DrawCreateRoleDialog(
        ReligionRolesBrowseViewModel viewModel,
        ImDrawListPtr drawList,
        List<RolesBrowseEvent> events)
    {
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        // Backdrop
        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f));

        // Dialog
        var dialogWidth = 400f;
        var dialogHeight = 200f;
        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;

        drawList.AddRectFilled(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.95f), 6f);
        drawList.AddRect(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f), 6f, ImDrawFlags.None, 2f);

        var padding = 16f;
        var currentY = dlgY + padding;

        // Title
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_CREATE_DIALOG_TITLE), dlgX + padding,
            currentY, 15f, ColorPalette.Gold);
        currentY += 30f;

        // Role name input
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_NAME_LABEL), dlgX + padding, currentY,
            13f, ColorPalette.White);
        currentY += 20f;

        // Actual text input using ImGui
        var inputX = dlgX + padding;
        var inputWidth = dialogWidth - padding * 2;

        ImGui.SetCursorScreenPos(new Vector2(inputX, currentY));
        ImGui.PushItemWidth(inputWidth);

        var tempName = viewModel.NewRoleName ?? string.Empty;
        if (ImGui.InputText("##newRoleName", ref tempName, 100))
            events.Add(new RolesBrowseEvent.CreateRoleNameChanged(tempName));

        ImGui.PopItemWidth();
        currentY += 32f + 20f;

        // Buttons
        var btnWidth = 120f;
        var btnHeight = 32f;
        var btnY = dlgY + dialogHeight - padding - btnHeight;
        var btnSpacing = 10f;
        var btn2X = dlgX + dialogWidth - padding - btnWidth;
        var btn1X = btn2X - btnWidth - btnSpacing;

        if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                btn1X, btnY, btnWidth, btnHeight))
            events.Add(new RolesBrowseEvent.CreateRoleCancel());

        var canCreate = !string.IsNullOrWhiteSpace(viewModel.NewRoleName);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_CREATE_BUTTON_TEXT), btn2X, btnY,
                btnWidth, btnHeight, true, canCreate))
            events.Add(new RolesBrowseEvent.CreateRoleConfirm(viewModel.NewRoleName));
    }

    private static void DrawRoleEditor(
        ReligionRolesBrowseViewModel viewModel,
        ImDrawListPtr drawList,
        List<RolesBrowseEvent> events)
    {
        if (viewModel.EditingRoleUID == null) return;

        var role = viewModel.Roles.FirstOrDefault(r => r.RoleUID == viewModel.EditingRoleUID);
        if (role == null) return;

        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        // Backdrop
        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f));

        // Dialog
        var dialogWidth = 500f;
        var dialogHeight = 600f;
        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;

        drawList.AddRectFilled(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.95f), 6f);
        drawList.AddRect(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f), 6f, ImDrawFlags.None, 2f);

        var padding = 16f;
        var currentY = dlgY + padding;

        // Title
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_EDIT_TITLE, role.RoleName),
            dlgX + padding, currentY, 15f,
            ColorPalette.Gold);
        currentY += 30f;

        // Role name (non-editable for founder, editable for others)
        if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_FOUNDER_NO_EDIT), dlgX + padding,
                currentY,
                dialogWidth - padding * 2);
            currentY += 20f;
        }
        else
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_NAME_LABEL), dlgX + padding,
                currentY, 13f, ColorPalette.White);
            currentY += 20f;

            var inputX = dlgX + padding;
            var inputWidth = dialogWidth - padding * 2;

            ImGui.SetCursorScreenPos(new Vector2(inputX, currentY));
            ImGui.PushItemWidth(inputWidth);

            var tempName = viewModel.EditingRoleName ?? string.Empty;
            if (ImGui.InputText("##editRoleName", ref tempName, 100))
                events.Add(new RolesBrowseEvent.EditRoleNameChanged(tempName));

            ImGui.PopItemWidth();
            currentY += 32f + 20f;
        }

        // Permissions section
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_PERMISSIONS_LABEL), dlgX + padding,
            currentY, 14f, ColorPalette.Gold);
        currentY += 25f;

        // Permission checkboxes
        foreach (var perm in RolePermissions.AllPermissions)
        {
            var isEnabled = viewModel.EditingPermissions.Contains(perm);
            var displayName = RolePermissions.GetDisplayName(perm);
            var description = RolePermissions.GetDescription(perm);

            var checkboxX = dlgX + padding;
            var checkboxSize = 16f;

            // Checkbox
            var checkboxRect = new Vector2(checkboxX, currentY);
            var checkboxRectEnd = new Vector2(checkboxX + checkboxSize, currentY + checkboxSize);
            drawList.AddRect(checkboxRect, checkboxRectEnd, ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f),
                2f);

            if (isEnabled)
                drawList.AddRectFilled(new Vector2(checkboxX + 3f, currentY + 3f),
                    new Vector2(checkboxX + checkboxSize - 3f, currentY + checkboxSize - 3f),
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));

            // Check for click
            var mousePos = ImGui.GetMousePos();
            var isHovering = mousePos.X >= checkboxX && mousePos.X <= checkboxX + checkboxSize &&
                             mousePos.Y >= currentY && mousePos.Y <= currentY + checkboxSize;

            if (isHovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                events.Add(new RolesBrowseEvent.EditRolePermissionToggled(perm, !isEnabled));

            // Label
            drawList.AddText(new Vector2(checkboxX + checkboxSize + 8f, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), displayName);

            currentY += 22f;
        }

        currentY += 10f;

        // Buttons
        var btnWidth = 120f;
        var btnHeight = 32f;
        var btnY = dlgY + dialogHeight - padding - btnHeight;
        var btnSpacing = 10f;
        var btn2X = dlgX + dialogWidth - padding - btnWidth;
        var btn1X = btn2X - btnWidth - btnSpacing;

        if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                btn1X, btnY, btnWidth, btnHeight))
            events.Add(new RolesBrowseEvent.EditRoleCancel());

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_SAVE_BUTTON), btn2X, btnY, btnWidth,
                btnHeight, true))
            events.Add(new RolesBrowseEvent.EditRoleSave(role.RoleUID, viewModel.EditingRoleName,
                viewModel.EditingPermissions));
    }

    private static void DrawDeleteConfirmation(
        ReligionRolesBrowseViewModel viewModel,
        ImDrawListPtr drawList,
        List<RolesBrowseEvent> events)
    {
        ConfirmOverlay.Draw(
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_DELETE_CONFIRM_TITLE),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_DELETE_CONFIRM_MESSAGE,
                viewModel.DeleteRoleName ?? "Unknown"),
            out var confirmed,
            out var canceled,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_DELETE_CONFIRM_BUTTON));

        if (confirmed && viewModel.DeleteRoleUID != null)
            events.Add(new RolesBrowseEvent.DeleteRoleConfirm(viewModel.DeleteRoleUID));
        else if (canceled) events.Add(new RolesBrowseEvent.DeleteRoleCancel());
    }

    private static float ComputeContentHeight(ReligionRolesBrowseViewModel viewModel)
    {
        var height = 80f; // Header + padding

        if (viewModel.CanManageRoles()) height += 42f; // Create button

        var roleCount = viewModel.Roles.Count;
        height += roleCount * (RoleCardHeight + RoleCardSpacing);

        return height;
    }
}