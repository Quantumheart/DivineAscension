using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Create;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for the "Found an Order" ledger chapter (I.vi, #322). Serif
/// chapter title with prose intro, manuscript-phrased name fields, domain
/// tab row, public/private vow toggle whose label swaps with state, and a
/// right-aligned "Inscribe the Founding Vow" action. Ornamental dividers
/// segment Names / Domain / Visibility / Vow.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionCreateRenderer
{
    private static float FormWidth => UiScale.Scaled(500f);
    private static float DividerHeight => UiScale.Scaled(18f);
    private static float DividerYPadding => UiScale.Scaled(6f);
    private static float SectionLabelHeight => UiScale.Scaled(22f);
    private static float FieldRowHeight => UiScale.Scaled(40f);
    private static float InputHeight => UiScale.Scaled(32f);
    private static float FooterTopPadding => UiScale.Scaled(12f);

    public static ReligionCreateRenderResult Draw(
        ReligionCreateViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<CreateEvent>();

        // === CHAPTER TITLE STRIP — shared codex chrome ===
        var selectedDomain = DomainHelper.ParseDeityType(viewModel.Domain);
        var strip = ChapterStripRenderer.Draw(drawList,
            viewModel.X, viewModel.Y, viewModel.Width, scrollY: 0f,
            title: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_CREATE_TITLE),
            rightGlyph: selectedDomain);
        var currentY = strip.BodyY;
        var contentWidth = strip.ContentWidth;

        // Centered form column inside the chapter content width.
        var formX = viewModel.X + (contentWidth - FormWidth) / 2f;

        // === PROSE INTRO ===
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_CREATE_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, formX, currentY, FormWidth, Body, ColorPalette.White);
        currentY += TextRenderer.MeasureWrappedHeight(intro, FormWidth, Body) + UiScale.Scaled(8f);

        currentY = DrawDivider(drawList, formX, currentY, FormWidth);

        // === NAMES ===
        currentY = DrawReligionNameGroup(viewModel, drawList, formX, currentY, FormWidth, events);
        currentY = DrawDeityNameGroup(viewModel, drawList, formX, currentY, FormWidth, events);
        currentY = DrawMottoGroup(viewModel, drawList, formX, currentY, FormWidth, events);

        currentY = DrawDivider(drawList, formX, currentY, FormWidth);

        // === DOMAIN ===
        var hoveredDomainName = DrawDomainGroup(viewModel, drawList, formX, FormWidth, events, ref currentY);

        currentY = DrawDivider(drawList, formX, currentY, FormWidth);

        // === VISIBILITY VOW ===
        var vowLabel = LocalizationService.Instance.Get(viewModel.IsPublic
            ? LocalizationKeys.UI_RELIGION_PUBLIC_VOW
            : LocalizationKeys.UI_RELIGION_PRIVATE_VOW);
        var newIsPublic = CheckboxRenderer.DrawCheckbox(drawList, vowLabel, formX, currentY, viewModel.IsPublic);
        if (newIsPublic != viewModel.IsPublic)
            events.Add(new CreateEvent.IsPublicChanged(newIsPublic));
        currentY += UiScale.Scaled(32f);

        // Error message (if any) before the vow button.
        if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
        {
            TextRenderer.DrawErrorText(drawList, viewModel.ErrorMessage, formX, currentY);
            currentY += UiScale.Scaled(26f);
        }

        currentY = DrawDivider(drawList, formX, currentY, FormWidth);

        // === VOW BUTTON (right-aligned) ===
        currentY += FooterTopPadding;
        var buttonWidth = UiScale.Scaled(220f);
        var buttonHeight = UiScale.Scaled(36f);
        var buttonX = formX + FormWidth - buttonWidth;

        if (ButtonRenderer.DrawButton(
                drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_CREATE_BUTTON),
                buttonX,
                currentY,
                buttonWidth,
                buttonHeight,
                isPrimary: true,
                enabled: viewModel.CanCreate))
        {
            events.Add(new CreateEvent.SubmitClicked());
        }

        currentY += buttonHeight + UiScale.Scaled(6f);

        if (!string.IsNullOrEmpty(hoveredDomainName))
        {
            var mousePos = ImGui.GetMousePos();
            DeityTooltipRenderer.Draw(
                hoveredDomainName,
                mousePos.X,
                mousePos.Y,
                viewModel.Width,
                viewModel.Height);
        }

        return new ReligionCreateRenderResult(events, currentY - viewModel.Y);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawReligionNameGroup(
        ReligionCreateViewModel viewModel, ImDrawListPtr drawList,
        float formX, float currentY, float fieldWidth, List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_LABEL),
            formX, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        var newReligionName = TextInput.Draw(
            drawList,
            "##createReligionName",
            viewModel.ReligionName,
            formX, currentY, fieldWidth, InputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_PLACEHOLDER),
            32);

        if (newReligionName != viewModel.ReligionName)
            events.Add(new CreateEvent.NameChanged(newReligionName));

        currentY += FieldRowHeight;

        if (!string.IsNullOrWhiteSpace(viewModel.ReligionName))
        {
            if (viewModel.ReligionName.Length < 3)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_ERROR_TOO_SHORT),
                    formX, currentY);
                currentY += UiScale.Scaled(25f);
            }
            else if (viewModel.ReligionName.Length > 32)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_ERROR_TOO_LONG),
                    formX, currentY);
                currentY += UiScale.Scaled(25f);
            }
            else if (viewModel.ReligionNameHasProfanity)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_ERROR_PROFANITY,
                        viewModel.ReligionNameProfanityWord ?? ""), formX, currentY);
                currentY += UiScale.Scaled(25f);
            }
        }

        return currentY;
    }

    private static float DrawDeityNameGroup(
        ReligionCreateViewModel viewModel, ImDrawListPtr drawList,
        float formX, float currentY, float fieldWidth, List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_LABEL),
            formX, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        var newDeityName = TextInput.Draw(
            drawList,
            "##createDeityName",
            viewModel.DeityName,
            formX, currentY, fieldWidth, InputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_PLACEHOLDER),
            48);

        if (newDeityName != viewModel.DeityName)
            events.Add(new CreateEvent.DeityNameChanged(newDeityName));

        currentY += FieldRowHeight;

        if (!string.IsNullOrWhiteSpace(viewModel.DeityName))
        {
            if (viewModel.DeityName.Length < 2)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_ERROR_TOO_SHORT),
                    formX, currentY);
                currentY += UiScale.Scaled(25f);
            }
            else if (viewModel.DeityName.Length > 48)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_ERROR_TOO_LONG),
                    formX, currentY);
                currentY += UiScale.Scaled(25f);
            }
            else if (viewModel.DeityNameHasProfanity)
            {
                TextRenderer.DrawErrorText(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_ERROR_PROFANITY,
                        viewModel.DeityNameProfanityWord ?? ""), formX, currentY);
                currentY += UiScale.Scaled(25f);
            }
        }

        return currentY;
    }

    private static float DrawMottoGroup(
        ReligionCreateViewModel viewModel, ImDrawListPtr drawList,
        float formX, float currentY, float fieldWidth, List<CreateEvent> events)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MOTTO_HEADING),
            formX, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        var newMotto = TextInput.Draw(
            drawList,
            "##createReligionMotto",
            viewModel.Motto,
            formX, currentY, fieldWidth, InputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MOTTO_PLACEHOLDER),
            80);

        if (newMotto != viewModel.Motto)
            events.Add(new CreateEvent.MottoChanged(newMotto));

        currentY += FieldRowHeight;

        if (viewModel.MottoTooLong)
        {
            TextRenderer.DrawErrorText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_MOTTO_TOO_LONG),
                formX, currentY);
            currentY += UiScale.Scaled(25f);
        }
        else if (viewModel.MottoHasProfanity)
        {
            TextRenderer.DrawErrorText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_NAME_ERROR_PROFANITY,
                    viewModel.MottoProfanityWord ?? ""), formX, currentY);
            currentY += UiScale.Scaled(25f);
        }

        return currentY;
    }

    private static string? DrawDomainGroup(
        ReligionCreateViewModel viewModel, ImDrawListPtr drawList,
        float formX, float fieldWidth, List<CreateEvent> events, ref float currentY)
    {
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_PATRON_HEADING),
            formX, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight + UiScale.Scaled(4f);

        var currentDomainIndex = viewModel.GetCurrentDomainIndex();
        var domains = viewModel.AvailableDomains;
        var count = domains.Length;
        if (count == 0) return null;

        var buttonHeight = UiScale.Scaled(36f);
        var spacing = UiScale.Scaled(6f);
        var buttonWidth = (fieldWidth - spacing * (count - 1)) / count;

        string? hoveredDomainName = null;
        for (var i = 0; i < count; i++)
        {
            var bx = formX + i * (buttonWidth + spacing);
            var domainName = domains[i];
            var isSelected = i == currentDomainIndex;
            var (clicked, hovering) = DrawDomainButton(drawList, domainName, bx, currentY, buttonWidth, buttonHeight, isSelected);
            if (clicked && !isSelected)
                events.Add(new CreateEvent.DeityChanged(domainName));
            if (hovering)
                hoveredDomainName = domainName;
        }

        currentY += buttonHeight + UiScale.Scaled(4f);
        return hoveredDomainName;
    }

    private static (bool clicked, bool hovering) DrawDomainButton(
        ImDrawListPtr drawList, string domainName,
        float x, float y, float width, float height, bool isSelected)
    {
        var topLeft = new Vector2(x, y);
        var bottomRight = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var hovering = mousePos.X >= x && mousePos.X <= x + width &&
                       mousePos.Y >= y && mousePos.Y <= y + height;

        var bgColor = isSelected
            ? ColorPalette.Gold * 0.4f
            : hovering
                ? ColorPalette.LightBrown
                : ColorPalette.DarkBrown;
        if (hovering && !isSelected) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        drawList.AddRectFilled(topLeft, bottomRight, ImGui.ColorConvertFloat4ToU32(bgColor), UiScale.Scaled(4f));
        drawList.AddRect(topLeft, bottomRight,
            ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.BorderColor),
            UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(2f));

        // Glyph centered in the button — tooltip provides the domain name on hover.
        var glyphSize = height * 0.7f;
        var glyphMin = new Vector2(x + (width - glyphSize) / 2f, y + (height - glyphSize) / 2f);
        var glyphMax = new Vector2(glyphMin.X + glyphSize, glyphMin.Y + glyphSize);
        var domain = DomainHelper.ParseDeityType(domainName);
        DomainGlyphRenderer.Draw(drawList, domain, glyphMin, glyphMax, ColorPalette.LightText);

        var clicked = hovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        return (clicked, hovering);
    }
}
