using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization.Info;

/// <summary>
///     Founder-only inline edit affordance for the civilization's capital. Collapsed
///     state shows nothing extra (the seat line is already rendered in the header stat
///     block); editing mode shows a name input plus a holy-site binding picker.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationInfoCapitalRenderer
{
    private const float EditGlyphSize = 20f;
    private const float InputHeight = 26f;
    private const float ButtonHeight = 26f;
    private const float ButtonWidth = 80f;
    private const float ButtonGap = 8f;
    private const float SectionBottomSpacing = 8f;
    private const float SiteRowHeight = 22f;
    private const float MaxSitesShown = 6f;

    public static float Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        if (!vm.IsFounder)
            return y;

        var currentY = y;

        if (!vm.IsEditingCapital)
        {
            var glyphX = x + width - EditGlyphSize;
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    glyphX, currentY,
                    EditGlyphSize, EditGlyphSize,
                    isPrimary: false, enabled: true))
            {
                events.Add(new InfoEvent.EditCapitalOpen());
            }
            ChromeRenderer.DrawPencil(drawList,
                glyphX + EditGlyphSize / 2f,
                currentY + EditGlyphSize / 2f,
                EditGlyphSize - 8f,
                ColorPalette.LightText);

            return currentY + EditGlyphSize + SectionBottomSpacing;
        }

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_SEAT),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SubsectionLabel + 6f;

        var newName = TextInput.Draw(drawList, "##civCapitalName",
            vm.CapitalNameText, x, currentY, width, InputHeight,
            placeholder: string.Empty, maxLength: 64);
        if (newName != vm.CapitalNameText)
            events.Add(new InfoEvent.CapitalNameChanged(newName));

        currentY += InputHeight + 8f;

        // Binding picker — flat list, "(none)" row first
        var picked = DrawBindingList(vm, drawList, x, currentY, width, events);
        currentY = picked;

        // Buttons
        currentY += 6f;
        var rightX = x + width;
        var cancelX = rightX - ButtonWidth;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CANCEL),
                cancelX, currentY, ButtonWidth, ButtonHeight,
                isPrimary: false, enabled: true))
        {
            events.Add(new InfoEvent.EditCapitalCancel());
        }

        if (vm.HasCapitalChanges)
        {
            var saveX = cancelX - ButtonGap - ButtonWidth;
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_SAVE),
                    saveX, currentY, ButtonWidth, ButtonHeight,
                    isPrimary: true, enabled: true))
            {
                events.Add(new InfoEvent.SaveCapitalClicked());
            }
        }

        currentY += ButtonHeight + SectionBottomSpacing;
        return currentY;
    }

    private static float DrawBindingList(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<InfoEvent> events)
    {
        var currentY = y;
        var totalSites = 0;
        foreach (var list in vm.EligibleCapitalSites.Values)
            totalSites += list?.Count ?? 0;

        var rowCount = 1 /* (none) */ + totalSites;
        var listHeight = System.MathF.Min(rowCount, MaxSitesShown) * SiteRowHeight;
        var listEndY = currentY + listHeight;

        // Frame
        drawList.AddRect(new Vector2(x, currentY), new Vector2(x + width, listEndY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f), 0f, ImDrawFlags.None, 1f);

        ImGui.SetCursorScreenPos(new Vector2(x + 4f, currentY + 2f));
        ImGui.BeginChild("##capitalBindingList", new Vector2(width - 8f, listHeight - 4f), false);

        DrawBindingRow(drawList, events, vm.CapitalBindingText, string.Empty,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CAPITAL_NONE));

        foreach (var (religionUid, sites) in vm.EligibleCapitalSites)
        {
            if (sites == null) continue;
            foreach (var site in sites)
            {
                var label = $"{site.SiteName}";
                DrawBindingRow(drawList, events, vm.CapitalBindingText, site.SiteUID, label);
            }
        }

        ImGui.EndChild();

        return listEndY;
    }

    private static void DrawBindingRow(
        ImDrawListPtr drawList,
        List<InfoEvent> events,
        string selectedSiteId,
        string siteId,
        string label)
    {
        var isSelected = (selectedSiteId ?? string.Empty) == (siteId ?? string.Empty);
        var prefix = isSelected ? "* " : "  ";
        if (ImGui.Selectable(prefix + label, isSelected, ImGuiSelectableFlags.None, new Vector2(0, SiteRowHeight - 4f)))
        {
            events.Add(new InfoEvent.CapitalBindingChanged(siteId ?? string.Empty));
        }
    }
}
