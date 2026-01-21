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
    private const float BackButtonWidth = 160f;
    private const float BackButtonHeight = 32f;
    private const float MarkButtonWidth = 130f;
    private const float MarkButtonHeight = 36f;
    private const float IconSize = 85f;
    private const float SectionSpacing = 20f;
    private const float FieldHeight = 32f;
    private const float LabelWidth = 150f;
    private const float EditButtonWidth = 40f;
    private const float EditButtonHeight = 18f;
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
                vm.X, currentY + 8f, vm.Width);
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
        var markButtonX = vm.X + vm.Width - MarkButtonWidth - 16f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_MARK),
                markButtonX, currentY, MarkButtonWidth, MarkButtonHeight,
                isPrimary: true))
        {
            events.Add(new DetailEvent.MarkClicked());
        }

        currentY += 44f;

        // Draw background panel
        var backgroundY = currentY;
        var backgroundHeight = vm.Height - (currentY - vm.Y) - 8f;
        drawList.AddRectFilled(
            new Vector2(vm.X, backgroundY),
            new Vector2(vm.X + vm.Width, backgroundY + backgroundHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground),
            4f);

        // Clip content to container boundaries
        drawList.PushClipRect(new Vector2(vm.X, backgroundY), new Vector2(vm.X + vm.Width, backgroundY + backgroundHeight), true);

        currentY += 16f;

        // Two-column layout
        var leftColumnX = vm.X + 97f;
        var leftColumnWidth = (vm.Width / 2) - 110f; // Half width minus icon space
        var rightColumnX = vm.X + (vm.Width / 2) + 20f; // Start right column with padding
        var dividerX = vm.X + (vm.Width / 2) + 5f; // Vertical divider line

        var leftColumnY = currentY;
        var rightColumnY = currentY;

        // Draw vertical divider line between columns
        drawList.AddLine(
            new Vector2(dividerX, currentY - 10f),
            new Vector2(dividerX, backgroundY + backgroundHeight - 10f),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)),
            1f);

        // LEFT COLUMN: Deity icon + Site Info + Coordinates + Description
        DrawDeityIcon(vm, drawList, leftColumnX, leftColumnY);

        var infoStartX = leftColumnX + 123f; // After icon
        DrawHolySiteInfo(vm, drawList, infoStartX, ref leftColumnY, events);

        leftColumnY += SectionSpacing;
        DrawCoordinatesSection(vm, drawList, infoStartX, ref leftColumnY);

        leftColumnY += SectionSpacing;
        DrawDescriptionSection(vm, drawList, infoStartX, ref leftColumnY, events);

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
                    new Vector2(x - 1f, y - 1f),
                    new Vector2(x + IconSize + 1f, y + IconSize + 1f),
                    borderColor, 4f, ImDrawFlags.None, 2f);

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
        List<DetailEvent> events)
    {
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var valueColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        // Name field (with edit button for consecrator)
        if (vm.IsEditingName)
        {
            DrawNameEditField(vm, drawList, x, y, events);
        }
        else
        {
            DrawNameDisplay(vm, drawList, x, y, labelColor, valueColor, events);
        }
        y += FieldHeight + 8f;

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
    ///     Draw active ritual progress
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
        drawList.AddText(ImGui.GetFont(), 16f, new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "Active Ritual");
        y += 24f;

        // Ritual name
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x, y), labelColor, ritual.RitualName);
        y += 20f;

        // Description
        if (!string.IsNullOrEmpty(ritual.Description))
        {
            drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), ritual.Description);
            y += 18f;
        }

        y += 8f;

        // Draw each requirement with progress bar
        foreach (var req in ritual.Requirements)
        {
            DrawRequirementProgress(drawList, x, ref y, req, labelColor, valueColor);
            y += 12f;
        }

        // Cancel button (for consecrator only)
        if (vm.IsConsecrator)
        {
            y += 8f;
            if (ButtonRenderer.DrawButton(drawList, "Cancel Ritual",
                    x, y, 120f, 28f, isPrimary: false))
            {
                events.Add(new DetailEvent.CancelRitualClicked());
            }
            y += 36f;
        }
    }

    /// <summary>
    ///     Draw a single requirement's progress
    /// </summary>
    private static void DrawRequirementProgress(
        ImDrawListPtr drawList,
        float x,
        ref float y,
        HolySiteResponsePacket.RequirementProgressInfo req,
        uint labelColor,
        uint valueColor)
    {
        // Requirement name and progress
        var progressText = $"{req.QuantityContributed}/{req.QuantityRequired}";
        var progressPercent = req.QuantityRequired > 0
            ? (float)req.QuantityContributed / req.QuantityRequired
            : 0f;

        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x, y), labelColor,
            $"{req.DisplayName}: {progressText}");
        y += 18f;

        // Progress bar
        var barWidth = 400f;
        var barHeight = 12f;
        var barX = x;
        var barY = y;

        // Background
        drawList.AddRectFilled(
            new Vector2(barX, barY),
            new Vector2(barX + barWidth, barY + barHeight),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f)),
            2f);

        // Progress fill
        var fillWidth = barWidth * Math.Min(progressPercent, 1f);
        var fillColor = progressPercent >= 1f
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.8f, 0.2f, 0.9f)) // Green when complete
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.6f, 0.2f, 0.9f)); // Gold when in progress

        if (fillWidth > 0)
        {
            drawList.AddRectFilled(
                new Vector2(barX, barY),
                new Vector2(barX + fillWidth, barY + barHeight),
                fillColor,
                2f);
        }

        // Border
        drawList.AddRect(
            new Vector2(barX, barY),
            new Vector2(barX + barWidth, barY + barHeight),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 1f)),
            2f, ImDrawFlags.None, 1f);

        y += barHeight + 6f;

        // Top contributors (show top 3)
        if (req.TopContributors != null && req.TopContributors.Count > 0)
        {
            var contributorsText = "Contributors: " + string.Join(", ",
                req.TopContributors.Take(3).Select(c => $"{c.PlayerName} ({c.Quantity})"));

            drawList.AddText(ImGui.GetFont(), 11f, new Vector2(x + 8f, y),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 1f)),
                contributorsText);
            y += 16f;
        }
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
        drawList.AddText(ImGui.GetFont(), 16f, new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "Ritual Progress");
        y += 24f;

        // Calculate completed rituals (tier - 1, since tier 1 is base)
        var completedRituals = currentTier - 1;

        // Total possible rituals for this holy site (2: tier 1→2 and tier 2→3)
        var totalRituals = 2;
        var undiscoveredRituals = totalRituals - completedRituals;

        // Display completion stats
        var completionText = $"{completedRituals} Rituals Completed / {undiscoveredRituals} Undiscovered";
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x, y),
            labelColor, completionText);
        y += 20f;

        // Show discovery hint if not max tier
        if (currentTier < 3)
        {
            drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
                "Offer sacred items at the altar to discover new rituals.");
            y += 18f;
        }
        else
        {
            // Max tier reached
            drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
                "All rituals completed! This is a Cathedral.");
            y += 18f;
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
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x, y + 8f), labelColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_NAME));

        // Value
        var valueX = x + LabelWidth;
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(valueX, y + 8f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.SiteDetails.SiteName);

        // Edit button (for consecrator)
        if (vm.IsConsecrator)
        {
            var editButtonX = valueX + ImGui.CalcTextSize(vm.SiteDetails.SiteName).X + 12f;
            if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_EDIT),
                    editButtonX, y + 7f, EditButtonWidth, EditButtonHeight,
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
        List<DetailEvent> events)
    {
        // Label
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x, y + 8f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown),
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_NAME));

        // Text input
        var inputX = x + LabelWidth;
        var inputWidth = 300f;

        var editValue = vm.EditingNameValue ?? vm.SiteDetails.SiteName;
        var newValue = TextInput.Draw(drawList, "##holysite_name_edit", editValue, inputX, y, inputWidth, FieldHeight, maxLength: 50);

        if (newValue != editValue)
        {
            // Emit event when value changes so state manager can update EditingNameValue
            events.Add(new DetailEvent.RenameValueChanged(newValue));
        }

        // Save button
        var saveButtonX = inputX + inputWidth + 8f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_SAVE),
                saveButtonX, y, 60f, FieldHeight, isPrimary: true))
        {
            events.Add(new DetailEvent.RenameSave(newValue));
        }

        // Cancel button
        var cancelButtonX = saveButtonX + 68f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                cancelButtonX, y, 60f, FieldHeight))
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
        List<DetailEvent> events)
    {
        // Section header
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddText(ImGui.GetFont(), 16f, new Vector2(x, y), headerColor,
            LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_DESCRIPTION));

        // Edit button (for consecrator, only in display mode)
        if (vm.IsConsecrator && !vm.IsEditingDescription)
        {
            var editButtonX = x + ImGui.CalcTextSize(
                LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_DESCRIPTION)).X + 12f;
            if (ButtonRenderer.DrawButton(drawList, LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_EDIT),
                    editButtonX, y - 1f, EditButtonWidth, EditButtonHeight,
                    isPrimary: false))
            {
                events.Add(new DetailEvent.EditDescriptionClicked());
            }
        }

        y += 32f;

        if (vm.IsEditingDescription)
        {
            DrawDescriptionEditField(vm, drawList, x, ref y, events);
        }
        else
        {
            DrawDescriptionDisplay(vm, drawList, x, ref y);
        }
    }

    /// <summary>
    ///     Draw description in display mode
    /// </summary>
    private static void DrawDescriptionDisplay(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        var descriptionText = string.IsNullOrEmpty(vm.SiteDetails.Description)
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_HOLYSITES_DETAIL_NO_DESCRIPTION)
            : vm.SiteDetails.Description;

        var wrappedWidth = 800f;
        ImGui.PushTextWrapPos(x + wrappedWidth);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x, y), textColor, descriptionText);
        ImGui.PopTextWrapPos();

        var textSize = ImGui.CalcTextSize(descriptionText, wrappedWidth);
        y += textSize.Y + 8f;
    }

    /// <summary>
    ///     Draw description in edit mode (with save/cancel buttons)
    /// </summary>
    private static void DrawDescriptionEditField(
        HolySiteDetailViewModel vm,
        ImDrawListPtr drawList,
        float x,
        ref float y,
        List<DetailEvent> events)
    {
        var inputWidth = 800f;
        var inputHeight = 100f;

        var editValue = vm.EditingDescriptionValue ?? vm.SiteDetails.Description;
        var newValue = TextInput.DrawMultiline(drawList, "##holysite_description_edit", editValue, x, y, inputWidth, inputHeight, maxLength: 200);

        if (newValue != editValue)
        {
            // Emit event when value changes so state manager can update EditingDescriptionValue
            events.Add(new DetailEvent.DescriptionValueChanged(newValue));
        }

        y += inputHeight + 8f;

        // Character count
        var charCount = newValue?.Length ?? 0;
        var charCountText = $"{charCount} / {DescriptionMaxChars:F0}";
        var charCountColor = charCount > DescriptionMaxChars
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.Red)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x + inputWidth - 60f, y), charCountColor, charCountText);

        y += 24f;

        // Save button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_SAVE),
                x, y, 80f, FieldHeight, isPrimary: true,
                enabled: charCount <= DescriptionMaxChars))
        {
            events.Add(new DetailEvent.DescriptionSave(newValue ?? ""));
        }

        // Cancel button
        var cancelButtonX = x + 88f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_CANCEL),
                cancelButtonX, y, 80f, FieldHeight))
        {
            events.Add(new DetailEvent.DescriptionCancel());
        }

        y += FieldHeight + 8f;
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
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x, y + 8f), labelColor, label);

        // Value
        var valueX = x + LabelWidth;
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(valueX, y + 8f), valueColor, value);
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
