using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization.Info;

/// <summary>
///     Founder-only inline edit affordance for the civilization's capital. The seat
///     line is already rendered in the stat block; this renderer adds a pencil glyph
///     when collapsed and a name input + holy-site binding dropdown when editing.
///     Binding picker uses the shared <see cref="Dropdown" /> component.
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
    private const float DropdownHeight = 32f;
    private const float DropdownMenuItemHeight = 28f;

    public readonly record struct CapitalEditLayout(bool HasDropdown, float DropdownX, float DropdownY, float DropdownWidth);

    public static (float bottomY, CapitalEditLayout layout) Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        if (!vm.IsFounder)
            return (y, default);

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

            return (currentY + EditGlyphSize + SectionBottomSpacing, default);
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

        var dropdownX = x;
        var dropdownY = currentY;
        var dropdownWidth = width;
        var label = ResolveSelectedLabel(vm);

        if (Dropdown.DrawButton(drawList, dropdownX, dropdownY, dropdownWidth, DropdownHeight,
                label, vm.IsCapitalSiteDropdownOpen))
        {
            events.Add(new InfoEvent.ToggleCapitalSiteDropdown(!vm.IsCapitalSiteDropdownOpen));
        }

        currentY += DropdownHeight + 8f;

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
        return (currentY, new CapitalEditLayout(true, dropdownX, dropdownY, dropdownWidth));
    }

    /// <summary>
    ///     Second-pass overlay for the binding dropdown menu — called after the
    ///     main pane content so the menu can paint on top.
    /// </summary>
    public static bool DrawDropdownOverlay(
        CivilizationInfoViewModel vm,
        CapitalEditLayout layout,
        List<InfoEvent> events)
    {
        if (!layout.HasDropdown || !vm.IsCapitalSiteDropdownOpen)
            return false;

        var drawList = ImGui.GetWindowDrawList();
        var (items, ids, selectedIndex) = BuildItems(vm);

        Dropdown.DrawMenuVisual(drawList, layout.DropdownX, layout.DropdownY, layout.DropdownWidth, DropdownHeight,
            items, selectedIndex, itemHeight: DropdownMenuItemHeight);

        var (newIndex, shouldClose, consumed) = Dropdown.DrawMenuAndHandleInteraction(
            layout.DropdownX, layout.DropdownY, layout.DropdownWidth, DropdownHeight,
            items, selectedIndex, itemHeight: DropdownMenuItemHeight);

        if (newIndex != selectedIndex && newIndex >= 0 && newIndex < ids.Count)
            events.Add(new InfoEvent.CapitalBindingChanged(ids[newIndex]));

        if (shouldClose)
            events.Add(new InfoEvent.ToggleCapitalSiteDropdown(false));

        return consumed;
    }

    private static string ResolveSelectedLabel(CivilizationInfoViewModel vm)
    {
        var current = vm.CapitalBindingText ?? string.Empty;
        if (string.IsNullOrEmpty(current))
            return LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CAPITAL_NONE);

        foreach (var (_, sites) in vm.EligibleCapitalSites)
        {
            if (sites == null) continue;
            foreach (var s in sites)
            {
                if (s.SiteUID == current)
                    return s.SiteName;
            }
        }

        return current;
    }

    private static (string[] items, List<string> ids, int selectedIndex) BuildItems(CivilizationInfoViewModel vm)
    {
        var items = new List<string>();
        var ids = new List<string>();

        items.Add(LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CAPITAL_NONE));
        ids.Add(string.Empty);

        foreach (var (_, sites) in vm.EligibleCapitalSites)
        {
            if (sites == null) continue;
            foreach (var site in sites)
            {
                items.Add(site.SiteName);
                ids.Add(site.SiteUID);
            }
        }

        var current = vm.CapitalBindingText ?? string.Empty;
        var idx = ids.IndexOf(current);
        if (idx < 0) idx = 0;

        return (items.ToArray(), ids, idx);
    }
}
