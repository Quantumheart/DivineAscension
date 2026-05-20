using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Compact strip above the deity selector showing per-deity favor rank +
///     unlocked/total counts. Read-only — clicks are handled by the selector below it.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CrossDeitySummaryRenderer
{
    public const float Height = 38f;
    private const float Padding = 4f;

    public static void Draw(float x, float y, float width, IReadOnlyList<DeityBlessingSummary> summaries)
    {
        if (summaries.Count == 0) return;

        var drawList = ImGui.GetWindowDrawList();

        var bgMin = new Vector2(x, y);
        var bgMax = new Vector2(x + width, y + Height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.DarkBrown, 0.7f));
        drawList.AddRectFilled(bgMin, bgMax, bgColor, 4f);
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.4f));
        drawList.AddRect(bgMin, bgMax, borderColor, 4f, ImDrawFlags.None, 1f);

        var colWidth = (width - Padding * 2) / summaries.Count;
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var gold = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);

        for (var i = 0; i < summaries.Count; i++)
        {
            var s = summaries[i];
            var colX = x + Padding + i * colWidth;
            var colTop = y + 4f;

            var label = $"{DomainShort(s.Domain)} {RankRequirements.GetFavorRankName(s.FavorRank)}";
            var labelSize = ImGui.CalcTextSize(label);
            drawList.AddText(ImGui.GetFont(), 12f,
                new Vector2(colX + (colWidth - labelSize.X) / 2f, colTop),
                s.IsPatron ? gold : textColor,
                label);

            var counts = $"P {s.UnlockedPlayer}/{s.TotalPlayer}  R {s.UnlockedReligion}/{s.TotalReligion}";
            var countsSize = ImGui.CalcTextSize(counts);
            drawList.AddText(ImGui.GetFont(), 12f,
                new Vector2(colX + (colWidth - countsSize.X) / 2f, colTop + 16f),
                textColor,
                counts);

            if (s.IsPatron)
            {
                drawList.AddText(ImGui.GetFont(), 12f,
                    new Vector2(colX + 2f, colTop), gold, "*");
            }
        }
    }

    private static string DomainShort(DeityDomain d) => d switch
    {
        DeityDomain.Craft => "C",
        DeityDomain.Wild => "W",
        DeityDomain.Conquest => "Q",
        DeityDomain.Harvest => "H",
        DeityDomain.Stone => "S",
        _ => "?"
    };
}
