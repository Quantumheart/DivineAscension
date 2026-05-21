using System;
using System.Collections.Generic;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for religion info header and stats grid
/// Displays: Religion name, deity, member count, founder, prestige
/// </summary>
internal static class ReligionInfoHeaderRenderer
{
    /// <summary>
    /// Renders the header section with religion name and info grid
    /// Returns the updated Y position after rendering
    /// </summary>
    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;

        // === RELIGION HEADER ===
        // Mirror the CivilizationInfo layout: deity icon at left, religion
        // name (white, SectionHeader) next to it, then a gold bracket tag
        // showing the prestige rank tier.
        const float iconSize = 32f;
        var deityDomain = DomainHelper.ParseDeityType(viewModel.Deity);
        var iconTextureId = DeityIconLoader.GetDeityTextureId(deityDomain);
        if (iconTextureId != IntPtr.Zero)
        {
            var iconMin = new Vector2(x, currentY);
            var iconMax = new Vector2(x + iconSize, currentY + iconSize);
            var tint = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tint);

            var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
            drawList.AddRect(iconMin, iconMax, borderColor, 4f, ImDrawFlags.None, 1f);
        }

        TextRenderer.DrawLabel(drawList, viewModel.ReligionName,
            x + iconSize + 12f, currentY + 4f, PageTitle, ColorPalette.White);

        // Approximate the scaled name width and place the bracket tag after it.
        var nameWidthScaled = ImGui.CalcTextSize(viewModel.ReligionName).X
                              * (PageTitle / SubsectionLabel);
        if (!string.IsNullOrEmpty(viewModel.PrestigeRank))
        {
            var rankText = $"[{viewModel.PrestigeRank}]";
            drawList.AddText(ImGui.GetFont(), SubsectionLabel,
                new Vector2(x + iconSize + 12f + nameWidthScaled + 8f, currentY + 6f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), rankText);
        }
        currentY += Math.Max(iconSize + 4f, 32f);

        // Ornamental divider under the header.
        ChromeRenderer.DrawDivider(drawList, x, currentY, width);
        currentY += 20f;

        // Deity · · · · · Hroth (Wild) — leader row, matching the rest.
        // Edit affordance for founders moves to its own row underneath when
        // not actively editing, so the leader's right-aligned value stays
        // clean. Editing flow drops below.
        if (viewModel.IsEditingDeityName)
        {
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
                x, currentY, Body, ColorPalette.Grey);
            currentY = DrawDeityNameEditMode(viewModel, drawList, x, currentY, width, events);
        }
        else
        {
            var deityDisplay = !string.IsNullOrWhiteSpace(viewModel.DeityName)
                ? $"{viewModel.DeityName} ({viewModel.Deity})"
                : viewModel.Deity;

            ChromeRenderer.DrawLeader(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
                deityDisplay,
                x, currentY, width);
            currentY += 22f;

            if (viewModel.IsFounder)
            {
                const float editButtonWidth = 60f;
                const float editButtonHeight = 20f;
                if (ButtonRenderer.DrawButton(drawList, "Edit",
                        x + width - editButtonWidth, currentY,
                        editButtonWidth, editButtonHeight,
                        isPrimary: false, enabled: true))
                {
                    events.Add(new InfoEvent.EditDeityNameOpen());
                }
                currentY += editButtonHeight + 4f;
            }
        }

        // Members · · · · · 12
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
            viewModel.MemberCount.ToString(),
            x, currentY, width);
        currentY += 22f;

        // Founder · · · · · Aelric    (value painted gold)
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_FOUNDER_LABEL),
            viewModel.GetFounderDisplayName(),
            x, currentY, width,
            valueColor: ColorPalette.Gold);
        currentY += 22f;

        // Prestige · · · · · 42 (II)
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_LABEL),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_VALUE,
                viewModel.Prestige, viewModel.PrestigeRank),
            x, currentY, width);
        currentY += 28f;

        return currentY;
    }

    /// <summary>
    /// Renders the deity name edit mode UI
    /// </summary>
    private static float DrawDeityNameEditMode(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;
        const float inputHeight = 28f;
        const float inputWidth = 300f;
        const float buttonWidth = 60f;
        const float buttonHeight = 24f;
        const float buttonGap = 8f;

        // Input field
        var newValue = TextInput.Draw(
            drawList,
            "##editDeityName",
            viewModel.EditDeityNameValue,
            x + 80f,
            currentY,
            inputWidth,
            inputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_PLACEHOLDER),
            48);

        if (newValue != viewModel.EditDeityNameValue)
        {
            events.Add(new InfoEvent.EditDeityNameChanged(newValue));
        }

        currentY += inputHeight + 4f;

        // Error message
        if (!string.IsNullOrEmpty(viewModel.DeityNameError))
        {
            TextRenderer.DrawErrorText(drawList, viewModel.DeityNameError, x + 80f, currentY);
            currentY += 20f;
        }

        // Save/Cancel buttons
        var buttonX = x + 80f;

        // Save button
        var canSave = !viewModel.IsSavingDeityName &&
                      !string.IsNullOrWhiteSpace(viewModel.EditDeityNameValue) &&
                      viewModel.EditDeityNameValue.Length >= 2 &&
                      viewModel.EditDeityNameValue.Length <= 48;

        if (ButtonRenderer.DrawButton(
                drawList,
                viewModel.IsSavingDeityName
                    ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVING)
                    : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVE),
                buttonX,
                currentY,
                buttonWidth,
                buttonHeight,
                isPrimary: true,
                enabled: canSave))
        {
            events.Add(new InfoEvent.EditDeityNameSave(viewModel.EditDeityNameValue));
        }

        buttonX += buttonWidth + buttonGap;

        // Cancel button
        if (ButtonRenderer.DrawButton(
                drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_CANCEL),
                buttonX,
                currentY,
                buttonWidth,
                buttonHeight,
                isPrimary: false,
                enabled: !viewModel.IsSavingDeityName))
        {
            events.Add(new InfoEvent.EditDeityNameCancel());
        }

        currentY += buttonHeight + 8f;

        // Member count (show on separate row in edit mode)
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
            x, currentY, Body, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), viewModel.MemberCount.ToString());

        currentY += 22f;

        return currentY;
    }
}