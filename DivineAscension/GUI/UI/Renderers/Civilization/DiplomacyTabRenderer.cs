using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Components.Banners;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.GUI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

internal static class DiplomacyTabRenderer
{
    private const float SectionSpacing = 20f;
    private const float HeaderSize = 16f;
    private const float LabelSize = 13f;
    private const float TableRowHeight = 24f;

    public static DiplomacyTabRendererResult Draw(
        DiplomacyTabViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DiplomacyEvent>();
        var currentY = vm.Y;

        // Loading state
        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_LOADING),
                vm.X, currentY + 8f, vm.Width);
            return new DiplomacyTabRendererResult(events, vm.Height);
        }

        // Not in civilization state
        if (!vm.HasCivilization)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_NO_CIVILIZATION),
                vm.X, currentY + 8f, vm.Width);
            return new DiplomacyTabRendererResult(events, vm.Height);
        }

        // Error message display using ErrorBannerRenderer
        if (!string.IsNullOrEmpty(vm.ErrorMessage))
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, vm.X, currentY, vm.Width, vm.ErrorMessage,
                out _, out var dismissClicked, showRetry: false);
            currentY += consumed;

            if (dismissClicked)
            {
                events.Add(new DiplomacyEvent.DismissError());
            }
        }

        // Collect events from panels
        var panelEvents = new List<DiplomacyEvent>();

        // Panel 1: Current Relationships
        currentY = DrawRelationshipsPanel(vm, drawList, currentY, panelEvents);
        currentY += SectionSpacing;

        // Panel 2: Pending Proposals
        currentY = DrawProposalsPanel(vm, drawList, currentY, panelEvents);
        currentY += SectionSpacing;

        // Panel 3: Propose Relationship
        var (finalY, civButtonY, typeButtonY) = DrawProposePanel(vm, drawList, currentY, panelEvents);
        currentY = finalY;

        // Track whether either dropdown consumed a click
        var dropdownConsumedClick = false;

        // Draw dropdown menus AFTER everything else (z-ordering)
        if (vm.IsCivDropdownOpen)
        {
            var civDropdownX = vm.X + 10f;
            var civDropdownY = civButtonY; // Use actual button position
            var civDropdownW = 500f;
            var civDropdownH = 30f;
            var civItems = vm.AvailableCivilizations.Select(c => c.Name).ToArray();
            var selectedCivIndex = vm.AvailableCivilizations.FindIndex(c => c.CivId == vm.SelectedCivId);

            Dropdown.DrawMenuVisual(drawList, civDropdownX, civDropdownY, civDropdownW, civDropdownH, civItems,
                selectedCivIndex);
            var (newCivIndex, shouldCloseCiv, clickConsumedCiv) = Dropdown.DrawMenuAndHandleInteraction(
                civDropdownX, civDropdownY, civDropdownW, civDropdownH, civItems, selectedCivIndex);

            // Track if this dropdown consumed a click
            dropdownConsumedClick = dropdownConsumedClick || clickConsumedCiv;

            if (newCivIndex != selectedCivIndex && newCivIndex >= 0 && newCivIndex < vm.AvailableCivilizations.Count)
            {
                events.Add(new DiplomacyEvent.SelectCivilization(vm.AvailableCivilizations[newCivIndex].CivId));
            }

            if (shouldCloseCiv) events.Add(new DiplomacyEvent.ToggleCivDropdown(false));
        }

        if (vm.IsTypeDropdownOpen)
        {
            var typeDropdownX = vm.X + 10f;
            var typeDropdownY = typeButtonY; // Use actual button position
            var typeDropdownW = 500f;
            var typeDropdownH = 30f;
            var typeItems = new[]
            {
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TYPE_NAP),
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TYPE_ALLIANCE)
            };
            var typeIndex = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact ? 0 : 1;

            Dropdown.DrawMenuVisual(drawList, typeDropdownX, typeDropdownY, typeDropdownW, typeDropdownH, typeItems,
                typeIndex);
            var (newTypeIndex, shouldCloseType, clickConsumedType) = Dropdown.DrawMenuAndHandleInteraction(
                typeDropdownX, typeDropdownY, typeDropdownW, typeDropdownH, typeItems, typeIndex);

            // Track if this dropdown consumed a click
            dropdownConsumedClick = dropdownConsumedClick || clickConsumedType;

            if (newTypeIndex != typeIndex)
            {
                var newType = newTypeIndex == 0 ? DiplomaticStatus.NonAggressionPact : DiplomaticStatus.Alliance;
                events.Add(new DiplomacyEvent.SelectProposalType(newType));
            }

            if (shouldCloseType) events.Add(new DiplomacyEvent.ToggleTypeDropdown(false));
        }

        // Add panel events ONLY if no dropdown consumed the click
        if (!dropdownConsumedClick)
        {
            events.AddRange(panelEvents);
        }

        return new DiplomacyTabRendererResult(events, currentY - vm.Y);
    }

    private static float DrawRelationshipsPanel(
        DiplomacyTabViewModel vm,
        ImDrawListPtr drawList,
        float startY,
        List<DiplomacyEvent> events)
    {
        var currentY = startY;

        // Section header
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CURRENT_RELATIONSHIPS),
            vm.X, currentY, HeaderSize, ColorPalette.Gold);
        currentY += 24f;

        if (!vm.ActiveRelationships.Any())
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_NO_RELATIONSHIPS),
                vm.X + 10f, currentY, vm.Width - 20f);
            return currentY + 20f;
        }

        // Table headers (widened columns for better spacing and readability)
        var col1 = vm.X; // Civilization
        var col2 = vm.X + 220f; // Status - 220px spacing
        var col3 = vm.X + 420f; // Established - 200px spacing (was 100px)
        var col4 = vm.X + 560f; // Expires - 140px spacing (was 100px)
        var col5 = vm.X + 700f; // Violations - 140px spacing
        var col6 = vm.X + 820f; // Actions - 120px spacing

        // Draw headers with clipping to prevent overlap
        drawList.PushClipRect(new Vector2(col1, currentY), new Vector2(col2 - 10f, currentY + TableRowHeight));
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_COL_CIVILIZATION),
            col1, currentY, LabelSize, ColorPalette.Grey);
        drawList.PopClipRect();

        drawList.PushClipRect(new Vector2(col2, currentY), new Vector2(col3 - 10f, currentY + TableRowHeight));
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_COL_STATUS),
            col2, currentY, LabelSize, ColorPalette.Grey);
        drawList.PopClipRect();

        drawList.PushClipRect(new Vector2(col3, currentY), new Vector2(col4 - 10f, currentY + TableRowHeight));
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_COL_ESTABLISHED),
            col3, currentY, LabelSize, ColorPalette.Grey);
        drawList.PopClipRect();

        drawList.PushClipRect(new Vector2(col4, currentY), new Vector2(col5 - 10f, currentY + TableRowHeight));
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_COL_EXPIRES),
            col4, currentY, LabelSize, ColorPalette.Grey);
        drawList.PopClipRect();

        drawList.PushClipRect(new Vector2(col5, currentY), new Vector2(col6 - 10f, currentY + TableRowHeight));
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_COL_VIOLATIONS),
            col5, currentY, LabelSize, ColorPalette.Grey);
        drawList.PopClipRect();

        drawList.PushClipRect(new Vector2(col6, currentY), new Vector2(vm.X + vm.Width, currentY + TableRowHeight));
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_COL_ACTIONS),
            col6, currentY, LabelSize, ColorPalette.Grey);
        drawList.PopClipRect();

        currentY += TableRowHeight;

        // Draw each relationship
        var rowIndex = 0;
        foreach (var rel in vm.ActiveRelationships)
        {
            // Add alternating row background for visual separation
            var isEvenRow = rowIndex % 2 == 0;
            if (isEvenRow)
            {
                var rowBgColor = new Vector4(0.15f, 0.15f, 0.15f, 0.3f);
                drawList.AddRectFilled(
                    new Vector2(vm.X, currentY),
                    new Vector2(vm.X + vm.Width, currentY + TableRowHeight),
                    ImGui.ColorConvertFloat4ToU32(rowBgColor)
                );
            }

            // Civilization name - clipped to prevent overflow
            drawList.PushClipRect(new Vector2(col1, currentY), new Vector2(col2 - 10f, currentY + TableRowHeight));
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col1, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), rel.OtherCivName);
            drawList.PopClipRect();

            // Status (color-coded) - clipped to prevent overflow
            var statusColor = GetStatusColor(rel.Status);
            var statusText = GetStatusText(rel.Status);
            drawList.PushClipRect(new Vector2(col2, currentY), new Vector2(col3 - 10f, currentY + TableRowHeight));
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col2, currentY),
                ImGui.ColorConvertFloat4ToU32(statusColor), statusText);
            drawList.PopClipRect();

            // Established date - right-aligned for better readability
            var establishedText = rel.EstablishedDate.ToString("MM/dd/yy");
            var establishedTextSize = ImGui.CalcTextSize(establishedText);
            var establishedX = col4 - 15f - establishedTextSize.X; // Right-align within column
            drawList.PushClipRect(new Vector2(col3, currentY), new Vector2(col4 - 10f, currentY + TableRowHeight));
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(establishedX, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), establishedText);
            drawList.PopClipRect();

            // Expires date - right-aligned for better readability
            var expiresText = rel.ExpiresDate.HasValue
                ? rel.ExpiresDate.Value.ToString("MM/dd/yy")
                : LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PERMANENT);
            var expiresTextSize = ImGui.CalcTextSize(expiresText);
            var expiresX = col5 - 15f - expiresTextSize.X; // Right-align within column
            drawList.PushClipRect(new Vector2(col4, currentY), new Vector2(col5 - 10f, currentY + TableRowHeight));
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(expiresX, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), expiresText);
            drawList.PopClipRect();

            // Violations - center-aligned for better readability
            if (rel.Status is DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact)
            {
                var violationColor = rel.ViolationCount >= 2 ? ColorPalette.Red : ColorPalette.White;
                var violationsText = $"{rel.ViolationCount}/{DiplomacyConstants.MaxViolations}";
                var violationsTextSize = ImGui.CalcTextSize(violationsText);
                var violationsX = col5 + (140f - violationsTextSize.X) / 2f; // Center within column
                drawList.PushClipRect(new Vector2(col5, currentY), new Vector2(col6 - 10f, currentY + TableRowHeight));
                drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(violationsX, currentY),
                    ImGui.ColorConvertFloat4ToU32(violationColor), violationsText);
                drawList.PopClipRect();
            }

            // Actions
            var actionX = col6;
            if (rel.Status is DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact)
            {
                // Show break scheduled countdown or schedule break button
                if (rel.BreakScheduledDate.HasValue)
                {
                    var hoursRemaining = (rel.BreakScheduledDate.Value - DateTime.UtcNow).TotalHours;
                    if (hoursRemaining > 0)
                    {
                        var formattedTime =
                            DiplomacyNotificationHelper.FormatTimeRemaining(rel.BreakScheduledDate.Value);
                        var countdownText = LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_BREAKS_IN,
                            formattedTime);
                        var isCritical = DiplomacyNotificationHelper.IsTimeCritical(rel.BreakScheduledDate.Value);
                        var timeColor = isCritical ? ColorPalette.Red : ColorPalette.Yellow;
                        drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(actionX, currentY),
                            ImGui.ColorConvertFloat4ToU32(timeColor), countdownText);

                        // Cancel break button
                        if (ButtonRenderer.DrawSmallButton(drawList,
                                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CANCEL_BUTTON),
                                actionX + 110f, currentY - 2f, 60f, 20f))
                        {
                            events.Add(new DiplomacyEvent.CancelBreak(rel.OtherCivId));
                        }
                    }
                }
                else
                {
                    if (ButtonRenderer.DrawSmallButton(drawList,
                            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_SCHEDULE_BREAK_BUTTON),
                            actionX, currentY - 2f, 140f, 20f))
                    {
                        events.Add(new DiplomacyEvent.ScheduleBreak(rel.OtherCivId));
                    }
                }
            }
            else if (rel.Status == DiplomaticStatus.War)
            {
                if (ButtonRenderer.DrawSmallButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_DECLARE_PEACE_BUTTON),
                        actionX, currentY - 2f, 140f, 20f))
                {
                    events.Add(new DiplomacyEvent.DeclarePeace(rel.OtherCivId));
                }
            }

            currentY += TableRowHeight;
            rowIndex++;
        }

        return currentY;
    }

    private static float DrawProposalsPanel(
        DiplomacyTabViewModel vm,
        ImDrawListPtr drawList,
        float startY,
        List<DiplomacyEvent> events)
    {
        var currentY = startY;

        // Section header
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PENDING_PROPOSALS),
            vm.X, currentY, HeaderSize, ColorPalette.Gold);
        currentY += 24f;

        // Incoming proposals
        if (vm.IncomingProposals.Any())
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_INCOMING_LABEL),
                vm.X, currentY, LabelSize, ColorPalette.White);
            currentY += 20f;

            foreach (var proposal in vm.IncomingProposals)
            {
                var formattedTime = DiplomacyNotificationHelper.FormatTimeRemaining(proposal.ExpiresDate);
                var isCritical = DiplomacyNotificationHelper.IsTimeCritical(proposal.ExpiresDate);
                var statusText = GetStatusText(proposal.ProposedStatus);
                var proposalText =
                    $"{proposal.OtherCivName} proposes {statusText} (expires in {formattedTime})";

                var textColor = isCritical ? ColorPalette.Red : ColorPalette.White;
                drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(vm.X + 10f, currentY),
                    ImGui.ColorConvertFloat4ToU32(textColor), proposalText);

                currentY += 18f;

                // Accept/Decline buttons
                if (ButtonRenderer.DrawSmallButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_ACCEPT_BUTTON),
                        vm.X + 20f, currentY, 70f, 20f))
                {
                    events.Add(new DiplomacyEvent.AcceptProposal(proposal.ProposalId));
                }

                if (ButtonRenderer.DrawSmallButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_DECLINE_BUTTON),
                        vm.X + 100f, currentY, 70f, 20f))
                {
                    events.Add(new DiplomacyEvent.DeclineProposal(proposal.ProposalId));
                }

                currentY += 26f;
            }
        }

        // Outgoing proposals
        if (vm.OutgoingProposals.Any())
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_OUTGOING_LABEL),
                vm.X, currentY, LabelSize, ColorPalette.White);
            currentY += 20f;

            foreach (var proposal in vm.OutgoingProposals)
            {
                var formattedTime = DiplomacyNotificationHelper.FormatTimeRemaining(proposal.ExpiresDate);
                var isCritical = DiplomacyNotificationHelper.IsTimeCritical(proposal.ExpiresDate);
                var statusText = GetStatusText(proposal.ProposedStatus);
                var proposalText = LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PROPOSAL_TO,
                    proposal.OtherCivName, statusText, formattedTime);

                var textColor = isCritical ? ColorPalette.Red : ColorPalette.Grey;
                drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(vm.X + 10f, currentY),
                    ImGui.ColorConvertFloat4ToU32(textColor), proposalText);

                currentY += 22f;
            }
        }

        if (!vm.IncomingProposals.Any() && !vm.OutgoingProposals.Any())
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_NO_PROPOSALS),
                vm.X + 10f, currentY, vm.Width - 20f);
            currentY += 20f;
        }

        return currentY;
    }

    private static (float currentY, float civButtonY, float typeButtonY) DrawProposePanel(
        DiplomacyTabViewModel vm,
        ImDrawListPtr drawList,
        float startY,
        List<DiplomacyEvent> events)
    {
        var currentY = startY;
        var civDropdownButtonY = 0f;  // Track civilization dropdown button Y position
        var typeDropdownButtonY = 0f; // Track type dropdown button Y position

        // Section header
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PROPOSE_NEW),
            vm.X, currentY, HeaderSize, ColorPalette.Gold);
        currentY += 24f;

        if (!vm.AvailableCivilizations.Any())
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_NO_CIVS_AVAILABLE),
                vm.X + 10f, currentY, vm.Width - 20f);
            return (currentY + 20f, 0f, 0f); // No dropdowns available, return zeros
        }

        // Civilization selection
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TARGET_CIV_LABEL),
            vm.X, currentY, LabelSize, ColorPalette.Grey);
        currentY += 18f;

        var civDropdownX = vm.X + 10f;
        var civDropdownY = currentY;
        var civDropdownW = 500f;
        var civDropdownH = 30f;
        civDropdownButtonY = civDropdownY; // Store button position for dropdown rendering

        var selectedCivIndex = vm.AvailableCivilizations.FindIndex(c => c.CivId == vm.SelectedCivId);
        var selectedCivName = selectedCivIndex >= 0 && selectedCivIndex < vm.AvailableCivilizations.Count
            ? vm.AvailableCivilizations[selectedCivIndex].Name
            : LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_SELECT_CIV_PLACEHOLDER);
        var civItems = vm.AvailableCivilizations.Select(c => c.Name).ToArray();

        // Draw civilization dropdown button
        if (Dropdown.DrawButton(drawList, civDropdownX, civDropdownY, civDropdownW, civDropdownH, selectedCivName,
                vm.IsCivDropdownOpen))
            events.Add(new DiplomacyEvent.ToggleCivDropdown(!vm.IsCivDropdownOpen));

        currentY += 36f;

        // Relationship type selection
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_RELATIONSHIP_TYPE_LABEL),
            vm.X, currentY, LabelSize, ColorPalette.Grey);
        currentY += 18f;

        var typeDropdownX = vm.X + 10f;
        var typeDropdownY = currentY;
        var typeDropdownW = 500f;
        var typeDropdownH = 30f;
        typeDropdownButtonY = typeDropdownY; // Store button position for dropdown rendering

        var typeItems = new[]
        {
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TYPE_NAP),
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TYPE_ALLIANCE)
        };
        var typeIndex = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact ? 0 : 1;
        var selectedTypeName = typeItems[typeIndex];

        // Draw relationship type dropdown button
        if (Dropdown.DrawButton(drawList, typeDropdownX, typeDropdownY, typeDropdownW, typeDropdownH,
                selectedTypeName, vm.IsTypeDropdownOpen))
            events.Add(new DiplomacyEvent.ToggleTypeDropdown(!vm.IsTypeDropdownOpen));

        currentY += 30f;

        // Duration display
        var durationLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_DURATION_LABEL);
        var duration = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_DURATION_3DAYS)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_DURATION_PERMANENT);
        TextRenderer.DrawLabel(drawList, $"{durationLabel} {duration}", vm.X, currentY, LabelSize, ColorPalette.Grey);
        currentY += 24f;

        // Rank requirement check
        var requiredRank = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact
            ? DiplomacyConstants.NonAggressionPactRequiredRank
            : DiplomacyConstants.AllianceRequiredRank;

        var hasRank = vm.CurrentRank >= requiredRank;
        if (!hasRank)
        {
            var requiredRankName = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact
                ? DiplomacyConstants.NonAggressionPactRankName
                : DiplomacyConstants.AllianceRankName;
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_INSUFFICIENT_RANK,
                    requiredRankName, requiredRank),
                vm.X, currentY, LabelSize, ColorPalette.Red);
            currentY += 24f;
        }

        // Send Proposal button
        var canSendProposal = hasRank && !string.IsNullOrEmpty(vm.SelectedCivId);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_SEND_PROPOSAL_BUTTON),
                vm.X, currentY, 150f, 28f, true, canSendProposal))
        {
            events.Add(new DiplomacyEvent.ProposeRelationship(vm.SelectedCivId, vm.SelectedProposalType));
        }

        // Declare War button (separate, red, requires civilization selection)
        var canDeclareWar = !string.IsNullOrEmpty(vm.SelectedCivId);
        var warButtonColor = ColorPalette.Red * 0.6f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_DECLARE_WAR_BUTTON),
                vm.X + 170f, currentY, 120f, 28f, true, canDeclareWar, warButtonColor))
        {
            if (string.IsNullOrEmpty(vm.ConfirmWarCivId))
            {
                events.Add(new DiplomacyEvent.ShowWarConfirmation(vm.SelectedCivId));
            }
        }

        currentY += 36f;

        // War confirmation dialog
        if (!string.IsNullOrEmpty(vm.ConfirmWarCivId))
        {
            var confirmCivName = vm.AvailableCivilizations.FirstOrDefault(c => c.CivId == vm.ConfirmWarCivId)?.Name ??
                                 LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_UNKNOWN_CIV);
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CONFIRM_WAR_MESSAGE, confirmCivName),
                vm.X, currentY, LabelSize, ColorPalette.Red);
            currentY += 20f;

            if (ButtonRenderer.DrawActionButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_YES_DECLARE_WAR),
                    vm.X, currentY, 150f, 24f, true))
            {
                events.Add(new DiplomacyEvent.DeclareWar(vm.ConfirmWarCivId));
            }

            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                    vm.X + 160f, currentY, 80f, 24f))
            {
                events.Add(new DiplomacyEvent.CancelWarConfirmation());
            }

            currentY += 30f;
        }

        return (currentY, civDropdownButtonY, typeDropdownButtonY);
    }

    private static Vector4 GetStatusColor(DiplomaticStatus status)
    {
        return status switch
        {
            DiplomaticStatus.Alliance => ColorPalette.Green,
            DiplomaticStatus.NonAggressionPact => ColorPalette.Yellow,
            DiplomaticStatus.War => ColorPalette.Red,
            _ => ColorPalette.Grey
        };
    }

    private static string GetStatusText(DiplomaticStatus status)
    {
        return status switch
        {
            DiplomaticStatus.Alliance =>
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_STATUS_ALLIANCE),
            DiplomaticStatus.NonAggressionPact =>
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_STATUS_NAP),
            DiplomaticStatus.War => LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_STATUS_WAR),
            _ => LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_STATUS_NEUTRAL)
        };
    }
}

