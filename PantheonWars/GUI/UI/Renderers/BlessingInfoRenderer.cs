using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Renders the selected blessing details panel at the bottom of the dialog
///     Shows: Name, description, requirements, stats, effects
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingInfoRenderer
{
    /// <summary>
    ///     Draw the blessing info panel
    /// </summary>
    /// <param name="manager">Blessing dialog state manager</param>
    /// <param name="api">Client API</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="height">Available height</param>
    /// <returns>Height used by this renderer</returns>
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var drawList = ImGui.GetWindowDrawList();
        var startPos = new Vector2(x, y);
        var endPos = new Vector2(x + width, y + height);

        // Draw panel background
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(startPos, endPos, bgColor, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRect(startPos, endPos, borderColor, 4f, ImDrawFlags.None, 2f);

        // Get selected blessing state
        var selectedState = manager.GetSelectedBlessingState();

        if (selectedState == null)
        {
            // No blessing selected - show prompt
            var promptText = "Select a blessing to view details";
            var textSize = ImGui.CalcTextSize(promptText);
            var textPos = new Vector2(
                x + (width - textSize.X) / 2,
                y + (height - textSize.Y) / 2
            );
            var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(textPos, textColor, promptText);

            return height;
        }

        // Blessing selected - draw detailed info
        const float padding = 16f;
        var currentY = y + padding;
        var contentWidth = width - padding * 2;

        // Blessing name (title)
        var titleColor = selectedState.IsUnlocked ? ColorPalette.Gold : ColorPalette.White;
        var titleColorU32 = ImGui.ColorConvertFloat4ToU32(titleColor);
        drawList.AddText(ImGui.GetFont(), 20f, new Vector2(x + padding, currentY), titleColorU32,
            selectedState.Blessing.Name);
        currentY += 28f;

        // Status badge
        var statusText = selectedState.VisualState switch
        {
            BlessingNodeVisualState.Unlocked => "[UNLOCKED]",
            BlessingNodeVisualState.Unlockable => "[AVAILABLE]",
            _ => "[LOCKED]"
        };

        var statusColor = selectedState.VisualState switch
        {
            BlessingNodeVisualState.Unlocked => ColorPalette.Gold,
            BlessingNodeVisualState.Unlockable => ColorPalette.Green,
            _ => ColorPalette.Red
        };

        var statusColorU32 = ImGui.ColorConvertFloat4ToU32(statusColor);
        drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x + padding, currentY), statusColorU32, statusText);
        currentY += 20f;

        // Category and kind
        var metaText =
            $"{selectedState.Blessing.Category} | {selectedState.Blessing.Kind} Blessing | Tier {selectedState.Tier}";
        var metaColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x + padding, currentY), metaColorU32, metaText);
        currentY += 20f;

        // Separator line
        var lineY = currentY;
        var lineStart = new Vector2(x + padding, lineY);
        var lineEnd = new Vector2(x + width - padding, lineY);
        var lineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f);
        drawList.AddLine(lineStart, lineEnd, lineColor, 1f);
        currentY += 8f;

        // Description (word-wrapped)
        var descriptionColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        DrawWrappedText(drawList, selectedState.Blessing.Description,
            x + padding, currentY, contentWidth, descriptionColorU32, 14f);
        currentY += 40f; // Approximate space for description

        // Requirements section (check if space available)
        if (!selectedState.IsUnlocked && currentY < y + height - 60f)
        {
            currentY += 8f;
            var reqTitleColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + padding, currentY), reqTitleColorU32,
                "Requirements:");
            currentY += 18f;

            // Check if we have space for requirements
            if (currentY < y + height - 40f)
            {
                // Rank requirement
                var rankReq = selectedState.Blessing.Kind == BlessingKind.Player
                    ? $"  Favor Rank: {GetRankName(selectedState.Blessing.RequiredFavorRank, true)}"
                    : $"  Prestige Rank: {GetRankName(selectedState.Blessing.RequiredPrestigeRank, false)}";

                drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + padding + 8, currentY), descriptionColorU32,
                    rankReq);
                currentY += 18f;

                // Prerequisites
                if (selectedState.Blessing.PrerequisiteBlessings is { Count: > 0 })
                    foreach (var prereqId in selectedState.Blessing.PrerequisiteBlessings)
                    {
                        // Check if we still have space
                        if (currentY > y + height - 20f) break;

                        var prereqState = manager.ReligionStateManager.GetBlessingState(prereqId);
                        var prereqName = prereqState?.Blessing.Name ?? prereqId;
                        var prereqText = $"  Unlock: {prereqName}";

                        // Truncate text if it's too long to fit within the panel
                        var maxTextWidth = contentWidth - 8f; // Account for the indentation
                        var textSize = ImGui.CalcTextSize(prereqText);
                        if (textSize.X > maxTextWidth)
                        {
                            // Truncate and add ellipsis
                            var targetLength = prereqName.Length;
                            while (targetLength > 0)
                            {
                                var truncatedName = prereqName.Substring(0, targetLength) + "...";
                                var truncatedText = $"  Unlock: {truncatedName}";
                                var truncatedSize = ImGui.CalcTextSize(truncatedText);
                                if (truncatedSize.X <= maxTextWidth)
                                {
                                    prereqText = truncatedText;
                                    break;
                                }

                                targetLength--;
                            }
                        }

                        var prereqColor = prereqState?.IsUnlocked ?? false ? ColorPalette.Green : ColorPalette.Red;
                        var prereqColorU32 = ImGui.ColorConvertFloat4ToU32(prereqColor);

                        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + padding + 8, currentY),
                            prereqColorU32, prereqText);
                        currentY += 18f;
                    }
            }
        }

        // Stats section (if space available)
        if (currentY < y + height - 60f && selectedState.Blessing.StatModifiers.Count > 0)
        {
            currentY += 8f;
            var statsTitleColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddText(ImGui.GetFont(), 16f, new Vector2(x + padding, currentY), statsTitleColorU32,
                "Effects:");
            currentY += 22f;

            foreach (var stat in selectedState.Blessing.StatModifiers)
            {
                // Format stat display
                var statText = FormatStatModifier(stat.Key, stat.Value);
                var statColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Green);

                drawList.AddText(ImGui.GetFont(), 14f, new Vector2(x + padding + 8, currentY), statColorU32,
                    statText);
                currentY += 18f;

                // Prevent overflow
                if (currentY > y + height - 20f) break;
            }
        }

        return height;
    }

    /// <summary>
    ///     Draw text with word wrapping
    /// </summary>
    private static void DrawWrappedText(ImDrawListPtr drawList, string text,
        float x, float y, float maxWidth, uint color, float fontSize)
    {
        // Simple wrapping - split by words
        var words = text.Split(' ');
        var currentLine = "";
        var currentY = y;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = ImGui.CalcTextSize(testLine);

            if (testSize.X > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                // Draw current line and start new one
                drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, currentY), color, currentLine);
                currentY += fontSize + 4f;
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        // Draw last line
        if (!string.IsNullOrEmpty(currentLine))
            drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, currentY), color, currentLine);
    }

    /// <summary>
    ///     Format stat modifier for display
    /// </summary>
    private static string FormatStatModifier(string statName, float value)
    {
        // Normalize stat name to lowercase for matching
        var statLower = statName.ToLower();

        // Check if this is a percentage stat
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

        var isPercentage = percentageStats.Any(ps => statLower.Contains(ps.ToLower()));

        // Map stat names to friendly display names
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
            _ => statName // Fallback to original name
        };

        var sign = value >= 0 ? "+" : "";

        if (isPercentage) return $"  {sign}{value * 100:0.#}% {displayName}";

        return $"  {sign}{value:0.#} {displayName}";
    }

    /// <summary>
    ///     Get rank name from rank number
    /// </summary>
    private static string GetRankName(int rank, bool isFavorRank)
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