using System.Numerics;
using DivineAscension.GUI.Models.Blessing.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing.Info;

internal static class BlessingInfoSectionRequirements
{
    public static float Draw(BlessingNodeState selectedState, BlessingInfoViewModel vm, float currentY, float padding,
        float contentWidth)
    {
        if (selectedState.IsUnlocked || currentY >= vm.Y + vm.Height - 60f)
            return currentY;

        var drawList = ImGui.GetWindowDrawList();

        currentY += 8f;
        var reqTitleColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 14f, new Vector2(vm.X + padding, currentY), reqTitleColorU32,
            "Requirements:");
        currentY += 18f;

        // Check if we have space for requirements
        if (currentY < vm.Y + vm.Height - 40f)
        {
            // Rank requirement
            var rankReq = selectedState.Blessing.Kind == BlessingKind.Player
                ? $"  Favor Rank: {BlessingInfoTextUtils.GetRankName(selectedState.Blessing.RequiredFavorRank, true)}"
                : $"  Prestige Rank: {BlessingInfoTextUtils.GetRankName(selectedState.Blessing.RequiredPrestigeRank, false)}";

            var descriptionColorU32 = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
            drawList.AddText(ImGui.GetFont(), 14f, new Vector2(vm.X + padding + 8, currentY), descriptionColorU32,
                rankReq);
            currentY += 18f;

            // Prerequisites
            if (selectedState.Blessing.PrerequisiteBlessings is { Count: > 0 })
                foreach (var prereqId in selectedState.Blessing.PrerequisiteBlessings)
                {
                    if (currentY > vm.Y + vm.Height - 20f) break;

                    vm.BlessingStates.TryGetValue(prereqId, out var prereqState);
                    var prereqName = prereqState?.Blessing.Name ?? prereqId;
                    var prereqText = $"  Unlock: {prereqName}";

                    // Truncate text if too long
                    var maxTextWidth = contentWidth - 8f; // Account for indentation
                    var textSize = ImGui.CalcTextSize(prereqText);
                    if (textSize.X > maxTextWidth)
                    {
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

                    drawList.AddText(ImGui.GetFont(), 14f, new Vector2(vm.X + padding + 8, currentY),
                        prereqColorU32, prereqText);
                    currentY += 18f;
                }
        }

        return currentY;
    }
}