using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Create;
using DivineAscension.GUI.UI.Components;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
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
    private const float DividerSpacingTop = 8f;
    private const float DividerSpacingBottom = 14f;
    private const float SectionGap = 8f;
    private const float LabelHeight = 22f;
    private const float NameInputHeight = 30f;
    private const float DescriptionInputHeight = 80f;
    private const float ErrorRowHeight = 22f;
    private const float ButtonWidth = 200f;
    private const float ButtonHeight = 36f;

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
        currentY += MathF.Max(TextRenderer.MeasureWrappedHeight(intro, contentWidth, Body), 20f) + 8f;

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawNameSection(drawList, vm, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawDescriptionSection(drawList, vm, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawSigilSection(drawList, vm, currentY, contentWidth, events);

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

        currentY += NameInputHeight + 4f;
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

        currentY += DescriptionInputHeight + 4f;
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

    private static float DrawSigilSection(
        ImDrawListPtr drawList,
        CivilizationCreateViewModel vm,
        float y,
        float contentWidth,
        List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_CREATE_ICON_LABEL),
            vm.X, y, SubsectionLabel, ColorPalette.Gold);
        var currentY = y + LabelHeight;

        var (clickedIcon, pickerHeight) = IconPicker.Draw(
            drawList,
            CivilizationIconLoader.GetAvailableIcons(),
            vm.SelectedIcon,
            vm.X,
            currentY,
            contentWidth);

        if (clickedIcon != null)
            events.Add(new CreateEvent.IconSelected(clickedIcon));

        return currentY + pickerHeight + SectionGap;
    }

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

        return y + ButtonHeight + 12f;
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerSpacingTop;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return dividerY + DividerSpacingBottom;
    }
}
