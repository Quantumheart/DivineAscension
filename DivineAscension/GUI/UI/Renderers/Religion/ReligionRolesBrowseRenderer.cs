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
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for the "Vestments" ledger chapter (#318). Founder-only:
/// chapter title + prose intro, two-line ledger row per role (diamond + name,
/// optional badge, dotted leader, "N wear" count, pencil edit affordance),
/// then an inscribe-new-vestment form. The Strike (delete) action lives
/// inside the edit overlay rather than on the row.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionRolesBrowseRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float RowLine1Height = 24f;
    private const float RowLine2Height = 22f;
    private const float RowBottomSpacing = 10f;
    private const float RowHeight = RowLine1Height + RowLine2Height + RowBottomSpacing;
    private const float PencilSize = 22f;
    private const float BadgeGap = 10f;
    private const float DiamondInset = 6f;
    private const float NameInset = 16f;
    private const float ScrollbarWidth = 16f;
    private const float InscribeLabelHeight = 22f;
    private const float InscribeInputHeight = 30f;
    private const float InscribeButtonHeight = 30f;
    private const float InscribeButtonWidth = 140f;
    private const float InscribeGap = 8f;
    private const string LeaderDot = "·";

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

        if (viewModel.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_LOADING),
                x, currentY + 8f, width, Secondary, ColorPalette.Grey);
            return new ReligionRolesBrowseRenderResult(events, height);
        }

        if (!viewModel.HasReligion)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_NOT_IN_RELIGION),
                x, currentY + 8f, width, Secondary, ColorPalette.Grey);
            return new ReligionRolesBrowseRenderResult(events, height);
        }

        if (!viewModel.HasRolesData)
        {
            var errorMsg = viewModel.RolesData?.ErrorMessage ??
                           LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_FAILED);
            TextRenderer.DrawInfoText(drawList, errorMsg, x, currentY + 8f, width, Secondary, ColorPalette.Grey);
            return new ReligionRolesBrowseRenderResult(events, height);
        }

        var contentHeight = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeight - height);

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

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_TAB_ROLES));
        var contentWidth = strip.ContentWidth;
        currentY = strip.BodyY;

        // Prose intro on parchment → iron-gall ink.
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_CHAPTER_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, x, currentY, contentWidth, Body, ColorPalette.White);
        currentY += MathF.Max(TextRenderer.MeasureWrappedHeight(intro, contentWidth, Body), 20f) + 8f;

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        var roles = viewModel.Roles.OrderBy(r => r.DisplayOrder).ThenBy(r => r.RoleName).ToList();
        foreach (var role in roles)
        {
            currentY = DrawRoleRow(drawList, viewModel, role, x, currentY, contentWidth, events);
        }

        if (viewModel.CanManageRoles())
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawInscribeForm(drawList, viewModel, x, currentY, contentWidth, events);
        }

        drawList.PopClipRect();

        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        if (viewModel.ShowRoleEditor) DrawRoleEditor(viewModel, drawList, events);
        if (viewModel.ShowDeleteConfirm) DrawDeleteConfirmation(viewModel, drawList, events);

        return new ReligionRolesBrowseRenderResult(events, contentHeight);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawRoleRow(
        ImDrawListPtr drawList,
        ReligionRolesBrowseViewModel viewModel,
        RoleData role,
        float x, float y, float width,
        List<RolesBrowseEvent> events)
    {
        var line1Y = y;
        var midY = line1Y + RowLine1Height / 2f;

        // Diamond ornament + role name. Founder gets rubric ink; default ink for others.
        var nameColor = role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID
            ? ColorPalette.Vermilion
            : ColorPalette.White;
        ChromeRenderer.DrawDiamond(drawList, x + DiamondInset, midY, 4f, ColorPalette.Gold);
        TextRenderer.DrawLabel(drawList, role.RoleName, x + NameInset, line1Y + 2f, Body, nameColor);
        var nameWidth = ImGui.CalcTextSize(role.RoleName).X;

        // Optional badge after the name — protected (rubric), default (gold leaf).
        var badgeStart = x + NameInset + nameWidth + BadgeGap;
        var badgeEnd = badgeStart;
        if (role.IsProtected)
        {
            var badge = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_BADGE_PROTECTED);
            ChromeRenderer.DrawDiamond(drawList, badgeStart + 4f, midY, 3f, ColorPalette.Vermilion);
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(badgeStart + 12f, line1Y + 4f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Vermilion), badge);
            badgeEnd = badgeStart + 12f + ImGui.CalcTextSize(badge).X * (Secondary / SubsectionLabel);
        }
        else if (role.IsDefault)
        {
            var badge = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_BADGE_DEFAULT);
            ChromeRenderer.DrawDiamond(drawList, badgeStart + 4f, midY, 3f, ColorPalette.Gold);
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(badgeStart + 12f, line1Y + 4f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), badge);
            badgeEnd = badgeStart + 12f + ImGui.CalcTextSize(badge).X * (Secondary / SubsectionLabel);
        }

        // Right side: pencil (if editable) then wear count, dotted leader fills the gap.
        var canEdit = viewModel.CanEditRole(role);
        var rightCursor = x + width;
        if (canEdit)
        {
            var px = rightCursor - PencilSize;
            var py = line1Y + (RowLine1Height - PencilSize) / 2f;
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    px, py, PencilSize, PencilSize, isPrimary: false, enabled: true))
            {
                events.Add(new RolesBrowseEvent.EditRoleOpen(role.RoleUID));
            }
            ChromeRenderer.DrawPencil(drawList,
                px + PencilSize / 2f, py + PencilSize / 2f, PencilSize - 8f,
                ColorPalette.LightText);
            rightCursor = px - 8f;
        }

        var memberCount = viewModel.GetMemberCount(role.RoleUID);
        var wearText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_WEAR_COUNT, memberCount);
        var wearSize = ImGui.CalcTextSize(wearText);
        var wearX = rightCursor - wearSize.X;
        drawList.AddText(new Vector2(wearX, line1Y + 4f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), wearText);

        DrawDots(drawList, badgeEnd + 6f, wearX - 6f, line1Y + 4f);

        // Line 2 — auto-phrased permission summary, indented under the name.
        var summary = RolePermissionPhrases.BuildSummary(role.RoleUID, role.Permissions);
        TextRenderer.DrawInfoText(drawList, summary,
            x + NameInset, line1Y + RowLine1Height, width - NameInset,
            Secondary, ColorPalette.Grey);

        return y + RowHeight;
    }

    private static void DrawDots(ImDrawListPtr drawList, float startX, float endX, float y)
    {
        var widthAvail = endX - startX;
        if (widthAvail <= 0f) return;
        var dotWidth = ImGui.CalcTextSize(LeaderDot).X;
        if (dotWidth <= 0f) return;
        var step = dotWidth * 2f;
        var dotCount = (int)(widthAvail / step);
        if (dotCount <= 0) return;
        var dotsTextWidth = dotCount * step - (step - dotWidth);
        var dotsX = startX + (widthAvail - dotsTextWidth) / 2f;
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.45f);
        for (var i = 0; i < dotCount; i++)
            drawList.AddText(new Vector2(dotsX + i * step, y), color, LeaderDot);
    }

    private static float DrawInscribeForm(
        ImDrawListPtr drawList,
        ReligionRolesBrowseViewModel viewModel,
        float x, float y, float width,
        List<RolesBrowseEvent> events)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_INSCRIBE_LABEL),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += InscribeLabelHeight;

        var inputWidth = width - InscribeButtonWidth - InscribeGap;
        ImGui.SetCursorScreenPos(new Vector2(x, currentY));
        ImGui.PushItemWidth(inputWidth);
        var tempName = viewModel.NewRoleName ?? string.Empty;
        if (ImGui.InputTextWithHint("##inscribeVestment", LocalizationService.Instance
                    .Get(LocalizationKeys.UI_RELIGION_ROLES_INSCRIBE_PLACEHOLDER),
                ref tempName, 100))
        {
            events.Add(new RolesBrowseEvent.CreateRoleNameChanged(tempName));
        }
        ImGui.PopItemWidth();

        var canCreate = !string.IsNullOrWhiteSpace(viewModel.NewRoleName);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_INSCRIBE_BUTTON),
                x + width - InscribeButtonWidth, currentY,
                InscribeButtonWidth, InscribeButtonHeight,
                isPrimary: true, enabled: canCreate))
        {
            events.Add(new RolesBrowseEvent.CreateRoleConfirm(viewModel.NewRoleName));
        }

        currentY += InscribeInputHeight + 8f;
        return currentY;
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

        // Warm-dark page dim, per palette §4.
        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay));

        const float dialogWidth = 500f;
        const float dialogHeight = 600f;
        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;

        // Parchment mini-page surface with a faded-ink border.
        drawList.AddRectFilled(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Background), 6f);
        drawList.AddRect(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 6f, ImDrawFlags.None, 1.5f);

        const float padding = 18f;
        var bodyWidth = dialogWidth - padding * 2;
        var currentY = dlgY + padding;

        // Title strip — serif gold at PageTitle, ornamental divider below.
        var title = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_EDIT_TITLE, role.RoleName);
        drawList.AddText(ImGui.GetFont(), PageTitle,
            new Vector2(dlgX + padding, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), title);
        currentY += PageTitle + 6f;
        ChromeRenderer.DrawDivider(drawList, dlgX + padding, currentY, bodyWidth);
        currentY += 16f;

        // Name field — ink labels on parchment.
        if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_FOUNDER_NO_EDIT),
                dlgX + padding, currentY, bodyWidth,
                Secondary, ColorPalette.Grey);
            currentY += 24f;
        }
        else
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_NAME_LABEL),
                dlgX + padding, currentY, Body, ColorPalette.Grey);
            currentY += 22f;

            var newName = TextInput.Draw(drawList, "##editRoleName",
                viewModel.EditingRoleName ?? string.Empty,
                dlgX + padding, currentY, bodyWidth, 28f, string.Empty, 100);
            if (newName != (viewModel.EditingRoleName ?? string.Empty))
                events.Add(new RolesBrowseEvent.EditRoleNameChanged(newName));
            currentY += 28f + 14f;
        }

        ChromeRenderer.DrawDivider(drawList, dlgX + padding, currentY, bodyWidth);
        currentY += 16f;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_PERMISSIONS_LABEL),
            dlgX + padding, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += 24f;

        // Permission rows: diamond bullet (filled gold when granted, faded
        // outline when withheld) + display name in ink. Whole row is the
        // hit target so the click area matches the visual row.
        const float rowHeight = 22f;
        const float bulletRadius = 5f;
        var mousePos = ImGui.GetMousePos();
        foreach (var perm in RolePermissions.AllPermissions)
        {
            var isEnabled = viewModel.EditingPermissions.Contains(perm);
            var displayName = RolePermissions.GetDisplayName(perm);

            var rowMinX = dlgX + padding;
            var rowMaxX = dlgX + dialogWidth - padding;
            var rowMidY = currentY + rowHeight / 2f;

            var hovering = mousePos.X >= rowMinX && mousePos.X <= rowMaxX &&
                           mousePos.Y >= currentY && mousePos.Y <= currentY + rowHeight;
            if (hovering)
            {
                drawList.AddRectFilled(new Vector2(rowMinX, currentY),
                    new Vector2(rowMaxX, currentY + rowHeight),
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.12f)),
                    3f);
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    events.Add(new RolesBrowseEvent.EditRolePermissionToggled(perm, !isEnabled));
            }

            if (isEnabled)
            {
                ChromeRenderer.DrawDiamond(drawList,
                    rowMinX + bulletRadius, rowMidY, bulletRadius, ColorPalette.Gold);
            }
            else
            {
                // Hollow diamond — four edges, no fill.
                var cx = rowMinX + bulletRadius;
                var col = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
                var top = new Vector2(cx, rowMidY - bulletRadius);
                var right = new Vector2(cx + bulletRadius, rowMidY);
                var bottom = new Vector2(cx, rowMidY + bulletRadius);
                var left = new Vector2(cx - bulletRadius, rowMidY);
                drawList.AddLine(top, right, col, 1f);
                drawList.AddLine(right, bottom, col, 1f);
                drawList.AddLine(bottom, left, col, 1f);
                drawList.AddLine(left, top, col, 1f);
            }

            var labelColor = isEnabled ? ColorPalette.White : ColorPalette.Grey;
            drawList.AddText(ImGui.GetFont(), Body,
                new Vector2(rowMinX + bulletRadius * 2f + 10f, currentY + 2f),
                ImGui.ColorConvertFloat4ToU32(labelColor), displayName);

            currentY += rowHeight;
        }

        // Footer: Strike † (left, destructive) + Cancel / Save (right). Strike
        // is dagger-marked, no red tint — moved into the overlay per #318.
        const float btnWidth = 120f;
        const float btnHeight = 32f;
        var btnY = dlgY + dialogHeight - padding - btnHeight;

        if (viewModel.CanDeleteRole(role))
        {
            const float strikeWidth = 170f;
            var strikeLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_STRIKE_BUTTON);
            if (ButtonRenderer.DrawButton(drawList, strikeLabel,
                    dlgX + padding, btnY, strikeWidth, btnHeight,
                    isPrimary: false, enabled: true))
            {
                events.Add(new RolesBrowseEvent.DeleteRoleOpen(role.RoleUID, role.RoleName));
            }

            var labelSize = ImGui.CalcTextSize(strikeLabel);
            var labelRightX = dlgX + padding + (strikeWidth + labelSize.X) / 2f;
            var daggerCx = labelRightX + 8f;
            var maxDaggerCx = dlgX + padding + strikeWidth - 14f / 2f - 4f;
            if (daggerCx > maxDaggerCx) daggerCx = maxDaggerCx;
            ChromeRenderer.DrawDagger(drawList, daggerCx, btnY + btnHeight / 2f, 14f, ColorPalette.LightText);
        }

        var btn2X = dlgX + dialogWidth - padding - btnWidth;
        var btn1X = btn2X - btnWidth - 10f;

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                btn1X, btnY, btnWidth, btnHeight))
            events.Add(new RolesBrowseEvent.EditRoleCancel());

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_SAVE_BUTTON),
                btn2X, btnY, btnWidth, btnHeight, isPrimary: true))
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
            out var confirmed, out var canceled,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_DELETE_CONFIRM_BUTTON));

        if (confirmed && viewModel.DeleteRoleUID != null)
            events.Add(new RolesBrowseEvent.DeleteRoleConfirm(viewModel.DeleteRoleUID));
        else if (canceled)
            events.Add(new RolesBrowseEvent.DeleteRoleCancel());
    }

    private static float ComputeContentHeight(ReligionRolesBrowseViewModel viewModel)
    {
        var h = ChapterStripRenderer.TopPadding + PaneHeaderRenderer.TotalHeight;
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROLES_CHAPTER_INTRO);
        var contentWidth = viewModel.Width - ChapterStripRenderer.ScrollbarGutter;
        h += MathF.Max(TextRenderer.MeasureWrappedHeight(intro, contentWidth, Body), 20f) + 8f;
        h += DividerHeight;
        h += viewModel.Roles.Count * RowHeight;
        if (viewModel.CanManageRoles())
        {
            h += DividerHeight;
            h += InscribeLabelHeight + InscribeInputHeight + 8f;
        }
        return h;
    }
}
