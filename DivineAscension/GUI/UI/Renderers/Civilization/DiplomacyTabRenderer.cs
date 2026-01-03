using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Components.Banners;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.GUI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;
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
            TextRenderer.DrawInfoText(drawList, "Loading diplomacy data...", vm.X, currentY + 8f, vm.Width);
            return new DiplomacyTabRendererResult(events, vm.Height);
        }

        // Not in civilization state
        if (!vm.HasCivilization)
        {
            TextRenderer.DrawInfoText(drawList, "Your religion must be in a civilization to use diplomacy.", vm.X,
                currentY + 8f, vm.Width);
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

        // Panel 1: Current Relationships
        currentY = DrawRelationshipsPanel(vm, drawList, currentY, events);
        currentY += SectionSpacing;

        // Panel 2: Pending Proposals
        currentY = DrawProposalsPanel(vm, drawList, currentY, events);
        currentY += SectionSpacing;

        // Panel 3: Propose Relationship
        currentY = DrawProposePanel(vm, drawList, currentY, events);

        // Draw dropdown menus AFTER everything else (z-ordering)
        if (vm.IsCivDropdownOpen)
        {
            var civDropdownX = vm.X + 10f;
            var civDropdownY = 18f + vm.Y + 18f; // Match the dropdown button position
            var civDropdownW = 500f;
            var civDropdownH = 30f;
            var civItems = vm.AvailableCivilizations.Select(c => c.Name).ToArray();
            var selectedCivIndex = vm.AvailableCivilizations.FindIndex(c => c.CivId == vm.SelectedCivId);

            Dropdown.DrawMenuVisual(drawList, civDropdownX, civDropdownY, civDropdownW, civDropdownH, civItems,
                selectedCivIndex);
            var (newCivIndex, shouldCloseCiv, clickConsumedCiv) = Dropdown.DrawMenuAndHandleInteraction(
                civDropdownX, civDropdownY, civDropdownW, civDropdownH, civItems, selectedCivIndex);

            if (newCivIndex != selectedCivIndex && newCivIndex >= 0 && newCivIndex < vm.AvailableCivilizations.Count)
            {
                events.Add(new DiplomacyEvent.SelectCivilization(vm.AvailableCivilizations[newCivIndex].CivId));
            }

            if (shouldCloseCiv) events.Add(new DiplomacyEvent.ToggleCivDropdown(false));
        }

        if (vm.IsTypeDropdownOpen)
        {
            var typeDropdownX = vm.X + 10f;
            var typeDropdownY = 18f + vm.Y + 18f + 36f + 18f; // Match the type dropdown button position
            var typeDropdownW = 500f;
            var typeDropdownH = 30f;
            var typeItems = new[] { "Non-Aggression Pact (Rank 1: Established)", "Alliance (Rank 2: Renowned)" };
            var typeIndex = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact ? 0 : 1;

            Dropdown.DrawMenuVisual(drawList, typeDropdownX, typeDropdownY, typeDropdownW, typeDropdownH, typeItems,
                typeIndex);
            var (newTypeIndex, shouldCloseType, clickConsumedType) = Dropdown.DrawMenuAndHandleInteraction(
                typeDropdownX, typeDropdownY, typeDropdownW, typeDropdownH, typeItems, typeIndex);

            if (newTypeIndex != typeIndex)
            {
                var newType = newTypeIndex == 0 ? DiplomaticStatus.NonAggressionPact : DiplomaticStatus.Alliance;
                events.Add(new DiplomacyEvent.SelectProposalType(newType));
            }

            if (shouldCloseType) events.Add(new DiplomacyEvent.ToggleTypeDropdown(false));
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
        TextRenderer.DrawLabel(drawList, "Current Relationships", vm.X, currentY, HeaderSize, ColorPalette.Gold);
        currentY += 24f;

        if (!vm.ActiveRelationships.Any())
        {
            TextRenderer.DrawInfoText(drawList, "No active relationships.", vm.X + 10f, currentY, vm.Width - 20f);
            return currentY + 20f;
        }

        // Table headers
        var col1 = vm.X;
        var col2 = vm.X + 200f;
        var col3 = vm.X + 300f;
        var col4 = vm.X + 400f;
        var col5 = vm.X + 500f;
        var col6 = vm.X + 620f;

        TextRenderer.DrawLabel(drawList, "Civilization", col1, currentY, LabelSize, ColorPalette.Grey);
        TextRenderer.DrawLabel(drawList, "Status", col2, currentY, LabelSize, ColorPalette.Grey);
        TextRenderer.DrawLabel(drawList, "Established", col3, currentY, LabelSize, ColorPalette.Grey);
        TextRenderer.DrawLabel(drawList, "Expires", col4, currentY, LabelSize, ColorPalette.Grey);
        TextRenderer.DrawLabel(drawList, "Violations", col5, currentY, LabelSize, ColorPalette.Grey);
        TextRenderer.DrawLabel(drawList, "Actions", col6, currentY, LabelSize, ColorPalette.Grey);
        currentY += TableRowHeight;

        // Draw each relationship
        foreach (var rel in vm.ActiveRelationships)
        {
            // Civilization name
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col1, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), rel.OtherCivName);

            // Status (color-coded)
            var statusColor = GetStatusColor(rel.Status);
            var statusText = GetStatusText(rel.Status);
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col2, currentY),
                ImGui.ColorConvertFloat4ToU32(statusColor), statusText);

            // Established date
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col3, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), rel.EstablishedDate.ToString("MM/dd/yy"));

            // Expires date
            var expiresText = rel.ExpiresDate.HasValue
                ? rel.ExpiresDate.Value.ToString("MM/dd/yy")
                : "Permanent";
            drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col4, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), expiresText);

            // Violations
            if (rel.Status is DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact)
            {
                var violationColor = rel.ViolationCount >= 2 ? ColorPalette.Red : ColorPalette.White;
                drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(col5, currentY),
                    ImGui.ColorConvertFloat4ToU32(violationColor),
                    $"{rel.ViolationCount}/{DiplomacyConstants.MaxViolations}");
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
                        var formattedTime = DiplomacyNotificationHelper.FormatTimeRemaining(rel.BreakScheduledDate.Value);
                        var countdownText = $"Breaks in {formattedTime}";
                        var isCritical = DiplomacyNotificationHelper.IsTimeCritical(rel.BreakScheduledDate.Value);
                        var timeColor = isCritical ? ColorPalette.Red : ColorPalette.Yellow;
                        drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(actionX, currentY),
                            ImGui.ColorConvertFloat4ToU32(timeColor), countdownText);

                        // Cancel break button
                        ImGui.SetCursorScreenPos(new Vector2(actionX + 110f, currentY - 2f));
                        if (ImGui.SmallButton($"Cancel##{rel.RelationshipId}"))
                        {
                            events.Add(new DiplomacyEvent.CancelBreak(rel.OtherCivId));
                        }
                    }
                }
                else
                {
                    ImGui.SetCursorScreenPos(new Vector2(actionX, currentY - 2f));
                    if (ImGui.SmallButton($"Schedule Break##{rel.RelationshipId}"))
                    {
                        events.Add(new DiplomacyEvent.ScheduleBreak(rel.OtherCivId));
                    }
                }
            }
            else if (rel.Status == DiplomaticStatus.War)
            {
                ImGui.SetCursorScreenPos(new Vector2(actionX, currentY - 2f));
                if (ImGui.SmallButton($"Declare Peace##{rel.RelationshipId}"))
                {
                    events.Add(new DiplomacyEvent.DeclarePeace(rel.OtherCivId));
                }
            }

            currentY += TableRowHeight;
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
        TextRenderer.DrawLabel(drawList, "Pending Proposals", vm.X, currentY, HeaderSize, ColorPalette.Gold);
        currentY += 24f;

        // Incoming proposals
        if (vm.IncomingProposals.Any())
        {
            TextRenderer.DrawLabel(drawList, "Incoming:", vm.X, currentY, LabelSize, ColorPalette.White);
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
                ImGui.SetCursorScreenPos(new Vector2(vm.X + 20f, currentY));
                if (ImGui.SmallButton($"Accept##{proposal.ProposalId}"))
                {
                    events.Add(new DiplomacyEvent.AcceptProposal(proposal.ProposalId));
                }

                ImGui.SameLine();
                if (ImGui.SmallButton($"Decline##{proposal.ProposalId}"))
                {
                    events.Add(new DiplomacyEvent.DeclineProposal(proposal.ProposalId));
                }

                currentY += 26f;
            }
        }

        // Outgoing proposals
        if (vm.OutgoingProposals.Any())
        {
            TextRenderer.DrawLabel(drawList, "Outgoing:", vm.X, currentY, LabelSize, ColorPalette.White);
            currentY += 20f;

            foreach (var proposal in vm.OutgoingProposals)
            {
                var formattedTime = DiplomacyNotificationHelper.FormatTimeRemaining(proposal.ExpiresDate);
                var isCritical = DiplomacyNotificationHelper.IsTimeCritical(proposal.ExpiresDate);
                var statusText = GetStatusText(proposal.ProposedStatus);
                var proposalText =
                    $"To {proposal.OtherCivName}: {statusText} (expires in {formattedTime})";

                var textColor = isCritical ? ColorPalette.Red : ColorPalette.Grey;
                drawList.AddText(ImGui.GetFont(), LabelSize, new Vector2(vm.X + 10f, currentY),
                    ImGui.ColorConvertFloat4ToU32(textColor), proposalText);

                currentY += 22f;
            }
        }

        if (!vm.IncomingProposals.Any() && !vm.OutgoingProposals.Any())
        {
            TextRenderer.DrawInfoText(drawList, "No pending proposals.", vm.X + 10f, currentY, vm.Width - 20f);
            currentY += 20f;
        }

        return currentY;
    }

    private static float DrawProposePanel(
        DiplomacyTabViewModel vm,
        ImDrawListPtr drawList,
        float startY,
        List<DiplomacyEvent> events)
    {
        var currentY = startY;

        // Section header
        TextRenderer.DrawLabel(drawList, "Propose New Relationship", vm.X, currentY, HeaderSize, ColorPalette.Gold);
        currentY += 24f;

        if (!vm.AvailableCivilizations.Any())
        {
            TextRenderer.DrawInfoText(drawList, "No civilizations available for new relationships.", vm.X + 10f,
                currentY, vm.Width - 20f);
            return currentY + 20f;
        }

        // Civilization selection
        TextRenderer.DrawLabel(drawList, "Target Civilization:", vm.X, currentY, LabelSize, ColorPalette.Grey);
        currentY += 18f;

        var civDropdownX = vm.X + 10f;
        var civDropdownY = currentY;
        var civDropdownW = 500f;
        var civDropdownH = 30f;

        var selectedCivIndex = vm.AvailableCivilizations.FindIndex(c => c.CivId == vm.SelectedCivId);
        var selectedCivName = selectedCivIndex >= 0 && selectedCivIndex < vm.AvailableCivilizations.Count
            ? vm.AvailableCivilizations[selectedCivIndex].Name
            : "Select a civilization...";
        var civItems = vm.AvailableCivilizations.Select(c => c.Name).ToArray();

        // Draw civilization dropdown button
        if (Dropdown.DrawButton(drawList, civDropdownX, civDropdownY, civDropdownW, civDropdownH, selectedCivName,
                vm.IsCivDropdownOpen))
            events.Add(new DiplomacyEvent.ToggleCivDropdown(!vm.IsCivDropdownOpen));

        currentY += 36f;

        // Relationship type selection
        TextRenderer.DrawLabel(drawList, "Relationship Type:", vm.X, currentY, LabelSize, ColorPalette.Grey);
        currentY += 18f;

        var typeDropdownX = vm.X + 10f;
        var typeDropdownY = currentY;
        var typeDropdownW = 500f;
        var typeDropdownH = 30f;

        var typeItems = new[] { "Non-Aggression Pact (Rank 1: Established)", "Alliance (Rank 2: Renowned)" };
        var typeIndex = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact ? 0 : 1;
        var selectedTypeName = typeItems[typeIndex];

        // Draw relationship type dropdown button
        if (Dropdown.DrawButton(drawList, typeDropdownX, typeDropdownY, typeDropdownW, typeDropdownH,
                selectedTypeName, vm.IsTypeDropdownOpen))
            events.Add(new DiplomacyEvent.ToggleTypeDropdown(!vm.IsTypeDropdownOpen));

        currentY += 30f;

        // Duration display
        var duration = vm.SelectedProposalType == DiplomaticStatus.NonAggressionPact ? "3 days" : "Permanent";
        TextRenderer.DrawLabel(drawList, $"Duration: {duration}", vm.X, currentY, LabelSize, ColorPalette.Grey);
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
                $"Insufficient Rank: Requires {requiredRankName} (Rank {requiredRank})", vm.X, currentY,
                LabelSize, ColorPalette.Red);
            currentY += 24f;
        }

        // Send Proposal button
        var canSendProposal = hasRank && !string.IsNullOrEmpty(vm.SelectedCivId);
        if (ButtonRenderer.DrawButton(drawList, "Send Proposal", vm.X, currentY, 150f, 28f, true, canSendProposal))
        {
            events.Add(new DiplomacyEvent.ProposeRelationship(vm.SelectedCivId, vm.SelectedProposalType));
        }

        // Declare War button (separate, red, requires civilization selection)
        var canDeclareWar = !string.IsNullOrEmpty(vm.SelectedCivId);
        var warButtonColor = ColorPalette.Red * 0.6f;
        if (ButtonRenderer.DrawButton(drawList, "Declare War", vm.X + 170f, currentY, 120f, 28f, true, canDeclareWar, warButtonColor))
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
                                 "Unknown";
            TextRenderer.DrawLabel(drawList, $"Confirm war declaration against {confirmCivName}?", vm.X, currentY,
                LabelSize, ColorPalette.Red);
            currentY += 20f;

            if (ButtonRenderer.DrawActionButton(drawList, "Yes, Declare War", vm.X, currentY, 150f, 24f, true))
            {
                events.Add(new DiplomacyEvent.DeclareWar(vm.ConfirmWarCivId));
            }

            if (ButtonRenderer.DrawButton(drawList, "Cancel", vm.X + 160f, currentY, 80f, 24f))
            {
                events.Add(new DiplomacyEvent.CancelWarConfirmation());
            }

            currentY += 30f;
        }

        return currentY;
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
            DiplomaticStatus.Alliance => "Alliance",
            DiplomaticStatus.NonAggressionPact => "Non-Aggression Pact",
            DiplomaticStatus.War => "War",
            _ => "Neutral"
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
