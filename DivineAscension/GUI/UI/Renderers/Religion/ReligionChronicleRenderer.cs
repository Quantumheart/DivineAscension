using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Ledger-chapter chronicle section for the "This Order" pane (#373). Renders the
/// religion's chronicle — a heading, an ornamental divider, then dated prose
/// entries oldest-first (a chronicle reads forward). Collapses to nothing when the
/// chronicle is empty. Mirrors <see cref="Civilization.CivilizationChronicleRenderer" />.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionChronicleRenderer
{
    private const float SectionLabelHeight = 22f;
    private const float OrnateDividerHeight = 18f;
    private const float OrnateDividerYPadding = 6f;
    private const float DiamondHalfSize = 3.5f;
    private const float DiamondLeftPadding = 4f;
    private const float ProseIndent = 18f;
    private const float EntryGap = 8f;

    /// <summary>
    ///     Draws the chronicle section and returns the Y coordinate after it. Returns
    ///     <paramref name="y" /> unchanged (drawing nothing) when there are no entries.
    /// </summary>
    public static float Draw(
        ReligionInfoViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        if (!vm.HasChronicle)
            return y;

        var currentY = y;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_CHRONICLE_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        ChromeRenderer.DrawDividerOrnate(drawList, x, currentY + OrnateDividerYPadding, width);
        currentY += OrnateDividerHeight;

        var proseWidth = width - ProseIndent;
        foreach (var entry in vm.Chronicle)
        {
            var centerY = currentY + (Secondary + 6f) / 2f;
            ChromeRenderer.DrawDiamond(drawList,
                x + DiamondLeftPadding + DiamondHalfSize, centerY,
                DiamondHalfSize,
                ColorPalette.Gold * 0.6f);

            var text = ComposeLine(entry);
            TextRenderer.DrawInfoText(drawList, text, x + ProseIndent, currentY, proseWidth,
                Secondary, ColorPalette.LightText);

            currentY += TextRenderer.MeasureWrappedHeight(text, proseWidth, Secondary) + EntryGap;
        }

        return currentY + 4f;
    }

    /// <summary>
    ///     Measures the height the chronicle section will consume, mirroring
    ///     <see cref="Draw" /> so the pane can size its scroll region.
    /// </summary>
    public static float MeasureHeight(ReligionInfoViewModel vm, float width)
    {
        if (!vm.HasChronicle)
            return 0f;

        var h = SectionLabelHeight + OrnateDividerHeight;
        var proseWidth = width - ProseIndent;
        foreach (var entry in vm.Chronicle)
            h += TextRenderer.MeasureWrappedHeight(ComposeLine(entry), proseWidth, Secondary) + EntryGap;

        return h + 4f;
    }

    private static string ComposeLine(PlayerReligionInfoResponsePacket.ChronicleEntryDto entry)
    {
        var day = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_CHRONICLE_DAY,
            entry.InGameDay);
        return $"{day} · {entry.Line}";
    }
}
