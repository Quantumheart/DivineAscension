using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Create;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
/// Pure renderer for the "Found a Realm" ledger chapter (II.iv, #328). Chapter
/// title + prose intro, then Name / Description / Sigil / Action sections
/// separated by ornamental dividers, ending in a single centred "Raise the
/// Banner" button. The sidebar already provides the back path, so no cancel.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationCreateRenderer
{
    private static float DividerSpacingTop => UiScale.Scaled(8f);
    private static float DividerSpacingBottom => UiScale.Scaled(14f);
    private static float SectionGap => UiScale.Scaled(8f);
    private static float LabelHeight => UiScale.Scaled(22f);
    private static float NameInputHeight => UiScale.Scaled(30f);
    private static float DescriptionInputHeight => UiScale.Scaled(80f);
    private static float ErrorRowHeight => UiScale.Scaled(22f);
    private static float ButtonWidth => UiScale.Scaled(200f);
    private static float ButtonHeight => UiScale.Scaled(36f);

    public static CivilizationCreateRenderResult Draw(
        CivilizationCreateViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<CreateEvent>();

        var strip = ChapterStripRenderer.Draw(drawList, vm.X, vm.Y, vm.Width, 0f,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_CHAPTER_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // Prose intro on parchment → iron-gall ink.
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_CHAPTER_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, vm.X, currentY, contentWidth, Body, ColorPalette.White);
        currentY += MathF.Max(TextRenderer.MeasureWrappedHeight(intro, contentWidth, Body), UiScale.Scaled(20f)) + UiScale.Scaled(8f);

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawNameSection(drawList, vm, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawDescriptionSection(drawList, vm, currentY, contentWidth, events);

        // Sigil section hidden — current PNG picker does not match the ledger
        // chrome the rest of the civ flow uses. Tracked in #385; the data path
        // (Civilization.Icon, packet field, CivilizationIconLoader) is left
        // intact so the section can return without a schema change.

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawEthosSection(drawList, vm, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawFoundingAction(drawList, vm, currentY, contentWidth, events);

        return new CivilizationCreateRenderResult(events, currentY - vm.Y);
    }

    private static float DrawNameSection(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y,
        float contentWidth,
        List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_NAME_LABEL),
            vm.X, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + LabelHeight;

        var newName = TextInput.Draw(drawList, "##createCivName", vm.CivilizationName,
            vm.X, currentY, contentWidth, NameInputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_NAME_PLACEHOLDER), 32);

        if (newName != vm.CivilizationName)
            events.Add(new CreateEvent.NameChanged(newName));

        currentY += NameInputHeight + UiScale.Scaled(4f);
        currentY = DrawNameValidation(drawList, vm, currentY);

        return currentY + SectionGap;
    }

    private static float DrawNameValidation(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y)
    {
        if (string.IsNullOrWhiteSpace(vm.CivilizationName)) return y;

        string? message = null;
        if (vm.CivilizationName.Length < 3)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_NAME_ERROR_TOO_SHORT);
        }
        else if (vm.CivilizationName.Length > 32)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_NAME_ERROR_TOO_LONG);
        }
        else if (vm.HasProfanity)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_NAME_ERROR_PROFANITY,
                vm.ProfanityMatchedWord ?? string.Empty);
        }

        if (message == null) return y;
        TextRenderer.DrawErrorText(drawList, message, vm.X, y);
        return y + ErrorRowHeight;
    }

    private static float DrawDescriptionSection(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y,
        float contentWidth,
        List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_DESCRIPTION_LABEL),
            vm.X, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + LabelHeight;

        var newDescription = TextInput.DrawMultiline(drawList, "##createCivDescription", vm.Description,
            vm.X, currentY, contentWidth, DescriptionInputHeight, 200);

        if (newDescription != vm.Description)
            events.Add(new CreateEvent.DescriptionChanged(newDescription));

        currentY += DescriptionInputHeight + UiScale.Scaled(4f);
        currentY = DrawDescriptionValidation(drawList, vm, currentY);

        return currentY + SectionGap;
    }

    private static float DrawDescriptionValidation(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y)
    {
        if (string.IsNullOrEmpty(vm.Description)) return y;

        string? message = null;
        if (vm.Description.Length > 200)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DESCRIPTION_ERROR_TOO_LONG);
        }
        else if (vm.HasProfanityInDescription)
        {
            message = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DESCRIPTION_ERROR_PROFANITY,
                vm.ProfanityMatchedWordInDescription ?? string.Empty);
        }

        if (message == null) return y;
        TextRenderer.DrawErrorText(drawList, message, vm.X, y);
        return y + ErrorRowHeight;
    }

    private static readonly CivilizationEthos[] EthosOrder =
    {
        CivilizationEthos.Sovereign,
        CivilizationEthos.Mercantile,
        CivilizationEthos.Martial,
        CivilizationEthos.Mystic,
        CivilizationEthos.Ascetic
    };

    private static float EthosButtonHeight => UiScale.Scaled(30f);
    private static float EthosButtonGap => UiScale.Scaled(6f);
    private static float EthosHintTopPadding => UiScale.Scaled(4f);

    private static float DrawEthosSection(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y,
        float contentWidth,
        List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_ETHOS_LABEL),
            vm.X, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + LabelHeight;

        var totalGaps = EthosButtonGap * (EthosOrder.Length - 1);
        var buttonWidth = MathF.Floor((contentWidth - totalGaps) / EthosOrder.Length);

        for (var i = 0; i < EthosOrder.Length; i++)
        {
            var ethos = EthosOrder[i];
            var buttonX = vm.X + i * (buttonWidth + EthosButtonGap);
            var isSelected = ethos == vm.SelectedEthos;
            var label = LocalizationService.Instance.Get(EthosLocKey(ethos));

            if (ButtonRenderer.DrawButton(drawList, label,
                    buttonX, currentY, buttonWidth, EthosButtonHeight,
                    isPrimary: isSelected,
                    enabled: vm.UserIsReligionFounder && !vm.UserInCivilization))
            {
                events.Add(new CreateEvent.EthosSelected(ethos));
            }
        }

        currentY += EthosButtonHeight + EthosHintTopPadding;

        var hint = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_ETHOS_HINT);
        TextRenderer.DrawInfoText(drawList, hint, vm.X, currentY, contentWidth, Secondary, ColorPalette.Grey);
        currentY += MathF.Max(TextRenderer.MeasureWrappedHeight(hint, contentWidth, Secondary),
            Secondary + LinePadding);

        return currentY + SectionGap;
    }

    private static string EthosLocKey(CivilizationEthos ethos) => ethos switch
    {
        CivilizationEthos.Mercantile => LocalizationKeys.CIVILIZATION_ETHOS_MERCANTILE,
        CivilizationEthos.Martial => LocalizationKeys.CIVILIZATION_ETHOS_MARTIAL,
        CivilizationEthos.Mystic => LocalizationKeys.CIVILIZATION_ETHOS_MYSTIC,
        CivilizationEthos.Ascetic => LocalizationKeys.CIVILIZATION_ETHOS_ASCETIC,
        _ => LocalizationKeys.CIVILIZATION_ETHOS_SOVEREIGN
    };

    private static float DrawFoundingAction(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y,
        float contentWidth,
        List<CreateEvent> events)
    {
        var buttonX = vm.X + (contentWidth - ButtonWidth) / 2f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_BUTTON),
                buttonX, y, ButtonWidth, ButtonHeight,
                isPrimary: true, enabled: vm.CanCreate))
        {
            events.Add(new CreateEvent.SubmitClicked());
        }

        return y + ButtonHeight + UiScale.Scaled(12f);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerSpacingTop;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return dividerY + DividerSpacingBottom;
    }
}