// ViewModel for Diplomacy Tab
public readonly struct DiplomacyTabViewModel(
    float x,
    float y,
    float width,
    float height,
    bool isLoading,
    bool hasCivilization,
    string? errorMessage,
    List<DiplomacyInfoResponsePacket.RelationshipInfo> activeRelationships,
    List<DiplomacyInfoResponsePacket.ProposalInfo> incomingProposals,
    List<DiplomacyInfoResponsePacket.ProposalInfo> outgoingProposals,
    List<CivilizationInfo> availableCivilizations,
    string selectedCivId,
    DiplomaticStatus selectedProposalType,
    int currentRank,
    string? confirmWarCivId,
    bool isCivDropdownOpen,
    bool isTypeDropdownOpen)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public bool IsLoading { get; } = isLoading;
    public bool HasCivilization { get; } = hasCivilization;
    public string? ErrorMessage { get; } = errorMessage;
    public List<DiplomacyInfoResponsePacket.RelationshipInfo> ActiveRelationships { get; } = activeRelationships;
    public List<DiplomacyInfoResponsePacket.ProposalInfo> IncomingProposals { get; } = incomingProposals;
    public List<DiplomacyInfoResponsePacket.ProposalInfo> OutgoingProposals { get; } = outgoingProposals;
    public List<CivilizationInfo> AvailableCivilizations { get; } = availableCivilizations;
    public string SelectedCivId { get; } = selectedCivId;
    public DiplomaticStatus SelectedProposalType { get; } = selectedProposalType;
    public int CurrentRank { get; } = currentRank;
    public string? ConfirmWarCivId { get; } = confirmWarCivId;
    public bool IsCivDropdownOpen { get; } = isCivDropdownOpen;
    public bool IsTypeDropdownOpen { get; } = isTypeDropdownOpen;
}

