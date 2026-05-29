using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Caravan;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Caravan;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Caravan;

/// <summary>
///     Renders the caravan trade table as a paper "Bill of Barter" ledger: a chapter
///     strip with the Caravan glyph, two side-by-side offer columns (self always left),
///     dotted-leader item lines, a per-side wax-seal mark, the local player's pack as
///     click-to-add entries, and Seal / Leave actions. Stateless — returns the click
///     intents for the dialog to act on. UI only; no item movement (#433).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CaravanTradeRenderer
{
    private const float Padding = 18f;
    private const float ColumnGap = 28f;
    private const float RowHeight = 26f;
    private const float SectionGap = 14f;

    /// <summary>A pack item the local player can lay on the table.</summary>
    public readonly record struct PackEntry(string Code, string Name, int Quantity);

    public readonly record struct Result(
        int AddPackIndex,
        int RemoveOfferIndex,
        bool SealToggleClicked,
        bool LeaveClicked);

    public static Result Draw(
        CaravanTradeState state,
        string myUid,
        IReadOnlyList<PackEntry> pack,
        float windowX,
        float windowY,
        float windowWidth,
        float windowHeight)
    {
        var drawList = ImGui.GetWindowDrawList();
        var x = windowX + Padding;
        var contentWidth = windowWidth - Padding * 2;

        var addPackIndex = -1;
        var removeOfferIndex = -1;
        var sealClicked = false;
        var leaveClicked = false;

        // Chapter strip with Caravan wagon-wheel glyph.
        var strip = ChapterStripRenderer.Draw(
            drawList, x, windowY, contentWidth, 0f,
            LocalizationService.Instance.Get("caravantrade.title"),
            rightGlyph: DeityDomain.Caravan);
        var y = strip.BodyY + SectionGap;

        // Prose intro.
        TextRenderer.DrawInfoText(drawList,
            LocalizationService.Instance.Get("caravantrade.intro"),
            x, y, contentWidth, FontSizes.Body, ColorPalette.Grey);
        y += RowHeight + 4f;

        // Two columns: self on the left.
        var colWidth = (contentWidth - ColumnGap) / 2f;
        var leftX = x;
        var rightX = x + colWidth + ColumnGap;
        var columnsTop = y;

        var myName = string.IsNullOrEmpty(state.MyName(myUid)) ? "You" : state.MyName(myUid);
        var theirName = state.HasPartner(myUid)
            ? state.TheirName(myUid)
            : LocalizationService.Instance.Get("caravantrade.awaiting_partner");

        // Left (editable) column — clicking an entry returns it to the pack.
        var leftBottom = DrawMyColumn(drawList, leftX, columnsTop, colWidth,
            myName, state.MyOffer(myUid), state.MyReady(myUid), ref removeOfferIndex);

        // Right (read-only) column — the partner's offer.
        var rightBottom = DrawTheirColumn(drawList, rightX, columnsTop, colWidth,
            theirName, state.TheirOffer(myUid), state.TheirReady(myUid), state.HasPartner(myUid));

        y = (leftBottom > rightBottom ? leftBottom : rightBottom) + SectionGap;

        // Divider.
        ChromeRenderer.DrawDivider(drawList, x, y, contentWidth);
        y += SectionGap;

        // Local pack — click-to-add.
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get("caravantrade.your_pack"),
            x, y, FontSizes.SubsectionLabel, ColorPalette.Gold);
        y += RowHeight;

        var canAdd = state.MyOffer(myUid).Count < CaravanTradeSlotLimit;
        var packX = x;
        var packTop = y;
        for (var i = 0; i < pack.Count; i++)
        {
            var entry = pack[i];
            var label = $"{entry.Name} ×{entry.Quantity}";
            var btnWidth = ImGui.CalcTextSize(label).X + 24f;
            if (packX + btnWidth > x + contentWidth)
            {
                packX = x;
                packTop += RowHeight + 4f;
            }

            if (ButtonRenderer.DrawButton(drawList, label, packX, packTop, btnWidth, RowHeight,
                    isPrimary: false, enabled: canAdd))
            {
                addPackIndex = i;
            }

            packX += btnWidth + 6f;
        }

        y = packTop + RowHeight + SectionGap;

        // Footer prose + actions.
        ChromeRenderer.DrawDivider(drawList, x, y, contentWidth);
        y += SectionGap;
        TextRenderer.DrawInfoText(drawList,
            LocalizationService.Instance.Get("caravantrade.switcheroo_note"),
            x, y, contentWidth, FontSizes.Secondary, ColorPalette.MutedText);
        y += RowHeight + 4f;

        const float buttonWidth = 200f;
        const float buttonHeight = 34f;
        var sealLabel = LocalizationService.Instance.Get(
            state.MyReady(myUid) ? "caravantrade.unseal" : "caravantrade.seal");
        var sealEnabled = state.HasPartner(myUid);
        var leaveLabel = LocalizationService.Instance.Get("caravantrade.leave");

        var actionsY = windowY + windowHeight - buttonHeight - Padding;
        if (actionsY < y) actionsY = y;
        var sealX = x + (contentWidth / 2f) - buttonWidth - 10f;
        var leaveX = x + (contentWidth / 2f) + 10f;

        if (ButtonRenderer.DrawButton(drawList, sealLabel, sealX, actionsY, buttonWidth, buttonHeight,
                isPrimary: true, enabled: sealEnabled))
        {
            sealClicked = true;
        }

        if (ButtonRenderer.DrawButton(drawList, leaveLabel, leaveX, actionsY, buttonWidth, buttonHeight,
                isPrimary: false, enabled: true))
        {
            leaveClicked = true;
        }

        return new Result(addPackIndex, removeOfferIndex, sealClicked, leaveClicked);
    }

    private const int CaravanTradeSlotLimit = 9;

    private static float DrawMyColumn(ImDrawListPtr drawList, float x, float y, float width,
        string name, List<TradeOfferSlot> offer, bool ready, ref int removeIndex)
    {
        TextRenderer.DrawLabel(drawList, $"❧ {name} lays down", x, y, FontSizes.SubsectionLabel,
            ColorPalette.White);
        var rowY = y + RowHeight;

        for (var i = 0; i < offer.Count; i++)
        {
            var slot = offer[i];
            var label = $"{slot.DisplayName} ×{slot.Quantity}";
            if (ButtonRenderer.DrawButton(drawList, label, x, rowY, width, RowHeight - 4f,
                    isPrimary: false, enabled: true))
            {
                removeIndex = i;
            }

            rowY += RowHeight;
        }

        rowY = DrawHandsFree(drawList, x, rowY, offer.Count);
        DrawSealMark(drawList, x, rowY, ready);
        return rowY + RowHeight;
    }

    private static float DrawTheirColumn(ImDrawListPtr drawList, float x, float y, float width,
        string name, List<TradeOfferSlot> offer, bool ready, bool hasPartner)
    {
        TextRenderer.DrawLabel(drawList, $"{name} lays down ❧", x, y, FontSizes.SubsectionLabel,
            hasPartner ? ColorPalette.White : ColorPalette.MutedText);
        var rowY = y + RowHeight;

        if (hasPartner)
        {
            foreach (var slot in offer)
            {
                DrawLeaderLine(drawList, x, rowY, width, slot.DisplayName, $"×{slot.Quantity}");
                rowY += RowHeight;
            }

            rowY = DrawHandsFree(drawList, x, rowY, offer.Count);
            DrawSealMark(drawList, x, rowY, ready);
        }

        return rowY + RowHeight;
    }

    private static float DrawHandsFree(ImDrawListPtr drawList, float x, float rowY, int used)
    {
        var free = CaravanTradeSlotLimit - used;
        if (free <= 0) return rowY;
        TextRenderer.DrawLabel(drawList,
            $"· · · ({free} hands free)", x, rowY, FontSizes.Secondary, ColorPalette.MutedText);
        return rowY + RowHeight;
    }

    private static void DrawSealMark(ImDrawListPtr drawList, float x, float rowY, bool ready)
    {
        var mark = ready
            ? "† " + LocalizationService.Instance.Get("caravantrade.sealed")
            : "◇ " + LocalizationService.Instance.Get("caravantrade.pondering");
        TextRenderer.DrawLabel(drawList, mark, x, rowY, FontSizes.Body,
            ready ? ColorPalette.Verdigris : ColorPalette.MutedText);
    }

    /// <summary>Draws "name ........ value" with a dotted leader filling the gap.</summary>
    private static void DrawLeaderLine(ImDrawListPtr drawList, float x, float y, float width,
        string left, string right)
    {
        TextRenderer.DrawLabel(drawList, left, x, y, FontSizes.Body, ColorPalette.White);
        var rightWidth = ImGui.CalcTextSize(right).X;
        var rightX = x + width - rightWidth;
        TextRenderer.DrawLabel(drawList, right, rightX, y, FontSizes.Body, ColorPalette.White);

        var leftWidth = ImGui.CalcTextSize(left).X;
        var dotsStart = x + leftWidth + 6f;
        var dotsEnd = rightX - 6f;
        if (dotsEnd <= dotsStart) return;

        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
        var dotY = y + FontSizes.Body * 0.6f;
        for (var dx = dotsStart; dx < dotsEnd; dx += 6f)
            drawList.AddCircleFilled(new Vector2(dx, dotY), 1f, color);
    }
}
