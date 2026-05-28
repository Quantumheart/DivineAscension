using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Components.Banners;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
/// Pure renderer for the "Propose an Accord" ledger chapter (#330). Sibling
/// of "The Accords" (II.v). Reuses the diplomacy VM / event union so the
/// existing reducer in <c>CivilizationDiplomacyPresenter.ProcessEvents</c>
/// handles dropdown toggling, selection, and the Propose request. Founder-
/// only — the sidebar gates entry, but the renderer also mirrors the check.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ProposeAccordRenderer
{
    private const float DividerHeight = 22f;
    private const float DividerYPadding = 6f;
    private const float SectionHeadingHeight = 24f;
    private const float ProseLineHeight = 18f;
    private const float ProseBottomSpacing = 12f;
    private const float LabelToInputGap = 6f;
    private const float DropdownWidth = 360f;
    private const float DropdownHeight = 30f;
    private const float DropdownBottomSpacing = 18f;
    private const float SendButtonWidth = 180f;
    private const float SendButtonHeight = 32f;
    private const float SendButtonTopSpacing = 12f;
    private const float SendButtonBottomSpacing = 6f;

    public static ProposeAccordRenderResult Draw(
        ProposeAccordViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DiplomacyEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (!vm.HasCivilization)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_NO_CIVILIZATION),
                x, y, width, height);
            return new ProposeAccordRenderResult(events, height);
        }

        if (!vm.IsFounder)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_NOT_FOUNDER),
                x, y, width, height);
            return new ProposeAccordRenderResult(events, height);
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY: 0f,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_CHAPTER_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        if (!string.IsNullOrEmpty(vm.ErrorMessage))
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, x, currentY, contentWidth, vm.ErrorMessage,
                out _, out var dismissClicked, showRetry: false);
            currentY += consumed;
            if (dismissClicked) events.Add(new DiplomacyEvent.DismissError());
        }

        currentY = DrawIntro(drawList, x, currentY, contentWidth);
        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        // Recipient + manner section: track button anchors so the dropdown
        // menus can be drawn over the chapter after the rest of the page.
        var (afterFieldsY, civButtonY, typeButtonY) = DrawFields(vm, drawList, x, currentY, contentWidth, events);
        currentY = afterFieldsY;
        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        currentY = DrawPreview(vm, drawList, x, currentY, contentWidth);
        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        currentY = DrawSendAction(vm, drawList, x, currentY, contentWidth, events);

        var dropdownConsumedClick = DrawDropdownMenus(vm, drawList, x, civButtonY, typeButtonY, events);

        drawList.PopClipRect();

        // War confirmation modal — sits above the chapter content. Server is
        // authoritative; this is just the friction step before firing the
        // DeclareWar packet.
        if (!string.IsNullOrEmpty(vm.ConfirmWarCivId))
        {
            var civName = vm.AvailableCivilizations.FirstOrDefault(c => c.CivId == vm.ConfirmWarCivId)?.Name
                          ?? LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_UNKNOWN_CIV);
            ConfirmOverlay.Draw(
                LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_WAR_CONFIRM_TITLE),
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CONFIRM_WAR_MESSAGE, civName),
                out var confirmed, out var cancelled,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_YES_DECLARE_WAR));

            if (confirmed) events.Add(new DiplomacyEvent.DeclareWar(vm.ConfirmWarCivId));
            if (cancelled) events.Add(new DiplomacyEvent.CancelWarConfirmation());
        }

        if (dropdownConsumedClick)
        {
            // Drop any selection / submit events the underlying buttons fired
            // beneath the open menu — matches the old DiplomacyTabRenderer
            // z-ordering behaviour.
            events.RemoveAll(e => e is not DiplomacyEvent.SelectCivilization
                and not DiplomacyEvent.SelectProposalType
                and not DiplomacyEvent.ToggleCivDropdown
                and not DiplomacyEvent.ToggleTypeDropdown
                and not DiplomacyEvent.DismissError);
        }

        return new ProposeAccordRenderResult(events, height);
    }

    private static float DrawIntro(ImDrawListPtr drawList, float x, float y, float width)
    {
        var prose = LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_INTRO);
        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var measured = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (measured > 0 ? measured : ProseLineHeight) + ProseBottomSpacing;
    }

    private static (float currentY, float civButtonY, float typeButtonY) DrawFields(
        ProposeAccordViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<DiplomacyEvent> events)
    {
        var currentY = y;

        // Recipient.
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_RECIPIENT_LABEL),
            x, currentY, Body, ColorPalette.Grey);
        currentY += Body + LabelToInputGap;

        var civButtonY = currentY;
        if (vm.AvailableCivilizations.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_NO_REALMS),
                x, currentY, width, Body, ColorPalette.Grey);
            currentY += ProseLineHeight + DropdownBottomSpacing;
        }
        else
        {
            var selectedCivIndex = vm.AvailableCivilizations.FindIndex(c => c.CivId == vm.SelectedCivId);
            var recipientLabel = selectedCivIndex >= 0
                ? vm.AvailableCivilizations[selectedCivIndex].Name
                : LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_RECIPIENT_PLACEHOLDER);
            if (Dropdown.DrawButton(drawList, x, currentY, DropdownWidth, DropdownHeight,
                    recipientLabel, vm.IsCivDropdownOpen))
                events.Add(new DiplomacyEvent.ToggleCivDropdown(!vm.IsCivDropdownOpen));
            currentY += DropdownHeight + DropdownBottomSpacing;
        }

        // Manner.
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_MANNER_LABEL),
            x, currentY, Body, ColorPalette.Grey);
        currentY += Body + LabelToInputGap;

        var typeButtonY = currentY;
        var typeLabel = ProposalTypeLabel(vm.SelectedProposalType);
        if (Dropdown.DrawButton(drawList, x, currentY, DropdownWidth, DropdownHeight,
                typeLabel, vm.IsTypeDropdownOpen))
            events.Add(new DiplomacyEvent.ToggleTypeDropdown(!vm.IsTypeDropdownOpen));
        currentY += DropdownHeight + DropdownBottomSpacing;

        return (currentY, civButtonY, typeButtonY);
    }

    private static float DrawPreview(
        ProposeAccordViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_PREVIEW_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionHeadingHeight;

        var description = ProposalDescription(vm.SelectedProposalType);
        var color = description is null
            ? ColorPalette.Grey
            : vm.SelectedProposalType == DiplomaticStatus.War
                ? ColorPalette.Red
                : ColorPalette.White;
        var prose = description ?? LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_PREVIEW_NONE);

        TextRenderer.DrawInfoText(drawList, prose, x, currentY, width, Body, color);
        var measured = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        currentY += (measured > 0 ? measured : ProseLineHeight) + ProseBottomSpacing;

        // War has no rank gate — only NAP / Alliance do.
        if (description is not null
            && vm.SelectedProposalType != DiplomaticStatus.War
            && vm.RequiredRankForSelection > vm.CurrentRank)
        {
            var rankLine = LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_RANK_REQUIRED,
                vm.RequiredRankNameForSelection, vm.RequiredRankForSelection);
            TextRenderer.DrawInfoText(drawList, rankLine, x, currentY, width, Body, ColorPalette.Red);
            var rankH = TextRenderer.MeasureWrappedHeight(rankLine, width, Body);
            currentY += (rankH > 0 ? rankH : ProseLineHeight) + ProseBottomSpacing;
        }

        return currentY;
    }

    private static float DrawSendAction(
        ProposeAccordViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<DiplomacyEvent> events)
    {
        var currentY = y + SendButtonTopSpacing;
        var isWar = vm.SelectedProposalType == DiplomaticStatus.War;
        var canSend = vm.AvailableCivilizations.Count > 0
                      && !string.IsNullOrEmpty(vm.SelectedCivId)
                      && (isWar || vm.RequiredRankForSelection <= vm.CurrentRank);

        var buttonLabel = LocalizationService.Instance.Get(isWar
            ? LocalizationKeys.UI_PROPOSE_ACCORD_DECLARE_WAR_BUTTON
            : LocalizationKeys.UI_PROPOSE_ACCORD_SEND_BUTTON);

        var buttonX = x + width - SendButtonWidth;
        if (ButtonRenderer.DrawButton(drawList,
                buttonLabel,
                buttonX, currentY, SendButtonWidth, SendButtonHeight,
                isPrimary: true, enabled: canSend))
        {
            if (isWar)
                events.Add(new DiplomacyEvent.ShowWarConfirmation(vm.SelectedCivId));
            else
                events.Add(new DiplomacyEvent.ProposeRelationship(vm.SelectedCivId, vm.SelectedProposalType));
        }

        return currentY + SendButtonHeight + SendButtonBottomSpacing;
    }

    private static bool DrawDropdownMenus(
        ProposeAccordViewModel vm,
        ImDrawListPtr drawList,
        float x, float civButtonY, float typeButtonY,
        List<DiplomacyEvent> events)
    {
        var dropdownConsumedClick = false;

        if (vm.IsCivDropdownOpen && vm.AvailableCivilizations.Count > 0)
        {
            var items = vm.AvailableCivilizations.Select(c => c.Name).ToArray();
            var selectedIndex = vm.AvailableCivilizations.FindIndex(c => c.CivId == vm.SelectedCivId);

            Dropdown.DrawMenuVisual(drawList, x, civButtonY, DropdownWidth, DropdownHeight, items, selectedIndex);
            var (newIndex, shouldClose, consumed) = Dropdown.DrawMenuAndHandleInteraction(
                x, civButtonY, DropdownWidth, DropdownHeight, items, selectedIndex);

            dropdownConsumedClick |= consumed;

            if (newIndex != selectedIndex && newIndex >= 0 && newIndex < vm.AvailableCivilizations.Count)
                events.Add(new DiplomacyEvent.SelectCivilization(vm.AvailableCivilizations[newIndex].CivId));

            if (shouldClose) events.Add(new DiplomacyEvent.ToggleCivDropdown(false));
        }

        if (vm.IsTypeDropdownOpen)
        {
            var items = new[]
            {
                ProposalTypeLabel(DiplomaticStatus.NonAggressionPact),
                ProposalTypeLabel(DiplomaticStatus.Alliance),
                ProposalTypeLabel(DiplomaticStatus.War),
            };
            var typeIndex = vm.SelectedProposalType switch
            {
                DiplomaticStatus.NonAggressionPact => 0,
                DiplomaticStatus.Alliance => 1,
                DiplomaticStatus.War => 2,
                _ => 0,
            };

            Dropdown.DrawMenuVisual(drawList, x, typeButtonY, DropdownWidth, DropdownHeight, items, typeIndex);
            var (newIndex, shouldClose, consumed) = Dropdown.DrawMenuAndHandleInteraction(
                x, typeButtonY, DropdownWidth, DropdownHeight, items, typeIndex);

            dropdownConsumedClick |= consumed;

            if (newIndex != typeIndex)
            {
                var newType = newIndex switch
                {
                    0 => DiplomaticStatus.NonAggressionPact,
                    1 => DiplomaticStatus.Alliance,
                    2 => DiplomaticStatus.War,
                    _ => DiplomaticStatus.NonAggressionPact,
                };
                events.Add(new DiplomacyEvent.SelectProposalType(newType));
            }

            if (shouldClose) events.Add(new DiplomacyEvent.ToggleTypeDropdown(false));
        }

        return dropdownConsumedClick;
    }

    private static float DrawOrnateDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDividerOrnate(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static string ProposalTypeLabel(DiplomaticStatus status) => status switch
    {
        DiplomaticStatus.Alliance =>
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TYPE_ALLIANCE),
        DiplomaticStatus.NonAggressionPact =>
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_TYPE_NAP),
        DiplomaticStatus.War =>
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_TYPE_WAR),
        _ => LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_MANNER_PLACEHOLDER),
    };

    private static string? ProposalDescription(DiplomaticStatus status) => status switch
    {
        DiplomaticStatus.Alliance =>
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_DESC_ALLIANCE),
        DiplomaticStatus.NonAggressionPact =>
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_DESC_NAP),
        DiplomaticStatus.War =>
            LocalizationService.Instance.Get(LocalizationKeys.UI_PROPOSE_ACCORD_DESC_WAR),
        _ => null,
    };

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }
}

