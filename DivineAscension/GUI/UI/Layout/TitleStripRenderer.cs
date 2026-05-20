using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Systems;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Layout;

/// <summary>
///     Codex chrome strip painted across the top of the dialog. Carries the
///     app title on the left, the player's deity + favor rank identity on the
///     right, and hosts the close button at the right edge.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TitleStripRenderer
{
    private const string TitleText = "Divine Ascension";
    private const float Padding = 12f;
    private const float CloseButtonSize = 24f;
    private const float IdentityGap = 12f;
    private const float OrnamentHalfSize = 5f;
    private const float OrnamentGap = 8f;
    private const string Ellipsis = "…";
    private const string IdentitySeparator = " · ";

    /// <summary>
    ///     Draw the title strip into <paramref name="rect" />. Returns true if
    ///     the embedded close button was clicked this frame.
    /// </summary>
    public static bool Draw(UiRect rect, GuiDialogManager manager)
    {
        if (rect.W <= 0f || rect.H <= 0f) return false;

        var drawList = ImGui.GetWindowDrawList();

        var topLeft = new Vector2(rect.X, rect.Y);
        var botRight = new Vector2(rect.Right, rect.Bottom);
        drawList.AddRectFilled(topLeft, botRight,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown), 0f);
        drawList.AddLine(
            new Vector2(rect.X, rect.Bottom),
            new Vector2(rect.Right, rect.Bottom),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f),
            1f);

        // Close button anchored at the right edge of the strip.
        var closeY = rect.Y + (rect.H - CloseButtonSize) / 2f;
        var closeX = rect.Right - Padding - CloseButtonSize;
        var closeClicked = ButtonRenderer.DrawCloseButton(drawList, closeX, closeY, CloseButtonSize);

        // Left: a flanking diamond, then the app title in gold, then a
        // second flanking diamond. Diamonds are drawn primitives rather than
        // ✦ glyphs because ImGui's default font ranges don't include
        // Dingbats — text glyphs would render as `?`. Measure the title at
        // its actual render size (SubsectionLabel) so the right diamond
        // doesn't drift outwards by the difference between the default font
        // size and the size we draw at.
        var renderScale = SubsectionLabel / ImGui.GetFontSize();
        var titleSize = ImGui.CalcTextSize(TitleText) * renderScale;
        var titleY = rect.Y + (rect.H - titleSize.Y) / 2f;
        var titleColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);

        var ornamentCenterY = rect.Y + rect.H / 2f;
        var leftOrnamentCx = rect.X + Padding + OrnamentHalfSize;
        var titleX = leftOrnamentCx + OrnamentHalfSize + OrnamentGap;
        ChromeRenderer.DrawDiamond(drawList, leftOrnamentCx, ornamentCenterY, OrnamentHalfSize);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel,
            new Vector2(titleX, titleY), titleColor, TitleText);
        var rightOrnamentCx = titleX + titleSize.X + OrnamentGap + OrnamentHalfSize;
        ChromeRenderer.DrawDiamond(drawList, rightOrnamentCx, ornamentCenterY, OrnamentHalfSize);

        var titleRightEdge = rightOrnamentCx + OrnamentHalfSize;

        // Right: identity line (deity name + favor rank), only when the
        // player has a religion. Sits to the left of the close button with a
        // small gap; truncates with an ellipsis if it can't fit.
        if (manager.HasReligion())
        {
            DrawIdentity(drawList, rect, manager,
                leftBoundary: titleRightEdge + IdentityGap,
                rightBoundary: closeX - IdentityGap);
        }

        return closeClicked;
    }

    private static void DrawIdentity(ImDrawListPtr drawList, UiRect rect,
        GuiDialogManager manager, float leftBoundary, float rightBoundary)
    {
        var available = rightBoundary - leftBoundary;
        if (available <= 0f) return;

        var deityName = manager.ReligionStateManager.CurrentDeityName ?? string.Empty;
        var favor = manager.ReligionStateManager.GetPlayerFavorProgress();
        var rankName = RankRequirements.GetFavorRankName(favor.CurrentRank);

        // Layout: <deity><separator><rank>. If the deity name is missing,
        // collapse the separator so we don't render a leading " · ". If even
        // the rank alone won't fit, drop the whole identity line.
        var rankSize = ImGui.CalcTextSize(rankName);
        if (rankSize.X >= available) return;

        string deityFits;
        string separator;
        if (string.IsNullOrEmpty(deityName))
        {
            deityFits = string.Empty;
            separator = string.Empty;
        }
        else
        {
            var separatorSize = ImGui.CalcTextSize(IdentitySeparator);
            var deityBudget = available - rankSize.X - separatorSize.X;
            if (deityBudget <= 0f)
            {
                deityFits = string.Empty;
                separator = string.Empty;
            }
            else
            {
                deityFits = Truncate(deityName, deityBudget);
                separator = string.IsNullOrEmpty(deityFits) ? string.Empty : IdentitySeparator;
            }
        }

        var combined = deityFits + separator + rankName;
        var combinedSize = ImGui.CalcTextSize(combined);
        var textY = rect.Y + (rect.H - combinedSize.Y) / 2f;
        // Right-align against the close button.
        var textX = rightBoundary - combinedSize.X;

        var whiteColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var greyColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        if (!string.IsNullOrEmpty(deityFits))
        {
            var deitySize = ImGui.CalcTextSize(deityFits);
            drawList.AddText(ImGui.GetFont(), Body,
                new Vector2(textX, textY), whiteColor, deityFits);
            textX += deitySize.X;
        }

        if (separator.Length > 0)
        {
            var sepSize = ImGui.CalcTextSize(separator);
            drawList.AddText(ImGui.GetFont(), Body,
                new Vector2(textX, textY), greyColor, separator);
            textX += sepSize.X;
        }

        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(textX, textY), greyColor, rankName);
    }

    private static string Truncate(string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (ImGui.CalcTextSize(text).X <= maxWidth) return text;

        var ellipsisWidth = ImGui.CalcTextSize(Ellipsis).X;
        if (ellipsisWidth > maxWidth) return string.Empty;

        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            var candidate = text.Substring(0, mid) + Ellipsis;
            if (ImGui.CalcTextSize(candidate).X <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo <= 0 ? Ellipsis : text.Substring(0, lo) + Ellipsis;
    }
}
