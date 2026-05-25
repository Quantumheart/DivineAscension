using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events;
using DivineAscension.GUI.UI.Components.Banners;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.GUI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
/// Pure renderer for the "The Accords" ledger chapter (#329). Standing pacts
/// rendered as glyph-led prose entries (Alliance → NAP → War), pending
/// proposals as envelope rows, and a closing pointer to the sibling Propose
/// page. The send-proposal form, dropdowns, and war-declaration overlay live
/// on that sibling page; their VM/state fields are still carried here so the
/// state manager can compose them when that chapter ships.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DiplomacyTabRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float OrnateDividerHeight = 22f;
    private const float SectionHeadingHeight = 26f;
    private const float ProseLineHeight = 18f;
    private const float ProseBottomSpacing = 10f;
    private const float ScrollbarWidth = 16f;

    // Standing-accord entry geometry.
    private const float EntryGlyphSize = 18f;
    private const float EntryGlyphGap = 12f;
    private const float EntryLeftPadding = 6f;
    private const float EntryLineHeight = 20f;
    private const float EntryActionHeight = 24f;
    private const float EntryActionWidth = 150f;
    private const float EntryBottomSpacing = 10f;
    private const float SlimDividerHeight = 18f;
    private const float SlimDividerYPadding = 4f;

    // Pending proposal row geometry.
    private const float ProposalEnvelopeSize = 18f;
    private const float ProposalGlyphGap = 10f;
    private const float ProposalTextLeading = 4f;
    private const float ProposalActionHeight = 22f;
    private const float ProposalAcceptWidth = 78f;
    private const float ProposalRefuseWidth = 78f;
    private const float ProposalActionGap = 8f;
    private const float ProposalRowHeight = 28f;
    private const float ProposalRowSpacing = 4f;

    public static DiplomacyTabRendererResult Draw(
        DiplomacyTabViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DiplomacyEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_LOADING),
                x, y, width, height);
            return new DiplomacyTabRendererResult(events, height);
        }

        if (!vm.HasCivilization)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_NO_CIVILIZATION),
                x, y, width, height);
            return new DiplomacyTabRendererResult(events, height);
        }

        var contentHeightEstimate = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = vm.ScrollY;
        if (isHover && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new DiplomacyEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CHAPTER_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // Error banner (still surfaced above the chapter body so dismissals
        // remain reachable even when the page is heavily scrolled).
        if (!string.IsNullOrEmpty(vm.ErrorMessage))
        {
            var consumed = ErrorBannerRenderer.Draw(drawList, x, currentY, contentWidth, vm.ErrorMessage,
                out _, out var dismissClicked, showRetry: false);
            currentY += consumed;
            if (dismissClicked) events.Add(new DiplomacyEvent.DismissError());
        }

        currentY = DrawIntro(drawList, vm, x, currentY, contentWidth);

        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        currentY = DrawStandingSection(drawList, vm, x, currentY, contentWidth, events);

        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        currentY = DrawPendingSection(drawList, vm, x, currentY, contentWidth, events);

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new DiplomacyTabRendererResult(events, height);
    }

    private static float DrawIntro(
        ImDrawListPtr drawList, DiplomacyTabViewModel vm, float x, float y, float width)
    {
        var prose = string.IsNullOrWhiteSpace(vm.CurrentCivilizationName)
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CHAPTER_INTRO_NO_REALM)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CHAPTER_INTRO,
                vm.CurrentCivilizationName);

        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var measured = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (measured > 0 ? measured : ProseLineHeight) + ProseBottomSpacing;
    }

    private static float DrawStandingSection(
        ImDrawListPtr drawList,
        DiplomacyTabViewModel vm,
        float x, float y, float width,
        List<DiplomacyEvent> events)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_STANDING_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionHeadingHeight;

        var ordered = vm.ActiveRelationships
            .OrderBy(r => StatusGroupOrder(r.Status))
            .ThenByDescending(r => r.EstablishedDate)
            .ToList();

        if (ordered.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_STANDING_EMPTY),
                x + EntryLeftPadding, currentY, width - EntryLeftPadding, Body, ColorPalette.Grey);
            return currentY + EntryLineHeight + EntryBottomSpacing;
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            currentY = DrawStandingEntry(drawList, ordered[i], x, currentY, width, events);
            if (i < ordered.Count - 1)
                currentY = DrawSlimDivider(drawList, x, currentY, width);
        }

        return currentY + EntryBottomSpacing;
    }

    private static float DrawStandingEntry(
        ImDrawListPtr drawList,
        DiplomacyInfoResponsePacket.RelationshipInfo rel,
        float x, float y, float width,
        List<DiplomacyEvent> events)
    {
        var entryX = x + EntryLeftPadding;
        var glyphCx = entryX + EntryGlyphSize / 2f;
        var headerCy = y + EntryLineHeight / 2f;
        DrawStatusMark(drawList, rel.Status, glyphCx, headerCy, EntryGlyphSize);

        var textX = entryX + EntryGlyphSize + EntryGlyphGap;
        var textRight = x + width;
        var sentence = StatusSentence(rel.Status, rel.OtherCivName);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), sentence);

        var lineY = y + EntryLineHeight;
        var swornLine = LocalizationService.Instance.Get(
            LocalizationKeys.UI_DIPLOMACY_SWORN_ON, FormatDate(rel.EstablishedDate));
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, lineY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), swornLine);
        lineY += EntryLineHeight;

        // Expiry line (skipped for War — wars don't expire on a clock).
        if (rel.Status != DiplomaticStatus.War)
        {
            var expiryLine = rel.ExpiresDate.HasValue
                ? LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_EXPIRES_ON,
                    FormatDate(rel.ExpiresDate.Value))
                : LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PERMANENT_LINE);
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, lineY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), expiryLine);
            lineY += EntryLineHeight;
        }

        // Grievance prose (alliance / NAP only — no grievance counter on a war).
        if (rel.Status is DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact)
        {
            var grievanceText = GrievanceLine(rel.ViolationCount);
            var grievanceColor = rel.ViolationCount >= 2 ? ColorPalette.Red : ColorPalette.Grey;
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, lineY),
                ImGui.ColorConvertFloat4ToU32(grievanceColor), grievanceText);
            lineY += EntryLineHeight;
        }

        // Scheduled-break countdown prose for alliance / NAP — sits above the
        // Recall button so the reader scans cause then remedy.
        var hasScheduledBreak = false;
        if (rel.Status is DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact
            && rel.BreakScheduledDate.HasValue
            && (rel.BreakScheduledDate.Value - DateTime.UtcNow).TotalSeconds > 0)
        {
            hasScheduledBreak = true;
            var formatted = DiplomacyNotificationHelper.FormatTimeRemaining(rel.BreakScheduledDate.Value);
            var countdown = LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_BREAK_COUNTDOWN, formatted);
            var isCritical = DiplomacyNotificationHelper.IsTimeCritical(rel.BreakScheduledDate.Value);
            var countdownColor = isCritical ? ColorPalette.Red : ColorPalette.White;
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, lineY),
                ImGui.ColorConvertFloat4ToU32(countdownColor), countdown);
            lineY += EntryLineHeight;
        }

        // Right-aligned action button per status.
        var actionY = lineY;
        DrawEntryAction(drawList, rel, hasScheduledBreak, textRight, actionY, events);
        var bottomY = actionY + EntryActionHeight + 4f;
        return bottomY;
    }

    private static void DrawEntryAction(
        ImDrawListPtr drawList,
        DiplomacyInfoResponsePacket.RelationshipInfo rel,
        bool hasScheduledBreak,
        float rightEdge,
        float y,
        List<DiplomacyEvent> events)
    {
        var buttonX = rightEdge - EntryActionWidth;
        switch (rel.Status)
        {
            case DiplomaticStatus.War:
                if (ButtonRenderer.DrawButton(drawList,
                        LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_SUE_FOR_PEACE_BUTTON),
                        buttonX, y, EntryActionWidth, EntryActionHeight))
                {
                    events.Add(new DiplomacyEvent.DeclarePeace(rel.OtherCivId));
                }
                break;

            case DiplomaticStatus.Alliance:
            case DiplomaticStatus.NonAggressionPact:
                if (hasScheduledBreak)
                {
                    if (ButtonRenderer.DrawButton(drawList,
                            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_RECALL_BUTTON),
                            buttonX, y, EntryActionWidth, EntryActionHeight))
                    {
                        events.Add(new DiplomacyEvent.CancelBreak(rel.OtherCivId));
                    }
                }
                else
                {
                    if (ButtonRenderer.DrawButton(drawList,
                            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_SCHEDULE_BREAK_VERB),
                            buttonX, y, EntryActionWidth, EntryActionHeight))
                    {
                        events.Add(new DiplomacyEvent.ScheduleBreak(rel.OtherCivId));
                    }
                }
                break;
        }
    }

    private static float DrawPendingSection(
        ImDrawListPtr drawList,
        DiplomacyTabViewModel vm,
        float x, float y, float width,
        List<DiplomacyEvent> events)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PENDING_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionHeadingHeight;

        if (vm.IncomingProposals.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_PENDING_EMPTY),
                x + EntryLeftPadding, currentY, width - EntryLeftPadding, Body, ColorPalette.Grey);
            return currentY + EntryLineHeight + EntryBottomSpacing;
        }

        foreach (var proposal in vm.IncomingProposals)
        {
            currentY = DrawProposalRow(drawList, proposal, x, currentY, width, events);
            currentY += ProposalRowSpacing;
        }

        return currentY + EntryBottomSpacing - ProposalRowSpacing;
    }

    private static float DrawProposalRow(
        ImDrawListPtr drawList,
        DiplomacyInfoResponsePacket.ProposalInfo proposal,
        float x, float y, float width,
        List<DiplomacyEvent> events)
    {
        var rowX = x + EntryLeftPadding;
        var rowCy = y + ProposalRowHeight / 2f;

        ChromeRenderer.DrawEnvelope(drawList,
            rowX + ProposalEnvelopeSize / 2f, rowCy, ProposalEnvelopeSize, ColorPalette.White);

        var textX = rowX + ProposalEnvelopeSize + ProposalGlyphGap;
        var sentence = ProposalSentence(proposal);
        var sentenceColor = DiplomacyNotificationHelper.IsTimeCritical(proposal.ExpiresDate)
            ? ColorPalette.Red
            : ColorPalette.White;
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(textX, y + ProposalTextLeading),
            ImGui.ColorConvertFloat4ToU32(sentenceColor), sentence);

        var rightEdge = x + width;
        var refuseX = rightEdge - ProposalRefuseWidth;
        var acceptX = refuseX - ProposalActionGap - ProposalAcceptWidth;
        var buttonY = y + (ProposalRowHeight - ProposalActionHeight) / 2f;

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_ACCEPT_VERB),
                acceptX, buttonY, ProposalAcceptWidth, ProposalActionHeight, isPrimary: true))
        {
            events.Add(new DiplomacyEvent.AcceptProposal(proposal.ProposalId));
        }

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_REFUSE_VERB),
                refuseX, buttonY, ProposalRefuseWidth, ProposalActionHeight))
        {
            events.Add(new DiplomacyEvent.DeclineProposal(proposal.ProposalId));
        }

        return y + ProposalRowHeight;
    }

    private static float DrawOrnateDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDividerOrnate(drawList, x, dividerY, width);
        return y + OrnateDividerHeight;
    }

    private static float DrawSlimDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var inset = width * 0.20f;
        var dividerY = y + SlimDividerYPadding;
        ChromeRenderer.DrawDivider(drawList,
            x + inset, dividerY, width - inset * 2f,
            ColorPalette.Gold * 0.35f);
        return y + SlimDividerHeight;
    }

    private static void DrawStatusMark(
        ImDrawListPtr drawList, DiplomaticStatus status, float cx, float cy, float size)
    {
        var half = size / 2f;
        switch (status)
        {
            case DiplomaticStatus.Alliance:
                // Filled gold diamond — sworn bond.
                ChromeRenderer.DrawDiamond(drawList, cx, cy, half, ColorPalette.Gold);
                break;

            case DiplomaticStatus.NonAggressionPact:
                // Outlined diamond in cream — quieter than alliance, still a pact.
                DrawDiamondOutline(drawList, cx, cy, half, ColorPalette.LightText);
                break;

            case DiplomaticStatus.War:
                // Crossed blades — two diagonals through the centre.
                var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Vermilion);
                drawList.AddLine(new Vector2(cx - half, cy - half),
                    new Vector2(cx + half, cy + half), color, 2f);
                drawList.AddLine(new Vector2(cx + half, cy - half),
                    new Vector2(cx - half, cy + half), color, 2f);
                break;
        }
    }

    private static void DrawDiamondOutline(
        ImDrawListPtr drawList, float cx, float cy, float halfSize, Vector4 color)
    {
        var col = ImGui.ColorConvertFloat4ToU32(color);
        var top = new Vector2(cx, cy - halfSize);
        var right = new Vector2(cx + halfSize, cy);
        var bottom = new Vector2(cx, cy + halfSize);
        var left = new Vector2(cx - halfSize, cy);
        drawList.AddLine(top, right, col, 1.5f);
        drawList.AddLine(right, bottom, col, 1.5f);
        drawList.AddLine(bottom, left, col, 1.5f);
        drawList.AddLine(left, top, col, 1.5f);
    }

    private static int StatusGroupOrder(DiplomaticStatus status) => status switch
    {
        DiplomaticStatus.Alliance => 0,
        DiplomaticStatus.NonAggressionPact => 1,
        DiplomaticStatus.War => 2,
        _ => 3,
    };

    private static string StatusSentence(DiplomaticStatus status, string otherName)
    {
        return status switch
        {
            DiplomaticStatus.Alliance => LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_SENTENCE_ALLIANCE, otherName),
            DiplomaticStatus.NonAggressionPact => LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_SENTENCE_NAP, otherName),
            DiplomaticStatus.War => LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_SENTENCE_WAR, otherName),
            _ => otherName,
        };
    }

    private static string GrievanceLine(int count)
    {
        var clamped = Math.Clamp(count, 0, 3);
        return clamped switch
        {
            0 => LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_GRIEVANCES_0),
            1 => LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_GRIEVANCES_1),
            2 => LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_GRIEVANCES_2),
            _ => LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_GRIEVANCES_3),
        };
    }

    private static string ProposalSentence(DiplomacyInfoResponsePacket.ProposalInfo proposal)
    {
        // War proposals from the other side read as a "peace offer" once we
        // already have peace; in practice ProposalInfo only carries NAP /
        // Alliance, but keep a defensive branch.
        return proposal.ProposedStatus switch
        {
            DiplomaticStatus.Alliance => LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_PROPOSAL_OFFER_ALLIANCE, proposal.OtherCivName),
            DiplomaticStatus.NonAggressionPact => LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_PROPOSAL_OFFER_NAP, proposal.OtherCivName),
            _ => LocalizationService.Instance.Get(
                LocalizationKeys.UI_DIPLOMACY_PROPOSAL_OFFER_PEACE, proposal.OtherCivName),
        };
    }

    // Wall-clock ISO date until the VS-calendar phrasing in #316 lands.
    private static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");

    private static float ComputeContentHeight(DiplomacyTabViewModel vm)
    {
        var h = PaneHeaderRenderer.TotalHeight;
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        // Error banner reserves a rough line + padding when present.
        if (!string.IsNullOrEmpty(vm.ErrorMessage)) h += 48f;

        // Intro prose.
        var intro = string.IsNullOrWhiteSpace(vm.CurrentCivilizationName)
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CHAPTER_INTRO_NO_REALM)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_DIPLOMACY_CHAPTER_INTRO,
                vm.CurrentCivilizationName);
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, contentWidth, Body);
        h += (introHeight > 0 ? introHeight : ProseLineHeight) + ProseBottomSpacing;

        h += OrnateDividerHeight;

        // Standing section.
        h += SectionHeadingHeight;
        if (vm.ActiveRelationships.Count == 0)
        {
            h += EntryLineHeight + EntryBottomSpacing;
        }
        else
        {
            foreach (var rel in vm.ActiveRelationships)
                h += EstimateEntryHeight(rel);
            h += (vm.ActiveRelationships.Count - 1) * SlimDividerHeight;
            h += EntryBottomSpacing;
        }

        h += OrnateDividerHeight;

        // Pending section.
        h += SectionHeadingHeight;
        if (vm.IncomingProposals.Count == 0)
            h += EntryLineHeight + EntryBottomSpacing;
        else
            h += vm.IncomingProposals.Count * (ProposalRowHeight + ProposalRowSpacing) + EntryBottomSpacing
                 - ProposalRowSpacing;

        return h;
    }

    private static float EstimateEntryHeight(DiplomacyInfoResponsePacket.RelationshipInfo rel)
    {
        var lines = 2; // sentence + sworn-on
        if (rel.Status != DiplomaticStatus.War) lines += 1;       // expiry / permanent
        if (rel.Status is DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact)
        {
            lines += 1;                                            // grievance line
            if (rel.BreakScheduledDate.HasValue
                && (rel.BreakScheduledDate.Value - DateTime.UtcNow).TotalSeconds > 0)
                lines += 1;                                        // countdown line
        }
        var h = lines * EntryLineHeight + EntryActionHeight + 4f;
        return h;
    }

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }
}