// todo: migrate to correct path
public readonly struct ProposeAccordViewModel(
    float x,
    float y,
    float width,
    float height,
    bool hasCivilization,
    bool isFounder,
    string? errorMessage,
    List<CivilizationInfo> availableCivilizations,
    string selectedCivId,
    DiplomaticStatus selectedProposalType,
    int currentRank,
    int requiredRankForSelection,
    string requiredRankNameForSelection,
    bool isCivDropdownOpen,
    bool isTypeDropdownOpen,
    string? confirmWarCivId)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public bool HasCivilization { get; } = hasCivilization;
    public bool IsFounder { get; } = isFounder;
    public string? ErrorMessage { get; } = errorMessage;
    public List<CivilizationInfo> AvailableCivilizations { get; } = availableCivilizations;
    public string SelectedCivId { get; } = selectedCivId;
    public DiplomaticStatus SelectedProposalType { get; } = selectedProposalType;
    public int CurrentRank { get; } = currentRank;
    public int RequiredRankForSelection { get; } = requiredRankForSelection;
    public string RequiredRankNameForSelection { get; } = requiredRankNameForSelection;
    public bool IsCivDropdownOpen { get; } = isCivDropdownOpen;
    public bool IsTypeDropdownOpen { get; } = isTypeDropdownOpen;
    public string? ConfirmWarCivId { get; } = confirmWarCivId;
}

// todo: migrate to correct path
public record ProposeAccordRenderResult(List<DiplomacyEvent> Events, float Height);
