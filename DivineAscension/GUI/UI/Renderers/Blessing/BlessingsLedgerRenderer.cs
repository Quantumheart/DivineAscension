using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders a tier-grouped, dotted-leader index of blessings. Each row is one
///     blessing with a left-margin glyph (unlock state) and a right-aligned cost.
///     Locked rows that fail a prereq get an indented "requires:" hint below.
///     Emits <see cref="TreeEvent.Hovered"/>, <see cref="TreeEvent.Selected"/>,
///     and <see cref="TreeEvent.DoubleClicked"/> for the row under the cursor.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingsLedgerRenderer
{
    private const float Padding = 16f;
    private const float GlyphColumnWidth = 24f;
    private const float RowHeight = 22f;
    private const float PrereqIndent = 36f;
    private const float PrereqRowHeight = 18f;
    private const float TierHeadingHeight = 24f;
    private const float TierBottomGap = 10f;
    private const float DividerGap = 8f;

    private const string GlyphUnlocked = "*";
    private const string GlyphAvailable = "o";
    private const string GlyphLocked = ".";
    private const string GlyphBlocked = "X";

    internal readonly record struct Result(
        IReadOnlyList<TreeEvent> Events,
        float Height,
        string? HoveringBlessingId);

    /// <summary>
    /// Compute the height the ledger will occupy at draw time without rendering.
    /// Walks the same tier groups and prereq logic as <see cref="Draw"/>.
    /// </summary>
    internal static float MeasureHeight(IReadOnlyDictionary<string, BlessingNodeState> states)
    {
        if (states.Count == 0) return RowHeight;

        BlessingTierCalculator.AssignTiers(states);

        var nameById = new Dictionary<string, string>(states.Count);
        foreach (var s in states.Values)
            nameById[s.Blessing.BlessingId] = s.Blessing.Name;

        var height = 0f;
        var groups = states.Values
            .GroupBy(s => s.Tier)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            height += TierHeadingHeight + DividerGap;
            foreach (var s in group)
            {
                height += RowHeight;
                if (!s.IsUnlocked && BuildLockHint(s, states, nameById) != null)
                    height += PrereqRowHeight;
            }
            height += TierBottomGap;
        }
        return height;
    }

    internal static Result Draw(
        float x, float y, float width,
        IReadOnlyDictionary<string, BlessingNodeState> states,
        string costUnit,
        string? selectedBlessingId,
        string? tierHeaderSuffix = null)
    {
        var drawList = ImGui.GetWindowDrawList();
        var events = new List<TreeEvent>(4);
        string? hovering = null;

        if (states.Count == 0)
        {
            var empty = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_TREE_NO_BLESSINGS);
            TextRenderer.DrawInfoText(drawList, empty, x + Padding, y,
                width - Padding * 2, Body, ColorPalette.MutedText);
            return new Result(events, RowHeight, null);
        }

        BlessingTierCalculator.AssignTiers(states);

        // Stable name lookup for prereq labels.
        var nameById = new Dictionary<string, string>(states.Count);
        foreach (var s in states.Values)
            nameById[s.Blessing.BlessingId] = s.Blessing.Name;

        var tierGroups = states.Values
            .GroupBy(s => s.Tier)
            .OrderBy(g => g.Key)
            .ToList();

        var mousePos = ImGui.GetMousePos();
        var currentY = y;

        foreach (var group in tierGroups)
        {
            // Tier heading: "Tier i — <suffix>" or "Tier ii"
            var label = $"Tier {ToRoman(group.Key)}";
            if (!string.IsNullOrEmpty(tierHeaderSuffix) && group.Key == 1)
                label += "  —  " + tierHeaderSuffix;
            TextRenderer.DrawLabel(drawList, label, x + Padding, currentY,
                SubsectionLabel, ColorPalette.Gold);
            currentY += TierHeadingHeight;

            ChromeRenderer.DrawDivider(drawList, x, currentY, width);
            currentY += DividerGap;

            foreach (var s in group.OrderBy(g => g.Blessing.Name))
            {
                var rowMin = new Vector2(x + Padding, currentY);
                var rowMax = new Vector2(x + width - Padding, currentY + RowHeight);

                var costValue = (int)(s.Blessing.Cost * s.NonPatronCostMultiplier);
                var costText = s.NonPatronCostMultiplier > 1.0f
                    ? $"{costValue} {costUnit}  (1.5x)"
                    : $"{costValue} {costUnit}";

                var (glyph, glyphColor, labelColor, valueColor) = StyleFor(s);

                // Glyph column.
                TextRenderer.DrawLabel(drawList, glyph, rowMin.X, rowMin.Y,
                    Body, glyphColor);

                // Leader row (label dot-leader cost).
                var leaderX = rowMin.X + GlyphColumnWidth;
                var leaderWidth = (rowMax.X - leaderX);
                ChromeRenderer.DrawLeader(drawList,
                    s.Blessing.Name, costText,
                    leaderX, rowMin.Y, leaderWidth,
                    labelColor: labelColor, valueColor: valueColor);

                // Hit test: full row.
                var hovered = mousePos.X >= rowMin.X && mousePos.X <= rowMax.X
                                                     && mousePos.Y >= rowMin.Y && mousePos.Y <= rowMax.Y;
                if (hovered)
                {
                    hovering = s.Blessing.BlessingId;
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        events.Add(new TreeEvent.DoubleClicked(s.Blessing.BlessingId));
                    else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        events.Add(new TreeEvent.Selected(s.Blessing.BlessingId));
                }

                currentY += RowHeight;

                // Prereq indent (only when locked AND has an unmet prereq / patron gate).
                if (!s.IsUnlocked)
                {
                    var hint = BuildLockHint(s, states, nameById);
                    if (hint != null)
                    {
                        TextRenderer.DrawInfoText(drawList, hint,
                            rowMin.X + PrereqIndent, currentY,
                            width - Padding * 2 - PrereqIndent,
                            Secondary, ColorPalette.MutedText);
                        currentY += PrereqRowHeight;
                    }
                }
            }

            currentY += TierBottomGap;
        }

        events.Add(new TreeEvent.Hovered(hovering));

        return new Result(events, currentY - y, hovering);
    }

    private static (string Glyph, Vector4 GlyphColor, Vector4 LabelColor, Vector4 ValueColor) StyleFor(
        BlessingNodeState s)
    {
        if (s.IsUnlocked)
            return (GlyphUnlocked, ColorPalette.Gold, ColorPalette.Gold, ColorPalette.Gold);

        if (s.IsBranchLocked)
            return (GlyphBlocked, ColorPalette.ErrorRed, ColorPalette.MutedText, ColorPalette.MutedText);

        if (s.CanUnlock)
            return (GlyphAvailable, ColorPalette.Green, ColorPalette.White, ColorPalette.White);

        return (GlyphLocked, ColorPalette.MutedText, ColorPalette.MutedText, ColorPalette.MutedText);
    }

    private static string? BuildLockHint(BlessingNodeState s,
        IReadOnlyDictionary<string, BlessingNodeState> states,
        IReadOnlyDictionary<string, string> nameById)
    {
        if (s.IsBranchLocked && !string.IsNullOrEmpty(s.LockedByBranch))
            return $"L  branch locked by {s.LockedByBranch}";

        var prereqs = s.Blessing.PrerequisiteBlessings;
        if (prereqs != null && prereqs.Count > 0)
        {
            var missing = new List<string>();
            foreach (var pid in prereqs)
            {
                if (states.TryGetValue(pid, out var ps) && ps.IsUnlocked) continue;
                if (nameById.TryGetValue(pid, out var pname))
                    missing.Add(pname);
            }
            if (missing.Count > 0)
                return $"L  requires: {string.Join(", ", missing)}";
        }

        // Capstone patron gate is encoded on the Blessing model; the tooltip
        // already calls it out, so we don't double up here.
        return null;
    }

    private static string ToRoman(int n) => n switch
    {
        1 => "i",
        2 => "ii",
        3 => "iii",
        4 => "iv",
        5 => "v",
        6 => "vi",
        7 => "vii",
        8 => "viii",
        9 => "ix",
        10 => "x",
        _ => n.ToString()
    };
}