public record CivilizationInfo(string CivId, string Name);

// Result containing events
public record DiplomacyTabRendererResult(List<DiplomacyEvent> Events, float Height);

// Event types
public abstract record DiplomacyEvent
{
    public record ProposeRelationship(string TargetCivId, DiplomaticStatus ProposedStatus) : DiplomacyEvent;

    public record AcceptProposal(string ProposalId) : DiplomacyEvent;

    public record DeclineProposal(string ProposalId) : DiplomacyEvent;

    public record ScheduleBreak(string TargetCivId) : DiplomacyEvent;

    public record CancelBreak(string TargetCivId) : DiplomacyEvent;

    public record DeclareWar(string TargetCivId) : DiplomacyEvent;

    public record DeclarePeace(string TargetCivId) : DiplomacyEvent;

    public record SelectCivilization(string CivId) : DiplomacyEvent;

    public record SelectProposalType(DiplomaticStatus ProposalType) : DiplomacyEvent;

    public record ShowWarConfirmation(string CivId) : DiplomacyEvent;

    public record CancelWarConfirmation() : DiplomacyEvent;

    public record ToggleCivDropdown(bool IsOpen) : DiplomacyEvent;

    public record ToggleTypeDropdown(bool IsOpen) : DiplomacyEvent;

    public record DismissError() : DiplomacyEvent;
}