// ViewModel for Diplomacy Tab — chapter renderer + future Propose sibling
// page share this VM, so dropdown / proposal-form fields are still here even
// though "The Accords" page no longer reads them. New ScrollY field drives the
// chapter's scroll position; CurrentCivilizationName feeds the prose intro.
// todo: migrate to correct path
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
    bool isTypeDropdownOpen,
    string currentCivilizationName,
    float scrollY)
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
    public string CurrentCivilizationName { get; } = currentCivilizationName;
    public float ScrollY { get; } = scrollY;
}

// todo: migrate to correct path
public record CivilizationInfo(string CivId, string Name);

// Result containing events
// todo migrate to correct path
public record DiplomacyTabRendererResult(List<DiplomacyEvent> Events, float Height);

// Event types
// todo migrate to correct path
public abstract record DiplomacyEvent
{
    public record ProposeRelationship(string TargetCivId, DiplomaticStatus ProposedStatus) : DiplomacyEvent;

    public record AcceptProposal(string ProposalId) : DiplomacyEvent;

    public record DeclineProposal(string ProposalId) : DiplomacyEvent;

    public record ScheduleBreak(string TargetCivId) : DiplomacyEvent;

    public record CancelBreak(string TargetCivId) : DiplomacyEvent;

    public record DeclareWar(string TargetCivId) : DiplomacyEvent, IModalControlEvent;

    public record DeclarePeace(string TargetCivId) : DiplomacyEvent;

    public record SelectCivilization(string CivId) : DiplomacyEvent;

    public record SelectProposalType(DiplomaticStatus ProposalType) : DiplomacyEvent;

    public record ShowWarConfirmation(string CivId) : DiplomacyEvent;

    public record CancelWarConfirmation() : DiplomacyEvent, IModalControlEvent;

    public record ToggleCivDropdown(bool IsOpen) : DiplomacyEvent;

    public record ToggleTypeDropdown(bool IsOpen) : DiplomacyEvent;

    public record DismissError() : DiplomacyEvent;

    public record ScrollChanged(float NewScrollY) : DiplomacyEvent;
}
