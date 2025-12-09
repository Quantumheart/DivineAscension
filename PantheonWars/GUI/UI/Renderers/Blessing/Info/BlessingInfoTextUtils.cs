using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace PantheonWars.GUI.UI.Renderers.Blessing.Info;

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
            var s when s.Contains("walkspeed") => "Movement Speed",
            var s when s.Contains("meleeDamage") || s.Contains("meleeweaponsdamage") => "Melee Damage",
            var s when s.Contains("rangedDamage") || s.Contains("rangedweaponsdamage") => "Ranged Damage",
            var s when s.Contains("maxhealth") && s.Contains("multiplier") => "Max Health",
            var s when s.Contains("maxhealth") && s.Contains("points") => "Max Health",
            var s when s.Contains("maxhealth") => "Max Health",
            var s when s.Contains("armor") => "Armor",
            var s when s.Contains("speed") => "Speed",
            var s when s.Contains("damage") => "Damage",
            var s when s.Contains("health") => "Health",
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
            return rank switch
            {
                0 => "Initiate",
                1 => "Devoted",
                2 => "Zealot",
                3 => "Champion",
                4 => "Exalted",
                _ => $"Rank {rank}"
            };

        return rank switch
        {
            0 => "Fledgling",
            1 => "Established",
            2 => "Renowned",
            3 => "Legendary",
            4 => "Mythic",
            _ => $"Rank {rank}"
        };
    }
}