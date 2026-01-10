using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

internal static class BlessingInfoTextUtils
{
    public static void DrawWrappedText(string text,
        float x, float y, float maxWidth, uint color, float fontSize)
    {
        var drawList = ImGui.GetWindowDrawList();
        var words = text.Split(' ');
        var currentLine = string.Empty;
        var currentY = y;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = ImGui.CalcTextSize(testLine);
            if (testSize.X > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, currentY), color, currentLine);
                currentY += fontSize + 4f;
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, currentY), color, currentLine);
    }

    public static string FormatStatModifier(string statName, float value)
    {
        var statLower = statName.ToLower();
        var percentageStats = new[]
        {
            "walkspeed",
            "meleeDamage",
            "meleeweaponsdamage",
            "rangedDamage",
            "rangedweaponsdamage",
            "maxhealthExtraMultiplier",
            "maxhealthextramultiplier"
        };

        var isPercentage = Enumerable.Any(percentageStats, ps => statLower.Contains(ps.ToLower()));

        var displayName = statLower switch
        {
            var s when s.Contains("walkspeed") =>
                LocalizationService.Instance.Get(LocalizationKeys.STAT_MOVEMENT_SPEED),
            var s when s.Contains("meleeDamage") || s.Contains("meleeweaponsdamage") =>
                LocalizationService.Instance.Get(LocalizationKeys.STAT_MELEE_DAMAGE),
            var s when s.Contains("rangedDamage") || s.Contains("rangedweaponsdamage") => LocalizationService.Instance
                .Get(LocalizationKeys.STAT_RANGED_DAMAGE),
            var s when s.Contains("maxhealth") && s.Contains("multiplier") => LocalizationService.Instance.Get(
                LocalizationKeys.STAT_MAX_HEALTH),
            var s when s.Contains("maxhealth") && s.Contains("points") => LocalizationService.Instance.Get(
                LocalizationKeys.STAT_MAX_HEALTH),
            var s when s.Contains("maxhealth") => LocalizationService.Instance.Get(LocalizationKeys.STAT_MAX_HEALTH),
            var s when s.Contains("armor") => LocalizationService.Instance.Get(LocalizationKeys.STAT_ARMOR),
            var s when s.Contains("speed") => LocalizationService.Instance.Get(LocalizationKeys.STAT_SPEED),
            var s when s.Contains("damage") => LocalizationService.Instance.Get(LocalizationKeys.STAT_DAMAGE),
            var s when s.Contains("health") => LocalizationService.Instance.Get(LocalizationKeys.STAT_HEALTH),
            _ => statName
        };

        var sign = value >= 0 ? "+" : string.Empty;
        return isPercentage
            ? $"  {sign}{value * 100:0.#}% {displayName}"
            : $"  {sign}{value:0.#} {displayName}";
    }

    public static string GetRankName(int rank, bool isFavorRank)
    {
        if (isFavorRank)
        {
            if (rank >= 0 && rank <= 4)
                return ((FavorRank)rank).ToLocalizedString();
            return LocalizationService.Instance.Get(LocalizationKeys.UI_RANK_UNKNOWN, rank);
        }

        if (rank >= 0 && rank <= 4)
            return ((PrestigeRank)rank).ToLocalizedString();
        return LocalizationService.Instance.Get(LocalizationKeys.UI_RANK_UNKNOWN, rank);
    }
}