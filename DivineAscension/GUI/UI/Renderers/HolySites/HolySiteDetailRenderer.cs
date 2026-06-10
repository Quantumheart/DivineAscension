using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.HolySite;
using DivineAscension.GUI.Models.HolySite.Detail;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using static DivineAscension.GUI.UI.Utilities.FontSizes;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.HolySites;

/// <summary>
///     Renders detailed view of a holy site with rename and description editing.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class HolySiteDetailRenderer
{
    // Layout constants
    private static float BackButtonWidth => UiScale.Scaled(160f);
    private static float BackButtonHeight => UiScale.Scaled(32f);
    private static float MarkButtonWidth => UiScale.Scaled(130f);
    private static float MarkButtonHeight => UiScale.Scaled(36f);
    private static float IconSize => UiScale.Scaled(85f);
    private static float SectionSpacing => UiScale.Scaled(20f);
    private static float FieldHeight => UiScale.Scaled(32f);
    private static float LabelWidth => UiScale.Scaled(150f);
    private static float EditButtonWidth => UiScale.Scaled(40f);
    private static float EditButtonHeight => UiScale.Scaled(18f);
    private const float DescriptionMaxChars = 200f;

    /// <summary>
    ///     Pure renderer: builds visuals from the view model and emits UI events.
    /// </summary>
    public static HolySiteDetailRendererResult Draw(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DetailEvent>();
        var currentY = vm.Y;

        // Loading state
        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_LOADING),
                vm.X, currentY + UiScale.Scaled(8f), vm.Width);
            return new HolySiteDetailRendererResult(events, vm.Height);
        }

        // Error state
        if (!string.IsNullOrEmpty(vm.ErrorMsg))
        {
            DrawErrorState(vm, drawList, currentY);
            return new HolySiteDetailRendererResult(events, vm.Height);
        }

        // Back button (top left)
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_BACK),
                vm.X, currentY, BackButtonWidth, BackButtonHeight,
                directoryPath: "GUI", iconName: "back"))
        {
            events.Add(new DetailEvent.BackToBrowseClicked());
        }

        // Mark button (top right)
        var markButtonX = vm.X + vm.Width - MarkButtonWidth - UiScale.Scaled(16f);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_MARK),
                markButtonX, currentY, MarkButtonWidth, MarkButtonHeight,
                isPrimary: true))
        {
            events.Add(new DetailEvent.MarkClicked());
        }

        currentY += Spacing.BackButtonRow;

        // Draw background panel
        var backgroundY = currentY;
        var backgroundHeight = vm.Height - (currentY - vm.Y) - UiScale.Scaled(8f);
        drawList.AddRectFilled(
            new Vector2(vm.X, backgroundY),
            new Vector2(vm.X + vm.Width, backgroundY + backgroundHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground),
            UiScale.Scaled(4f));

        // Clip content to container boundaries
        drawList.PushClipRect(new Vector2(vm.X, backgroundY),
            new Vector2(vm.X + vm.Width, backgroundY + backgroundHeight), true);

        currentY += UiScale.Scaled(16f);

        // Two-column layout
        var leftColumnX = vm.X + UiScale.Scaled(97f);
        var leftColumnWidth = (vm.Width / 2) - UiScale.Scaled(110f); // Half width minus icon space
        var rightColumnX = vm.X + (vm.Width / 2) + UiScale.Scaled(20f); // Start right column with padding
        var dividerX = vm.X + (vm.Width / 2) + UiScale.Scaled(5f); // Vertical divider line

        var leftColumnY = currentY;
        var rightColumnY = currentY;

        // Draw vertical divider line between columns
        drawList.AddLine(
            new Vector2(dividerX, currentY - UiScale.Scaled(10f)),
            new Vector2(dividerX, backgroundY + backgroundHeight - UiScale.Scaled(10f)),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)),
            UiScale.Scaled(1f));

        // LEFT COLUMN: Deity icon + Site Info + Coordinates + Description
        DrawDeityIcon(vm, drawList, leftColumnX, leftColumnY);

        var infoStartX = leftColumnX + UiScale.Scaled(123f); // After icon

        // Calculate max width for name field
        // Total available: dividerX - infoStartX - 20f (padding)
        // Need to fit: Label (150f) + Input + Spacing (8f) + Save (60f) + Spacing (8f) + Cancel (60f)
        var nameFieldMaxWidth =
            dividerX - infoStartX - UiScale.Scaled(150f) - UiScale.Scaled(8f) - UiScale.Scaled(60f) - UiScale.Scaled(8f) - UiScale.Scaled(60f) - UiScale.Scaled(20f); // LabelWidth + buttons + padding
        DrawHolySiteInfo(vm, drawList, infoStartX, ref leftColumnY, nameFieldMaxWidth, events);

        leftColumnY += SectionSpacing;
        DrawCoordinatesSection(vm, drawList, infoStartX, ref leftColumnY);

        leftColumnY += SectionSpacing;

        // Calculate max width for description (respect divider boundary)
        var descriptionMaxWidth = dividerX - infoStartX - UiScale.Scaled(20f); // 20f padding from divider
        DrawDescriptionSection(vm, drawList, infoStartX, ref leftColumnY, descriptionMaxWidth, events);

        // RIGHT COLUMN: Ritual section
        DrawRitualSection(vm, drawList, rightColumnX, ref rightColumnY, events);

        // Use the tallest column to set final currentY
        currentY = Math.Max(leftColumnY, rightColumnY);

        // End clipping
        drawList.PopClipRect();

        return new HolySiteDetailRendererResult(events, vm.Height);
    }

    /// <summary>
    ///     Draw deity icon with border
    /// </summary>
    private static void DrawDeityIcon(HolySiteDetailViewModel vm, ImDrawListPtr drawList, float x, float y)
    {
        if (Enum.TryParse<DeityDomain>(vm.SiteDetails.Domain, out var deityType))
        {
            var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
            if (deityTextureId != IntPtr.Zero)
            {
                var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);

                // Draw border
                drawList.AddRect(
                    new Vector2(x - UiScale.Scaled(1f), y - UiScale.Scaled(1f)),
                    new Vector2(x + IconSize + UiScale.Scaled(1f), y + IconSize + UiScale.Scaled(1f)),
                    borderColor, UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(2f));

                // Draw icon
                drawList.AddImage(deityTextureId,
                    new Vector2(x, y),
                    new Vector2(x + IconSize, y + IconSize),
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            }
        }
    }

    /// <summary>
    ///     Draw holy site information fields (name, tier, volume, multipliers)
    /// </summary>
    private static void DrawHolySiteInfo(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        float nameFieldMaxWidth,
        List<DetailEvent> events)
    {
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var valueColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        // Name field (with edit button for consecrator)
        if (vm.IsEditingName)
        {
            DrawNameEditField(vm, drawList, x, y, nameFieldMaxWidth, events);
        }
        else
        {
            DrawNameDisplay(vm, drawList, x, y, labelColor, valueColor, events);
        }

        y += FieldHeight + UiScale.Scaled(8f);

        // Tier
        DrawLabelValuePair(drawList, "Tier:", vm.SiteDetails.Tier.ToString(), x, y, labelColor, valueColor);
        y += FieldHeight;

        // Volume
        DrawLabelValuePair(drawList, "Volume:", vm.SiteDetails.Volume.ToString(), x, y, labelColor, valueColor);
        y += FieldHeight;

        // Prayer Multiplier
        DrawLabelValuePair(drawList, "Prayer Multiplier:",
            $"{vm.SiteDetails.PrayerMultiplier:F2}x", x, y, labelColor, valueColor);
        y += FieldHeight;
    }

    /// <summary>
    ///     Draw ritual section showing active ritual progress or start ritual buttons
    /// </summary>
    private static void DrawRitualSection(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        List<DetailEvent> events)
    {
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var valueColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        if (vm.SiteDetails.ActiveRitual != null)
        {
            // Active ritual in progress
            DrawActiveRitual(vm, drawList, x, ref y, labelColor, valueColor, events);
        }
        else
        {
            // Show ritual completion stats (completed / undiscovered)
            DrawRitualCompletionStats(vm, drawList, x, ref y, labelColor);
        }
    }

    /// <summary>
    ///     Draw active ritual progress with step-based workflow
    /// </summary>
    private static void DrawActiveRitual(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        uint labelColor,
        uint valueColor,
        List<DetailEvent> events)
    {
        var ritual = vm.SiteDetails.ActiveRitual!;

        // Section header
        drawList.AddText(ImGui.GetFont(), TableHeader, new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "Active Ritual");
        y += UiScale.Scaled(24f);

        // Ritual name
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x, y), labelColor, ritual.RitualName);
        y += UiScale.Scaled(20f);

        // Description
        if (!string.IsNullOrEmpty(ritual.Description))
        {
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), ritual.Description);
            y += UiScale.Scaled(18f);
        }

        y += UiScale.Scaled(12f);

        // Draw all step checkboxes (3-5 steps depending on ritual)
        foreach (var step in ritual.Steps)
        {
            DrawStepCheckbox(drawList, x, ref y, step, labelColor);
            y += UiScale.Scaled(8f);
        }

        // Cancel button (for consecrator only)
        if (vm.IsConsecrator)
        {
            y += UiScale.Scaled(8f);
            if (ButtonRenderer.DrawButton(drawList, "Cancel Ritual",
                    x, y, UiScale.Scaled(120f), UiScale.Scaled(28f), isPrimary: false))
            {
                events.Add(new DetailEvent.CancelRitualClicked());
            }

            y += UiScale.Scaled(36f);
        }
    }

    /// <summary>
    ///     Draw a single step checkbox (discovered or undiscovered)
    /// </summary>
    private static void DrawStepCheckbox(
        ImDrawListPtr drawList,
        float x,
        ref float y,
        HolySiteResponsePacket.StepProgressInfo step,
        uint labelColor)
    {
        // Handle undiscovered steps differently
        if (!step.IsDiscovered)
        {
            DrawUndiscoveredStep(drawList, x, ref y);
            return;
        }

        // Discovered step rendering with checkbox
        var checkboxSize = UiScale.Scaled(18f);

        // Checkbox background (green if complete, dark gray if not)
        var bgColor = step.IsComplete
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Green, 0.9f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f));

        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + checkboxSize, y + checkboxSize),
            bgColor, UiScale.Scaled(3f));

        // Checkbox border
        drawList.AddRect(
            new Vector2(x, y),
            new Vector2(x + checkboxSize, y + checkboxSize),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.MutedText),
            UiScale.Scaled(3f), ImDrawFlags.None, UiScale.Scaled(1.5f));

        // Checkmark (if complete)
        if (step.IsComplete)
        {
            var checkColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddLine(
                new Vector2(x + UiScale.Scaled(3f), y + checkboxSize / 2),
                new Vector2(x + checkboxSize / 2, y + checkboxSize - UiScale.Scaled(3f)),
                checkColor, UiScale.Scaled(2f));
            drawList.AddLine(
                new Vector2(x + checkboxSize / 2, y + checkboxSize - UiScale.Scaled(3f)),
                new Vector2(x + checkboxSize - UiScale.Scaled(3f), y + UiScale.Scaled(3f)),
                checkColor, UiScale.Scaled(2f));
        }

        // Step name
        var textX = x + checkboxSize + UiScale.Scaled(10f);
        var textColor = step.IsComplete
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold)
            : labelColor;
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(textX, y), textColor, step.StepName);
        y += UiScale.Scaled(20f);

        // Contributors (only for discovered steps)
        if (step.TopContributors != null && step.TopContributors.Count > 0)
        {
            var contributorsText = "  Contributors: " + string.Join(", ",
                step.TopContributors.Take(3).Select(c => $"{c.PlayerName} ({c.Quantity})"));
            drawList.AddText(ImGui.GetFont(), Small, new Vector2(textX, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.DisabledGray),
                contributorsText);
            y += UiScale.Scaled(16f);
        }
    }

    /// <summary>
    ///     Draw an undiscovered step placeholder with mystery icon
    /// </summary>
    private static void DrawUndiscoveredStep(ImDrawListPtr drawList, float x, ref float y)
    {
        var iconSize = UiScale.Scaled(18f);
        var mysteryColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.4f, 0.6f, 0.9f)); // Purple tint

        // Question mark icon background
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + iconSize, y + iconSize),
            mysteryColor, UiScale.Scaled(3f));

        // Question mark icon border
        drawList.AddRect(
            new Vector2(x, y),
            new Vector2(x + iconSize, y + iconSize),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.5f, 0.7f, 1f)),
            UiScale.Scaled(3f), ImDrawFlags.None, UiScale.Scaled(1.5f));

        // Question mark symbol
        var questionMarkColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x + UiScale.Scaled(4f), y + UiScale.Scaled(1f)), questionMarkColor, "?");

        // "Undiscovered Step" text
        var textX = x + iconSize + UiScale.Scaled(10f);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DisabledGray);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(textX, y), textColor, "??? Undiscovered Step");
        y += UiScale.Scaled(20f);

        // Hint text
        var hintText = "  Offer sacred items to discover this step";
        var hintColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.MutedText);
        drawList.AddText(ImGui.GetFont(), Small, new Vector2(textX, y), hintColor, hintText);
        y += UiScale.Scaled(16f);
    }

    /// <summary>
    ///     Draw ritual completion statistics
    /// </summary>
    private static void DrawRitualCompletionStats(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        uint labelColor)
    {
        var currentTier = vm.SiteDetails.Tier;

        // Section header
        drawList.AddText(ImGui.GetFont(), TableHeader, new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "Ritual Progress");
        y += UiScale.Scaled(24f);

        // Calculate completed rituals (tier - 1, since tier 1 is base)
        var completedRituals = currentTier - 1;

        // Total possible rituals for this holy site (2: tier 1→2 and tier 2→3)
        var totalRituals = 2;
        var undiscoveredRituals = totalRituals - completedRituals;

        // Display completion stats
        var completionText = $"{completedRituals} Rituals Completed / {undiscoveredRituals} Undiscovered";
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, y),
            labelColor, completionText);
        y += UiScale.Scaled(20f);

        // Show discovery hint if not max tier
        if (currentTier < 3)
        {
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
                "Offer sacred items at the altar to discover new rituals.");
            y += UiScale.Scaled(18f);
        }
        else
        {
            // Max tier reached
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
                "All rituals completed! This is a Cathedral.");
            y += UiScale.Scaled(18f);
        }
    }

    /// <summary>
    ///     Draw name in display mode (with edit button for consecrator)
    /// </summary>
    private static void DrawNameDisplay(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        uint labelColor,
        uint valueColor,
        List<DetailEvent> events)
    {
        // Label
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x, y + UiScale.Scaled(8f)), labelColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_NAME));

        // Value
        var valueX = x + LabelWidth;
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(valueX, y + UiScale.Scaled(8f)),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.SiteDetails.SiteName);

        // Edit button (for consecrator)
        if (vm.IsConsecrator)
        {
            var editButtonX = valueX + ImGui.CalcTextSize(vm.SiteDetails.SiteName).X + UiScale.Scaled(12f);
            if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_EDIT),
                    editButtonX, y + UiScale.Scaled(7f), EditButtonWidth, EditButtonHeight,
                    isPrimary: false))
            {
                events.Add(new DetailEvent.RenameClicked());
            }
        }
    }

    /// <summary>
    ///     Draw name in edit mode (with save/cancel buttons)
    /// </summary>
    private static void DrawNameEditField(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        float maxWidth,
        List<DetailEvent> events)
    {
        // Label
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x, y + UiScale.Scaled(8f)),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_NAME));

        // Text input
        var inputX = x + LabelWidth;

        var editValue = vm.EditingNameValue ?? vm.SiteDetails.SiteName;
        var newValue = TextInput.Draw(drawList, "##holysite_name_edit", editValue, inputX, y, maxWidth, FieldHeight,
            maxLength: 50);

        if (newValue != editValue)
        {
            // Emit event when value changes so state manager can update EditingNameValue
            events.Add(new DetailEvent.RenameValueChanged(newValue));
        }

        // Save button
        var saveButtonX = inputX + maxWidth + UiScale.Scaled(8f);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_SAVE),
                saveButtonX, y, UiScale.Scaled(60f), FieldHeight, isPrimary: true))
        {
            events.Add(new DetailEvent.RenameSave(newValue));
        }

        // Cancel button
        var cancelButtonX = saveButtonX + UiScale.Scaled(68f);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                cancelButtonX, y, UiScale.Scaled(60f), FieldHeight))
        {
            events.Add(new DetailEvent.RenameCancel());
        }
    }

    /// <summary>
    ///     Draw coordinates section with location display
    /// </summary>
    private static void DrawCoordinatesSection(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y)
    {
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var valueColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        var coordinatesText = $"({vm.SiteDetails.Center.X}, {vm.SiteDetails.Center.Y}, {vm.SiteDetails.Center.Z})";
        DrawLabelValuePair(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_COORDINATES) + ":",
            coordinatesText, x, y, labelColor, valueColor);
        y += FieldHeight;
    }

    /// <summary>
    ///     Draw description section (display + edit for consecrator)
    /// </summary>
    private static void DrawDescriptionSection(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        float maxWidth,
        List<DetailEvent> events)
    {
        // Section header
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddText(ImGui.GetFont(), TableHeader, new Vector2(x, y), headerColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_DESCRIPTION));

        // Edit button (for consecrator, only in display mode)
        if (vm.IsConsecrator && !vm.IsEditingDescription)
        {
            var editButtonX = x + ImGui.CalcTextSize(
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_DESCRIPTION)).X + UiScale.Scaled(12f);
            if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_EDIT),
                    editButtonX, y - UiScale.Scaled(1f), EditButtonWidth, EditButtonHeight,
                    isPrimary: false))
            {
                events.Add(new DetailEvent.EditDescriptionClicked());
            }
        }

        y += UiScale.Scaled(32f);

        if (vm.IsEditingDescription)
        {
            DrawDescriptionEditField(vm, drawList, x, ref y, maxWidth, events);
        }
        else
        {
            DrawDescriptionDisplay(vm, drawList, x, ref y, maxWidth);
        }
    }

    /// <summary>
    ///     Draw description in display mode
    /// </summary>
    private static void DrawDescriptionDisplay(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        float maxWidth)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        var descriptionText = string.IsNullOrEmpty(vm.SiteDetails.Description)
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_NO_DESCRIPTION)
            : vm.SiteDetails.Description;

        ImGui.PushTextWrapPos(x + maxWidth);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, y), textColor, descriptionText);
        ImGui.PopTextWrapPos();

        var textSize = ImGui.CalcTextSize(descriptionText, maxWidth);
        y += textSize.Y + UiScale.Scaled(8f);
    }

    /// <summary>
    ///     Draw description in edit mode (with save/cancel buttons)
    /// </summary>
    private static void DrawDescriptionEditField(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        float maxWidth,
        List<DetailEvent> events)
    {
        var inputHeight = UiScale.Scaled(100f);

        var editValue = vm.EditingDescriptionValue ?? vm.SiteDetails.Description;
        var newValue = TextInput.DrawMultiline(drawList, "##holysite_description_edit", editValue, x, y, maxWidth,
            inputHeight, maxLength: 200);

        if (newValue != editValue)
        {
            // Emit event when value changes so state manager can update EditingDescriptionValue
            events.Add(new DetailEvent.DescriptionValueChanged(newValue));
        }

        y += inputHeight + UiScale.Scaled(8f);

        // Character count
        var charCount = newValue?.Length ?? 0;
        var charCountText = $"{charCount} / {DescriptionMaxChars:F0}";
        var charCountColor = charCount > DescriptionMaxChars
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.Red)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(x + maxWidth - UiScale.Scaled(60f), y), charCountColor, charCountText);

        y += UiScale.Scaled(24f);

        // Save button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_SAVE),
                x, y, UiScale.Scaled(80f), FieldHeight, isPrimary: true,
                enabled: charCount <= DescriptionMaxChars))
        {
            events.Add(new DetailEvent.DescriptionSave(newValue ?? ""));
        }

        // Cancel button
        var cancelButtonX = x + UiScale.Scaled(88f);
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                cancelButtonX, y, UiScale.Scaled(80f), FieldHeight))
        {
            events.Add(new DetailEvent.DescriptionCancel());
        }

        y += FieldHeight + UiScale.Scaled(8f);
    }

    /// <summary>
    ///     Helper: Draw a label-value pair
    /// </summary>
    private static void DrawLabelValuePair(
        ImDrawListPtr drawList,
        string label,
        string value,
        float x,
        float y,
        uint labelColor,
        uint valueColor)
    {
        // Label
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(x, y + UiScale.Scaled(8f)), labelColor, label);

        // Value
        var valueX = x + LabelWidth;
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(valueX, y + UiScale.Scaled(8f)), valueColor, value);
    }

    /// <summary>
    ///     Draw error state message
    /// </summary>
    private static void DrawErrorState(HolySiteDetailViewModel vm, ImDrawListPtr drawList, float y)
    {
        var errorText = vm.ErrorMsg ?? "Unknown error";
        var textSize = ImGui.CalcTextSize(errorText);
        var errorX = vm.X + (vm.Width - textSize.X) / 2f;
        var errorY = y + (vm.Height - textSize.Y) / 2f;
        var errorColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(new Vector2(errorX, errorY), errorColor, errorText);
    }
}