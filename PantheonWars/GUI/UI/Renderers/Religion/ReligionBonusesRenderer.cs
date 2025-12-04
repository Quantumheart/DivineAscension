using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models.Enum;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for displaying all progression bonuses
///     Shows prestige rank bonuses, civilization bonuses, and other progression systems
/// </summary>
internal static class ReligionBonusesRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.ReligionState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        // Check if player has a religion
        if (!manager.HasReligion())
        {
            TextRenderer.DrawInfoText(drawList, "Join a religion to see progression bonuses.", x, currentY + 8f, width);
            return height;
        }

        // === PRESTIGE RANK BONUSES SECTION ===
        TextRenderer.DrawLabel(drawList, "Prestige Rank Bonuses", x, currentY, 18f, ColorPalette.Gold);
        currentY += 30f;

        // Current prestige rank display
        var prestigeProgress = manager.GetReligionPrestigeProgress();
        var currentRank = (PrestigeRank)prestigeProgress.CurrentRank;
        var currentRankText = $"Current Rank: {currentRank} (Rank {prestigeProgress.CurrentRank})";
        TextRenderer.DrawLabel(drawList, currentRankText, x + 10f, currentY, 14f, ColorPalette.White);
        currentY += 25f;

        // Progress bar to next rank (if not max rank)
        if (!prestigeProgress.IsMaxRank)
        {
            var progressPercent = (float)prestigeProgress.CurrentPrestige / prestigeProgress.RequiredPrestige;
            var progressText = $"Progress to next rank: {prestigeProgress.CurrentPrestige}/{prestigeProgress.RequiredPrestige}";
            TextRenderer.DrawLabel(drawList, progressText, x + 10f, currentY, 13f, ColorPalette.Grey);
            currentY += 20f;

            // Draw progress bar
            var barWidth = width - 20f;
            var barHeight = 20f;
            var barX = x + 10f;

            // Background
            drawList.AddRectFilled(
                new Vector2(barX, currentY),
                new Vector2(barX + barWidth, currentY + barHeight),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown)
            );

            // Fill
            var fillWidth = barWidth * progressPercent;
            drawList.AddRectFilled(
                new Vector2(barX, currentY),
                new Vector2(barX + fillWidth, currentY + barHeight),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f)
            );

            // Border
            drawList.AddRect(
                new Vector2(barX, currentY),
                new Vector2(barX + barWidth, currentY + barHeight),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
                0f,
                ImDrawFlags.None,
                1f
            );

            currentY += 30f;
        }
        else
        {
            TextRenderer.DrawLabel(drawList, "Maximum prestige rank achieved!", x + 10f, currentY, 13f, ColorPalette.Gold);
            currentY += 25f;
        }

        // List prestige rank bonuses
        var prestigeBonuses = GetPrestigeBonuses(currentRank);
        if (prestigeBonuses.Length > 0)
        {
            TextRenderer.DrawLabel(drawList, "Active Bonuses:", x + 10f, currentY, 14f, ColorPalette.White);
            currentY += 25f;

            foreach (var bonus in prestigeBonuses)
            {
                // Bullet point
                drawList.AddCircleFilled(new Vector2(x + 25f, currentY + 8f), 3f,
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));

                // Bonus text
                drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + 35f, currentY),
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.White), bonus);
                currentY += 22f;
            }
        }

        currentY += 15f;

        // === CIVILIZATION BONUSES SECTION ===
        if (manager.HasCivilization())
        {
            // Divider
            drawList.AddLine(new Vector2(x, currentY), new Vector2(x + width, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f), 1f);
            currentY += 15f;

            TextRenderer.DrawLabel(drawList, "Civilization Bonuses", x, currentY, 18f, ColorPalette.Gold);
            currentY += 30f;

            var civName = manager.CurrentCivilizationName ?? "Your Civilization";
            TextRenderer.DrawLabel(drawList, $"Member of: {civName}", x + 10f, currentY, 14f, ColorPalette.White);
            currentY += 25f;

            var civBonuses = GetCivilizationBonuses();
            if (civBonuses.Length > 0)
            {
                TextRenderer.DrawLabel(drawList, "Active Bonuses:", x + 10f, currentY, 14f, ColorPalette.White);
                currentY += 25f;

                foreach (var bonus in civBonuses)
                {
                    // Bullet point
                    drawList.AddCircleFilled(new Vector2(x + 25f, currentY + 8f), 3f,
                        ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));

                    // Bonus text
                    drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + 35f, currentY),
                        ImGui.ColorConvertFloat4ToU32(ColorPalette.White), bonus);
                    currentY += 22f;
                }
            }
            else
            {
                TextRenderer.DrawInfoText(drawList, "No civilization bonuses active.", x + 10f, currentY, width - 20f);
                currentY += 25f;
            }
        }
        else
        {
            // Divider
            drawList.AddLine(new Vector2(x, currentY), new Vector2(x + width, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f), 1f);
            currentY += 15f;

            TextRenderer.DrawLabel(drawList, "Civilization Bonuses", x, currentY, 18f, ColorPalette.Grey * 0.7f);
            currentY += 30f;

            TextRenderer.DrawInfoText(drawList, "Join or create a civilization to unlock additional bonuses.", x + 10f, currentY, width - 20f);
            currentY += 25f;
        }

        return height;
    }

    /// <summary>
    ///     Get prestige rank bonuses based on current rank
    ///     TODO: This should be fetched from server or config
    /// </summary>
    private static string[] GetPrestigeBonuses(PrestigeRank rank)
    {
        return rank switch
        {
            PrestigeRank.Fledgling => new[]
            {
                "+5% favor gain from all sources",
                "Can invite up to 5 members"
            },
            PrestigeRank.Established => new[]
            {
                "+10% favor gain from all sources",
                "Can invite up to 10 members",
                "+5% blessing effectiveness"
            },
            PrestigeRank.Renowned => new[]
            {
                "+15% favor gain from all sources",
                "Can invite up to 20 members",
                "+10% blessing effectiveness",
                "Access to advanced religion blessings"
            },
            PrestigeRank.Legendary => new[]
            {
                "+20% favor gain from all sources",
                "Can invite up to 50 members",
                "+15% blessing effectiveness",
                "Access to legendary religion blessings",
                "+10% prestige gain"
            },
            PrestigeRank.Mythic => new[]
            {
                "+25% favor gain from all sources",
                "Unlimited member invites",
                "+20% blessing effectiveness",
                "Access to all religion blessings",
                "+20% prestige gain",
                "Divine favor: Rare blessings cost 50% less"
            },
            _ => System.Array.Empty<string>()
        };
    }

    /// <summary>
    ///     Get civilization bonuses
    ///     TODO: This should be fetched from server
    /// </summary>
    private static string[] GetCivilizationBonuses()
    {
        // Placeholder bonuses
        return new[]
        {
            "+5% favor gain from group activities",
            "+10% prestige gain for all member religions",
            "Shared blessing pool access",
            "Civilization-wide divine events"
        };
    }
}
